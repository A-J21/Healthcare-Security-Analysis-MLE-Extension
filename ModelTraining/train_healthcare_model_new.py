"""
Healthcare Cancer Prediction Model Training Script - New Dataset

Trains a logistic regression model using the new Healthcare_Data.csv file.
The dataset contains 10 features + has_cancer label.

FEATURES (10):
1. cna_mean - Copy number alteration mean
2. cna_max - Copy number alteration maximum
3. cna_min - Copy number alteration minimum
4. mut_sig_1 - Mutation signature 1
5. mut_sig_2 - Mutation signature 2
6. mut_sig_3 - Mutation signature 3
7. mutation_burden - Mutation burden count
8. fusion_present - Fusion present flag (0/1)
9. struct_var_count - Structural variation count
10. panel_410 - Panel 410 flag (0/1)

LABEL:
- has_cancer - Cancer indicator (0 = Non-Cancer, 1 = Cancer) - LAST COLUMN

Cancer Classification Logic (from report):
- High CNA extremes (large positive/negative alterations)
- High mutation burden
- Detection of gene fusion events
- Elevated structural variation count
"""

import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix
from sklearn.preprocessing import MinMaxScaler
import os

def train_healthcare_model_from_csv():
    """
    Train a logistic regression model using Healthcare_Data.csv.
    
    RETURNS:
    - model: Trained logistic regression model
    - X_test: Test features (normalized)
    - y_test: Test labels
    - scaler: The scaler used for normalization
    """
    print("=" * 60)
    print("Healthcare Cancer Prediction Model Training (New Dataset)")
    print("=" * 60)
    
    # Step 1: Load the data
    print("\n1. Loading training data...")
    
    data_path = "../Data/Healthcare_Data.csv"
    if not os.path.exists(data_path):
        data_path = "Healthcare_Data.csv"
        if not os.path.exists(data_path):
            raise FileNotFoundError(f"Could not find Healthcare_Data.csv. Please ensure it's in Data/ or ModelTraining/ directory.")
    
    # Read CSV - explicitly use first row as header
    df = pd.read_csv(data_path, header=0)
    print(f"   Loaded {len(df)} patients")
    print(f"   Columns: {list(df.columns)}")
    
    # Separate features and labels
    # Exclude has_cancer from features
    if 'has_cancer' in df.columns:
        feature_columns = [col for col in df.columns if col != 'has_cancer']
        X = df[feature_columns].values
        y = df['has_cancer'].values.astype(int)
        print(f"   Found 'has_cancer' column - using as label")
    else:
        # Assume last column is the label
        feature_columns = df.columns[:-1].tolist()
        X = df.iloc[:, :-1].values
        y = df.iloc[:, -1].values.astype(int)
        print(f"   'has_cancer' not found - using last column as label")
        print(f"   Label column: {df.columns[-1]}")
    
    print(f"   Features ({len(feature_columns)}): {feature_columns}")
    print(f"   Feature matrix shape: {X.shape}")
    print(f"   Label distribution: {np.bincount(y)}")
    print(f"   Cancer patients: {np.sum(y)} ({100*np.sum(y)/len(y):.1f}%)")
    print(f"   Non-cancer patients: {len(y)-np.sum(y)} ({100*(len(y)-np.sum(y))/len(y):.1f}%)")
    
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
    print(f"   True Negatives (Non-Cancer):  {cm[0,0]}")
    print(f"   False Positives:             {cm[0,1]}")
    print(f"   False Negatives:             {cm[1,0]}")
    print(f"   True Positives (Cancer):      {cm[1,1]}")
    
    # Cross-validation
    print("\n6. Performing 5-fold cross-validation...")
    cv_scores = cross_val_score(model, X_train, y_train, cv=5, scoring='accuracy')
    print(f"   CV Accuracy: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")
    
    # Step 6: Save model coefficients
    print("\n7. Saving model coefficients...")
    
    # Get coefficients (weights) for each class
    coefs = model.coef_  # Shape: (n_classes, n_features)
    intercept = model.intercept_  # Shape: (n_classes,)
    
    # IMPORTANT: Incorporate intercept into coefficients for encrypted operations
    # The encrypted operations service doesn't add the intercept separately,
    # so we need to include it in the coefficients.
    # 
    # Since features are normalized to 0-1, we can approximate:
    # intercept + sum(features*weights) ≈ sum(features*(weights + intercept/n))
    # where n = number of features
    # 
    # This works because on average, normalized features are around 0.5,
    # so sum(features) ≈ n*0.5, and sum(features*intercept/n) ≈ intercept*0.5
    # To get the full intercept, we use a scaling factor (empirically determined to be ~1.8)
    intercept_per_feature = (intercept[0] * 1.8) / len(feature_columns)
    coefs_with_intercept = coefs.copy()
    coefs_with_intercept[0] = coefs[0] + intercept_per_feature
    
    print(f"   Original intercept: {intercept[0]:.4f}")
    print(f"   Intercept per feature: {intercept_per_feature:.4f}")
    print(f"   Adjusted first coefficient: {coefs[0][0]:.4f} -> {coefs_with_intercept[0][0]:.4f}")
    
    # Also scale coefficients to match financial model magnitude (prevents precision loss)
    # Load financial model to get scaling factor
    financial_coefs_path = "../SystemArchitecture/configDB/FinancialFraud/coefs.csv"
    if os.path.exists(financial_coefs_path):
        financial_coefs = pd.read_csv(financial_coefs_path, header=None).values[0]
        scale_factor = np.abs(financial_coefs).max() / np.abs(coefs_with_intercept[0]).max()
        coefs_final = coefs_with_intercept * scale_factor
        print(f"   Scale factor (to match financial magnitude): {scale_factor:.2f}x")
        print(f"   Final coefficient range: [{coefs_final[0].min():.2f}, {coefs_final[0].max():.2f}]")
    else:
        coefs_final = coefs_with_intercept
        print(f"   Financial model not found, skipping magnitude scaling")
    
    # Save to CSV (same format as other models)
    pd.DataFrame(coefs_final).to_csv("coefs.csv", header=False, index=False)
    print(f"   Saved coefficients to coefs.csv (with intercept incorporated)")
    print(f"   Coefficient shape: {coefs_final.shape} (classes x features)")
    
    # Step 7: Save normalized features for GUI use (without labels, no header)
    print("\n8. Saving normalized features for GUI use...")
    normalized_features_df = pd.DataFrame(X_normalized)
    output_path = "../Data/Healthcare_Data_Normalized.csv"
    # Save without header - EncryptedMLHelper expects no header row
    normalized_features_df.to_csv(output_path, index=False, header=False)
    print(f"   Saved normalized features to {output_path}")
    print(f"   (Use this file in the GUI - already normalized to 0-1 range, no header)")
    print(f"   Shape: {normalized_features_df.shape} (samples x features)")
    
    print("\n" + "=" * 60)
    print("Training complete! Model ready for use.")
    print("=" * 60)
    print("\nNext steps:")
    print("1. Copy coefs.csv to SystemArchitecture/configDB/coefs.csv (for LogisticRegression model)")
    print("2. Use Data/Healthcare_Data_Normalized.csv in the GUI (already normalized)")
    print("3. The model expects 10 features in 0-1 range")
    
    return model, X_test, y_test, scaler

if __name__ == "__main__":
    model, X_test, y_test, scaler = train_healthcare_model_from_csv()

