using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks; 
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;
using System.IO;
using System.Collections.Generic;
// using System.Runtime.Serialization.Formatters.Binary; // no longer used
using System.Text;

namespace CDTS_PROJECT
{
    /// Query: Data structure that holds the encrypted data we send to the server.
    /// 
    /// WHAT IT CONTAINS:
    /// - publicKey: The public key (allows server to perform operations on encrypted data)
    /// - encryptedFeatureValues: The actual encrypted feature data
    /// 
    /// NOTE: In the current implementation, we don't actually send the publicKey
    /// (it's commented out), but the structure is kept for potential future use.
    public class Query{
        public PublicKey publicKey { get; set;}
        public List<List<Ciphertext>> encryptedFeatureValues { get; set;}
    }

    /// Program: Main client application for Machine Learning with Encryption (MLE).
    /// 
    /// WHAT THIS PROGRAM DOES:
    /// 1. Reads a CSV file containing feature data (e.g., patient genomic data)
    /// 2. Encrypts the data using homomorphic encryption
    /// 3. Sends the encrypted data to the server
    /// 4. Receives encrypted predictions back
    /// 5. Decrypts the predictions and saves them to a CSV file
    /// 
    /// KEY SECURITY FEATURE:
    /// The server NEVER sees the unencrypted data. It performs machine learning
    /// inference on encrypted data and returns encrypted results.
    class Program
    {
        // ContextManager: Sets up encryption parameters (like the "settings" for encryption)
        static ContextManager contextManager = new ContextManager();
        
        // KeyManager: Creates encryption keys (public key for encryption, secret key for decryption)
        static KeyManager keyManager = new KeyManager(contextManager);
        
        // HttpClient: Used to send HTTP requests to the server
        static HttpClient client = new HttpClient(); //instantiate HttpClient

        /// getPredictions: Sends encrypted data to the server and receives encrypted predictions back.
        /// 
        /// WHAT IT DOES:
        /// 1. Serializes (converts to bytes) the encrypted data so it can be sent over the network
        /// 2. Sends it to the server via HTTP POST request
        /// 3. Receives encrypted results back
        /// 4. Deserializes (converts from bytes) the encrypted results
        /// 
        /// PARAMETERS:
        /// - query: Contains the encrypted feature values we want to get predictions for
        /// 
        /// RETURNS:
        /// Encrypted predictions from the server (still encrypted - we'll decrypt them later)
        /// 
        /// HOW THE SERVER RESPONSE IS STRUCTURED:
        /// The server sends back a stream with this structure:
        /// [encrypted results data][array of sizes][total size (8 bytes)]
        /// We read it backwards: first get the total size, then the sizes array, then the actual data.
        static async Task<List<List<Ciphertext>>> getPredictions(Query query)
        {
            // NOTE: Public key serialization is commented out - not currently used
            // The server doesn't need the public key in the current implementation
            //serialized publicKey
            //MemoryStream pkStream = new MemoryStream();
            //query.publicKey.Save(pkStream);
            //pkStream.Seek(0, SeekOrigin.Begin);
            //StreamContent pkStreamContent = new StreamContent(pkStream);

            // Serialize encryptedFeatureValues: Convert encrypted data to a stream of bytes
            // This is necessary because Ciphertext objects can't be sent over HTTP directly
            MemoryStream encryptedFeatureValuesStream = new MemoryStream();
            // convert2DCipherListToStream writes the encrypted data to the stream
            // and returns an array telling us how many bytes each encrypted value takes
            long[] columnSizes = Logics.EncryptedMLHelper.convert2DCipherListToStream(query.encryptedFeatureValues, encryptedFeatureValuesStream);
            encryptedFeatureValuesStream.Seek(0, SeekOrigin.Begin); // Reset to start of stream
            StreamContent encryptedFeatureValuesStreamContent = new StreamContent(encryptedFeatureValuesStream);

            // Serialize columnSizes as a UTF-8 comma-separated string
            // The server will parse this string back into a long[]
            string sizesStr = string.Join(",", columnSizes);
            byte[] sizesBytes = Encoding.UTF8.GetBytes(sizesStr);
            MemoryStream columnSizesStream = new MemoryStream(sizesBytes);
            columnSizesStream.Seek(0, SeekOrigin.Begin);
            StreamContent columnSizesStreamContent = new StreamContent(columnSizesStream);

            // Create a multipart form to send both pieces of data
            // Multipart forms allow sending multiple pieces of data in one HTTP request
            var content = new MultipartFormDataContent(); 
            //content.Add(pkStreamContent, "privateKey"); // Not currently used
            content.Add(encryptedFeatureValuesStreamContent, "encryptedFeatureValues");
            content.Add(columnSizesStreamContent, "columnSizes");
            
            // Send the POST request to the server
            // The server endpoint is "api/encryptedML" and we specify which model to use
            // Currently hardcoded to "LogisticRegression" but could be made user-selectable
            // For end-to-end encrypted testing use the server test endpoint that accepts multipart encrypted data
            var response = await client.PostAsync("api/test/TestEncrypted", content);

            // Check if the request was successful
            if (response.StatusCode != HttpStatusCode.OK){
                // If not, read the error message and throw an exception
                String badResponseContent = await response.Content.ReadAsStringAsync();
                throw new Exception("Response Status Code: "+response.StatusCode+"\nResponse Message: "+ badResponseContent); 
            } 

            // Read the response stream (contains encrypted predictions)
            var responseContent = await response.Content.ReadAsStreamAsync();
            
            // The server sends data in this format: [encrypted data][sizes array][total size]
            // We need to read it backwards to know how to parse it
            
            // Step 1: Read the last 8 bytes to get the total size of encrypted data
            responseContent.Seek(-8, SeekOrigin.End); // Go to 8 bytes from the end
            BinaryReader reader = new BinaryReader(responseContent);  
            UInt64 sizeOfEncryptedWeightedSums = reader.ReadUInt64(); // Read the 8-byte size value

            // Step 2: Calculate where the sizes array starts and how long it is
            // The sizes array is between the encrypted data and the total size
            responseContent.Seek((long)sizeOfEncryptedWeightedSums, SeekOrigin.Begin);
            // Total stream length - (encrypted data size + 8 bytes for total size) = sizes array length
            UInt64  lengthOfWeightedSumSizes =  (UInt64)responseContent.Length - (sizeOfEncryptedWeightedSums + 8);
            
            // Step 3: Read the sizes array (tells us how many bytes each encrypted result takes)
            MemoryStream tempStream = new MemoryStream(reader.ReadBytes((int)lengthOfWeightedSumSizes));
            tempStream.Seek(0, SeekOrigin.Begin);
            // Sizes are sent as a UTF-8 comma-separated string
            string sizesStrResp = Encoding.UTF8.GetString(tempStream.ToArray());
            var partsResp = sizesStrResp.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            long[] weightSumSizes = Array.ConvertAll(partsResp, s => long.Parse(s));
            tempStream.Close();

            // Step 4: Read the actual encrypted results data
            responseContent.Seek(0, SeekOrigin.Begin); // Go back to the start
            tempStream = new MemoryStream(reader.ReadBytes((int)sizeOfEncryptedWeightedSums));
            tempStream.Seek(0, SeekOrigin.Begin);
            
            // Step 5: Extract the encrypted results from the stream
            // This reconstructs the 2D list of Ciphertext objects from the byte stream
            List<List<Ciphertext>> encryptedWeightedSums = Logics.EncryptedMLHelper.extract2DCipherListFromStream(tempStream, weightSumSizes, contextManager.Context);
            tempStream.Close();

            // Return the encrypted predictions (still encrypted - we'll decrypt them in Main)
            return encryptedWeightedSums;
        }
        /// Main: Entry point of the program.
        /// 
        /// Since we're using async/await, we need to call RunAsync and wait for it to complete.
        static void Main()
        {
            // RunAsync is an async method, so we need to wait for it to complete
            // GetAwaiter().GetResult() blocks until the async operation finishes
            RunAsync().GetAwaiter().GetResult();
        }

        /// RunAsync: Main program logic - orchestrates the entire encryption and prediction process.
        /// 
        /// FLOW:
        /// 1. Create encryption keys
        /// 2. Display welcome banner
        /// 3. Get CSV filename from user
        /// 4. Read and encrypt the data
        /// 5. Send encrypted data to server
        /// 6. Receive encrypted predictions
        /// 7. Decrypt predictions
        /// 8. Save results to CSV file
        static async Task RunAsync()
        {
            // Enable BinaryFormatter for this test harness (required on newer .NET versions)
            // NOTE: BinaryFormatter is insecure for untrusted data; this switch is used here
            // to allow the existing serialization logic to run in this local test environment.
            AppContext.SetSwitch("Switch.System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
            // If invoked with --local-test, run an in-process encrypted operations test
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "--local-test")
            {
                Console.WriteLine("Running local encrypted compute test (no HTTP)...");
                // Create keys
                var localKeyPair = keyManager.CreateKeys();
                using PublicKey pubKey = localKeyPair.publicKey;
                using SecretKey secKey = localKeyPair.secretKey;

                // Use a small test CSV by default (relative to repo root)
                string testCsv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "DataGenerators", "test_financial_small.csv"));
                if (!File.Exists(testCsv))
                {
                    Console.WriteLine("Test CSV not found: " + testCsv);
                    return;
                }

                // Read, encrypt
                var featureList = Logics.EncryptedMLHelper.ReadValuesToList(testCsv);
                var encryptedFeatureValues = Logics.EncryptedMLHelper.encryptValues(featureList, pubKey, contextManager.Context);

                // Load model coefficients from ModelTraining/coefs.csv
                string coefsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ModelTraining", "coefs.csv"));
                if (!File.Exists(coefsPath))
                {
                    Console.WriteLine("coefs.csv not found: " + coefsPath);
                    return;
                }
                string line = File.ReadAllText(coefsPath).Trim();
                string[] parts = line.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                double[] weights = Array.ConvertAll(parts, s => double.Parse(s));

                // Build model parameters (single-class logistic regression)
                int precision = 1000;
                int n_weights = weights.Length;
                var weightList = new System.Collections.Generic.List<double[]>() { weights };

                // Perform encrypted computation in-process (simulate server)
                var encoder = new IntegerEncoder(contextManager.Context);
                var evaluator = new Evaluator(contextManager.Context);

                var weightedSums = new System.Collections.Generic.List<System.Collections.Generic.List<Ciphertext>>();

                foreach (var sample in encryptedFeatureValues)
                {
                    if (sample.Count != n_weights)
                    {
                        Console.WriteLine($"Feature count mismatch: {sample.Count} vs expected {n_weights}");
                        return;
                    }
                    var sampleSums = new System.Collections.Generic.List<Ciphertext>();
                    foreach (var classWeights in weightList)
                    {
                        var weightedFeatures = new System.Collections.Generic.List<Ciphertext>();
                        for (int i = 0; i < sample.Count; i++)
                        {
                            long curWeight = (long)(classWeights[i] * precision);
                            if (curWeight == 0) continue;
                            var scaled = encoder.Encode(curWeight);
                            var weighted = new Ciphertext();
                            evaluator.MultiplyPlain(sample[i], scaled, weighted);
                            weightedFeatures.Add(weighted);
                        }
                        var sum = new Ciphertext();
                        evaluator.AddMany(weightedFeatures, sum);
                        sampleSums.Add(sum);
                    }
                    weightedSums.Add(sampleSums);
                }

                // Decrypt results
                var results = Logics.EncryptedMLHelper.decryptValues(weightedSums, secKey, contextManager.Context);
                Console.WriteLine("Decrypted weighted sums (per sample):");
                foreach (var row in results)
                {
                    Console.WriteLine(string.Join(",", row));
                }
                return;
            }
            // Step 1: Create a new pair of encryption keys for this session
            // Public key: Can be shared (used for encryption)
            // Secret key: Must stay secret (used for decryption)
            KeyPair keyPair = keyManager.CreateKeys();

            // Extract the keys from the pair
            // "using" ensures the keys are properly disposed when we're done
            using PublicKey publicKey = keyPair.publicKey;
            using SecretKey secretKey = keyPair.secretKey;

            String FileName = ""; // Will hold the CSV filename entered by the user
            
            // Application information for the welcome banner
            String applicationName = "Machine Learning with Encryption"; 
            String version = "Prototype v 1.0.0";

            // Display welcome banner (ASCII art)
            Console.Write(new String('-', Console.WindowWidth));
            Console.SetCursorPosition((Console.WindowWidth - applicationName.Length) / 2, Console.CursorTop);
            Console.WriteLine(applicationName);

            // Display ASCII art box with "M.L.E." inside
            Console.SetCursorPosition((Console.WindowWidth - "______".Length) / 2, Console.CursorTop);
            Console.WriteLine("______");
            Console.SetCursorPosition((Console.WindowWidth - "/  __  \\".Length) / 2, Console.CursorTop);
            Console.WriteLine("/  __  \\");
            Console.SetCursorPosition((Console.WindowWidth - "_|_|__|_|_".Length) / 2, Console.CursorTop);
            Console.WriteLine("_|_|__|_|_");
            Console.SetCursorPosition((Console.WindowWidth - "|          |".Length) / 2, Console.CursorTop);
            Console.WriteLine("|          |");
            Console.SetCursorPosition((Console.WindowWidth - "|  M.L.E.  |".Length) / 2, Console.CursorTop);
            Console.WriteLine("|  M.L.E.  |");
            Console.SetCursorPosition((Console.WindowWidth - "|__________|".Length) / 2, Console.CursorTop);
            Console.WriteLine("|__________|");

            // Display version and separator line
            Console.Write(new String('-', Console.WindowWidth));
            Console.WriteLine(version+"\n");
            
            // Wrap everything in try-catch to handle errors gracefully shout out Dr. Lehr
            try
            {
                // Step 2: Get the CSV filename from the user or command-line argument
                // If a CLI argument was provided (and it's not the --local-test flag), use it
                if (args.Length > 1 && args[1] != "--local-test")
                {
                    FileName = args[1];
                }
                else
                {
                    Console.Write("\tEnter the name of the CSV containing the samples to be encrypted: ");
                    FileName = Console.ReadLine();
                }

                // Step 3: Validate that it's a CSV file
                string fileExtension = Path.GetExtension(FileName);
                if (fileExtension != ".csv")
                {
                    Console.WriteLine("file type must be CSV");
                    return; // Exit if not a CSV file
                }

                // Step 4: Build the full file path and check if the file exists
                string filePath = FileName;
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
                }
                filePath = Path.GetFullPath(filePath);

                if (!File.Exists(filePath))
                {
                    throw new Exception("File " + filePath + " does not exists.");
                }
                
                // Step 5: Read the CSV file and convert it to a 2D list of floats
                // Each row is a sample, each column is a feature
                List<List<float>> featureList = Logics.EncryptedMLHelper.ReadValuesToList(filePath);
                
                // Step 6: Encrypt all the feature values
                // This converts plain numbers to encrypted Ciphertext objects
                // The server will never see the actual values
                List<List<Ciphertext>> encryptedFeatureValues = Logics.EncryptedMLHelper.encryptValues(featureList, publicKey, contextManager.Context);

                // Step 7: Configure the HTTP client to connect to the server
                // Change this URL to match your server's address
                // Use port 5000 by default since the server was started on that port during testing
                client.BaseAddress = new Uri("http://localhost:5000/");
                client.DefaultRequestHeaders.Accept.Clear();
            

                // Step 8: Create a Query object containing the encrypted data
                Query query = new Query
                {
                    publicKey = publicKey, // Not currently used but kept in structure
                    encryptedFeatureValues = encryptedFeatureValues // The actual encrypted data
                };
               

                // Step 9: Send the encrypted data to the server and get encrypted predictions back
                Console.WriteLine("\tSending Query...");
                List<List<Ciphertext>> encryptedWeightedSums = await getPredictions(query);
                
                // Step 10: Decrypt the predictions
                // Only we (the client) can decrypt because only we have the secret key
                var results = Logics.EncryptedMLHelper.decryptValues(encryptedWeightedSums, secretKey, contextManager.Context);
                
                // Step 11: Save the results to a CSV file
                var csv = new StringBuilder();

                using (System.IO.StreamWriter file = new System.IO.StreamWriter("Result.csv")){
                    // Write each sample's results as a comma-separated row
                    foreach (var sample in results){
                        // Write all values except the last one (with commas)
                        for (int i = 0; i < (sample.Count -1 ); i++ ){
                            file.Write(sample[i].ToString()+",");
                        }
                        // Write the last value (without a comma, with a newline)
                        file.Write(sample[sample.Count -1 ].ToString()+"\n");
                    }
                }
                
                // Success message
                Console.WriteLine("\n\tSuccess, Results saved to Results.csv.\n");
            
            }
            // Step 12: Handle any errors that occurred
            catch (Exception e)
            {
                Console.WriteLine("\n\tError:");
                Console.WriteLine("\t"+e.Message);
                // Some errors have inner exceptions with more details
                if (e.InnerException != null){
                    Console.WriteLine("\t\t"+e.InnerException.Message);
                }
                Console.WriteLine("\n");
            }
        }
    }
}
