"""
Run inference using the training routine's model and save predictions.

This script imports `train_financial_model.train_financial_model` which returns
the trained scikit-learn model and a test set. It uses the model to predict
probabilities and class labels for the test set and writes `test_predictions.csv`.
"""
import os
import pandas as pd
from train_financial_model import train_financial_model


def main():
    print("Running training and inference to produce test predictions...")
    model, X_test, y_test = train_financial_model()

    # Predict probabilities and labels
    probs = model.predict_proba(X_test)[:, 1]
    labels = model.predict(X_test)

    # Save to CSV
    out_df = pd.DataFrame({
        'probability_fraud': probs,
        'predicted_label': labels,
        'true_label': y_test
    })
    out_path = os.path.join(os.path.dirname(__file__), 'test_predictions.csv')
    out_df.to_csv(out_path, index=False)
    print(f"Saved test predictions to {out_path}")


if __name__ == '__main__':
    main()
