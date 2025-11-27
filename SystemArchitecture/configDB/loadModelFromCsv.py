"""
Load Model from CSV into MongoDB

WHAT THIS SCRIPT DOES:
Loads a trained machine learning model (coefficients/weights) from a CSV file
into MongoDB so the encrypted ML server can use it.

USAGE:
1. Place coefs.csv in this directory (from ModelTraining/)
2. Update the modelName variable below
3. Run: python3 loadModelFromCsv.py

MODELS:
- LogisticRegression (Healthcare - original)
- FinancialFraud (Business use case)
- AcademicGrade (Academic use case)
"""

import pandas as pd
import numpy as np
from pymongo import MongoClient

# Connect to MongoDB (running on localhost)
mongo = MongoClient('localhost', 27017)

# Load coefficients from CSV file
# CSV format: Each row = one class, each column = one feature weight
coefs = pd.read_csv("coefs.csv", header=None).to_numpy()

# Extract model dimensions
M_classes = coefs.shape[0]  # Number of classes (output categories)
N_weights = coefs.shape[1]  # Number of features (input dimensions)

# Precision: Scaling factor used during encryption
# Must match the precision used in the client (1000 in our case)
Precision = 1000

# Model type
modelType = "LogisticRegression"

# MODEL NAME: Change this to match your use case
# Options:
#   - "LogisticRegression" (Healthcare - original)
#   - "FinancialFraud" (Business use case)
#   - "AcademicGrade" (Academic use case)
modelName = "LogisticRegression"  # CHANGE THIS!

# Create document to insert into MongoDB
aDict = {
    "MClasses": M_classes,      # Number of output classes
    "NWeights": N_weights,      # Number of input features
    "Type": modelType,           # Model type (e.g., "LogisticRegression")
    "Precision": Precision,      # Scaling precision
    "Weights": coefs.tolist()    # Model weights (coefficients) as nested list
}

# Insert into MongoDB
# Database: "models", Collection: "models"
mongo.models["models"].insert_one(aDict)

print(f"Model '{modelName}' loaded successfully!")
print(f"  - Classes: {M_classes}")
print(f"  - Features: {N_weights}")
print(f"  - Type: {modelType}")
print(f"  - Precision: {Precision}")
