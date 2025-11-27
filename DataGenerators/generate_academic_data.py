"""
Academic Grade Prediction Data Generator

WHAT THIS SCRIPT DOES:
Generates realistic synthetic student academic data for grade prediction. (Still not sure how this is going to work but working on it)
The data includes features like study hours, attendance, assignment scores, etc.

USE CASE:
This demonstrates how the encrypted ML system could be used by universities to
predict student performance without exposing individual student grades or personal data.

FEATURES GENERATED:
1. Study hours per week (normalized)
2. Class attendance rate
3. Assignment average score
4. Midterm exam score
5. Participation score (class participation)
6. Previous GPA (from previous semesters)
7. Number of courses taken this semester
8. Hours spent on homework per week
9. Library usage (visits per week)
10. Online resource usage (hours per week)

LABEL:
0 = Will pass (grade >= C)
1 = At risk (grade < C)
"""

import numpy as np
import pandas as pd
from sklearn.preprocessing import MinMaxScaler
import random

def generate_academic_data(n_samples=1000, at_risk_ratio=0.2, random_seed=42):
    """
    Generate synthetic academic student data.
    
    PARAMETERS:
    - n_samples: Number of students to generate
    - at_risk_ratio: Proportion of at-risk students (default 20%)
    - random_seed: Random seed for reproducibility
    
    RETURNS:
    - X: Feature matrix (n_samples x 10 features)
    - y: Labels (0 = will pass, 1 = at risk)
    """
    np.random.seed(random_seed)
    random.seed(random_seed)
    
    n_at_risk = int(n_samples * at_risk_ratio)
    n_passing = n_samples - n_at_risk
    
    features = []
    labels = []
    
    # Generate passing students
    for i in range(n_passing):
        # Feature 1: Study hours per week (normalized, typically 10-40 hours)
        study_hours = np.random.normal(loc=20, scale=5)
        study_hours = max(5, min(study_hours, 50))
        study_hours_norm = study_hours / 50.0
        
        # Feature 2: Class attendance rate (0-1)
        attendance = np.random.beta(a=8, b=2)  # Skewed toward high attendance
        attendance = max(0.5, min(attendance, 1.0))
        
        # Feature 3: Assignment average score (0-1)
        assignment_avg = np.random.beta(a=6, b=2)
        assignment_avg = max(0.6, min(assignment_avg, 1.0))
        
        # Feature 4: Midterm exam score (0-1)
        midterm = np.random.beta(a=5, b=2)
        midterm = max(0.6, min(midterm, 1.0))
        
        # Feature 5: Participation score (0-1)
        participation = np.random.beta(a=5, b=2)
        participation = max(0.5, min(participation, 1.0))
        
        # Feature 6: Previous GPA (normalized, typically 2.0-4.0)
        prev_gpa = np.random.normal(loc=3.2, scale=0.5)
        prev_gpa = max(2.0, min(prev_gpa, 4.0))
        prev_gpa_norm = (prev_gpa - 2.0) / 2.0  # Normalize 2.0-4.0 to 0-1
        
        # Feature 7: Number of courses (normalized, typically 3-6)
        num_courses = np.random.choice([3, 4, 5, 6], p=[0.1, 0.3, 0.4, 0.2])
        num_courses_norm = (num_courses - 3) / 3.0  # Normalize 3-6 to 0-1
        
        # Feature 8: Homework hours per week (normalized)
        homework_hours = np.random.normal(loc=15, scale=4)
        homework_hours = max(5, min(homework_hours, 30))
        homework_hours_norm = homework_hours / 30.0
        
        # Feature 9: Library usage (visits per week, normalized)
        library_visits = np.random.poisson(lam=3)
        library_visits_norm = min(library_visits / 10.0, 1.0)
        
        # Feature 10: Online resource usage (hours per week, normalized)
        online_hours = np.random.normal(loc=5, scale=2)
        online_hours = max(0, min(online_hours, 15))
        online_hours_norm = online_hours / 15.0
        
        features.append([study_hours_norm, attendance, assignment_avg, midterm, 
                        participation, prev_gpa_norm, num_courses_norm, 
                        homework_hours_norm, library_visits_norm, online_hours_norm])
        labels.append(0)
    
    # Generate at-risk students (different patterns)
    for i in range(n_at_risk):
        # At-risk patterns: low study hours, poor attendance, low scores
        
        # Feature 1: Low study hours
        study_hours = np.random.normal(loc=8, scale=3)
        study_hours = max(0, min(study_hours, 20))
        study_hours_norm = study_hours / 50.0
        
        # Feature 2: Low attendance
        attendance = np.random.beta(a=2, b=5)  # Skewed toward low attendance
        attendance = max(0.0, min(attendance, 0.7))
        
        # Feature 3: Low assignment scores
        assignment_avg = np.random.beta(a=2, b=4)
        assignment_avg = max(0.0, min(assignment_avg, 0.7))
        
        # Feature 4: Low midterm scores
        midterm = np.random.beta(a=2, b=4)
        midterm = max(0.0, min(midterm, 0.6))
        
        # Feature 5: Low participation
        participation = np.random.beta(a=2, b=5)
        participation = max(0.0, min(participation, 0.6))
        
        # Feature 6: Lower previous GPA
        prev_gpa = np.random.normal(loc=2.5, scale=0.4)
        prev_gpa = max(2.0, min(prev_gpa, 3.5))
        prev_gpa_norm = (prev_gpa - 2.0) / 2.0
        
        # Feature 7: Often taking too many courses
        num_courses = np.random.choice([4, 5, 6], p=[0.2, 0.4, 0.4])  # More likely overloaded
        num_courses_norm = (num_courses - 3) / 3.0
        
        # Feature 8: Low homework hours
        homework_hours = np.random.normal(loc=8, scale=3)
        homework_hours = max(0, min(homework_hours, 20))
        homework_hours_norm = homework_hours / 30.0
        
        # Feature 9: Low library usage
        library_visits = np.random.poisson(lam=0.5)
        library_visits_norm = min(library_visits / 10.0, 1.0)
        
        # Feature 10: Either very low or very high online usage (distraction or avoidance)
        online_hours = np.random.choice([
            np.random.normal(loc=1, scale=0.5),  # Very low
            np.random.normal(loc=12, scale=2)   # Very high (distraction)
        ], p=[0.6, 0.4])
        online_hours = max(0, min(online_hours, 15))
        online_hours_norm = online_hours / 15.0
        
        features.append([study_hours_norm, attendance, assignment_avg, midterm, 
                        participation, prev_gpa_norm, num_courses_norm, 
                        homework_hours_norm, library_visits_norm, online_hours_norm])
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
    print("Generating academic grade prediction data...")
    X_train, y_train = generate_academic_data(n_samples=3000, at_risk_ratio=0.2)
    
    # Save to CSV files (no headers, as expected by the system)
    pd.DataFrame(X_train).to_csv("academic_features.csv", header=False, index=False)
    pd.DataFrame(y_train).to_csv("academic_labels.csv", header=False, index=False)
    
    print(f"Generated {len(X_train)} samples with {X_train.shape[1]} features")
    print(f"At-risk students: {np.sum(y_train)} ({100*np.sum(y_train)/len(y_train):.1f}%)")
    print("Saved to academic_features.csv and academic_labels.csv")

