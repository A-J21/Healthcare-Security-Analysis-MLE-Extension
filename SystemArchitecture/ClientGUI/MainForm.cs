using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using CDTS_PROJECT.Logics;
using Microsoft.Research.SEAL;
using System.Collections.Generic;
using MLE_GUI.Services;

namespace MLE_GUI
{
    /// <summary>
    /// MainForm: GUI application for Machine Learning with Encryption (MLE).
    /// 
    /// WHAT IT DOES:
    /// Provides a user-friendly interface to:
    /// - Select the mode (Healthcare, Business/Financial, Academic)
    /// - Select a CSV file with data to encrypt and analyze
    /// - Send encrypted data to the server
    /// - Display and save results
    /// 
    /// MODES:
    /// - Healthcare: Cancer genomics prediction (original use case)
    /// - Business: Financial fraud detection
    /// - Academic: Student grade prediction
    /// </summary>
    public partial class MainForm : Form
    {
        // UI Components
        private ComboBox modeComboBox;
        private ComboBox modelComboBox;
        private Button selectFileButton;
        private TextBox filePathTextBox;
        private Button processButton;
        private RichTextBox statusTextBox;
        private ProgressBar progressBar;
        private Label statusLabel;
        
        // Core components (same as console client)
        private ContextManager contextManager;
        private KeyManager keyManager;
        private LocalMLService localMLService;
        
        // Selected file path
        private string selectedFilePath = "";
        
        public MainForm()
        {
            InitializeComponent();
            InitializeCoreComponents();
        }
        
        /// <summary>
        /// InitializeCoreComponents: Sets up encryption components and local ML service.
        /// </summary>
        private void InitializeCoreComponents()
        {
            // Initialize encryption context and key manager (same as console client)
            contextManager = new ContextManager();
            keyManager = new KeyManager(contextManager);
            
            // Initialize local ML service (replaces HTTP client)
            // This service performs ML inference locally without needing a separate server
            localMLService = new LocalMLService();
            
            // Update model list based on available models
            UpdateModelList();
        }
        
        /// <summary>
        /// UpdateModelList: Updates the model combo box with available models.
        /// </summary>
        private void UpdateModelList()
        {
            try
            {
                var availableModels = localMLService.GetAvailableModels();
                modelComboBox.Items.Clear();
                foreach (var modelName in availableModels)
                {
                    modelComboBox.Items.Add(modelName);
                }
                if (modelComboBox.Items.Count > 0)
                {
                    modelComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // If models can't be loaded, keep default
                modelComboBox.Items.Clear();
                modelComboBox.Items.Add("LogisticRegression");
                modelComboBox.SelectedIndex = 0;
                UpdateStatus($"Warning: Could not load models: {ex.Message}");
            }
        }
        
        /// <summary>
        /// InitializeComponent: Creates and arranges all UI components.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Machine Learning with Encryption (MLE) - Local Mode";
            this.Size = new System.Drawing.Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Title label
            Label titleLabel = new Label
            {
                Text = "Machine Learning with Encryption",
                Font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(750, 30),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(titleLabel);
            
            // Mode selection
            Label modeLabel = new Label
            {
                Text = "Select Mode:",
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(150, 20)
            };
            this.Controls.Add(modeLabel);
            
            modeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(180, 68),
                Size = new System.Drawing.Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            modeComboBox.Items.AddRange(new string[] { "Healthcare", "Business (Financial)", "Academic" });
            modeComboBox.SelectedIndex = 0;
            modeComboBox.SelectedIndexChanged += ModeComboBox_SelectedIndexChanged;
            this.Controls.Add(modeComboBox);
            
            // Model selection
            Label modelLabel = new Label
            {
                Text = "Select Model:",
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(150, 20)
            };
            this.Controls.Add(modelLabel);
            
            modelComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(180, 108),
                Size = new System.Drawing.Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            modelComboBox.Items.Add("LogisticRegression");
            modelComboBox.SelectedIndex = 0;
            this.Controls.Add(modelComboBox);
            
            // File selection
            Label fileLabel = new Label
            {
                Text = "Data File (CSV):",
                Location = new System.Drawing.Point(20, 150),
                Size = new System.Drawing.Size(150, 20)
            };
            this.Controls.Add(fileLabel);
            
            filePathTextBox = new TextBox
            {
                Location = new System.Drawing.Point(180, 148),
                Size = new System.Drawing.Size(450, 25),
                ReadOnly = true
            };
            this.Controls.Add(filePathTextBox);
            
            selectFileButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(640, 147),
                Size = new System.Drawing.Size(100, 27)
            };
            selectFileButton.Click += SelectFileButton_Click;
            this.Controls.Add(selectFileButton);
            
            // Process button
            processButton = new Button
            {
                Text = "Process Encrypted ML",
                Location = new System.Drawing.Point(300, 200),
                Size = new System.Drawing.Size(200, 40),
                Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold),
                Enabled = false
            };
            processButton.Click += ProcessButton_Click;
            this.Controls.Add(processButton);
            
            // Status label
            statusLabel = new Label
            {
                Text = "Ready",
                Location = new System.Drawing.Point(20, 260),
                Size = new System.Drawing.Size(750, 20)
            };
            this.Controls.Add(statusLabel);
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 290),
                Size = new System.Drawing.Size(750, 25),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            this.Controls.Add(progressBar);
            
            // Status text box
            statusTextBox = new RichTextBox
            {
                Location = new System.Drawing.Point(20, 330),
                Size = new System.Drawing.Size(750, 200),
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9)
            };
            this.Controls.Add(statusTextBox);
            
            // Instructions
            Label instructionsLabel = new Label
            {
                Text = "Instructions: 1) Select mode 2) Select model 3) Choose CSV file 4) Click Process | Running locally - no server required!",
                Location = new System.Drawing.Point(20, 540),
                Size = new System.Drawing.Size(850, 20),
                Font = new System.Drawing.Font("Arial", 8),
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(instructionsLabel);
            
            // Add info label about local processing
            Label infoLabel = new Label
            {
                Text = "✓ All processing runs locally on this machine - encryption, ML inference, and decryption",
                Location = new System.Drawing.Point(20, 560),
                Size = new System.Drawing.Size(850, 20),
                Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            this.Controls.Add(infoLabel);
        }
        
        /// <summary>
        /// ModeComboBox_SelectedIndexChanged: Updates available models based on selected mode.
        /// </summary>
        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update model list based on available models from local service
            UpdateModelList();
            
            // Auto-select the appropriate model based on mode
            string selectedMode = modeComboBox.SelectedItem?.ToString() ?? "";
            string targetModel = "";
            
            if (selectedMode.Contains("Financial") || selectedMode.Contains("Business"))
            {
                targetModel = "FinancialFraud";
            }
            else if (selectedMode.Contains("Academic"))
            {
                targetModel = "AcademicGrade";
            }
            else if (selectedMode.Contains("Healthcare"))
            {
                targetModel = "LogisticRegression";
            }
            
            // Try to select the target model if it's available
            if (!string.IsNullOrEmpty(targetModel))
            {
                for (int i = 0; i < modelComboBox.Items.Count; i++)
                {
                    if (modelComboBox.Items[i].ToString() == targetModel)
                    {
                        modelComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            UpdateStatus($"Mode changed to: {modeComboBox.SelectedItem}");
        }
        
        /// <summary>
        /// SelectFileButton_Click: Opens file dialog to select CSV file.
        /// </summary>
        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = openFileDialog.FileName;
                    filePathTextBox.Text = selectedFilePath;
                    processButton.Enabled = true;
                    UpdateStatus($"File selected: {Path.GetFileName(selectedFilePath)}");
                }
            }
        }
        
        /// <summary>
        /// ProcessButton_Click: Main processing function - encrypts data and sends to server.
        /// </summary>
        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show("Please select a valid CSV file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Disable UI during processing
            processButton.Enabled = false;
            selectFileButton.Enabled = false;
            progressBar.Visible = true;
            statusTextBox.Clear();
            
            try
            {
                UpdateStatus("Starting encrypted ML processing...");
                AddStatusText("=" + new string('=', 60) + "\n");
                AddStatusText("Machine Learning with Encryption (Local Mode)\n");
                AddStatusText("=" + new string('=', 60) + "\n\n");
                
                // Step 1: Create encryption keys
                AddStatusText("Step 1: Creating encryption keys...\n");
                KeyPair keyPair = keyManager.CreateKeys();
                using PublicKey publicKey = keyPair.publicKey;
                using SecretKey secretKey = keyPair.secretKey;
                AddStatusText("   ✓ Keys created successfully\n\n");
                
                // Step 2: Read CSV file
                AddStatusText("Step 2: Reading CSV file...\n");
                List<List<float>> featureList = EncryptedMLHelper.ReadValuesToList(selectedFilePath);
                
                if (featureList.Count == 0)
                {
                    throw new Exception("CSV file is empty or could not be read.");
                }
                
                int featureCount = featureList[0].Count;
                AddStatusText($"   ✓ Loaded {featureList.Count} samples with {featureCount} features\n");
                
                // Validate feature count consistency
                for (int i = 1; i < featureList.Count; i++)
                {
                    if (featureList[i].Count != featureCount)
                    {
                        throw new Exception($"Inconsistent feature count: Sample 0 has {featureCount} features, but sample {i} has {featureList[i].Count} features.");
                    }
                }
                
                // Get model info for validation
                string modelName = modelComboBox.SelectedItem?.ToString() ?? "LogisticRegression";
                try
                {
                    string modelInfo = localMLService.GetModelInfo(modelName);
                    AddStatusText($"   Model: {modelInfo}\n");
                    
                    // Check if feature counts match
                    var availableModels = localMLService.GetAvailableModels();
                    if (availableModels.Contains(modelName))
                    {
                        // We'll validate this later, but show a warning if mismatch
                        AddStatusText($"   ⚠ Make sure your CSV has the correct number of features!\n");
                    }
                }
                catch (Exception ex)
                {
                    AddStatusText($"   ⚠ Warning: {ex.Message}\n");
                }
                
                AddStatusText("\n");
                
                // Step 3: Encrypt data
                AddStatusText("Step 3: Encrypting data...\n");
                List<List<Ciphertext>> encryptedFeatureValues = 
                    EncryptedMLHelper.encryptValues(featureList, publicKey, contextManager.Context);
                AddStatusText($"   ✓ Encrypted {encryptedFeatureValues.Count} samples\n\n");
                
                // Step 4: Perform encrypted ML inference locally
                AddStatusText("Step 4: Performing encrypted ML inference...\n");
                
                // Validate model requirements before processing
                try
                {
                    // This will throw if model not found, but we want to catch it with a better message
                    var availableModels = localMLService.GetAvailableModels();
                    if (!availableModels.Contains(modelName))
                    {
                        throw new Exception($"Model '{modelName}' not found. Available models: {string.Join(", ", availableModels)}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Model validation failed: {ex.Message}");
                }
                
                AddStatusText($"   Using model: {modelName}\n");
                AddStatusText($"   Input features: {featureList[0].Count}\n");
                
                List<List<Ciphertext>> encryptedWeightedSums = 
                    await localMLService.GetPredictionsAsync(encryptedFeatureValues, modelName);
                AddStatusText("   ✓ Encrypted predictions computed successfully\n\n");
                
                // Step 5: Decrypt results
                AddStatusText("Step 5: Decrypting results...\n");
                var results = EncryptedMLHelper.decryptValues(encryptedWeightedSums, secretKey, contextManager.Context);
                AddStatusText($"   ✓ Decrypted {results.Count} predictions\n\n");
                
                // Step 6: Save results
                AddStatusText("Step 6: Saving results...\n");
                string? directory = Path.GetDirectoryName(selectedFilePath);
                string outputPath = Path.Combine(directory ?? Directory.GetCurrentDirectory(), "Result.csv");
                using (StreamWriter file = new StreamWriter(outputPath))
                {
                    foreach (var sample in results)
                    {
                        for (int i = 0; i < (sample.Count - 1); i++)
                        {
                            file.Write(sample[i].ToString() + ",");
                        }
                        file.Write(sample[sample.Count - 1].ToString() + "\n");
                    }
                }
                AddStatusText($"   ✓ Results saved to: {outputPath}\n\n");
                
                AddStatusText("=" + new string('=', 60) + "\n");
                AddStatusText("SUCCESS! Processing complete.\n");
                AddStatusText("=" + new string('=', 60) + "\n");
                
                UpdateStatus("Processing complete! Results saved to Result.csv");
                MessageBox.Show($"Processing complete!\n\nResults saved to:\n{outputPath}", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddStatusText("\n" + new string('=', 60) + "\n");
                AddStatusText("ERROR: " + ex.Message + "\n");
                if (ex.InnerException != null)
                {
                    AddStatusText("Inner Exception: " + ex.InnerException.Message + "\n");
                }
                AddStatusText(new string('=', 60) + "\n");
                
                UpdateStatus("Error: " + ex.Message);
                MessageBox.Show($"Error occurred:\n\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable UI
                processButton.Enabled = true;
                selectFileButton.Enabled = true;
                progressBar.Visible = false;
            }
        }
        
        
        /// <summary>
        /// UpdateStatus: Updates the status label.
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            statusLabel.Text = message;
        }
        
        /// <summary>
        /// AddStatusText: Adds text to the status text box.
        /// </summary>
        private void AddStatusText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddStatusText), text);
                return;
            }
            statusTextBox.AppendText(text);
            statusTextBox.ScrollToCaret();
        }
    }
}

