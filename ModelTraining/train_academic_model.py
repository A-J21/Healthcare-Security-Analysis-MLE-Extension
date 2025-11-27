"""
Academic Grade Prediction Model Training Script

WHAT THIS SCRIPT DOES:
Trains a logistic regression model for academic grade prediction using the
encrypted ML framework. The trained model can then be loaded into MongoDB
for use by the encrypted ML server.

MODEL TYPE:
Logistic Regression (binary classification: will pass vs at risk)

FEATURES:
10 features (study hours, attendance, scores, etc.)

OUTPUT:
- coefs.csv: Model coefficients (weights) to be loaded into MongoDB
- Model performance metrics
"""

import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix
import sys
import os

# Add parent directory to path to import data generator
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from DataGenerators.generate_academic_data import generate_academic_data

def train_academic_model():
    """
    Train a logistic regression model for academic grade prediction.
    
    RETURNS:
    - model: Trained logistic regression model
    - X_test: Test features
    - y_test: Test labels
    """
    print("=" * 60)
    print("Academic Grade Prediction Model Training")
    print("=" * 60)
    
    # Step 1: Generate or load training data
    print("\n1. Loading training data...")
    
    # Check if data files exist, otherwise generate them
    if os.path.exists("academic_features.csv") and os.path.exists("academic_labels.csv"):
        print("   Found existing data files, loading...")
        X = pd.read_csv("academic_features.csv", header=None).to_numpy()
        y = pd.read_csv("academic_labels.csv", header=None).to_numpy().reshape((-1,))
    else:
        print("   Data files not found, generating new data...")
        X, y = generate_academic_data(n_samples=3000, at_risk_ratio=0.2)
        pd.DataFrame(X).to_csv("academic_features.csv", header=False, index=False)
        pd.DataFrame(y).to_csv("academic_labels.csv", header=False, index=False)
    
    print(f"   Loaded {len(X)} samples with {X.shape[1]} features")
    print(f"   At-risk students: {np.sum(y)} ({100*np.sum(y)/len(y):.1f}%)")
    
    # Step 2: Split into training and test sets
    print("\n2. Splitting data into training and test sets...")
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    print(f"   Training samples: {len(X_train)}")
    print(f"   Test samples: {len(X_test)}")
    
    # Step 3: Train the model
    print("\n3. Training Logistic Regression model...")
    # Use similar parameters to the healthcare model
    model = LogisticRegression(
        penalty='l2',
        C=1.0,
        solver='lbfgs',
        max_iter=1000,
        random_state=42
    )
    
    model.fit(X_train, y_train)
    print("   Training complete!")
    
    # Step 4: Evaluate the model
    print("\n4. Evaluating model performance...")
    
    # Training set predictions
    y_train_pred = model.predict(X_train)
    train_accuracy = accuracy_score(y_train, y_train_pred)
    train_precision = precision_score(y_train, y_train_pred)
    train_recall = recall_score(y_train, y_train_pred)
    train_f1 = f1_score(y_train, y_train_pred)
    
    print(f"\n   Training Set Performance:")
    print(f"   - Accuracy:  {train_accuracy:.4f}")
    print(f"   - Precision: {train_precision:.4f}")
    print(f"   - Recall:    {train_recall:.4f}")
    print(f"   - F1 Score:  {train_f1:.4f}")
    
    # Test set predictions
    y_test_pred = model.predict(X_test)
    test_accuracy = accuracy_score(y_test, y_test_pred)
    test_precision = precision_score(y_test, y_test_pred)
    test_recall = recall_score(y_test, y_test_pred)
    test_f1 = f1_score(y_test, y_test_pred)
    
    print(f"\n   Test Set Performance:")
    print(f"   - Accuracy:  {test_accuracy:.4f}")
    print(f"   - Precision: {test_precision:.4f}")
    print(f"   - Recall:    {test_recall:.4f}")
    print(f"   - F1 Score:  {test_f1:.4f}")
    
    # Confusion matrix
    cm = confusion_matrix(y_test, y_test_pred)
    print(f"\n   Confusion Matrix (Test Set):")
    print(f"   True Negatives (Will Pass):  {cm[0,0]}")
    print(f"   False Positives:              {cm[0,1]}")
    print(f"   False Negatives:              {cm[1,0]}")
    print(f"   True Positives (At Risk):    {cm[1,1]}")
    
    # Cross-validation
    print("\n5. Performing 5-fold cross-validation...")
    cv_scores = cross_val_score(model, X_train, y_train, cv=5, scoring='accuracy')
    print(f"   CV Accuracy: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")
    
    # Step 5: Save model coefficients
    print("\n6. Saving model coefficients...")
    
    # Get coefficients (weights) for each class
    # Logistic regression has one set of weights per class
    coefs = model.coef_  # Shape: (n_classes, n_features)
    
    # Save to CSV (same format as healthcare model)
    pd.DataFrame(coefs).to_csv("coefs.csv", header=False, index=False)
    print(f"   Saved coefficients to coefs.csv")
    print(f"   Coefficient shape: {coefs.shape} (classes x features)")
    
    print("\n" + "=" * 60)
    print("Training complete! Model ready for MongoDB.")
    print("=" * 60)
    print("\nNext steps:")
    print("1. Copy coefs.csv to SystemArchitecture/configDB/")
    print("2. Update loadModelFromCsv.py to use 'AcademicGrade' as model name")
    print("3. Run loadModelFromCsv.py to load the model into MongoDB")
    
    return model, X_test, y_test

if __name__ == "__main__":
    model, X_test, y_test = train_academic_model()

