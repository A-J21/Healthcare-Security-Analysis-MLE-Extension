using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Research.SEAL;

namespace MLE_GUI.Services
{
    /// <summary>
    /// LocalEncryptedOperationsService: Performs machine learning inference on encrypted data.
    /// 
    /// This is a local version of the server's encryptedOperationsService.
    /// It performs the same operations but runs locally instead of on a remote server.
    /// 
    /// WHAT IT DOES:
    /// For a logistic regression model, it calculates:
    ///   weighted_sum = feature1*weight1 + feature2*weight2 + ... + featureN*weightN
    /// But it does this entirely on encrypted data!
    /// </summary>
    public class LocalEncryptedOperationsService
    {
        private readonly SEALContext _context;

        /// <summary>
        /// Constructor: Initializes the service with encryption context.
        /// </summary>
        public LocalEncryptedOperationsService(SEALContext context)
        {
            _context = context;
        }

        /// <summary>
        /// CalculateWeightedSumAsync: Performs encrypted ML inference (async wrapper).
        /// </summary>
        public async Task<List<List<Ciphertext>>> CalculateWeightedSumAsync(Model selectedModel, Query query)
        {
            return await Task.Run(() => CalculateWeightedSum(selectedModel, query));
        }

        /// <summary>
        /// CalculateWeightedSum: Performs encrypted machine learning inference.
        /// 
        /// This is the core of the system - it performs calculations on encrypted data
        /// WITHOUT ever decrypting it. The server never sees the actual data values.
        /// </summary>
        public List<List<Ciphertext>> CalculateWeightedSum(Model selectedModel, Query query)
        {
            // Get the precision (scaling factor) from the model
            int precision = selectedModel.Precision;

            // Extract the encrypted feature values from the query
            List<List<Ciphertext>> encryptFeatureValues = query.encryptedFeatureValues;

            // Extract the model weights
            List<double[]> weights = selectedModel.Weights;
            int N_features = selectedModel.N_weights;

            // Initialize homomorphic encryption tools
            IntegerEncoder encoder = new IntegerEncoder(_context);
            Evaluator evaluator = new Evaluator(_context);

            // This will hold the encrypted weighted sums for all samples
            List<List<Ciphertext>> weightedSums = new List<List<Ciphertext>>();

            int sampleIndex = 0;

            // Process each sample (each row of encrypted data)
            foreach (List<Ciphertext> sample in encryptFeatureValues)
            {
                // Validate that this sample has the correct number of features
                if (sample.Count != N_features)
                {
                    throw new Exception($"Sample {sampleIndex} has {sample.Count} features but {N_features} expected");
                }

                // This will hold the weighted sums for all classes for this sample
                List<Ciphertext> sampleWeightedSums = new List<Ciphertext>();

                // For each class in the model
                foreach (double[] classWeights in weights)
                {
                    // Calculate weighted feature values for this class
                    List<Ciphertext> weightedFeatures = new List<Ciphertext>();

                    // For each feature in this sample
                    for (int i = 0; i < sample.Count; i++)
                    {
                        // Get the encrypted feature value
                        Ciphertext curFeature = sample[i];

                        // Scale the weight to match the precision used during encryption
                        long curWeight = (long)(classWeights[i] * precision);

                        // Skip if weight is zero
                        if (curWeight == 0)
                        {
                            continue;
                        }

                        // Encode the weight as a Plaintext
                        Plaintext scaledWeight = encoder.Encode(curWeight);

                        // Create a Ciphertext to hold the result
                        Ciphertext weightedFeature = new Ciphertext();

                        // HOMOMORPHIC MULTIPLICATION: Multiply encrypted feature by plain weight
                        evaluator.MultiplyPlain(curFeature, scaledWeight, weightedFeature);

                        // Store the weighted feature
                        weightedFeatures.Add(weightedFeature);
                    }

                    // HOMOMORPHIC ADDITION: Add all weighted features together
                    Ciphertext weightedSum = new Ciphertext();
                    evaluator.AddMany(weightedFeatures, weightedSum);
                    sampleWeightedSums.Add(weightedSum);

                    // Clean up memory
                    weightedFeatures = null;
                    GC.Collect();
                }

                // Add this sample's results to our list
                weightedSums.Add(sampleWeightedSums);

                sampleIndex++;
            }

            // Return all encrypted weighted sums
            return weightedSums;
        }
    }
}

