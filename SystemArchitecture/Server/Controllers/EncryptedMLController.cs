using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Research.SEAL;
using System.IO;
using System.Threading.Tasks; 
using System.Runtime.Serialization.Formatters.Binary;

using CDTS_PROJECT.Models;
using CDTS_PROJECT.Services;
using CDTS_PROJECT.Logics;
using CDTS_PROJECT.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace CDTS_PROJECT.Api.Controllers
{
    /// EncryptedMLController: API endpoint for encrypted machine learning inference.
    /// 
    /// WHAT IT DOES:
    /// This is the main API endpoint that the client calls. It:
    /// 1. Receives encrypted data from the client
    /// 2. Performs machine learning inference on the encrypted data
    /// 3. Returns encrypted predictions
    /// 
    /// ROUTE:
    /// POST /api/encryptedML?modelname=LogisticRegression
    /// 
    /// SECURITY:
    /// The server never sees unencrypted data. All operations are performed on encrypted Ciphertext objects.
    [Route("api/[controller]")] // Maps to /api/EncryptedML (controller name without "Controller")
    [ApiController] // Marks this as an API controller (enables automatic model validation, etc.)
    public class EncryptedMLController : ControllerBase
    {
        // Logger: For recording events and debugging
        private readonly ILogger<EncryptedMLController> _logger;
        
        // ModelService: Loads machine learning models from the database
        private readonly IModelService _modelService;
        
        // EncryptedOperationsService: Performs calculations on encrypted data
        private readonly IencryptedOperationsService _encryptedOperationsService;
        
        // ContextManager: Provides encryption context for working with encrypted data
        private readonly IContextManager _contextManager;

        /// Constructor: Initializes the controller with required services.
        /// 
        /// DEPENDENCY INJECTION:
        /// ASP.NET Core automatically provides these services when the controller is created.
        /// This makes the code testable and flexible.
        public EncryptedMLController(ILogger<EncryptedMLController> logger, IModelService modelService, IencryptedOperationsService encryptedOperationsService, IContextManager contextManager)
        {
            _logger = logger;
            _modelService = modelService;
            _encryptedOperationsService = encryptedOperationsService;
            _contextManager = contextManager;
        }

        /// GetWeightedSums: Main API endpoint for encrypted machine learning inference.
        /// 
        /// WHAT IT DOES:
        /// 1. Validates the model name (security check)
        /// 2. Loads the requested model from the database
        /// 3. Receives encrypted data from the client (via multipart form)
        /// 4. Performs encrypted inference (calculates weighted sums)
        /// 5. Returns encrypted results to the client
        /// 
        /// HTTP METHOD: POST
        /// ROUTE: /api/encryptedML?modelname=LogisticRegression
        /// 
        /// PARAMETERS:
        /// - modelname: Name of the ML model to use (from query string)
        /// 
        /// REQUEST BODY:
        /// Multipart form containing:
        /// - encryptedFeatureValues: The encrypted data (as bytes)
        /// - columnSizes: Array telling how many bytes each encrypted value takes
        /// 
        /// RESPONSE:
        /// Binary stream containing encrypted predictions
        /// 
        /// ATTRIBUTES:
        /// - [HttpPost]: Only accepts POST requests
        /// - [DisableFormValueModelBinding]: We parse the form manually (for performance with large encrypted data)
        /// - [DisableRequestSizeLimit]: Allow large requests (encrypted data can be big)
        [HttpPost]
        [DisableFormValueModelBinding] // Parse form manually for better performance with large data
        [DisableRequestSizeLimit] // Allow large requests (encrypted data can be several MB)
        public async Task<IActionResult> GetWeightedSums([FromQuery] string modelname) //return type will be List<List<Ciphertext>> which will be automaticall serialized into JSON in response
        {   
            // Step 1: Validate the model name (security: prevent injection attacks)
            // Only allow alphanumeric characters (letters and numbers)
            if (! EncryptedMLHelper.validateModelName(modelname)){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400; // Bad Request
                errorResponse.Value = "Model name should contain only alphanumeric characters.";
                throw errorResponse;
            }

            // Step 2: Load the requested model from the database
            // The model contains the weights needed for inference
            Model selectedModel = _modelService.Get(modelname);

            // Check if the model exists
            if (selectedModel == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 404; // Not Found
                errorResponse.Value = "Model with name "+modelname+" does not exist.";
                throw errorResponse;
            }

            // Step 3: Parse the multipart form data
            // The client sends data as a multipart form with multiple parts
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = EncryptedMLHelper.GetBoundary( MediaTypeHeaderValue.Parse(Request.ContentType));
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            // NOTE: Public key extraction is commented out - not currently used
            // The server doesn't need the public key for these operations
            //Use first part to initialize public key
            //var section = await reader.ReadNextSectionAsync();
            //if (section == null){
            //    HttpResponseException errorResponse =  new HttpResponseException();
            //    errorResponse.Status = 400;
            //    errorResponse.Value = "0 content parts received, expected 3";
            //    throw errorResponse;
            //}
            //MemoryStream tempStream = new MemoryStream();
            //await section.Body.CopyToAsync(tempStream);
            //tempStream.Seek(0, SeekOrigin.Begin);
            //PublicKey publicKey = new PublicKey();
            //try{
            //    publicKey.Load(_contextManager.Context, tempStream);
            //}catch (Exception ex){
            //    HttpResponseException errorResponse =  new HttpResponseException();
            //    errorResponse.Status = 400;
            //    errorResponse.Value = "Public Key Content Stream May be Corrupted. Error extracting public key." + ex.ToString();
            //    throw errorResponse;
            //}
            //tempStream.Close();

            // Step 4: Read the first part - encrypted feature values
            // This contains the actual encrypted data from the client
            var section = await reader.ReadNextSectionAsync();
            if (section == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400; // Bad Request
                errorResponse.Value = "0 content parts received, expected 2";
                throw errorResponse;
            }
            MemoryStream tempStream = new MemoryStream();
            await section.Body.CopyToAsync(tempStream); // Copy the encrypted data to our stream
            tempStream.Seek(0, SeekOrigin.Begin); // Reset to start

            // Step 5: Read the second part - column sizes
            // This tells us how many bytes each encrypted value takes
            // We need this to properly parse the encrypted data
            section = await reader.ReadNextSectionAsync();
            if (section == null){
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400; // Bad Request
                errorResponse.Value = "0 content parts received, expected 1";
                throw errorResponse;
            }
            MemoryStream tempStream2 = new MemoryStream();
            await section.Body.CopyToAsync(tempStream2);
            tempStream2.Seek(0, SeekOrigin.Begin);
            BinaryFormatter formatter = new BinaryFormatter(); // For converting bytes to objects

            // Deserialize the column sizes array
            List<long> columnSizes = new List<long>();  
            try{
                // Convert the byte stream back to an array of longs
                long[] columnSizesArray = (long[]) formatter.Deserialize(tempStream2);
                columnSizes.AddRange(columnSizesArray);
            }catch (Exception ex){
                // If deserialization fails, the data might be corrupted
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400; // Bad Request
                errorResponse.Value = "Column Sizes Content Stream May be Corrupted. Error Deserializing 2nd content part: "+ex.ToString();
                throw errorResponse;
            }
            tempStream2.Close();
            
            // Step 6: Extract the encrypted feature values from the stream
            // This reconstructs the 2D list of Ciphertext objects from the byte stream
            List<List<Ciphertext>> encryptedFeatureValues = new List<List<Ciphertext>>();
            try{
                // Use the column sizes to know how many bytes to read for each encrypted value
                encryptedFeatureValues = await EncryptedMLHelper.extract2DCipherListFromStreamAsync(tempStream, columnSizes.ToArray(), _contextManager.Context);
            }catch (Exception ex){
                // If extraction fails, the encrypted data might be corrupted
                HttpResponseException errorResponse =  new HttpResponseException();
                errorResponse.Status = 400; // Bad Request
                errorResponse.Value = "Encrypted Feature Content Stream Values May Be Corrupted. Error extracting encryptedfeatures from content stream: "+ex.ToString();
                throw errorResponse;
            }
            tempStream.Close();

            // Step 7: Create a Query object with the encrypted data
            Query query = new Query{
                //publicKey = publicKey, // Not currently used
                encryptedFeatureValues = encryptedFeatureValues // The encrypted data we just extracted
            };

            // Step 8: Perform encrypted machine learning inference
            // This is where the magic happens - we calculate predictions on encrypted data!
            List<List<Ciphertext>> encryptedWeightedSums = new List<List<Ciphertext>>();
            try{
                // The encryptedOperationsService performs all calculations on encrypted data
                // It never sees the actual values - only encrypted Ciphertext objects
                encryptedWeightedSums = await _encryptedOperationsService.calculateWeightedSumAsync(selectedModel, query);
            }catch (Exception ex){
                // Re-throw any errors (they'll be handled by the exception filter)
                throw ex;
            }

            // Step 9: Serialize the encrypted results for sending back to the client
            // We need to convert the Ciphertext objects back to bytes for network transmission
            MemoryStream encryptedFeatureValuesStream = new MemoryStream();
            // Convert encrypted results to a stream and get the sizes array
            long[] weightSumSizes = await EncryptedMLHelper.convert2DCipherListToStreamAsync(encryptedWeightedSums, encryptedFeatureValuesStream);
            UInt64 sizeOfEncryptedFeatureValues = (UInt64)(encryptedFeatureValuesStream.Length);
            
            // Step 10: Append the sizes array to the stream
            // The client needs this to know how to read the encrypted data back
            //serialize columnSizes (list of the sizes of each ciphertext object in query.encryptedFeatureValues)
            MemoryStream columnSizesStream = new MemoryStream();
            formatter.Serialize(encryptedFeatureValuesStream, weightSumSizes); //copy columnSizes to end of encryptedFeatureValuesStream
            
            // Step 11: Append the total size to the end of the stream
            // The client reads this first to know how much data to read
            // copy total size of the encryptedFeatureValues Cipher objects to the end of encryptedFeatureValuesStream
            BinaryWriter writer = new BinaryWriter(encryptedFeatureValuesStream);
            writer.Seek(0, SeekOrigin.End); // Go to the end
            writer.Write(sizeOfEncryptedFeatureValues); // Write the 8-byte size value
            
            // Step 12: Return the response
            // Reset to the start of the stream and send it as binary data
            encryptedFeatureValuesStream.Seek(0, SeekOrigin.Begin);

            // Return the stream as a file response
            // "application/octet-stream" means "raw binary data"
            return File(encryptedFeatureValuesStream, "application/octet-stream");;
        }
    }
}
