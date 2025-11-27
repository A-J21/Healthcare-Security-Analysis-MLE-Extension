# Machine Learning with Encryption - Extension Guide

## Overview

This guide explains the extensions made to the original Machine Learning with Encryption (MLE) framework to support multiple use cases beyond healthcare. The system now supports:

1. **Healthcare** (Original): Cancer genomics prediction
2. **Business/Financial**: Fraud detection in financial transactions
3. **Academic**: Student grade prediction

## What's New

### 1. Data Generators
New scripts to generate realistic synthetic data for testing:
- `DataGenerators/generate_financial_data.py`: Financial transaction data
- `DataGenerators/generate_academic_data.py`: Student academic data

### 2. Model Training Scripts
New training scripts for each use case:
- `ModelTraining/train_financial_model.py`: Train fraud detection model
- `ModelTraining/train_academic_model.py`: Train grade prediction model

### 3. GUI Application
A Windows Forms GUI application (`SystemArchitecture/ClientGUI/`) that provides:
- Mode selection (Healthcare, Business, Academic)
- Model selection
- File browser for CSV input
- Real-time status updates
- Results display

### 4. Updated Model Loading
Updated `loadModelFromCsv.py` to support multiple model types with clear instructions.

## Quick Start Guide

### Step 1: Generate Training Data

#### For Financial Fraud Detection:
```bash
cd DataGenerators
python generate_financial_data.py
```

This creates:
- `financial_features.csv`: 10 features per transaction
- `financial_labels.csv`: 0 = legitimate, 1 = fraudulent

#### For Academic Grade Prediction:
```bash
cd DataGenerators
python generate_academic_data.py
```

This creates:
- `academic_features.csv`: 10 features per student
- `academic_labels.csv`: 0 = will pass, 1 = at risk

### Step 2: Train Models

#### Train Financial Model:
```bash
cd ModelTraining
python train_financial_model.py
```

This will:
- Train a logistic regression model
- Evaluate performance
- Save `coefs.csv` with model weights

#### Train Academic Model:
```bash
cd ModelTraining
python train_academic_model.py
```

### Step 3: Load Model into MongoDB

1. Copy `coefs.csv` to `SystemArchitecture/configDB/`
2. Edit `loadModelFromCsv.py` and set:
   ```python
   modelName = "FinancialFraud"  # or "AcademicGrade"
   ```
3. Run:
   ```bash
   cd SystemArchitecture/configDB
   python3 loadModelFromCsv.py
   ```

### Step 4: Use the GUI Application

1. Build the GUI:
   ```bash
   cd SystemArchitecture/ClientGUI
   dotnet build
   dotnet run
   ```

2. In the GUI:
   - Select mode (Healthcare, Business, or Academic)
   - Select model (LogisticRegression)
   - Browse and select your CSV file
   - Click "Process Encrypted ML"
   - Results will be saved to `Result.csv` in the same directory

### Step 5: Use the Console Client (Alternative)

The original console client still works:

```bash
cd SystemArchitecture/Client
dotnet run
# Enter CSV filename when prompted
```

## Data Format Requirements

### CSV File Format

All CSV files must follow this format:
- **No header row**
- Each row = one sample (transaction, student, patient, etc.)
- Each column = one feature
- Values must be numeric (floats)
- Comma-separated

Example:
```
0.123,0.456,0.789,0.234,0.567,0.890,0.345,0.678,0.901,0.234
0.234,0.567,0.890,0.345,0.678,0.901,0.456,0.789,0.123,0.345
```

### Feature Counts

- **Healthcare**: 1835 features (genomic data)
- **Financial**: 10 features (transaction data)
- **Academic**: 10 features (student data)

**Important**: The number of features in your CSV must match the model's expected feature count!

## Use Case Details

### Healthcare (Original)
- **Purpose**: Predict cancer type from genomic data
- **Features**: 1835 genomic features
- **Model**: LogisticRegression
- **Data**: Genomic expression values

### Business/Financial
- **Purpose**: Detect fraudulent transactions
- **Features**: 10 features
  - Transaction amount
  - Time of day
  - Day of week
  - Merchant category
  - Transaction frequency
  - Average transaction amount
  - Distance from home
  - Device type
  - Transaction velocity
  - Account age
- **Model**: LogisticRegression
- **Output**: 0 = Legitimate, 1 = Fraudulent

### Academic
- **Purpose**: Predict if student is at risk of failing
- **Features**: 10 features
  - Study hours per week
  - Class attendance rate
  - Assignment average
  - Midterm exam score
  - Participation score
  - Previous GPA
  - Number of courses
  - Homework hours
  - Library usage
  - Online resource usage
- **Model**: LogisticRegression
- **Output**: 0 = Will pass, 1 = At risk

## Architecture Overview

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────┐
│  Windows Client │────────▶│  Ubuntu Server   │────────▶│   MongoDB   │
│  (GUI or CLI)   │  HTTP   │  (.NET + nginx)  │         │   (Models)  │
└─────────────────┘         └──────────────────┘         └─────────────┘
     Encrypts Data           Processes Encrypted          Stores Model
                            Data (never sees raw)         Weights
```

### Data Flow

1. **Client Side**:
   - Reads CSV file
   - Encrypts data using homomorphic encryption
   - Sends encrypted data to server

2. **Server Side**:
   - Receives encrypted data
   - Performs ML inference on encrypted data
   - Returns encrypted predictions
   - **Never sees unencrypted data!**

3. **Client Side**:
   - Receives encrypted predictions
   - Decrypts using secret key
   - Saves results to CSV

## Security Features

- **Homomorphic Encryption**: Server performs calculations without decrypting
- **Client-Side Encryption**: Data encrypted before leaving client
- **Server Never Sees Raw Data**: Only encrypted Ciphertext objects
- **Secret Key Stays on Client**: Only client can decrypt results

## Troubleshooting

### "Model not found" Error
- Ensure model is loaded in MongoDB
- Check model name matches exactly (case-sensitive)
- Verify MongoDB is running: `sudo systemctl status mongod`

### "Feature count mismatch" Error
- Check your CSV has the correct number of features
- Healthcare: 1835 features
- Financial: 10 features
- Academic: 10 features

### "Connection failed" Error
- Verify server is running: `sudo systemctl status MLE.service`
- Check server URL in client code (default: `http://localhost:8080/`)
- Verify port forwarding if using VM

### GUI Won't Start
- Ensure .NET Core 3.1 SDK is installed
- Check all dependencies are installed
- Try building from command line: `dotnet build`

## File Structure

```
Healthcare-Security-Analysis-MLE-Extension/
├── DataGenerators/              # Data generation scripts
│   ├── generate_financial_data.py
│   └── generate_academic_data.py
├── ModelTraining/               # Model training scripts
│   ├── compareModels.py         # Original healthcare training
│   ├── train_financial_model.py # Financial model training
│   └── train_academic_model.py  # Academic model training
├── SystemArchitecture/
│   ├── Client/                  # Console client (original)
│   ├── ClientGUI/               # NEW: GUI client
│   │   ├── MainForm.cs
│   │   ├── Program.cs
│   │   └── MLE_GUI.csproj
│   ├── Server/                  # Server application
│   └── configDB/                # Model loading scripts
│       └── loadModelFromCsv.py  # Updated with comments
└── README.md                    # Original documentation
```

## Ideas For Class Presentation
Not really sure how you guys want to divide who's doing what, but leaving this as a kind of guideline for now

1. **Privacy-Preserving ML**: Server never sees raw data
2. **Multiple Use Cases**: Same framework works for healthcare, finance, and education
3. **Homomorphic Encryption**: Perform calculations on encrypted data
4. **Real-World Applications**: Fraud detection, grade prediction, medical diagnosis
5. **Code Understandability**: Extensive comments for educational purposes

### Demo Flow:
1. Show GUI application
2. Select different modes (Healthcare, Business, Academic)
3. Process sample data
4. Show that server logs don't contain raw data
5. Explain how homomorphic encryption enables this

## Additional Resources
- Original Research Paper: `MLPrecisionMed.pdf`
- Original README: `README.md`
- Code Comments: All code files have EXTENSIVE inline documentation :)))))))))))

