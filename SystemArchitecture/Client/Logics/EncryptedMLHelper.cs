using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Logics
{
    /// EncryptedMLHelper: Helper class containing utility methods for working with encrypted data.
    /// 
    /// WHAT IT DOES:
    /// This class provides methods to:
    /// - Read CSV files and convert them to lists of numbers
    /// - Encrypt data using homomorphic encryption
    /// - Decrypt results from the server
    /// - Convert encrypted data to/from streams for network transmission
    /// 
    /// WHY IT'S NEEDED:
    /// Encrypted data (Ciphertext objects) can't be sent over the network directly.
    /// We need to serialize them (convert to bytes) first, then deserialize them (convert back) on the other end.
    public static class EncryptedMLHelper
    {
        /// extract2DCipherListFromStream: Reconstructs encrypted data from a stream.
        /// 
        /// WHAT IT DOES:
        /// When the server sends back encrypted results, they come as a stream of bytes.
        /// This method reads that stream and reconstructs the 2D list of encrypted values.
        /// 
        /// PARAMETERS:
        /// - encryptedFeatureValuesStream: The stream containing encrypted data from the server
        /// - columnSizes: An array telling us how many bytes each encrypted value takes
        ///   (Special: -1 means "end of current sample, start a new one")
        /// - context: The encryption context needed to load the encrypted data
        /// 
        /// RETURNS:
        /// A 2D list: List of samples, where each sample is a list of encrypted feature values
        static public List<List<Ciphertext>> extract2DCipherListFromStream(MemoryStream encryptedFeatureValuesStream, long[] columnSizes, SEALContext context){

            // This will hold all our encrypted samples
            // Structure: encryptedFeatureValues[sample_index][feature_index] = encrypted value
            List<List<Ciphertext>> encryptedFeatureValues = new List<List<Ciphertext>>();

            int offset = 0; // Track position in stream (I don't see how it's currently used but keeping for potential future use)
            
            // Current sample we're building up
            List<Ciphertext> curSample = new List<Ciphertext>();
            
            // Go through each entry in columnSizes
            // Each entry tells us how many bytes to read for the next encrypted value
            foreach (long size in columnSizes){

                // Special marker: -1 means "we've finished this sample, start a new one"
                if (size == -1){
                    // Save the completed sample to our list
                    encryptedFeatureValues.Add(curSample);
                    // Start a fresh sample list
                    curSample = new List<Ciphertext>();
                    continue; // Skip to next iteration
                }

                // Read the specified number of bytes from the stream
                // This is one encrypted value (a Ciphertext object)
                Byte[] tempByteArray = new byte[(int)size];
                encryptedFeatureValuesStream.Read(tempByteArray, 0, (int)size);
                
                // Create a temporary stream from those bytes
                MemoryStream tempStream = new MemoryStream(tempByteArray);
                tempStream.Seek(0, SeekOrigin.Begin); // Make sure we're at the start
                
                // Create a new Ciphertext object and load the encrypted data into it
                Ciphertext encryptedFeature = new Ciphertext();
                encryptedFeature.Load(context, tempStream); // Load using our encryption context
                tempStream.Close();

                // Add this encrypted feature to the current sample
                curSample.Add(encryptedFeature);
                
                offset += (int)size; // Track total bytes read (for potential debugging)
            }

            // Return the reconstructed 2D list of encrypted values
            return encryptedFeatureValues;

        }

        /// convert2DCipherListToStream: Converts encrypted data to a stream for network transmission.
        /// 
        /// WHAT IT DOES:
        /// Takes our 2D list of encrypted values and writes them to a stream (byte array).
        /// Also creates a "columnSizes" array that tells us how many bytes each encrypted value takes.
        /// This is needed because encrypted values can have different sizes.
        /// 
        /// PARAMETERS:
        /// - encryptedFeatureValues: 2D list of encrypted data [samples][features]
        /// - memoryStream: The stream to write the encrypted data to
        /// 
        /// RETURNS:
        /// An array of sizes - tells us how many bytes each encrypted value takes
        /// (Special: -1 marks the end of each sample)
        /// 
        /// HOW IT WORKS:
        /// 1. For each sample, for each encrypted feature:
        ///    - Write the encrypted value to the stream
        ///    - Record how many bytes it took
        /// 2. After each sample, add a -1 marker
        /// 3. Return the array of sizes so the receiver knows how to read it back
        static public long[] convert2DCipherListToStream(List<List<Ciphertext>> encryptedFeatureValues, MemoryStream memoryStream){

            // Initialize array to store the size of each encrypted value
            // Formula: (number of samples) * (features per sample + 1 marker per sample)
            // The +1 is for the -1 marker we add after each sample
            long[] columnSizes = new long[encryptedFeatureValues.Count * (encryptedFeatureValues[0].Count + 1)]; 
            int curIndex = 0; // Current position in the columnSizes array
            
            // Track stream position to calculate how many bytes each value takes
            long lastPosition = memoryStream.Position; 
            long curSize = 0;

            // Process each sample (row of data)
            foreach (List<Ciphertext> sample in encryptedFeatureValues) //for each sample
            {
                // Process each encrypted feature in this sample
                foreach (Ciphertext encryptedFeature in sample)//for each feature
                {
                    // Write the encrypted value to the stream
                    // This converts the Ciphertext object to bytes
                    encryptedFeature.Save(memoryStream); //write current column (i.e. feature) to stream

                    // Calculate how many bytes this encrypted value took
                    // Current position - previous position = bytes written
                    curSize = memoryStream.Position - lastPosition;
                    lastPosition = memoryStream.Position; // Update for next calculation
                    
                    // Record the size of this encrypted value
                    columnSizes[curIndex] = curSize;
                    curIndex++;
                }
                
                // Add a marker (-1) to indicate "end of this sample"
                // When reading back, we'll use this to know when to start a new sample
                columnSizes[curIndex] = -1;
                curIndex++;
            }   


            // Return the array of sizes
            // The receiver will use this to know how many bytes to read for each encrypted value
            return columnSizes;
        }

        /// ReadValuesToList: Reads a CSV file and converts it to a 2D list of floats.
        /// 
        /// WHAT IT DOES:
        /// Reads a CSV file where each line is a sample and each comma-separated value is a feature.
        /// Converts all values to floats and returns them as a 2D list.
        /// 
        /// PARAMETERS:
        /// - csvPath: Path to the CSV file to read
        /// 
        /// RETURNS:
        /// A 2D list: List of samples, where each sample is a list of feature values (floats)
        /// 
        /// CSV FORMAT EXPECTED:
        /// - No header row
        /// - Each line = one sample
        /// - Values separated by commas
        /// - Example: "0.123,0.456,0.789"
        static public List<List<float>> ReadValuesToList(string csvPath)
        {
            // This will hold all our samples
            // Structure: list[sample_index][feature_index] = feature value
            List<List<float>> list = new List<List<float>>();
            
            // Open the CSV file for reading
            using StreamReader reader = new StreamReader(csvPath);
            
            int lineNumber = 0;
            // Read line by line until we reach the end of the file
            while (!reader.EndOfStream)
            {
                lineNumber++;
                // Read one line (one sample)
                String? line = reader.ReadLine();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                // Trim whitespace and split by commas
                line = line.Trim();
                
                // Split the line by commas and filter out empty strings
                string[] parts = line.Split(',');
                List<string> validParts = new List<string>();
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        validParts.Add(trimmed);
                    }
                }
                
                if (validParts.Count == 0)
                {
                    continue; // Skip lines with no valid data
                }
                
                // Convert each part to a float
                List<float> featureList = new List<float>();
                for (int i = 0; i < validParts.Count; i++)
                {
                    try
                    {
                        float value = float.Parse(validParts[i]);
                        featureList.Add(value);
                    }
                    catch (FormatException)
                    {
                        throw new Exception($"Invalid number format on line {lineNumber}, column {i + 1}: '{validParts[i]}'. Expected a numeric value.");
                    }
                }
                
                // Add this sample to our list
                list.Add(featureList);
            }

            if (list.Count == 0)
            {
                throw new Exception("CSV file appears to be empty or contains no valid data.");
            }

            // Return the 2D list of all samples
            return list;
        }

        /// encryptValues: Encrypts all feature values using homomorphic encryption.
        /// 
        /// WHAT IT DOES:
        /// Takes plain (unencrypted) feature values and encrypts each one.
        /// The encrypted values can then be sent to the server without revealing the actual data.
        /// 
        /// PARAMETERS:
        /// - featureList: 2D list of plain (unencrypted) feature values
        /// - pk: Public key used for encryption (can be shared with server)
        /// - context: Encryption context containing the encryption parameters
        /// 
        /// RETURNS:
        /// A 2D list of encrypted values (Ciphertext objects)
        /// 
        /// HOW IT WORKS:
        /// 1. For each sample, for each feature value:
        ///    - Scale the value (multiply by 1000) because homomorphic encryption works with integers
        ///    - Encode the integer into a Plaintext object
        ///    - Encrypt the Plaintext to get a Ciphertext
        /// 2. Return all encrypted values
        /// 
        /// WHY SCALE BY 1000:
        /// Homomorphic encryption works with integers, not decimals.
        /// Multiplying by 1000 preserves 3 decimal places (e.g., 0.123 becomes 123).
        /// We'll divide by 1000 when decrypting to get the original value back.
        static public List<List<Ciphertext>> encryptValues(List<List<float>> featureList,  PublicKey pk, SEALContext context)
        {
            // IntegerEncoder: Converts integers to Plaintext objects (needed for encryption)
            // Plaintext is the "unencrypted but formatted" version of our data
            IntegerEncoder encoder = new IntegerEncoder(context);
            
            // Encryptor: Performs the actual encryption using the public key
            // The public key allows encryption but NOT decryption (that requires the secret key)
            Encryptor encryptor = new Encryptor(context, pk);

            // This will hold all our encrypted samples
            List<List<Ciphertext>> cList = new List<List<Ciphertext>>();
            
            // Process each sample (row of data)
            foreach (List<float> row in featureList)
            {
                // This will hold the encrypted features for this sample
                List<Ciphertext> cRow = new List<Ciphertext>();
                
                // Encrypt each feature value in this sample
                for (int i = 0; i < row.Count; i++)
                {
                    // Scale the float value to an integer
                    // Example: 0.123 * 1000 = 123
                    // This preserves 3 decimal places of precision
                    long featureItem = (long)(row[i] * 1000);
                    
                    // Encode the integer into a Plaintext object
                    // Plaintext is the format needed before encryption
                    Plaintext fPlain = encoder.Encode(featureItem);
                    
                    // Create a Ciphertext object to hold the encrypted result
                    Ciphertext fCipher = new Ciphertext();

                    // Encrypt the Plaintext using the public key
                    // The result is stored in fCipher
                    // Now the value is encrypted and can be sent to the server safely
                    encryptor.Encrypt(fPlain, fCipher);

                    // Add this encrypted feature to the current sample
                    cRow.Add(fCipher);
                }
                
                // Add this encrypted sample to our list
                cList.Add(cRow);
            }

            // Return all encrypted samples
            return cList;
        }

        /// decryptValues: Decrypts the results returned from the server.
        /// 
        /// WHAT IT DOES:
        /// The server performs calculations on encrypted data and returns encrypted results.
        /// This method decrypts those results so we can see the actual predictions.
        /// 
        /// PARAMETERS:
        /// - weihtedSums: 2D list of encrypted results from the server
        ///   (Note: "weihtedSums" is a typo but kept for consistency with existing code)
        /// - sk: Secret key used for decryption (only the client has this!)
        /// - context: Encryption context containing the encryption parameters
        /// 
        /// RETURNS:
        /// A 2D list of decrypted values (long integers)
        /// 
        /// HOW IT WORKS:
        /// 1. For each sample, for each encrypted result:
        ///    - Check the "noise budget" (homomorphic operations add "noise" - if it gets too high, decryption fails)
        ///    - Decrypt the Ciphertext to get a Plaintext
        ///    - Decode the Plaintext to get an integer
        ///    - Divide by (1000*1000) to undo scaling
        ///      (We multiplied by 1000 when encrypting, and the server multiplied by 1000 for weights)
        /// 2. Return all decrypted results
        /// 
        /// NOISE BUDGET:
        /// Homomorphic encryption operations add "noise" to the encrypted data.
        /// If too many operations are performed, the noise gets too high and decryption fails.
        /// We check this before decrypting to catch errors early.
        static public List<List<long>> decryptValues(List<List<Ciphertext>> weihtedSums,  SecretKey sk, SEALContext context)
        {
            // This will hold all our decrypted results
            List<List<long>> results = new List<List<long>>();

            // Decryptor: Performs decryption using the secret key
            // Only the client has the secret key, so only the client can decrypt
            Decryptor decryptor = new Decryptor(context, sk);
            
            // IntegerEncoder: Decodes Plaintext objects back to integers
            IntegerEncoder encoder = new IntegerEncoder(context);

            int sample_ind = 0; // Track which sample we're processing (for error messages)
            
            // Process each sample of encrypted results
            foreach (List<Ciphertext> sample in weihtedSums)
            {
                // This will hold the decrypted results for this sample
                List<long> resultsRow = new List<long>();
                
                // Decrypt each encrypted result in this sample
                foreach (Ciphertext encryptedClassScore in sample)
                {
                    // Create a Plaintext object to hold the decrypted result
                    Plaintext plainClassScore = new Plaintext();

                    // Check the "noise budget" - if it's 0, the encrypted value is too noisy to decrypt
                    // This happens when too many homomorphic operations have been performed
                    // It's a safety check to catch errors before attempting decryption
                    if (decryptor.InvariantNoiseBudget(encryptedClassScore) == 0){
                        throw new Exception("Noise budget depleated in sample "+sample_ind+". Aborting...");
                    }
                    
                    // Decrypt the Ciphertext to get a Plaintext
                    // This is where the magic happens - we can now see the result!
                    decryptor.Decrypt(encryptedClassScore, plainClassScore);
                    
                    // Decode the Plaintext to get an integer
                    // Then divide by (1000*1000) to undo the scaling:
                    // - We multiplied by 1000 when encrypting features
                    // - The server multiplied by 1000 when applying weights
                    // - So we need to divide by 1000*1000 to get back to the original scale
                    long ClassScore = encoder.DecodeInt64(plainClassScore)/(1000*1000);
                    
                    // Add this decrypted result to the current sample
                    resultsRow.Add(ClassScore);
                }
                
                // Add this sample's results to our list
                results.Add(resultsRow);
                sample_ind++; // Move to next sample
            }

            // Return all decrypted results
            return results;
        }
    
    }
}