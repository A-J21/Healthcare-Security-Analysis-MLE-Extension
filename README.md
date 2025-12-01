# Healthcare Security Analysis - Machine Learning with Encryption (MLE)

## Overview
This project implements a **local-only** Machine Learning with Encryption (MLE) framework for privacy-preserving predictions on sensitive data. Originally designed for cancer genomics prediction, this system has been extended to support multiple domains (Healthcare, Financial, Academic) and uses homomorphic encryption to perform machine learning inference without exposing raw data.

**Research Paper:** [Machine Learning in Precision Medicine to Preserve Privacy via Encryption](https://arxiv.org/abs/2102.03412)

## Key Features

- **All Local Processing** - No server or database required, everything runs on your machine
- **Interactive GUI** - Step-by-step visual interface for the encryption and ML process
- **Multiple Models** - Supports Healthcare, Financial Fraud Detection, and Academic Grade Prediction
- **Privacy-Preserving** - Uses homomorphic encryption so data is never decrypted during processing
- **Educational** - Visualizes the entire encrypted ML workflow

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows Application                       │
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │     GUI      │───▶│   Encryption │───▶│ Local ML     │  │
│  │  (Client)    │    │   (SEAL)     │    │ Service      │  │
│  └──────────────┘    └──────────────┘    └──────────────┘  │
│                                                              │
│  ┌──────────────┐                                         │
│  │  CSV Models  │  ← Model coefficients loaded from files  │
│  │  (configDB)  │                                         │
│  └──────────────┘                                         │
└─────────────────────────────────────────────────────────────┘

All processing happens locally - no network calls, no server, no database!
```

## Prerequisites

### Required Software

- **Windows 10/11**
- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Python 3.8+** (for model training only)
- **Visual Studio 2022** or **Visual Studio Code** (recommended, but not required if using command line)

### Optional (for Model Training)

- Python packages: `pandas`, `numpy`, `scikit-learn`
  ```bash
  pip install pandas numpy scikit-learn
  ```

## Installation & Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/A-J21/Healthcare-Security-Analysis-MLE-Extension.git
cd Healthcare-Security-Analysis-MLE-Extension
```

### Step 2: Install .NET 8.0 SDK

1. Download and install the .NET 8.0 SDK from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Verify installation:
   ```cmd
   dotnet --version
   ```
   Should display: `8.0.x` or higher

### Step 3: Restore Dependencies

```cmd
cd SystemArchitecture\ClientGUI
dotnet restore
```

This will automatically download the SEAL encryption library and other dependencies.

### Step 4: Build the Application

```cmd
dotnet build --configuration Release
```

Or build and run directly:

```cmd
dotnet run
```

### Step 5: Verify Models are Available

The application expects model coefficient files in:
- `SystemArchitecture/configDB/coefs.csv` (Healthcare model)
- `SystemArchitecture/configDB/FinancialFraud/coefs.csv` (Financial model)
- `SystemArchitecture/configDB/AcademicGrade/coefs.csv` (Academic model)

**Note:** Pre-trained models should already be included. If models are missing, see the [Model Training](#model-training) section.

## Usage Guide

### Running the Application

1. **Launch the GUI:**
   ```cmd
   cd SystemArchitecture\ClientGUI
   dotnet run
   ```

2. **Follow the Interactive Steps:**
   - **Step 1:** Select application mode (Healthcare, Financial, or Academic)
   - **Step 2:** Browse and select your CSV data file
   - **Step 3:** Generate encryption keys and encrypt your data
   - **Step 4:** Process encrypted data (simulated server processing - all local!)
   - **Step 5:** View encrypted results transmission info
   - **Step 6:** Decrypt and view results, save to CSV

### CSV Data Format

Your CSV files should have:
- **No header row**
- **Each row = one sample**
- **Each column = one feature**
- **Comma-separated numeric values (floats)**

Example format:
```
0.123,0.456,0.789,0.234,0.567
0.234,0.567,0.890,0.345,0.678
0.345,0.678,0.901,0.456,0.789
```

**Normalized Data:** For best results, use normalized data files:
- `Data/Healthcare_Data_Normalized.csv`
- `Data/Financial_Data_Normalized.csv`
- `Data/Academic_Data_Normalized.csv`

### Application Modes

#### Healthcare Mode
- **Purpose:** Cancer type prediction from genomic data
- **Features:** 10 genomic expression values
- **Model:** LogisticRegression
- **Use Case:** Medical diagnosis with privacy-preserving ML

#### Business (Financial) Mode
- **Purpose:** Fraudulent transaction detection
- **Features:** 10 transaction characteristics
  - Transaction amount, time, location, device trust, etc.
- **Model:** FinancialFraud(Modified LogisticRegression)
- **Use Case:** Fraud detection while protecting customer data

#### Academic Mode
- **Purpose:** Student at-risk prediction
- **Features:** 10 academic indicators
  - GPA, study hours, attendance, assignments, etc.
- **Model:** AcademicGrade(Modified LogisticRegression)
- **Use Case:** Early intervention for at-risk students

## Model Training

If you need to train or re-train models, use the training scripts in the `ModelTraining/` directory.

### Training a Healthcare Model

```bash
cd ModelTraining
python train_healthcare_model_new.py
```

This will:
1. Load `Data/Healthcare_Data.csv`
2. Train a logistic regression model
3. Save coefficients to `coefs.csv`
4. Copy to `SystemArchitecture/configDB/coefs.csv`

### Training a Financial Model

```bash
cd ModelTraining
python train_financial_model_new.py
```

Saves model to: `SystemArchitecture/configDB/FinancialFraud/coefs.csv`

See `ModelTraining/FINANCIAL_MODEL_TRAINING_GUIDE.md` for detailed information.

### Training an Academic Model

```bash
cd ModelTraining
python train_academic_model_new.py
```

Saves model to: `SystemArchitecture/configDB/AcademicGrade/coefs.csv`

**Note:** All models use Logistic Regression with normalized features (0-1 range).

## Project Structure

```
Healthcare-Security-Analysis-MLE-Extension/
├── SystemArchitecture/
│   ├── ClientGUI/              # Main GUI application (Windows Forms)
│   │   ├── MainFormModern.cs   # Main GUI window
│   │   ├── Services/           # Local ML services (replaces server)
│   │   └── Program.cs          # Entry point
│   ├── Client/                 # Shared encryption logic library
│   │   └── Logics/             # Encryption helpers, key management
│   └── configDB/               # Model coefficient files (CSV)
│       ├── coefs.csv           # Healthcare model
│       ├── FinancialFraud/     # Financial model
│       └── AcademicGrade/      # Academic model
├── ModelTraining/              # ML model training scripts
│   ├── train_healthcare_model_new.py
│   ├── train_financial_model_new.py
│   ├── train_academic_model_new.py
│   └── FINANCIAL_MODEL_TRAINING_GUIDE.md
├── Data/                       # Sample datasets
│   ├── Healthcare_Data.csv
│   ├── Financial_Data.csv
│   ├── Academic_Data.csv
│   └── *_Normalized.csv        # Pre-normalized files for prediction
└── README.md                   # This file
```

## How It Works

1. **Encryption:** Your data is encrypted using homomorphic encryption (BFV scheme) before any ML processing
2. **Local Processing:** The "server" operations (ML inference) run locally on your machine using the same encryption context
3. **Homomorphic Operations:** Mathematical operations (multiplication, addition) are performed directly on encrypted data
4. **Decryption:** Only you can decrypt the results using your secret key
5. **Privacy:** At no point is your data decrypted during the ML processing phase

**Key Security Feature:** Even though processing is simulated locally, the encryption ensures that if this were deployed as a real client-server system, the server would never see your unencrypted data.

## Troubleshooting

### Build Errors

**Error: "Unable to find .NET SDK"**
- Install .NET 8.0 SDK from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0)
- Restart your terminal/IDE after installation

**Error: "Package restore failed"**
- Check your internet connection
- Try: `dotnet nuget locals all --clear` then `dotnet restore`

### Runtime Errors

**Error: "Unable to load DLL 'sealc'"**
- The SEAL library should be included automatically
- Try rebuilding: `dotnet clean` then `dotnet build`

**Error: "Model 'X' not found"**
- Verify model CSV files exist in `SystemArchitecture/configDB/`
- Check file paths are correct (see [Model Training](#model-training))

**Error: "Feature count mismatch"**
- Ensure your CSV has the correct number of features:
  - Financial/Academic: 10 features per row
  - Healthcare: Check your specific model requirements
- Remove header rows if present
- Verify data is properly comma-separated

### Performance Issues

**Encryption/Decryption is slow:**
- Homomorphic encryption is computationally intensive
- Large datasets (>1000 samples) may take several minutes
- This is normal behavior for encrypted computation

**Application feels unresponsive:**
- Long-running operations are performed asynchronously
- Check the progress indicators in the GUI
- Consider using smaller datasets for testing

## Security Considerations

- Data is encrypted client-side using homomorphic encryption (BFV scheme)
- Processing never sees unencrypted data
- Secret keys never leave your machine
- Suitable for HIPAA, GDPR, PCI-DSS compliance scenarios
- **Note:** This is a demonstration system. For production use, consider additional security measures (HTTPS, secure key management, etc.)

## Citation
If you use this framework in your research, please cite:

```bibtex
@Article{Briguglio2021MachineLearningPrecisionMedicine-arxiv,
  author = {William Briguglio and Parisa Moghaddam and Waleed A. Yousef and Issa Traore and Mohammad Mamun},
  title = {Machine Learning in Precision Medicine to Preserve Privacy via Encryption},
  journal = {arXiv Preprint, arXiv:2102.03412},
  year = 2021,
  url = {https://github.com/isotlaboratory/Healthcare-Security-Analysis-MLE},
  primaryclass = {cs.LG}
}
```

## License

GPL-3.0 License - See LICENSE file for details

## Support

For issues or questions, please just don't have any, this project drained us emotionally, and we're likely to discontinue any interest in it after submission.

---

**Note:** This project has been updated from the original server-based architecture to run entirely locally. All server functionality has been replaced with local services while maintaining the same privacy-preserving encryption guarantees.
