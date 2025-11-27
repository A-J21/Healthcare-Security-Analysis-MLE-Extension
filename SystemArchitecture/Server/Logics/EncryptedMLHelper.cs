using System;
using System.IO;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.Research.SEAL;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CDTS_PROJECT.Logics
{
    /// EncryptedMLHelper: Server-side helper class for working with encrypted data.
    /// 
    /// WHAT IT DOES:
    /// Provides utility methods for:
    /// - Parsing HTTP multipart form data (extracting boundary markers)
    /// - Validating model names (security: prevent injection attacks)
    /// - Converting encrypted data to/from streams for network transmission
    /// 
    /// NOTE: This is the server-side version - it doesn't handle encryption/decryption
    /// (only the client has the secret key). It just works with already-encrypted data.
    public static class EncryptedMLHelper
    {
        /// GetBoundary: Extracts the boundary marker from HTTP multipart form data.
        /// 
        /// WHAT IT DOES:
        /// When data is sent as a multipart form, HTTP uses "boundary" markers to separate
        /// different parts. This method extracts that boundary string.
        /// 
        /// PARAMETERS:
        /// - contentType: The Content-Type header from the HTTP request
        /// 
        /// RETURNS:
        /// The boundary string (e.g., "----WebKitFormBoundary7MA4YWxkTrZu0gW")
        /// 
        /// WHY IT'S NEEDED:
        /// We need the boundary to parse the multipart form and extract the encrypted data.
        public static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            // Extract the boundary value from the Content-Type header
            // Boundaries are usually quoted, so we remove the quotes
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            // Validate that we actually got a boundary
            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            return boundary;
        }

        /// validateModelName: Validates that a model name is safe to use.
        /// 
        /// WHAT IT DOES:
        /// Checks that the model name contains only alphanumeric characters (letters and numbers).
        /// 
        /// PARAMETERS:
        /// - modelname: The model name to validate
        /// 
        /// RETURNS:
        /// true if valid (only letters and numbers), false otherwise
        /// 
        /// WHY IT'S NEEDED:
        /// Security: Prevents injection attacks. If we allowed special characters,
        /// an attacker could potentially inject malicious code or access unauthorized models.
        public static bool validateModelName(string modelname){
            // Use regex to check: ^ = start, [a-zA-Z0-9]+ = one or more letters/numbers, $ = end
            // This ensures the entire string is only alphanumeric characters
            return Regex.IsMatch(modelname, "^[a-zA-Z0-9]+$");
        }

        /// extract2DCipherListFromStreamAsync: Async wrapper for extract2DCipherListFromStream.
        /// 
        /// WHY ASYNC:
        /// Server operations should be async to avoid blocking threads.
        /// This allows the server to handle multiple requests simultaneously.
        public static async Task<List<List<Ciphertext>>> extract2DCipherListFromStreamAsync(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){
            // Run the synchronous version in a background task
            return await Task.Run(() => extract2DCipherListFromStream(encryptedFeatureValuesStream, columnSizes, context));
        }

        /// extract2DCipherListFromStream: Reconstructs encrypted data from a stream (server-side).
        /// 
        /// WHAT IT DOES:
        /// Same as the client version - reads encrypted data from a byte stream and reconstructs
        /// the 2D list of Ciphertext objects.
        /// 
        /// PARAMETERS:
        /// - encryptedFeatureValuesStream: Stream containing encrypted data from the client
        /// - columnSizes: Array telling us how many bytes each encrypted value takes
        ///   (-1 marks the end of each sample)
        /// - context: Encryption context (needed to load Ciphertext objects)
        /// 
        /// RETURNS:
        /// 2D list of encrypted values: [samples][features]
        /// 
        /// NOTE: This is identical to the client version - both sides need to serialize/deserialize
        /// encrypted data the same way.
        public static List<List<Ciphertext>> extract2DCipherListFromStream(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){

            // This will hold all encrypted samples
            List<List<Ciphertext>> encryptedFeatureValues = new List<List<Ciphertext>>();

            int offset = 0; // Track position (for potential debugging)
            List<Ciphertext> curSample = new List<Ciphertext>(); // Current sample being built
            
            // Process each entry in columnSizes
            foreach (long size in columnSizes){

                // -1 marks the end of a sample
                if (size == -1){
                    encryptedFeatureValues.Add(curSample); // Save completed sample
                    curSample = new List<Ciphertext>(); // Start fresh
                    continue;
                }

                // Read the specified number of bytes (one encrypted value)
                Byte[] tempByteArray = new byte[(int)size];
                encryptedFeatureValuesStream.Read(tempByteArray, 0, (int)size);
                
                // Create a temporary stream and load the Ciphertext
                MemoryStream tempStream = new MemoryStream(tempByteArray);
                tempStream.Seek(0, SeekOrigin.Begin);
                Ciphertext encryptedFeature = new Ciphertext();
                encryptedFeature.Load(context, tempStream); // Load using encryption context
                tempStream.Close();

                curSample.Add(encryptedFeature); // Add to current sample
                
                offset += (int)size; // Track bytes read
            }

            return encryptedFeatureValues;

        }


        /// convert2DCipherListToStreamAsync: Async wrapper for convert2DCipherListToStream.
        public static async Task<long[]> convert2DCipherListToStreamAsync(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){
            return await Task.Run(() => convert2DCipherListToStream(encryptedFeatureValues, memoryStream));
        }
        
        /// convert2DCipherListToStream: Converts encrypted data to a stream for network transmission (server-side).
        /// 
        /// WHAT IT DOES:
        /// Same as the client version - writes encrypted data to a stream and creates
        /// an array of sizes so the receiver knows how to read it back.
        /// 
        /// PARAMETERS:
        /// - encryptedFeatureValues: 2D list of encrypted data to send
        /// - memoryStream: Stream to write the data to
        /// 
        /// RETURNS:
        /// Array of sizes (how many bytes each encrypted value takes, with -1 marking sample boundaries)
        /// 
        /// NOTE: This is identical to the client version - both sides serialize data the same way.
        public static long[] convert2DCipherListToStream(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){

            // Initialize array to store sizes
            // Formula: (samples) * (features per sample + 1 marker per sample)
            long[] columnSizes = new long[encryptedFeatureValues.Count * (encryptedFeatureValues[0].Count + 1)]; 
            int curIndex = 0;
            
            // Track stream position to calculate byte sizes
            long lastPosition = memoryStream.Position; 
            long curSize = 0;

            // Process each sample
            foreach (List<Ciphertext> sample in encryptedFeatureValues) //for each sample
            {
                // Process each encrypted feature
                foreach (Ciphertext encryptedFeature in sample)//for each feature
                {
                    // Write encrypted value to stream
                    encryptedFeature.Save(memoryStream); //write current column (i.e. feature) to stream

                    // Calculate and record the size
                    curSize = memoryStream.Position - lastPosition;
                    lastPosition = memoryStream.Position;
                    columnSizes[curIndex] = curSize;
                    curIndex++;
                }
                // Mark end of sample
                columnSizes[curIndex] = -1;
                curIndex++;
            }   


            return columnSizes;
        }
    }
}