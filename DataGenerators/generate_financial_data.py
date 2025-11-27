"""
Financial Fraud Detection Data Generator

WHAT THIS SCRIPT DOES:
Generates realistic synthetic financial transaction data for fraud detection. Shoutout Ali for the idea
The data includes features like transaction amount, time, location, merchant type, etc.

USE CASE:
This demonstrates how the encrypted ML system could be used by banks or financial
institutions to detect fraudulent transactions without exposing customer transaction data.

FEATURES GENERATED:
1. Transaction amount (normalized)
2. Time of day (hour of transaction)
3. Day of week
4. Merchant category code
5. Transaction frequency (transactions in last 24 hours)
6. Average transaction amount (last 30 days)
7. Distance from home location
8. Device type (mobile, desktop, ATM)
9. Transaction velocity (amount / time since last transaction)
10. Account age (days since account creation)

LABEL:
0 = Legitimate transaction
1 = Fraudulent transaction
"""

import numpy as np
import pandas as pd
import random

def generate_financial_data(n_samples=1000, fraud_ratio=0.1, random_seed=42):
    """
    Generate synthetic financial transaction data.
    
    PARAMETERS:
    - n_samples: Number of transactions to generate
    - fraud_ratio: Proportion of fraudulent transactions (default 10%)
    - random_seed: Random seed for reproducibility
    
    RETURNS:
    - X: Feature matrix (n_samples x 10 features)
    - y: Labels (0 = legitimate, 1 = fraudulent)
    """
    np.random.seed(random_seed)
    random.seed(random_seed)
    
    n_fraud = int(n_samples * fraud_ratio)
    n_legitimate = n_samples - n_fraud
    
    features = []
    labels = []
    
    # Generate legitimate transactions
    for i in range(n_legitimate):
        # Feature 1: Transaction amount (normalized, typically $10-$5000)
        amount = np.random.lognormal(mean=4.5, sigma=1.0)  # Log-normal distribution
        amount = min(amount, 10000)  # Cap at $10,000
        amount_norm = amount / 10000.0  # Normalize to 0-1

        # Feature 2: Time of day (0-23 hours, normalized)
        hours = [6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]
        hour_probs = np.array(
            [0.02,0.03,0.05,0.08,0.10,0.10,0.10,0.10,0.10,0.08,0.05,0.03,0.02,0.02,0.02],
            dtype=float
        )
        hour_probs = hour_probs / hour_probs.sum()  # force exact sum = 1.0
        hour = np.random.choice(hours, p=hour_probs)
        hour_norm = hour / 23.0
        
        # Feature 3: Day of week (0=Monday, 6=Sunday, normalized)
        day = np.random.choice([0,1,2,3,4,5,6], p=[0.15,0.15,0.15,0.15,0.15,0.15,0.10])
        day_norm = day / 6.0
        
        # Feature 4: Merchant category (normalized code 0-1)
        merchant_cat = np.random.choice([0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9], 
                                       p=[0.15, 0.15, 0.10, 0.15, 0.10, 0.10, 0.10, 0.10, 0.05])
        
        # Feature 5: Transaction frequency (transactions in last 24h, normalized)
        freq = np.random.poisson(lam=2)  # Average 2 transactions per day
        freq_norm = min(freq / 10.0, 1.0)
        
        # Feature 6: Average transaction amount (last 30 days, normalized)
        avg_amount = np.random.lognormal(mean=4.3, sigma=0.8)
        avg_amount_norm = min(avg_amount / 10000.0, 1.0)
        
        # Feature 7: Distance from home (normalized, typically close to home)
        distance = np.random.exponential(scale=5.0)  # Most transactions near home
        distance_norm = min(distance / 100.0, 1.0)
        
        # Feature 8: Device type (0=mobile, 0.5=desktop, 1.0=ATM)
        device = np.random.choice([0.0, 0.5, 1.0], p=[0.6, 0.3, 0.1])
        
        # Feature 9: Transaction velocity (amount / time since last, normalized)
        time_since_last = np.random.exponential(scale=12.0)  # Hours
        velocity = amount / max(time_since_last, 0.1)
        velocity_norm = min(velocity / 1000.0, 1.0)
        
        # Feature 10: Account age (days, normalized)
        account_age = np.random.uniform(30, 3650)  # 1 month to 10 years
        account_age_norm = account_age / 3650.0
        
        features.append([amount_norm, hour_norm, day_norm, merchant_cat, 
                        freq_norm, avg_amount_norm, distance_norm, device, 
                        velocity_norm, account_age_norm])
        labels.append(0)
    
    # Generate fraudulent transactions (different patterns)
    for i in range(n_fraud):
        # Fraud patterns: larger amounts, unusual times, high velocity
        
        # Feature 1: Larger transaction amounts (fraudsters go big)
        amount = np.random.lognormal(mean=5.5, sigma=1.2)
        amount = min(amount, 10000)
        amount_norm = amount / 10000.0
        
        # Feature 2: Unusual times (late night/early morning)
        hour = np.random.choice([0,1,2,3,4,5,22,23], p=[0.1,0.1,0.15,0.15,0.1,0.1,0.15,0.15])
        hour_norm = hour / 23.0
        
        # Feature 3: Day of week (more on weekends)
        day = np.random.choice([0,1,2,3,4,5,6], p=[0.10,0.10,0.10,0.10,0.10,0.25,0.25])
        day_norm = day / 6.0
        
        # Feature 4: Unusual merchant categories
        merchant_cat = np.random.choice([0.1, 0.9], p=[0.3, 0.7])  # Prefer unusual categories
        
        # Feature 5: High transaction frequency (testing stolen card quickly)
        freq = np.random.poisson(lam=8)
        freq_norm = min(freq / 10.0, 1.0)
        
        # Feature 6: Different average amount pattern
        avg_amount = np.random.lognormal(mean=3.5, sigma=1.5)
        avg_amount_norm = min(avg_amount / 10000.0, 1.0)
        
        # Feature 7: Far from home location
        distance = np.random.exponential(scale=50.0)  # Much farther
        distance_norm = min(distance / 100.0, 1.0)
        
        # Feature 8: More likely mobile (easier to use stolen card)
        device = np.random.choice([0.0, 0.5, 1.0], p=[0.7, 0.2, 0.1])
        
        # Feature 9: Very high velocity (rapid transactions)
        time_since_last = np.random.exponential(scale=0.5)  # Very short time
        velocity = amount / max(time_since_last, 0.1)
        velocity_norm = min(velocity / 1000.0, 1.0)
        
        # Feature 10: Newer accounts (fraudsters create new accounts)
        account_age = np.random.uniform(1, 180)  # Less than 6 months
        account_age_norm = account_age / 3650.0
        
        features.append([amount_norm, hour_norm, day_norm, merchant_cat, 
                        freq_norm, avg_amount_norm, distance_norm, device, 
                        velocity_norm, account_age_norm])
        labels.append(1)
    
    # Convert to numpy arrays
    X = np.array(features)
    y = np.array(labels)
    
    # Shuffle the data
    indices = np.random.permutation(len(X))
    X = X[indices]
    y = y[indices]
    
    return X, y

if __name__ == "__main__":
    # Generate training data
    print("Generating financial fraud detection data...")
    X_train, y_train = generate_financial_data(n_samples=5000, fraud_ratio=0.1)
    
    # Save to CSV files (no headers, as expected by the system)
    pd.DataFrame(X_train).to_csv("financial_features.csv", header=False, index=False)
    pd.DataFrame(y_train).to_csv("financial_labels.csv", header=False, index=False)
    
    print(f"Generated {len(X_train)} samples with {X_train.shape[1]} features")
    print(f"Fraudulent transactions: {np.sum(y_train)} ({100*np.sum(y_train)/len(y_train):.1f}%)")
    print("Saved to financial_features.csv and financial_labels.csv")

