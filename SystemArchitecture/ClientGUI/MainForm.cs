using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using CDTS_PROJECT.Logics;
using Microsoft.Research.SEAL;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

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
        private HttpClient client;
        
        // Selected file path
        private string selectedFilePath = "";
        
        public MainForm()
        {
            InitializeComponent();
            InitializeCoreComponents();
        }
        
        /// <summary>
        /// InitializeCoreComponents: Sets up encryption components.
        /// </summary>
        private void InitializeCoreComponents()
        {
            // Initialize encryption context and key manager (same as console client)
            contextManager = new ContextManager();
            keyManager = new KeyManager(contextManager);
            client = new HttpClient();
            
            // Set default server address (can be changed in settings)
            client.BaseAddress = new Uri("http://localhost:8080/");
            client.DefaultRequestHeaders.Accept.Clear();
        }
        
        /// <summary>
        /// InitializeComponent: Creates and arranges all UI components.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Machine Learning with Encryption (MLE)";
            this.Size = new System.Drawing.Size(800, 600);
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
                Text = "Instructions: 1) Select mode 2) Select model 3) Choose CSV file 4) Click Process",
                Location = new System.Drawing.Point(20, 540),
                Size = new System.Drawing.Size(750, 20),
                Font = new System.Drawing.Font("Arial", 8),
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(instructionsLabel);
        }
        
        /// <summary>
        /// ModeComboBox_SelectedIndexChanged: Updates available models based on selected mode.
        /// </summary>
        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update model list based on mode
            // In a full implementation, you could query the server for available models
            modelComboBox.Items.Clear();
            modelComboBox.Items.Add("LogisticRegression");
            modelComboBox.SelectedIndex = 0;
            
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
                AddStatusText("Machine Learning with Encryption\n");
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
                AddStatusText($"   ✓ Loaded {featureList.Count} samples with {featureList[0].Count} features\n\n");
                
                // Step 3: Encrypt data
                AddStatusText("Step 3: Encrypting data...\n");
                List<List<Ciphertext>> encryptedFeatureValues = 
                    EncryptedMLHelper.encryptValues(featureList, publicKey, contextManager.Context);
                AddStatusText($"   ✓ Encrypted {encryptedFeatureValues.Count} samples\n\n");
                
                // Step 4: Send to server
                AddStatusText("Step 4: Sending encrypted data to server...\n");
                string modelName = modelComboBox.SelectedItem.ToString();
                List<List<Ciphertext>> encryptedWeightedSums = 
                    await GetPredictionsAsync(encryptedFeatureValues, modelName);
                AddStatusText("   ✓ Received encrypted predictions from server\n\n");
                
                // Step 5: Decrypt results
                AddStatusText("Step 5: Decrypting results...\n");
                var results = EncryptedMLHelper.decryptValues(encryptedWeightedSums, secretKey, contextManager.Context);
                AddStatusText($"   ✓ Decrypted {results.Count} predictions\n\n");
                
                // Step 6: Save results
                AddStatusText("Step 6: Saving results...\n");
                string outputPath = Path.Combine(Path.GetDirectoryName(selectedFilePath), "Result.csv");
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
        /// GetPredictionsAsync: Sends encrypted data to server and receives encrypted predictions.
        /// (Same logic as console client's getPredictions method)
        /// </summary>
        private async Task<List<List<Ciphertext>>> GetPredictionsAsync(
            List<List<Ciphertext>> encryptedFeatureValues, string modelName)
        {
            // Serialize encrypted data
            MemoryStream encryptedFeatureValuesStream = new MemoryStream();
            long[] columnSizes = EncryptedMLHelper.convert2DCipherListToStream(
                encryptedFeatureValues, encryptedFeatureValuesStream);
            encryptedFeatureValuesStream.Seek(0, SeekOrigin.Begin);
            StreamContent encryptedFeatureValuesStreamContent = new StreamContent(encryptedFeatureValuesStream);
            
            // Serialize column sizes
            MemoryStream columnSizesStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(columnSizesStream, columnSizes);
            columnSizesStream.Seek(0, SeekOrigin.Begin);
            StreamContent columnSizesStreamContent = new StreamContent(columnSizesStream);
            
            // Create multipart form
            var content = new MultipartFormDataContent();
            content.Add(encryptedFeatureValuesStreamContent, "encryptedFeatureValues");
            content.Add(columnSizesStreamContent, "columnSizes");
            
            // Send request
            var response = await client.PostAsync($"api/encryptedML?modelname={modelName}", content);
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                string badResponseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Response Status Code: {response.StatusCode}\nResponse Message: {badResponseContent}");
            }
            
            // Read response
            var responseContent = await response.Content.ReadAsStreamAsync();
            
            // Parse response (same as console client)
            responseContent.Seek(-8, SeekOrigin.End);
            BinaryReader reader = new BinaryReader(responseContent);
            UInt64 sizeOfEncryptedWeightedSums = reader.ReadUInt64();
            
            responseContent.Seek((long)sizeOfEncryptedWeightedSums, SeekOrigin.Begin);
            UInt64 lengthOfWeightedSumSizes = (UInt64)responseContent.Length - (sizeOfEncryptedWeightedSums + 8);
            
            MemoryStream tempStream = new MemoryStream(reader.ReadBytes((int)lengthOfWeightedSumSizes));
            long[] weightSumSizes = (long[])formatter.Deserialize(tempStream);
            tempStream.Close();
            
            responseContent.Seek(0, SeekOrigin.Begin);
            tempStream = new MemoryStream(reader.ReadBytes((int)sizeOfEncryptedWeightedSums));
            tempStream.Seek(0, SeekOrigin.Begin);
            
            List<List<Ciphertext>> encryptedWeightedSums = 
                EncryptedMLHelper.extract2DCipherListFromStream(tempStream, weightSumSizes, contextManager.Context);
            tempStream.Close();
            
            return encryptedWeightedSums;
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

