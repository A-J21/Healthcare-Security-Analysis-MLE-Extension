using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Research.SEAL;
using CDTS_PROJECT.Logics;

namespace MLE_GUI.Services
{
    /// Query: Simple data structure to hold encrypted feature values.
    /// 
    /// This structure wraps the encrypted data that needs to be processed.
    /// In a real client-server deployment, this would be sent over the network.
    public class Query
    {
        /// Encrypted feature values: 2D list where each row is a sample, 
        /// each column is an encrypted feature value.
        public List<List<Ciphertext>> encryptedFeatureValues { get; set; } = new List<List<Ciphertext>>();
    }

    /// Model: Represents a machine learning model with weights/coefficients.
    /// 
    /// This class stores the trained model information needed for encrypted inference.
    /// Models are loaded from CSV files containing coefficient matrices.
    public class Model
    {
        /// Model type/name (e.g., "LogisticRegression", "FinancialFraud", "AcademicGrade")
        public string Type { get; set; } = "";

        /// Number of output classes/categories (typically 1 for binary classification)
        public int M_classes { get; set; }

        /// Number of input features (must match encrypted data feature count)
        public int N_weights { get; set; }

        /// Precision/scaling factor used during encryption (typically 1000)
        public int Precision { get; set; }

        /// Model weights: List of weight arrays, one per class.
        /// Each array contains one weight per feature.
        public List<double[]> Weights { get; set; } = new List<double[]>();
    }

    /// LocalMLService: Provides local machine learning inference on encrypted data.
    /// 
    /// PURPOSE:
    /// This service simulates the server-side ML processing functionality locally,
    /// allowing the application to run entirely on a single machine without requiring
    /// a separate server or network infrastructure.
    /// 
    /// HOW IT WORKS:
    /// 1. Takes encrypted data from the GUI client
    /// 2. Loads the appropriate ML model from CSV files
    /// 3. Performs homomorphic operations (multiplication, addition) on encrypted data
    /// 4. Returns encrypted predictions without ever decrypting the input data
    /// 
    /// SECURITY NOTE:
    /// Even though processing happens locally in this demo, the encryption ensures
    /// that if this were deployed as a client-server system, the server would never
    /// see unencrypted data. The homomorphic properties allow computation on ciphertexts.
    public class LocalMLService
    {
        /// Service for loading and managing ML models from CSV file
        private readonly LocalModelService _modelService;

        /// Service for performing homomorphic operations on encrypted data
        private readonly LocalEncryptedOperationsService _encryptedOperationsService;

        ///Encryption context manager (provides encryption parameters and context)
        private readonly ContextManager _contextManager;

        /// Constructor: Initializes the local ML service with required components.
        /// 
        /// PARAMETERS:
        /// - modelsDirectory: Optional path to directory containing model CSV files.
        ///   If null, defaults to SystemArchitecture/configDB/
        public LocalMLService(string? modelsDirectory = null)
        {
            // Initialize model service to load models from CSV files
            // Models are stored as coefficient matrices in CSV format
            _modelService = new LocalModelService(modelsDirectory);

            // Initialize encryption context with standard BFV parameters
            // This must match the context used by the client for encryption
            _contextManager = new ContextManager();

            // Initialize encrypted operations service for performing ML inference
            // This service can do multiplication and addition on encrypted data
            _encryptedOperationsService = new LocalEncryptedOperationsService(_contextManager.Context);
        }

        /// <summary>
        /// GetPredictionsAsync: Performs encrypted ML inference on the provided encrypted data.
        /// 
        /// This is the main method that simulates server-side ML processing locally.
        /// It performs logistic regression inference entirely on encrypted data using
        /// homomorphic encryption operations.
        /// 
        /// PROCESS:
        /// 1. Loads the specified model from CSV files
        /// 2. Validates that input feature count matches model expectations
        /// 3. Performs weighted sum calculation: sum(feature_i * weight_i) for each sample
        /// 4. All operations happen on encrypted Ciphertext objects - data never decrypted
        /// 5. Returns encrypted predictions that only the client can decrypt
        /// 
        /// HOMOMORPHIC OPERATIONS:
        /// - Multiply encrypted feature values by plaintext model weights
        /// - Add encrypted weighted values together
        /// - Results remain encrypted throughout the process
        /// </summary>
        /// <param name="encryptedFeatureValues">
        /// 2D list of encrypted features: [samples][features]
        /// Each feature is a Ciphertext object encrypted with BFV scheme
        /// </param>
        /// <param name="modelName">
        /// Name of the model to use: "LogisticRegression", "FinancialFraud", or "AcademicGrade"
        /// </param>
        /// <returns>
        /// Encrypted predictions: 2D list [samples][classes]
        /// Predictions are still encrypted - must be decrypted by client using secret key
        /// </returns>
        /// <exception cref="Exception">
        /// Throws exception if model not found, feature count mismatch, or processing error
        /// </exception>
        public async Task<List<List<Ciphertext>>> GetPredictionsAsync(
            List<List<Ciphertext>> encryptedFeatureValues, 
            string modelName)
        {
            try
            {
                // Step 1: Load the requested model from CSV files
                // Models are stored as coefficient matrices in SystemArchitecture/configDB/
                Model selectedModel = _modelService.Get(modelName);

                // Step 2: Validate that encrypted data was provided
                if (encryptedFeatureValues == null || encryptedFeatureValues.Count == 0)
                {
                    throw new Exception("No encrypted feature values provided.");
                }

                // Step 3: Validate feature count matches model expectations
                // This prevents runtime errors during homomorphic operations
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

                // Step 4: Create a Query object containing the encrypted data
                // This structure mirrors what would be sent over HTTP in a client-server setup
                Query query = new Query
                {
                    encryptedFeatureValues = encryptedFeatureValues
                };

                // Step 5: Perform encrypted ML inference using homomorphic operations
                // This calculates: weighted_sum = feature1*weight1 + feature2*weight2 + ...
                // All operations are performed on encrypted Ciphertext objects
                // The service never sees the actual data values - only encrypted representations
                List<List<Ciphertext>> encryptedWeightedSums = 
                    await _encryptedOperationsService.CalculateWeightedSumAsync(selectedModel, query);

                // Step 6: Return encrypted predictions
                // These predictions are still encrypted and can only be decrypted by the client
                // using the secret key that was never shared with the server
                return encryptedWeightedSums;
            }
            catch (Exception ex)
            {
                // Wrap exceptions with additional context for easier debugging
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

