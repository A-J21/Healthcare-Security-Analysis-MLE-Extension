using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using CDTS_PROJECT.Logics;
using Microsoft.Research.SEAL;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using MLE_GUI.Services;

namespace MLE_GUI
{
    // Modernized MainForm with step-by-step visualization of the encrypted ML process.
    public class MainFormModern : Form
    {
        // Step panels
        private Panel step1Panel, step2Panel, step3Panel, step4Panel, step5Panel, step6Panel;
        private Panel currentStepPanel;
        
        // Step 1: Mode Selection
        private ComboBox modeComboBox;
        private Label modeDescriptionLabel;
        private PictureBox modeIcon;
        
        // Step 2: File Selection
        private Button selectFileButton;
        private TextBox filePathTextBox;
        private DataGridView filePreviewGrid;
        private Label fileInfoLabel;
        private RichTextBox fileFormatInfo;
        
        // Step 3: Encryption
        private Button encryptButton;
        private RichTextBox encryptionInfo;
        private Label publicKeyLabel, secretKeyLabel;
        private ProgressBar encryptionProgress;
        private Label encryptionStatus;
        private Panel keyVisualizationPanel;
        
        // Step 4: Server Simulation
        private Button processButton;
        private RichTextBox serverInfo;
        private ProgressBar serverProgress;
        private Label serverStatus;
        private Panel dataFlowPanel;
        
        // Step 5: Results Transmission
        private RichTextBox transmissionInfo;
        private ProgressBar transmissionProgress;
        
        // Step 6: Decryption & Results
        private Button decryptButton;
        private RichTextBox resultsDisplay;
        private Label resultsSummary;
        private DataGridView resultsGrid;
        private Button saveResultsButton;
        
        // Navigation
        private Button nextButton, previousButton, startOverButton;
        private Label stepIndicator;
        private int currentStep = 1;
        
        // Core components
        private ContextManager contextManager;
        private KeyManager keyManager;
        private LocalMLService localMLService;
        private KeyPair? currentKeyPair;
        private PublicKey? currentPublicKey;
        private SecretKey? currentSecretKey;
        private string selectedFilePath = "";
        private string selectedMode = "";
        private List<List<float>>? loadedData;
        private List<List<Ciphertext>>? encryptedData;
        private List<List<Ciphertext>>? encryptedPredictions;
        private List<List<long>>? decryptedResults;
        
        public MainFormModern()
        {
            InitializeComponent();
            InitializeCoreComponents();
            ShowStep(1);
        }
        
        private void InitializeCoreComponents()
        {
            contextManager = new ContextManager();
            keyManager = new KeyManager(contextManager);
            localMLService = new LocalMLService();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Encrypted Machine Learning - Interactive Demo";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Title
            Label titleLabel = new Label
            {
                Text = "Encrypted Machine Learning Demo",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1160, 40),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(titleLabel);
            
            // Step indicator
            stepIndicator = new Label
            {
                Text = "Step 1 of 6: Select Mode",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Location = new Point(20, 70),
                Size = new Size(1160, 25),
                ForeColor = Color.FromArgb(108, 117, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(stepIndicator);
            
            // Main content area
            Panel contentPanel = new Panel
            {
                Location = new Point(20, 105),
                Size = new Size(1160, 600),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(contentPanel);
            
            // Initialize step panels
            InitializeStepPanels(contentPanel);
            
            // Navigation buttons
            previousButton = new Button
            {
                Text = "Previous",
                Location = new Point(20, 720),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            previousButton.FlatAppearance.BorderSize = 0;
            previousButton.Click += PreviousButton_Click;
            this.Controls.Add(previousButton);
            
            nextButton = new Button
            {
                Text = "Next",
                Location = new Point(1020, 720),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            nextButton.FlatAppearance.BorderSize = 0;
            nextButton.Click += NextButton_Click;
            this.Controls.Add(nextButton);
            
            startOverButton = new Button
            {
                Text = "Start Over",
                Location = new Point(520, 720),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            startOverButton.FlatAppearance.BorderSize = 0;
            startOverButton.Click += StartOverButton_Click;
            this.Controls.Add(startOverButton);
        }
        
        private void InitializeStepPanels(Panel parent)
        {
            // Step 1: Mode Selection
            step1Panel = CreateStep1Panel();
            parent.Controls.Add(step1Panel);
            
            // Step 2: File Selection
            step2Panel = CreateStep2Panel();
            parent.Controls.Add(step2Panel);
            
            // Step 3: Encryption
            step3Panel = CreateStep3Panel();
            parent.Controls.Add(step3Panel);
            
            // Step 4: Server Simulation
            step4Panel = CreateStep4Panel();
            parent.Controls.Add(step4Panel);
            
            // Step 5: Results Transmission
            step5Panel = CreateStep5Panel();
            parent.Controls.Add(step5Panel);
            
            // Step 6: Decryption & Results
            step6Panel = CreateStep6Panel();
            parent.Controls.Add(step6Panel);
        }
        
        private Panel CreateStep1Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 1: Select Application Mode",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35),
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            panel.Controls.Add(title);
            
            Label description = new Label
            {
                Text = "Choose the type of data you want to analyze. The appropriate model will be automatically selected.",
                Font = new Font("Segoe UI", 11),
                Location = new Point(20, 65),
                Size = new Size(1120, 30),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            panel.Controls.Add(description);
            
            modeComboBox = new ComboBox
            {
                Location = new Point(400, 120),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            modeComboBox.Items.AddRange(new[] { "Healthcare", "Business (Financial)", "Academic" });
            modeComboBox.SelectedIndexChanged += ModeComboBox_SelectedIndexChanged;
            panel.Controls.Add(modeComboBox);
            
            modeDescriptionLabel = new Label
            {
                Location = new Point(50, 180),
                Size = new Size(1100, 200),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(73, 80, 87)
            };
            panel.Controls.Add(modeDescriptionLabel);
            
            UpdateModeDescription();
            
            return panel;
        }
        
        private Panel CreateStep2Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 2: Select Data File",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35)
            };
            panel.Controls.Add(title);
            
            selectFileButton = new Button
            {
                Text = "Browse for CSV File",
                Location = new Point(50, 70),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            selectFileButton.FlatAppearance.BorderSize = 0;
            selectFileButton.Click += SelectFileButton_Click;
            panel.Controls.Add(selectFileButton);
            
            filePathTextBox = new TextBox
            {
                Location = new Point(270, 75),
                Size = new Size(850, 30),
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            panel.Controls.Add(filePathTextBox);
            
            fileInfoLabel = new Label
            {
                Location = new Point(50, 130),
                Size = new Size(1100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69)
            };
            panel.Controls.Add(fileInfoLabel);
            
            // File preview
            Label previewLabel = new Label
            {
                Text = "File Preview (First 10 rows):",
                Location = new Point(50, 170),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            panel.Controls.Add(previewLabel);
            
            filePreviewGrid = new DataGridView
            {
                Location = new Point(50, 200),
                Size = new Size(1070, 200),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(222, 226, 230)
            };
            panel.Controls.Add(filePreviewGrid);
            
            // Format information
            fileFormatInfo = new RichTextBox
            {
                Location = new Point(50, 420),
                Size = new Size(1070, 150),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(fileFormatInfo);
            
            return panel;
        }
        
        private Panel CreateStep3Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 3: Encrypt Data",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35)
            };
            panel.Controls.Add(title);
            
            Label important = new Label
            {
                Text = "IMPORTANT: The 'server' (simulated locally) NEVER sees this encryption process or your raw data!",
                Location = new Point(50, 70),
                Size = new Size(1100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            panel.Controls.Add(important);
            
            encryptButton = new Button
            {
                Text = "Generate Keys & Encrypt",
                Location = new Point(50, 110),
                Size = new Size(250, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            encryptButton.FlatAppearance.BorderSize = 0;
            encryptButton.Click += EncryptButton_Click;
            panel.Controls.Add(encryptButton);
            
            encryptionInfo = new RichTextBox
            {
                Location = new Point(50, 170),
                Size = new Size(1070, 200),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(encryptionInfo);
            
            // Key visualization
            keyVisualizationPanel = new Panel
            {
                Location = new Point(50, 380),
                Size = new Size(1070, 150),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(keyVisualizationPanel);
            
            publicKeyLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(500, 50),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(0, 123, 255)
            };
            keyVisualizationPanel.Controls.Add(publicKeyLabel);
            
            secretKeyLabel = new Label
            {
                Location = new Point(550, 20),
                Size = new Size(500, 50),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 53, 69)
            };
            keyVisualizationPanel.Controls.Add(secretKeyLabel);
            
            encryptionProgress = new ProgressBar
            {
                Location = new Point(50, 540),
                Size = new Size(1070, 25),
                Style = ProgressBarStyle.Continuous
            };
            panel.Controls.Add(encryptionProgress);
            
            encryptionStatus = new Label
            {
                Location = new Point(50, 570),
                Size = new Size(1070, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            panel.Controls.Add(encryptionStatus);
            
            return panel;
        }
        
        private Panel CreateStep4Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 4: Simulated Server Processing",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35)
            };
            panel.Controls.Add(title);
            
            Label note = new Label
            {
                Text = "NOTE: This is a LOCAL simulation. In a real deployment, encrypted data would be sent over a network.",
                Location = new Point(50, 70),
                Size = new Size(1100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            panel.Controls.Add(note);
            
            processButton = new Button
            {
                Text = "Process Encrypted Data",
                Location = new Point(50, 110),
                Size = new Size(250, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            processButton.FlatAppearance.BorderSize = 0;
            processButton.Click += ProcessButton_Click;
            panel.Controls.Add(processButton);
            
            serverInfo = new RichTextBox
            {
                Location = new Point(50, 170),
                Size = new Size(1070, 300),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(serverInfo);
            
            // Data flow visualization
            dataFlowPanel = new Panel
            {
                Location = new Point(50, 480),
                Size = new Size(1070, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(dataFlowPanel);
            
            serverProgress = new ProgressBar
            {
                Location = new Point(50, 570),
                Size = new Size(1070, 25),
                Style = ProgressBarStyle.Continuous
            };
            panel.Controls.Add(serverProgress);
            
            serverStatus = new Label
            {
                Location = new Point(50, 600),
                Size = new Size(1070, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            panel.Controls.Add(serverStatus);
            
            return panel;
        }
        
        private Panel CreateStep5Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 5: Encrypted Results Transmission",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35)
            };
            panel.Controls.Add(title);
            
            transmissionInfo = new RichTextBox
            {
                Location = new Point(50, 70),
                Size = new Size(1070, 400),
                Font = new Font("Segoe UI", 10),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(transmissionInfo);
            
            transmissionProgress = new ProgressBar
            {
                Location = new Point(50, 480),
                Size = new Size(1070, 25),
                Style = ProgressBarStyle.Continuous
            };
            panel.Controls.Add(transmissionProgress);
            
            return panel;
        }
        
        private Panel CreateStep6Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            Label title = new Label
            {
                Text = "Step 6: Decrypt & View Results",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(1120, 35)
            };
            panel.Controls.Add(title);
            
            decryptButton = new Button
            {
                Text = "Decrypt Results",
                Location = new Point(50, 70),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            decryptButton.FlatAppearance.BorderSize = 0;
            decryptButton.Click += DecryptButton_Click;
            panel.Controls.Add(decryptButton);
            
            saveResultsButton = new Button
            {
                Text = "Save Results to CSV",
                Location = new Point(270, 70),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            saveResultsButton.FlatAppearance.BorderSize = 0;
            saveResultsButton.Click += SaveResultsButton_Click;
            panel.Controls.Add(saveResultsButton);
            
            resultsSummary = new Label
            {
                Location = new Point(50, 130),
                Size = new Size(1070, 60),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            panel.Controls.Add(resultsSummary);
            
            resultsDisplay = new RichTextBox
            {
                Location = new Point(50, 200),
                Size = new Size(520, 350),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(resultsDisplay);
            
            resultsGrid = new DataGridView
            {
                Location = new Point(590, 200),
                Size = new Size(530, 350),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(resultsGrid);
            
            return panel;
        }
        
        private void ShowStep(int step)
        {
            currentStep = step;
            
            // Hide all panels
            step1Panel.Visible = false;
            step2Panel.Visible = false;
            step3Panel.Visible = false;
            step4Panel.Visible = false;
            step5Panel.Visible = false;
            step6Panel.Visible = false;
            
            // Show current panel
            switch (step)
            {
                case 1:
                    step1Panel.Visible = true;
                    stepIndicator.Text = "Step 1 of 6: Select Mode";
                    previousButton.Enabled = false;
                    nextButton.Enabled = true;
                    nextButton.Text = "Next";
                    break;
                case 2:
                    step2Panel.Visible = true;
                    stepIndicator.Text = "Step 2 of 6: Select File";
                    previousButton.Enabled = true;
                    nextButton.Enabled = !string.IsNullOrEmpty(selectedFilePath);
                    nextButton.Text = "Next";
                    break;
                case 3:
                    step3Panel.Visible = true;
                    stepIndicator.Text = "Step 3 of 6: Encrypt Data";
                    previousButton.Enabled = true;
                    nextButton.Enabled = encryptedData != null;
                    nextButton.Text = "Next";
                    break;
                case 4:
                    step4Panel.Visible = true;
                    stepIndicator.Text = "Step 4 of 6: Server Processing";
                    previousButton.Enabled = true;
                    nextButton.Enabled = encryptedPredictions != null;
                    nextButton.Text = "Next";
                    break;
                case 5:
                    step5Panel.Visible = true;
                    stepIndicator.Text = "Step 5 of 6: Results Transmission";
                    previousButton.Enabled = true;
                    nextButton.Enabled = true;
                    nextButton.Text = "Next";
                    // Auto-populate transmission info if predictions exist
                    if (encryptedPredictions != null)
                    {
                        PopulateTransmissionInfo();
                    }
                    break;
                case 6:
                    step6Panel.Visible = true;
                    stepIndicator.Text = "Step 6 of 6: View Results";
                    previousButton.Enabled = true;
                    nextButton.Enabled = false;
                    nextButton.Text = "Complete";
                    startOverButton.Visible = true;
                    // Enable decrypt button if predictions exist
                    decryptButton.Enabled = (encryptedPredictions != null && currentSecretKey != null);
                    // If already decrypted, show results
                    if (decryptedResults != null)
                    {
                        DisplayResults();
                        saveResultsButton.Enabled = true;
                    }
                    break;
            }
        }
        
        private void NextButton_Click(object sender, EventArgs e)
        {
            if (currentStep < 6)
            {
                ShowStep(currentStep + 1);
            }
        }
        
        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (currentStep > 1)
            {
                ShowStep(currentStep - 1);
            }
        }
        
        private void StartOverButton_Click(object sender, EventArgs e)
        {
            // Reset everything
            selectedFilePath = "";
            selectedMode = "";
            loadedData = null;
            encryptedData = null;
            encryptedPredictions = null;
            decryptedResults = null;
            currentKeyPair = null;
            currentPublicKey = null;
            currentSecretKey = null;
            
            filePathTextBox.Text = "";
            filePreviewGrid.DataSource = null;
            fileInfoLabel.Text = "";
            fileFormatInfo.Text = "";
            encryptionInfo.Text = "";
            serverInfo.Text = "";
            transmissionInfo.Text = "";
            resultsDisplay.Text = "";
            resultsGrid.DataSource = null;
            resultsSummary.Text = "";
            
            encryptButton.Enabled = true;
            processButton.Enabled = false;
            decryptButton.Enabled = false;
            saveResultsButton.Enabled = false;
            
            startOverButton.Visible = false;
            ShowStep(1);
        }
        
        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedMode = modeComboBox.SelectedItem?.ToString() ?? "";
            UpdateModeDescription();
        }
        
        private void UpdateModeDescription()
        {
            string description = "";
            string modelName = "";
            
            switch (selectedMode)
            {
                case "Healthcare":
                    description = "Healthcare Mode: Predicts cancer types from genomic data.\n" +
                                 "• Features: 1835-5600 genomic expression values\n" +
                                 "• Model: LogisticRegression\n" +
                                 "• Use Case: Medical diagnosis with privacy-preserving ML";
                    modelName = "LogisticRegression";
                    break;
                case "Business (Financial)":
                    description = "Financial Mode: Detects fraudulent transactions.\n" +
                                 "• Features: 10 transaction characteristics\n" +
                                 "• Model: FinancialFraud\n" +
                                 "• Use Case: Fraud detection while protecting customer data";
                    modelName = "FinancialFraud";
                    break;
                case "Academic":
                    description = "Academic Mode: Predicts student success/at-risk status.\n" +
                                 "• Features: 10 academic indicators (study hours, attendance, etc.)\n" +
                                 "• Model: AcademicGrade\n" +
                                 "• Use Case: Early intervention for at-risk students";
                    modelName = "AcademicGrade";
                    break;
                default:
                    description = "Please select a mode to continue.";
                    break;
            }
            
            modeDescriptionLabel.Text = description;
        }
        
        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = dialog.FileName;
                    filePathTextBox.Text = selectedFilePath;
                    LoadFilePreview();
                    nextButton.Enabled = true;
                }
            }
        }
        
        private void LoadFilePreview()
        {
            try
            {
                loadedData = EncryptedMLHelper.ReadValuesToList(selectedFilePath);
                
                if (loadedData == null || loadedData.Count == 0)
                {
                    fileInfoLabel.Text = "Error: File is empty or could not be read.";
                    fileInfoLabel.ForeColor = Color.FromArgb(220, 53, 69);
                    return;
                }
                
                int featureCount = loadedData[0].Count;
                int sampleCount = loadedData.Count;
                
                fileInfoLabel.Text = $"File loaded: {sampleCount} samples, {featureCount} features per sample";
                fileInfoLabel.ForeColor = Color.FromArgb(40, 167, 69);
                
                // Show preview
                var previewData = loadedData.Take(10).Select((row, idx) => new
                {
                    Row = idx + 1,
                    Features = string.Join(", ", row.Take(5).Select(f => f.ToString("F3"))) + (row.Count > 5 ? "..." : "")
                }).ToList();
                
                filePreviewGrid.DataSource = previewData;
                
                // Format information
                StringBuilder formatInfo = new StringBuilder();
                formatInfo.AppendLine("📋 File Format Information:");
                formatInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                formatInfo.AppendLine($"• Total Samples: {sampleCount}");
                formatInfo.AppendLine($"• Features per Sample: {featureCount}");
                formatInfo.AppendLine($"• Format: No header row, comma-separated values");
                formatInfo.AppendLine($"• Data Type: Numeric (floats)");
                formatInfo.AppendLine();
                formatInfo.AppendLine("Expected Format:");
                formatInfo.AppendLine("  0.123,0.456,0.789,0.234,0.567,...");
                formatInfo.AppendLine("  0.234,0.567,0.890,0.345,0.678,...");
                formatInfo.AppendLine("  ...");
                
                fileFormatInfo.Text = formatInfo.ToString();
            }
            catch (Exception ex)
            {
                fileInfoLabel.Text = $"Error loading file: {ex.Message}";
                fileInfoLabel.ForeColor = Color.FromArgb(220, 53, 69);
            }
        }
        
        private async void EncryptButton_Click(object sender, EventArgs e)
        {
            if (loadedData == null || loadedData.Count == 0)
            {
                MessageBox.Show("Please select and load a file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            encryptButton.Enabled = false;
            encryptionProgress.Style = ProgressBarStyle.Marquee;
            encryptionStatus.Text = "Generating encryption keys...";
            
            try
            {
                // Generate keys
                currentKeyPair = keyManager.CreateKeys();
                currentPublicKey = currentKeyPair.publicKey;
                currentSecretKey = currentKeyPair.secretKey;
                
                encryptionInfo.Clear();
                encryptionInfo.AppendText("Encryption Key Generation:\n");
                encryptionInfo.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
                encryptionInfo.AppendText("✓ Public Key Generated (can be shared with server)\n");
                encryptionInfo.AppendText("✓ Secret Key Generated (MUST stay on client - never shared!)\n");
                encryptionInfo.AppendText("✓ Encryption Scheme: BFV (Brakerski-Fan-Vercauteren)\n");
                encryptionInfo.AppendText("✓ Security Level: High (2048-bit polynomial modulus)\n\n");
                
                publicKeyLabel.Text = "Public Key: Generated \n(Shared with server for encryption)";
                secretKeyLabel.Text = "Secret Key: Generated \n(Stays on client - NEVER shared!)";
                
                encryptionStatus.Text = "Encrypting data...";
                
                // Encrypt data
                await Task.Run(() =>
                {
                    encryptedData = EncryptedMLHelper.encryptValues(loadedData, currentPublicKey, contextManager.Context);
                });
                
                encryptionInfo.AppendText("\nData Encryption Process:\n");
                encryptionInfo.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
                encryptionInfo.AppendText($"Encrypted {encryptedData.Count} samples\n");
                encryptionInfo.AppendText($"Each feature value is now an encrypted Ciphertext object\n");
                encryptionInfo.AppendText($"Server cannot see original values - only encrypted data\n");
                encryptionInfo.AppendText($"Homomorphic properties allow computation on encrypted data\n\n");
                encryptionInfo.AppendText("IMPORTANT: The server simulation will NEVER see:\n");
                encryptionInfo.AppendText("   • The encryption process\n");
                encryptionInfo.AppendText("   • The original data values\n");
                encryptionInfo.AppendText("   • The secret key\n");
                encryptionInfo.AppendText("   • The decryption process\n");
                
                encryptionStatus.Text = $"Encryption complete! {encryptedData.Count} samples encrypted.";
                encryptionStatus.ForeColor = Color.FromArgb(40, 167, 69);
                encryptionProgress.Style = ProgressBarStyle.Continuous;
                encryptionProgress.Value = 100;
                
                nextButton.Enabled = true;
                processButton.Enabled = true;
            }
            catch (Exception ex)
            {
                encryptionStatus.Text = $"Error: {ex.Message}";
                encryptionStatus.ForeColor = Color.FromArgb(220, 53, 69);
                encryptionProgress.Style = ProgressBarStyle.Continuous;
                encryptionProgress.Value = 0;
                encryptButton.Enabled = true;
            }
        }
        
        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            if (encryptedData == null)
            {
                MessageBox.Show("Please encrypt the data first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            processButton.Enabled = false;
            serverProgress.Style = ProgressBarStyle.Marquee;
            serverStatus.Text = "Processing encrypted data...";
            
            try
            {
                string modelName = GetModelNameForMode(selectedMode);
                
                serverInfo.Clear();
                serverInfo.AppendText("Simulated Server Processing:\n");
                serverInfo.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
                serverInfo.AppendText("NOTE: This is running LOCALLY for demo purposes.\n");
                serverInfo.AppendText("   In production, this would run on a separate server.\n\n");
                serverInfo.AppendText($"Model Loaded: {modelName}\n");
                serverInfo.AppendText($"Received {encryptedData.Count} encrypted samples\n");
                serverInfo.AppendText($"Performing homomorphic operations on encrypted data...\n\n");
                
                // Draw data flow
                DrawDataFlow();
                
                // Process
                encryptedPredictions = await localMLService.GetPredictionsAsync(encryptedData, modelName);
                
                serverInfo.AppendText("\nComputations Complete:\n");
                serverInfo.AppendText($"   • Performed weighted sum calculations on encrypted data\n");
                serverInfo.AppendText($"   • Multiplied encrypted features by model weights\n");
                serverInfo.AppendText($"   • Added weighted features together (all on encrypted data!)\n");
                serverInfo.AppendText($"   • Generated {encryptedPredictions.Count} encrypted predictions\n\n");
                serverInfo.AppendText("Security Note:\n");
                serverInfo.AppendText("   The server NEVER saw:\n");
                serverInfo.AppendText("   • Original data values\n");
                serverInfo.AppendText("   • Decrypted predictions\n");
                serverInfo.AppendText("   • Any unencrypted information\n");
                
                serverStatus.Text = $"Processing complete! {encryptedPredictions.Count} encrypted predictions generated.";
                serverStatus.ForeColor = Color.FromArgb(40, 167, 69);
                serverProgress.Style = ProgressBarStyle.Continuous;
                serverProgress.Value = 100;
                
                nextButton.Enabled = true;
            }
            catch (Exception ex)
            {
                serverStatus.Text = $"Error: {ex.Message}";
                serverStatus.ForeColor = Color.FromArgb(220, 53, 69);
                serverProgress.Style = ProgressBarStyle.Continuous;
                serverProgress.Value = 0;
                processButton.Enabled = true;
            }
        }
        
        private void DrawDataFlow()
        {
            dataFlowPanel.Controls.Clear();
            
            Label clientLabel = new Label
            {
                Text = "Client\n(Encrypted Data)",
                Location = new Point(20, 20),
                Size = new Size(150, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            dataFlowPanel.Controls.Add(clientLabel);
            
            Label arrow1 = new Label
            {
                Text = "→",
                Location = new Point(190, 30),
                Size = new Size(50, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            dataFlowPanel.Controls.Add(arrow1);
            
            Label serverLabel = new Label
            {
                Text = "Server\n(ML Processing)",
                Location = new Point(260, 20),
                Size = new Size(150, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            dataFlowPanel.Controls.Add(serverLabel);
            
            Label arrow2 = new Label
            {
                Text = "→",
                Location = new Point(430, 30),
                Size = new Size(50, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            dataFlowPanel.Controls.Add(arrow2);
            
            Label encryptedLabel = new Label
            {
                Text = "Encrypted\nPredictions",
                Location = new Point(500, 20),
                Size = new Size(150, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            dataFlowPanel.Controls.Add(encryptedLabel);
        }
        
        private void PopulateTransmissionInfo()
        {
            if (encryptedPredictions == null)
                return;
            
            transmissionInfo.Clear();
            transmissionInfo.AppendText("Encrypted Results Transmission:\n");
            transmissionInfo.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
            transmissionInfo.AppendText("The server has completed processing and is sending encrypted results back to the client.\n\n");
            transmissionInfo.AppendText($"Status: {encryptedPredictions.Count} encrypted predictions ready for transmission\n\n");
            transmissionInfo.AppendText("Transmission Details:\n");
            transmissionInfo.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            transmissionInfo.AppendText("• All data remains encrypted during transmission\n");
            transmissionInfo.AppendText("• Server cannot decrypt the results (only client has secret key)\n");
            transmissionInfo.AppendText("• Results are in encrypted Ciphertext format\n");
            transmissionInfo.AppendText("• Client will decrypt using secret key in next step\n\n");
            transmissionInfo.AppendText("NOTE: In this demo, transmission is simulated locally.\n");
            transmissionInfo.AppendText("In production, this would occur over a secure network connection.\n");
            
            transmissionProgress.Value = 100;
        }
        
        private string GetModelNameForMode(string mode)
        {
            if (mode.Contains("Financial") || mode.Contains("Business"))
                return "FinancialFraud";
            if (mode.Contains("Academic"))
                return "AcademicGrade";
            return "LogisticRegression";
        }
        
        private void DecryptButton_Click(object sender, EventArgs e)
        {
            if (encryptedPredictions == null || currentSecretKey == null)
            {
                MessageBox.Show("Please complete previous steps first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            decryptButton.Enabled = false;
            
            try
            {
                decryptedResults = EncryptedMLHelper.decryptValues(encryptedPredictions, currentSecretKey, contextManager.Context);
                
                DisplayResults();
                saveResultsButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error decrypting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                decryptButton.Enabled = true;
            }
        }
        
        private void DisplayResults()
        {
            if (decryptedResults == null || decryptedResults.Count == 0)
                return;
            
            // Calculate statistics
            var scores = decryptedResults.Select(r => r[0]).ToList();
            int positiveCount = scores.Count(s => s > 0);
            int negativeCount = scores.Count(s => s <= 0);
            int highConfidence = scores.Count(s => Math.Abs(s) > 5);
            
            // Summary
            string modeText = selectedMode.Contains("Financial") ? "Fraudulent" : 
                            selectedMode.Contains("Academic") ? "At Risk" : "Positive";
            string modeText2 = selectedMode.Contains("Financial") ? "Legitimate" : 
                             selectedMode.Contains("Academic") ? "Will Pass" : "Negative";
            
            resultsSummary.Text = $"Results Summary:\n" +
                                $"   Total Predictions: {decryptedResults.Count}\n" +
                                $"   Predicted {modeText}: {positiveCount} ({100.0 * positiveCount / decryptedResults.Count:F1}%)\n" +
                                $"   Predicted {modeText2}: {negativeCount} ({100.0 * negativeCount / decryptedResults.Count:F1}%)\n" +
                                $"   High Confidence Predictions: {highConfidence}";
            
            // Detailed results
            resultsDisplay.Clear();
            resultsDisplay.AppendText("Decrypted Results:\n");
            resultsDisplay.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
            resultsDisplay.AppendText("Interpretation:\n");
            resultsDisplay.AppendText("• Positive values = " + (selectedMode.Contains("Financial") ? "Likely FRAUDULENT" : 
                                    selectedMode.Contains("Academic") ? "Likely AT RISK" : "Positive prediction") + "\n");
            resultsDisplay.AppendText("• Negative values = " + (selectedMode.Contains("Financial") ? "Likely LEGITIMATE" : 
                                    selectedMode.Contains("Academic") ? "Likely WILL PASS" : "Negative prediction") + "\n");
            resultsDisplay.AppendText("• Higher absolute values = Higher confidence\n\n");
            resultsDisplay.AppendText("Sample Results (first 20):\n");
            resultsDisplay.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            
            for (int i = 0; i < Math.Min(20, decryptedResults.Count); i++)
            {
                long score = decryptedResults[i][0];
                string prediction = score > 0 ? modeText : modeText2;
                string confidence = Math.Abs(score) > 5 ? "High" : Math.Abs(score) > 2 ? "Medium" : "Low";
                resultsDisplay.AppendText($"Sample {i + 1:D4}: Score = {score,6} → {prediction} ({confidence} confidence)\n");
            }
            
            if (decryptedResults.Count > 20)
            {
                resultsDisplay.AppendText($"\n... and {decryptedResults.Count - 20} more results\n");
            }
            
            // Grid view
            var gridData = decryptedResults.Take(100).Select((r, idx) => new
            {
                Sample = idx + 1,
                Score = r[0],
                Prediction = r[0] > 0 ? modeText : modeText2,
                Confidence = Math.Abs(r[0]) > 5 ? "High" : Math.Abs(r[0]) > 2 ? "Medium" : "Low"
            }).ToList();
            
            resultsGrid.DataSource = gridData;
        }
        
        private void SaveResultsButton_Click(object sender, EventArgs e)
        {
            if (decryptedResults == null)
                return;
            
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.FileName = "Result.csv";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(dialog.FileName))
                    {
                        foreach (var sample in decryptedResults)
                        {
                            for (int i = 0; i < sample.Count - 1; i++)
                            {
                                writer.Write(sample[i].ToString() + ",");
                            }
                            writer.Write(sample[sample.Count - 1].ToString() + "\n");
                        }
                    }
                    
                    MessageBox.Show($"Results saved to:\n{dialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

