using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;

namespace MLE_GUI.Services
{
    /// <summary>
    /// Query: Simple data structure to hold encrypted feature values.
    /// </summary>
    public class Query
    {
        public List<List<Ciphertext>> encryptedFeatureValues { get; set; }
    }

    /// <summary>
    /// Model: Represents a machine learning model with weights.
    /// </summary>
    public class Model
    {
        public string Type { get; set; }
        public int M_classes { get; set; }
        public int N_weights { get; set; }
        public int Precision { get; set; }
        public List<double[]> Weights { get; set; }
    }

    /// <summary>
    /// LocalMLService: Provides local machine learning inference on encrypted data.
    /// 
    /// This service wraps the server-side logic so that the GUI can perform 
    /// encrypted ML inference locally without needing a separate server.
    /// 
    /// WHAT IT DOES:
    /// - Takes encrypted data from the client
    /// - Performs ML inference on the encrypted data (homomorphic operations)
    /// - Returns encrypted predictions
    /// 
    /// The server logic is reused, but called directly instead of via HTTP.
    /// </summary>
    public class LocalMLService
    {
        private readonly LocalModelService _modelService;
        private readonly LocalEncryptedOperationsService _encryptedOperationsService;
        private readonly ContextManager _contextManager;

        /// <summary>
        /// Constructor: Initializes the local ML service with required components.
        /// </summary>
        public LocalMLService(string modelsDirectory = null)
        {
            // Initialize model service (loads models from CSV)
            _modelService = new LocalModelService(modelsDirectory);

            // Initialize encryption context (same as server)
            _contextManager = new ContextManager();

            // Initialize encrypted operations service (performs ML on encrypted data)
            _encryptedOperationsService = new LocalEncryptedOperationsService(_contextManager.Context);
        }

        /// <summary>
        /// GetPredictions: Performs encrypted ML inference on the provided encrypted data.
        /// 
        /// This is the main method that replaces the HTTP call to the server.
        /// It does the same thing the server would do, but locally.
        /// </summary>
        /// <param name="encryptedFeatureValues">Encrypted feature values from the client</param>
        /// <param name="modelName">Name of the model to use (e.g., "LogisticRegression")</param>
        /// <returns>Encrypted predictions (still encrypted - client will decrypt)</returns>
        public async Task<List<List<Ciphertext>>> GetPredictionsAsync(
            List<List<Ciphertext>> encryptedFeatureValues, 
            string modelName)
        {
            try
            {
                // Step 1: Load the requested model
                Model selectedModel = _modelService.Get(modelName);

                // Step 2: Validate feature count before processing
                if (encryptedFeatureValues == null || encryptedFeatureValues.Count == 0)
                {
                    throw new Exception("No encrypted feature values provided.");
                }

                int inputFeatureCount = encryptedFeatureValues[0].Count;
                int modelExpectedFeatures = selectedModel.N_weights;

                if (inputFeatureCount != modelExpectedFeatures)
                {
                    throw new Exception(
                        $"Feature count mismatch!\n" +
                        $"  - Your CSV data has: {inputFeatureCount} features per sample\n" +
                        $"  - Model '{modelName}' expects: {modelExpectedFeatures} features\n" +
                        $"  - Number of samples: {encryptedFeatureValues.Count}\n\n" +
                        $"Please ensure your CSV file has exactly {modelExpectedFeatures} comma-separated values per row.");
                }

                // Step 3: Create a Query object (same structure as HTTP request)
                Query query = new Query
                {
                    encryptedFeatureValues = encryptedFeatureValues
                };

                // Step 4: Perform encrypted ML inference
                // This is the same operation the server would perform
                // The server never sees unencrypted data - it only works with encrypted Ciphertext objects
                List<List<Ciphertext>> encryptedWeightedSums = 
                    await _encryptedOperationsService.CalculateWeightedSumAsync(selectedModel, query);

                // Step 5: Return encrypted results
                // The client will decrypt these using the secret key
                return encryptedWeightedSums;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get predictions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// GetAvailableModels: Returns list of available model names.
        /// </summary>
        public List<string> GetAvailableModels()
        {
            return _modelService.GetAvailableModelNames();
        }

        /// <summary>
        /// GetModelInfo: Returns formatted information about a model.
        /// </summary>
        public string GetModelInfo(string modelName)
        {
            return _modelService.GetModelInfo(modelName);
        }

        /// <summary>
        /// GetContext: Returns the encryption context (needed for some operations).
        /// </summary>
        public SEALContext GetContext()
        {
            return _contextManager.Context;
        }
    }
}

