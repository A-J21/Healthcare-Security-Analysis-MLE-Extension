# Review: Repurposing Genomic MLE Project to Financial Fraud Detection

Summary
- **Repurposing status:** Successful for the Python-side workflow (data generation + model training). The project contains explicit financial data generator and training scripts that run end-to-end and produce model coefficients compatible with the system's MongoDB loader.
- **What I ran:** `DataGenerators/generate_financial_data.py` and `ModelTraining/train_financial_model.py` using the workspace virtualenv Python.

What I checked
- `README.md` and `SETUP_AND_RUN_GUIDE.md` — they include a Financial Fraud Detection path (10 features), instructions to generate data, train, and load into MongoDB.
- `DataGenerators/generate_financial_data.py` — generates 10 normalized features and `financial_features.csv` / `financial_labels.csv` (works and matches the described 10-feature schema).
- `ModelTraining/train_financial_model.py` — trains a logistic regression using scikit-learn and writes `coefs.csv` (1 x 10 coefficients) for loading into MongoDB.
- `SystemArchitecture/configDB/loadModelFromCsv.py` — reads `coefs.csv` and inserts a model document into MongoDB; it expects `coefs.csv` and uses a `modelName` variable which should be changed manually to e.g. `"FinancialFraud"` before loading.

Commands I executed (powershell, run from your workstation)
- Configure Python environment: (already configured by the workspace tools)
  - Python used: `C:/Users/Caden Dengel/Desktop/school/cs4371/.venv/Scripts/python.exe`
- Install packages used by scripts (done through the environment tool): `pandas`, `numpy`, `scikit-learn`
- Generate data:
  - `& "C:/Users/Caden Dengel/Desktop/school/cs4371/.venv/Scripts/python.exe" "DataGenerators/generate_financial_data.py"`
  - Output files: `DataGenerators/financial_features.csv` (5000 rows x 10), `DataGenerators/financial_labels.csv`
- Train model:
  - From `ModelTraining` directory: `& "C:/Users/Caden Dengel/Desktop/school/cs4371/.venv/Scripts/python.exe" train_financial_model.py`
  - Output file: `ModelTraining/coefs.csv` (1 row, 10 coefficients)

Observed outputs (summary)
- `financial_features.csv`: generated 5000 samples, 10 features. Example rows present.
- `train_financial_model.py` output (summary printed):
  - Training samples: 4000, Test samples: 1000
  - Test set performance: Accuracy ~0.995, Precision ~0.99, Recall ~0.96, F1 ~0.975
  - `coefs.csv` saved; shape `(1,10)` (one row of weights matching 10 features)

What works
- Data generation and training pipeline for the financial use-case works end-to-end in this environment.
- The produced `coefs.csv` is formatted for the existing `loadModelFromCsv.py` loader (weights as CSV, no headers).

What I did not / could not fully test
- I did not load the model into MongoDB because MongoDB is not running in this environment (and the tool-runner didn't start it). `loadModelFromCsv.py` will insert a document into MongoDB running at `localhost:27017` — this requires a running MongoDB instance.
- I did not build or run the .NET server nor the Windows client. Those steps require .NET SDK and (for the client) a Windows runtime environment; they are documented and should work when the environment is set up as described in `README.md` and `SETUP_AND_RUN_GUIDE.md`.

Recommended next steps to fully verify the system
1. Start MongoDB locally on the target machine (Ubuntu or Windows with MongoDB) and run `SystemArchitecture/configDB/loadModelFromCsv.py` after copying `ModelTraining/coefs.csv` into that folder and changing `modelName` to `"FinancialFraud"`.
   - Example (on the server):
     - `cp ../../ModelTraining/coefs.csv .`
     - Edit `loadModelFromCsv.py`: set `modelName = "FinancialFraud"`
     - `python3 loadModelFromCsv.py`
2. Build and publish the `.NET` server as in `README.md`/`SETUP_AND_RUN_GUIDE.md`, deploy, and verify the server returns the expected model metadata from MongoDB.
3. Build and run the Windows client (or `Client` console) and perform an encrypted inference by sending a small `test_financial.csv` (5 rows) to the server.

Minor issues / suggestions
- `loadModelFromCsv.py` sets `modelName` but inserts no field containing that name in the document (the inserted dict lacks a `Name` or `ModelName` key). Consider adding a field such as `"Name": modelName` so the server and client can query models by name.
- The loader uses `Precision=1000` hard-coded. Confirm the client encryption precision matches this value; otherwise predictions will be incorrect.
- `SETUP_AND_RUN_GUIDE.md` and `README.md` both reference a `docs/FINANCIAL_MODEL.md` that is missing — consider removing the reference or providing that doc.

Conclusion
- The core repurposing (data generator + training) has been implemented cleanly and is functional: the system can produce a 10-feature financial fraud detection model and prepare the coefficients for the encrypted inference service.
- Full end-to-end verification (loading into MongoDB, running the .NET server, and performing encrypted predictions from the client) requires starting MongoDB and the .NET runtime and was not performed here.

If you want, I can:
- Attempt to run `loadModelFromCsv.py` against a local MongoDB if you want me to install and start MongoDB in this environment.
- Try to build the .NET server (requires `dotnet` SDK installed in the environment) and run a small integration test.
- Add the recommended `Name` field to the MongoDB document and optionally add a small script to verify the server returns the loaded model.

Report generated by: GitHub Copilot (assistant)
Date: 2025-11-26
