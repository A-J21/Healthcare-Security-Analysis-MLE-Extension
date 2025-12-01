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

**Using Git (recommended):**

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

**Build and Run Directly (for testing):**

```cmd
cd SystemArchitecture\ClientGUI
dotnet run
```

**Note:** The first build may take a few minutes as it downloads dependencies. Subsequent builds are faster.

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

## Functionality Status

### Working Features

- **Complete GUI Workflow**: All 6 steps of the encrypted ML process work end-to-end
- **Healthcare Model**: Cancer prediction using genomic data (10 features)
- **Financial Model**: Fraud detection using transaction data (10 features)
- **Academic Model**: Student at-risk prediction using academic indicators (10 features)
- **Data Encryption**: Homomorphic encryption using BFV scheme (SEAL library)
- **Encrypted ML Inference**: Logistic regression inference on encrypted data
- **Data Decryption**: Decryption of prediction results using secret key
- **CSV Import/Export**: Load data from CSV files and save results to CSV
- **Model Training**: Python scripts for training new models on custom datasets
- **Local Processing**: All operations run locally without server or database

### Limitations / Known Issues
- **Single Model Type**: Currently supports Logistic Regression only. Other model types (Random Forest, SVM) would require additional homomorphic operation implementations.
- **Performance**: Homomorphic encryption is computationally intensive. Large datasets (>1000 samples) may take several minutes to process.
- **Error Handling**: Some edge cases may not have comprehensive error messages
- **Platform Support**: Currently Windows-only due to Windows Forms GUI. The encryption library (SEAL) supports cross-platform, but the GUI does not.

### Not Working / Out of Scope
- Network-based client-server deployment (intentionally removed for local-only operation)
- Database integration (MongoDB functionality removed)
- Real-time model updates without application restart
- Model performance visualization/analytics
- HTTPS/TLS network encryption (wasn't needed for local-only operation)

## Academic References

### Foundational Research (Prior Work)

This project builds upon foundational research in privacy preserving machine learning and homomorphic encryption.  
The foundational paper that established the feasibility of performing machine-learning inference directly on encrypted data is:

**Dowlin, M., Gilad-Bachrach, R., Laine, K., Lauter, K., Naehrig, M., & Wernsing, J. (2016). _CryptoNets: Applying Neural Networks to Encrypted Data with High Throughput and Accuracy_.**  
Microsoft Research.  
https://www.microsoft.com/en-us/research/publication/cryptonets-applying-neural-networks-to-encrypted-data-with-high-throughput-and-accuracy/

This work demonstrated that deterministic, privacy preserving inference could be achieved using homomorphic encryption.. Serving as the intellectual foundation for this encrypted MLE pipeline.

### Contemporary Work (Building Upon This Project)

Modern research continues to extend these ideas, combining encrypted computation, precision medicine, and clinical genomics.  
One contemporary example that builds upon the lineage of privacy-preserving medical ML and uses the same MSK-IMPACT dataset referenced in our project is:

**Huang, T., et al. (2023). _Pan-cancer prediction of molecular tumor features using machine learning on the MSK-IMPACT clinical sequencing cohort_. Nature Genetics.**  
https://pmc.ncbi.nlm.nih.gov/articles/PMC10508812/

This paper demonstrates how current machine-learning approaches leverage large-scale clinical genomics datasets such as MSK-IMPACT to perform tumor-feature prediction, representing the continuation of the research trajectory that our project contributes to.

### This Project's Contribution

**Research Paper:** [Machine Learning in Precision Medicine to Preserve Privacy via Encryption](https://arxiv.org/abs/2102.03412)

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
