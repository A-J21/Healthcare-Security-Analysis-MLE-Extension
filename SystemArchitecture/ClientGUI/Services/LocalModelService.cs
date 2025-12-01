using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MLE_GUI.Services
{
    /// LocalModelService: Loads ML models from CSV files instead of a database. (Previously loaded MongoDB)
    /// 
    /// PURPOSE:
    /// This service replaces the original MongoDB-based model storage with a file-based
    /// approach, allowing the application to run entirely locally without requiring
    /// a database server or network infrastructure.
    /// 
    /// HOW IT WORKS:
    /// - Models are stored as CSV files containing coefficient/weight matrices
    /// - Each row in the CSV represents one output class
    /// - Each column represents the weight for one input feature
    /// - Models are loaded into memory at application startup
    /// 
    /// SUPPORTED MODELS:
    /// - LogisticRegression (Healthcare): Default model in configDB/coefs.csv
    /// - FinancialFraud, also LogisticRegression: Located in configDB/FinancialFraud/coefs.csv
    /// - AcademicGrade, also LogisticRegression: Located in configDB/AcademicGrade/coefs.csv
    public class LocalModelService
    {
        /// In-memory dictionary of loaded models, keyed by model name.
        /// Models are loaded once at startup and reused for all predictions.
        private readonly Dictionary<string, Model> _models = new Dictionary<string, Model>();

        /// Root directory containing model CSV files.
        /// Defaults to SystemArchitecture/configDB/ relative to project root.
        private readonly string _modelsDirectory;

        /// <summary>
        /// Constructor: Initializes the service and loads available models.
        /// </summary>
        /// <param name="modelsDirectory">Directory containing model CSV files (coefs.csv)</param>
        public LocalModelService(string modelsDirectory = null)
        {
            // Default to configDB directory if not specified
            if (string.IsNullOrEmpty(modelsDirectory))
            {
                // Get the base directory (project root)
                var baseDir = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "..", "..", "..", "..", ".."));
                _modelsDirectory = Path.Combine(baseDir, "SystemArchitecture", "configDB");
            }
            else
            {
                _modelsDirectory = modelsDirectory;
            }

            // Load available models
            LoadModels();
        }

        /// <summary>
        /// LoadModels: Scans for model CSV files and loads them.
        /// 
        /// Currently supports:
        /// - coefs.csv (default LogisticRegression model)
        /// - Models can be in subdirectories named after the model type
        /// - Also checks ModelTraining directory for trained models
        /// </summary>
        private void LoadModels()
        {
            // Load default model if coefs.csv exists
            string defaultCoefsPath = Path.Combine(_modelsDirectory, "coefs.csv");
            if (File.Exists(defaultCoefsPath))
            {
                var model = LoadModelFromCsv(defaultCoefsPath, "LogisticRegression");
                if (model != null)
                {
                    _models["LogisticRegression"] = model;
                }
            }

            // Try to load FinancialFraud model from multiple locations
            // First try: SystemArchitecture/configDB/FinancialFraud/coefs.csv
            string financialPath = Path.Combine(_modelsDirectory, "FinancialFraud", "coefs.csv");
            if (!File.Exists(financialPath))
            {
                // Second try: ModelTraining directory (where models are trained)
                var baseDir = Path.GetFullPath(Path.Combine(
                    _modelsDirectory, "..", "..", ".."));
                financialPath = Path.Combine(baseDir, "ModelTraining", "coefs.csv");
                // Only use this if it's actually a financial model (10 features)
                if (File.Exists(financialPath))
                {
                    try
                    {
                        var testModel = LoadModelFromCsv(financialPath, "FinancialFraud");
                        // Financial model should have 10 features
                        if (testModel != null && testModel.N_weights == 10)
                        {
                            _models["FinancialFraud"] = testModel;
                            return; // Successfully loaded
                        }
                    }
                    catch { } // If it fails, continue to next location
                }
            }
            else
            {
                var model = LoadModelFromCsv(financialPath, "FinancialFraud");
                if (model != null)
                {
                    _models["FinancialFraud"] = model;
                }
            }

            // Try to load AcademicGrade model from multiple locations
            string academicPath = Path.Combine(_modelsDirectory, "AcademicGrade", "coefs.csv");
            if (!File.Exists(academicPath))
            {
                // Try ModelTraining directory
                var baseDir = Path.GetFullPath(Path.Combine(
                    _modelsDirectory, "..", "..", ".."));
                academicPath = Path.Combine(baseDir, "ModelTraining", "coefs.csv");
                if (File.Exists(academicPath))
                {
                    try
                    {
                        var testModel = LoadModelFromCsv(academicPath, "AcademicGrade");
                        // Academic model should have 10 features
                        if (testModel != null && testModel.N_weights == 10)
                        {
                            _models["AcademicGrade"] = testModel;
                        }
                    }
                    catch { }
                }
            }
            else
            {
                var model = LoadModelFromCsv(academicPath, "AcademicGrade");
                if (model != null)
                {
                    _models["AcademicGrade"] = model;
                }
            }
        }

        /// <summary>
        /// LoadModelFromCsv: Loads a model from a CSV file.
        /// 
        /// CSV format: Each row = one class, each column = one feature weight
        /// </summary>
        private Model LoadModelFromCsv(string csvPath, string modelType)
        {
            try
            {
                var lines = File.ReadAllLines(csvPath);
                if (lines.Length == 0)
                {
                    return null;
                }

                // Parse CSV: each line is a class, each value is a weight
                var weights = new List<double[]>();
                int featureCount = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = line.Split(',')
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrEmpty(v))
                        .Select(double.Parse)
                        .ToArray();

                    if (values.Length > 0)
                    {
                        if (featureCount == 0)
                        {
                            featureCount = values.Length;
                        }
                        else if (values.Length != featureCount)
                        {
                            throw new Exception($"Inconsistent feature count in {csvPath}: expected {featureCount}, got {values.Length}");
                        }

                        weights.Add(values);
                    }
                }

                if (weights.Count == 0)
                {
                    throw new Exception($"Model CSV file is empty or contains no valid data: {csvPath}");
                }

                var model = new Model
                {
                    Type = modelType,
                    M_classes = weights.Count,
                    N_weights = featureCount,
                    Precision = 1000, // Standard precision used in the system
                    Weights = weights
                };

                // Log model info for debugging
                System.Diagnostics.Debug.WriteLine($"Loaded model: {modelType}, Classes: {model.M_classes}, Features: {model.N_weights}");

                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load model from {csvPath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get: Retrieves a model by type/name.
        /// </summary>
        public Model Get(string modelType)
        {
            if (_models.TryGetValue(modelType, out var model))
            {
                return model;
            }

            string availableModels = _models.Keys.Count > 0 
                ? string.Join(", ", _models.Keys) 
                : "none (no models loaded)";
            
            // Provide helpful instructions based on model type
            string instructions = "";
            if (modelType == "FinancialFraud")
            {
                instructions = "\n\nTo create the FinancialFraud model:\n" +
                    "1. cd ModelTraining\n" +
                    "2. python train_financial_model_new.py\n" +
                    "3. Copy coefs.csv to SystemArchitecture/configDB/FinancialFraud/coefs.csv";
            }
            else if (modelType == "AcademicGrade")
            {
                instructions = "\n\nTo create the AcademicGrade model:\n" +
                    "1. cd ModelTraining\n" +
                    "2. python train_academic_model_new.py\n" +
                    "3. Copy coefs.csv to SystemArchitecture/configDB/AcademicGrade/coefs.csv";
            }
            
            throw new Exception($"Model '{modelType}' not found. Available models: {availableModels}{instructions}");
        }

        /// <summary>
        /// GetModelInfo: Returns a formatted string with model information.
        /// </summary>
        public string GetModelInfo(string modelType)
        {
            if (_models.TryGetValue(modelType, out var model))
            {
                return $"Model: {model.Type}, Classes: {model.M_classes}, Features: {model.N_weights}, Precision: {model.Precision}";
            }
            return $"Model '{modelType}' not found";
        }

        /// <summary>
        /// GetAll: Returns all loaded models.
        /// </summary>
        public List<Model> GetAll()
        {
            return _models.Values.ToList();
        }

        /// <summary>
        /// GetAvailableModelNames: Returns list of available model names.
        /// </summary>
        public List<string> GetAvailableModelNames()
        {
            return _models.Keys.ToList();
        }
    }
}

