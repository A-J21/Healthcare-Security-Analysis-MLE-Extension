using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CDTS_PROJECT.Services;
using System.Collections.Generic;
using CDTS_PROJECT.Models;
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;
using System.Threading.Tasks;
using CDTS_PROJECT.Exceptions;

namespace CDTS_PROJECT.Services
{
    /// IencryptedOperationsService: Interface defining encrypted computation operations.
    /// 
    /// This interface allows us to swap implementations or mock the service for testing.
    public interface IencryptedOperationsService
    {
        List<List<Ciphertext>> calculateWeightedSum(Model selectedModel, Query query); 

        Task<List<List<Ciphertext>>> calculateWeightedSumAsync(Model selectedModel, Query query);

    }

    /// encryptedOperationsService: Performs machine learning inference on encrypted data.
    /// 
    /// THIS IS THE CORE OF THE SYSTEM:
    /// This service performs calculations on encrypted data WITHOUT ever decrypting it.
    /// The server never sees the actual data values - it only works with encrypted Ciphertext objects.
    /// 
    /// WHAT IT DOES:
    /// For a logistic regression model, it calculates:
    ///   weighted_sum = feature1*weight1 + feature2*weight2 + ... + featureN*weightN
    /// But it does this entirely on encrypted data!
    /// 
    /// HOW IT WORKS:
    /// 1. Takes encrypted feature values from the client
    /// 2. Multiplies each encrypted feature by its corresponding model weight (encrypted operation)
    /// 3. Adds all the weighted features together (encrypted operation)
    /// 4. Returns encrypted results (still encrypted - only the client can decrypt)
    public class encryptedOperationsService : IencryptedOperationsService
    {
        // Logger: For recording events and errors (useful for debugging)
        private readonly ILogger<encryptedOperationsService> _logger;

        // ContextManager: Provides the encryption context needed for homomorphic operations
        private readonly IContextManager _contextManager;

        /// Constructor: Initializes the service with logger and context manager.
        public encryptedOperationsService(ILogger<encryptedOperationsService> logger, IContextManager ContextManager)
        {
            _logger = logger;
            _contextManager = ContextManager;
        }

        /// calculateWeightedSumAsync: Async wrapper for calculateWeightedSum.
        /// 
        /// WHY ASYNC:
        /// Server operations should be async to avoid blocking threads.
        /// This allows handling multiple requests simultaneously.
        public async Task<List<List<Ciphertext>>> calculateWeightedSumAsync(Model selectedModel, Query query){
            return await Task.Run(() => calculateWeightedSum(selectedModel, query));
        }
        
        /// calculateWeightedSum: Performs encrypted machine learning inference.
        /// 
        /// WHAT IT DOES:
        /// Calculates the weighted sum for each class in a logistic regression model,
        /// but does it entirely on encrypted data.
        /// 
        /// PARAMETERS:
        /// - selectedModel: The machine learning model (contains weights for each feature)
        /// - query: Contains the encrypted feature values from the client
        /// 
        /// RETURNS:
        /// Encrypted weighted sums (one sum per class, per sample)
        /// 
        /// HOW IT WORKS (THE MAGIC OF HOMOMORPHIC ENCRYPTION):
        /// 1. For each sample:
        ///    a. For each class in the model:
        ///       - Multiply each encrypted feature by its weight (homomorphic multiplication)
        ///       - Add all weighted features together (homomorphic addition)
        ///    b. Return encrypted results
        /// 
        /// KEY INSIGHT:
        /// The server performs: encrypted_feature * weight = encrypted_result
        /// It never sees the actual feature values or the results!
        public List<List<Ciphertext>> calculateWeightedSum(Model selectedModel, Query query)
        {
            // Get the precision (scaling factor) from the model
            // This matches the precision used when encrypting (1000 in our case)
            int precision = selectedModel.Precision;
            
            // NOTE: Public key extraction is commented out - not currently used
            // The server doesn't need the public key for these operations
            //extract public key and encryption parameters from query
            //PublicKey publicKey = query.publicKey;

            // Extract the encrypted feature values from the query
            // This is the encrypted data from the client
            List<List<Ciphertext>> encryptFeatureValues = query.encryptedFeatureValues;

            // Extract the model weights from the Model object
            // weights[class_index][feature_index] = weight value
            // For logistic regression, we have one set of weights per class
            List<double[]> weights = selectedModel.Weights;
            int N_features = selectedModel.N_weights; // Number of features the model expects

            // Initialize homomorphic encryption tools
            // IntegerEncoder: Converts integers to Plaintext (needed for multiplying with encrypted values)
            IntegerEncoder encoder = new IntegerEncoder(_contextManager.Context); 
            // NOTE: Encryptor is commented out - server doesn't encrypt, only performs operations
            //Encryptor encryptor = new Encryptor(_contextManager.Context, publicKey);
            
            // Evaluator: Performs homomorphic operations (add, multiply) on encrypted data
            // This is the key object that lets us do math on encrypted values!
            Evaluator evaluator = new Evaluator(_contextManager.Context);

            // This will hold the encrypted weighted sums for all samples
            // Structure: weightedSums[sample_index][class_index] = encrypted weighted sum
            List<List<Ciphertext>> weightedSums = new List<List<Ciphertext>>(); //holds encrypted weighted sums for all classes for all samples
            
            int sampleIndex = 0; // Track which sample we're processing (for error messages)
            
            // Process each sample (each row of encrypted data)
            foreach (List<Ciphertext> sample in encryptFeatureValues){ //for each sample in encrypted values

                // Validate that this sample has the correct number of features
                // The model expects a specific number, and we need to match it
                if(sample.Count != N_features){
                    HttpResponseException errorResponse =  new HttpResponseException();
                    errorResponse.Status = 404;
                    errorResponse.Value = "Sample "+sampleIndex.ToString()+" has "+sample.Count.ToString()+" features but "+N_features.ToString()+" expected";
                    throw errorResponse;  
                }

                // This will hold the weighted sums for all classes for this sample
                List<Ciphertext> sampleWeightedSums = new List<Ciphertext>();  //holds encrypted weighted sums for all classes for this sample
                
                // For each class in the model (logistic regression has one set of weights per class)
                foreach(double[] classWeights in weights){ //for each class 

                    // Calculate weighted feature values for this class
                    // We'll multiply each encrypted feature by its weight, then add them all together
                    List<Ciphertext> weightedFeatures = new List<Ciphertext>();
                    
                    // For each feature in this sample
                    for (int i = 0; i < sample.Count; i++){ 
                        
                        // Get the encrypted feature value
                        Ciphertext curFeature = sample[i];
                        
                        // Scale the weight to match the precision used during encryption
                        // Example: weight = 0.5, precision = 1000 → curWeight = 500
                        long curWeight = (long)(classWeights[i] * precision);
                        
                        // Skip if weight is zero (no point multiplying by zero)
                        if (curWeight == 0){
                            continue;
                        }
                        
                        // Encode the weight as a Plaintext (unencrypted but formatted for homomorphic operations)
                        Plaintext scaledWeight = encoder.Encode(curWeight);
                        
                        // Create a Ciphertext to hold the result
                        Ciphertext weightedFeature = new Ciphertext();
                        
                        // HOMOMORPHIC MULTIPLICATION: Multiply encrypted feature by plain weight
                        // This is the magic! We can multiply an encrypted value by a plain value
                        // and get an encrypted result, without ever decrypting the feature!
                        evaluator.MultiplyPlain(curFeature, scaledWeight, weightedFeature);
                        
                        // Store the weighted feature
                        weightedFeatures.Add(weightedFeature);
                    }

                    // HOMOMORPHIC ADDITION: Add all weighted features together
                    // This gives us: feature1*weight1 + feature2*weight2 + ... + featureN*weightN
                    // All done on encrypted data!
                    Ciphertext weightedSum = new Ciphertext();
                    evaluator.AddMany(weightedFeatures, weightedSum); // Add all weighted features
                    sampleWeightedSums.Add(weightedSum);
                    
                    // Clean up memory (homomorphic operations use a lot of memory)
                    weightedFeatures = null;
                    GC.Collect(); // Force garbage collection to free memory
                }

                // Add this sample's results to our list
                weightedSums.Add(sampleWeightedSums);

                sampleIndex ++; // Move to next sample
            }

            // Return all encrypted weighted sums
            // The client will decrypt these to get the actual predictions
            return weightedSums; //replace with return weighted sums
        }

    }
}