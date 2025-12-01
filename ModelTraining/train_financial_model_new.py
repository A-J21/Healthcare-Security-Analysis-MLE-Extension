"""
Financial Fraud Detection Model Training Script - New Dataset

Trains a logistic regression model using Financial_Data.csv.
The dataset contains 10 features + is_fraud label.

FEATURES (10):
1. amount - Transaction amount
2. transaction_hour - Hour of day (0-23)
3. merchant_category - Merchant category code
4. geo_distance_km - Geographic distance
5. device_trust_score - Device trust score (0-1)
6. num_prev_transactions - Number of previous transactions
7. num_chargebacks - Number of chargebacks
8. card_age_months - Card age in months
9. is_international - International flag (0/1)
10. is_night - Night transaction flag (0/1)

LABEL:
- is_fraud - Fraud indicator (0/1) - LAST COLUMN
"""

import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix
from sklearn.preprocessing import MinMaxScaler
import os

def train_financial_model_from_csv():
    """
    Train a logistic regression model using Financial_Data.csv.
    
    RETURNS:
    - model: Trained logistic regression model
    - X_test: Test features (normalized)
    - y_test: Test labels
    - scaler: The scaler used for normalization
    """
    print("=" * 60)
    print("Financial Fraud Detection Model Training (New Dataset)")
    print("=" * 60)
    
    # Step 1: Load the data
    print("\n1. Loading training data...")
    
    data_path = "../Data/Financial_Data.csv"
    if not os.path.exists(data_path):
        data_path = "Financial_Data.csv"
        if not os.path.exists(data_path):
            raise FileNotFoundError(f"Could not find Financial_Data.csv")
    
    # Read CSV - explicitly use first row as header
    df = pd.read_csv(data_path, header=0)
    print(f"   Loaded {len(df)} transactions")
    print(f"   Columns: {list(df.columns)}")
    
    # The file should have: 10 feature columns + 1 label column (is_fraud)
    # If is_fraud is not in columns, assume last column is the label
    if 'is_fraud' in df.columns:
        feature_columns = [col for col in df.columns if col != 'is_fraud']
        X = df[feature_columns].values
        y = df['is_fraud'].values.astype(int)
        print(f"   Found 'is_fraud' column - using as label")
    else:
        # Assume last column is the label
        feature_columns = df.columns[:-1].tolist()
        X = df.iloc[:, :-1].values
        y = df.iloc[:, -1].values.astype(int)
        print(f"   'is_fraud' not found - using last column as label")
        print(f"   Label column: {df.columns[-1]}")
    
    print(f"   Features ({len(feature_columns)}): {feature_columns}")
    print(f"   Feature matrix shape: {X.shape}")
    print(f"   Label distribution: {np.bincount(y)}")
    print(f"   Fraudulent transactions: {np.sum(y)} ({100*np.sum(y)/len(y):.1f}%)")
    print(f"   Legitimate transactions: {len(y)-np.sum(y)} ({100*(len(y)-np.sum(y))/len(y):.1f}%)")
    
    # Step 2: Normalize features to 0-1 range
    # CRITICAL: Encrypted ML requires all features in 0-1 range
    print("\n2. Normalizing features to 0-1 range...")
    print("   (Required for encrypted ML compatibility)")
    
    scaler = MinMaxScaler()
    X_normalized = scaler.fit_transform(X)
    
    print(f"   Original feature ranges:")
    for i, col in enumerate(feature_columns):
        min_val = X[:, i].min()
        max_val = X[:, i].max()
        print(f"     {col:25s}: [{min_val:8.2f}, {max_val:8.2f}]")
    
    print(f"\n   Normalized feature ranges (all should be 0-1):")
    for i, col in enumerate(feature_columns):
        min_val = X_normalized[:, i].min()
        max_val = X_normalized[:, i].max()
        status = "OK" if 0 <= min_val and max_val <= 1 else "ERROR"
        print(f"     {col:25s}: [{min_val:.3f}, {max_val:.3f}] {status}")
    
    # Step 3: Split into training and test sets
    print("\n3. Splitting data into training and test sets...")
    X_train, X_test, y_train, y_test = train_test_split(
        X_normalized, y, test_size=0.2, random_state=42, stratify=y
    )
    print(f"   Training samples: {len(X_train)}")
    print(f"   Test samples: {len(X_test)}")
    
    # Step 4: Train the model
    print("\n4. Training Logistic Regression model...")
    model = LogisticRegression(
        penalty='l2',
        C=1.0,
        solver='lbfgs',
        max_iter=1000,
        random_state=42
    )
    
    model.fit(X_train, y_train)
    print("   Training complete!")
    
    # Step 5: Evaluate the model
    print("\n5. Evaluating model performance...")
    
    # Training set predictions
    y_train_pred = model.predict(X_train)
    train_accuracy = accuracy_score(y_train, y_train_pred)
    train_precision = precision_score(y_train, y_train_pred, zero_division=0)
    train_recall = recall_score(y_train, y_train_pred, zero_division=0)
    train_f1 = f1_score(y_train, y_train_pred, zero_division=0)
    
    print(f"\n   Training Set Performance:")
    print(f"   - Accuracy:  {train_accuracy:.4f}")
    print(f"   - Precision: {train_precision:.4f}")
    print(f"   - Recall:    {train_recall:.4f}")
    print(f"   - F1 Score:  {train_f1:.4f}")
    
    # Test set predictions
    y_test_pred = model.predict(X_test)
    test_accuracy = accuracy_score(y_test, y_test_pred)
    test_precision = precision_score(y_test, y_test_pred, zero_division=0)
    test_recall = recall_score(y_test, y_test_pred, zero_division=0)
    test_f1 = f1_score(y_test, y_test_pred, zero_division=0)
    
    print(f"\n   Test Set Performance:")
    print(f"   - Accuracy:  {test_accuracy:.4f}")
    print(f"   - Precision: {test_precision:.4f}")
    print(f"   - Recall:    {test_recall:.4f}")
    print(f"   - F1 Score:  {test_f1:.4f}")
    
    # Confusion matrix
    cm = confusion_matrix(y_test, y_test_pred)
    print(f"\n   Confusion Matrix (Test Set):")
    print(f"   True Negatives (Legitimate):  {cm[0,0]}")
    print(f"   False Positives:              {cm[0,1]}")
    print(f"   False Negatives:              {cm[1,0]}")
    print(f"   True Positives (Fraud):       {cm[1,1]}")
    
    # Cross-validation
    print("\n6. Performing 5-fold cross-validation...")
    cv_scores = cross_val_score(model, X_train, y_train, cv=5, scoring='accuracy')
    print(f"   CV Accuracy: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")
    
    # Step 6: Save model coefficients
    print("\n7. Saving model coefficients...")
    
    # Get coefficients (weights) for each class
    coefs = model.coef_  # Shape: (n_classes, n_features)
    
    # Save to CSV (same format as other models)
    pd.DataFrame(coefs).to_csv("coefs.csv", header=False, index=False)
    print(f"   Saved coefficients to coefs.csv")
    print(f"   Coefficient shape: {coefs.shape} (classes x features)")
    
    # Step 7: Save normalized features for GUI use (without labels, no header)
    print("\n8. Saving normalized features for GUI use...")
    normalized_features_df = pd.DataFrame(X_normalized)
    output_path = "../Data/Financial_Data_Normalized.csv"
    # Save without header - EncryptedMLHelper expects no header row
    normalized_features_df.to_csv(output_path, index=False, header=False)
    print(f"   Saved normalized features to {output_path}")
    print(f"   (Use this file in the GUI - already normalized to 0-1 range, no header)")
    print(f"   Shape: {normalized_features_df.shape} (samples x features)")
    
    # Also save original data without header for reference
    original_no_header = df.iloc[:, :-1]  # All features, no label
    original_no_header.to_csv("../Data/Financial_Data_NoHeader.csv", index=False, header=False)
    print(f"   Also saved original features (no header) to Data/Financial_Data_NoHeader.csv")
    
    print("\n" + "=" * 60)
    print("Training complete! Model ready for use.")
    print("=" * 60)
    print("\nNext steps:")
    print("1. Copy coefs.csv to SystemArchitecture/configDB/FinancialFraud/coefs.csv")
    print("2. Use Data/Financial_Data_Normalized.csv in the GUI")
    print("3. The model expects 10 features in 0-1 range")
    
    return model, X_test, y_test, scaler

if __name__ == "__main__":
    model, X_test, y_test, scaler = train_financial_model_from_csv()
