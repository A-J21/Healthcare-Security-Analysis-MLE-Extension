"""
Model Training and Comparison Script

WHAT THIS SCRIPT DOES:
This script trains and compares different machine learning models to find the best one
for the healthcare/genomics prediction task. It tests:
- Different model types (Logistic Regression, Random Forest, SVM)
- Different feature transformations (None, MinMax, Standardize, log, log+1)
- Different feature selection methods (Chi-squared, Mutual Information)
- Different numbers of features

The best model is then saved and can be loaded into MongoDB for use by the encrypted ML system.

HOW IT WORKS:
1. Loads training data (features and labels)
2. Tests many combinations of models, transformations, and feature selections
3. Uses cross-validation to evaluate each combination
4. Saves the best model configuration
"""

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.svm import LinearSVC, SVC
from sklearn.model_selection import GridSearchCV, cross_val_score, cross_validate, StratifiedKFold
from sklearn.feature_selection import RFE
from sklearn.metrics import cohen_kappa_score, make_scorer, accuracy_score
from joblib import dump, load
from sklearn.preprocessing import MinMaxScaler, StandardScaler
from warnings import simplefilter
from sklearn.exceptions import ConvergenceWarning
from sklearn.feature_selection import chi2, mutual_info_classif
from copy import deepcopy
import matplotlib.pyplot as plt

"""
This script uses the average accuracy across 5-fold cross validation to compare the preformance of different models, with different parameter settings, feature subsets, and feature selction techniques.
"""

def notrans(X):
    """
    Identity transformation function (no transformation).
    
    Used as one of the transformation options - sometimes no transformation is best.
    """
    return X

# Ignore warnings to keep output clean
# ConvergenceWarning: Some models might not fully converge, but that's okay for comparison
# FutureWarning: Warnings about future API changes
simplefilter(action='ignore', category=ConvergenceWarning)
simplefilter(action='ignore', category=FutureWarning)

# Load training data
# X_all: Feature matrix (each row is a sample, each column is a feature)
# Y: Labels (what we're trying to predict - e.g., cancer type)
X_all =  pd.read_csv("feature_table.csv", header=None).to_numpy() #load unencrypted feature table
Y = pd.read_csv("Y.csv", header=None).to_numpy().reshape((-1,)) # Reshape to 1D array

# Feature Selection: Rank features by how useful they are for prediction
# We'll use this to remove less important features later

# Method 1: Mutual Information - measures how much information a feature provides about the label
# disMask tells the function which features are discrete (categorical) vs continuous
# First 2 features are continuous, rest are discrete
disMask = [False, False] + ([True] * (X_all.shape[1] - 2 ) )  #tells mi_score which features are discrete
mi_scores = mutual_info_classif(X_all, Y, discrete_features=disMask )
# Sort features by importance (most important first)
mi_inds = np.argsort(mi_scores)

# Method 2: Chi-squared test - measures independence between feature and label
# Higher score = feature is more related to the label
chi_scores = chi2(X_all,Y)[0]
# Sort features by chi-squared score (highest score first)
chi_inds = np.argsort(chi_scores)

#np.save("chi_inds.npy",chi_inds)
#np.save("mi_inds.npy",mi_inds)
#chi_inds = np.load("Data/chi_inds.npy")
#mi_inds = np.load("Data/mi_inds.npy")

# Define the machine learning models to test
# We'll try different types to see which works best for this data
classifiers = [LogisticRegression(), RandomForestClassifier(), LinearSVC(), SVC()]
strClf = ["Logistic Regression","Random Forest","Linear SVM","Non-Linear SVM"]

# Define feature transformations to test
# Different transformations can help models learn better patterns
scaler1 = MinMaxScaler()  # Scales features to 0-1 range
scaler2 = StandardScaler()  # Scales features to have mean=0, std=1
trans = [notrans, scaler1.fit_transform, scaler2.fit_transform, np.log, np.log1p]
strTrans = ["None","MinMax","Standardize","log()","log(x+1)"]
# Explanation:
# - None: No transformation (use raw data)
# - MinMax: Scale to 0-1 (good for features with different ranges)
# - Standardize: Normalize to mean=0, std=1 (good for algorithms sensitive to scale)
# - log(): Natural logarithm (helps with skewed data)
# - log(x+1): Log with offset (handles zeros better than log)

# Define hyperparameter search spaces for each model
# GridSearchCV will try all combinations to find the best settings
parameterSets = [
    # Logistic Regression: Try different penalties (L1/L2) and regularization strengths (C)
    {"penalty":['l1','l2'], 'C':[0.1,1, 10], 'solver':["liblinear","lbfgs"]},
    # Random Forest: Try different split criteria and tree counts
    {'criterion':['gini', 'entropy'], 'n_estimators':[100,200,300], 'bootstrap':[False, True]},
    # Linear SVM: Try different penalties and loss functions
    {'penalty':['l1', 'l2'], 'C':[0.1, 1, 10], 'max_iter':[10000], 'loss':['hinge', 'squared_hinge']},
    # Non-linear SVM: Try different kernels and regularization
    {'kernel':['poly', 'rbf'], 'C':[0.1, 1, 10]}
]

# Open output file to save comparison results
fp = open("ModelComparison.txt","w")

# Track the best model found so far
max_score = 0.0 #keep track of best score
winnerString = "" #record best params and feature transformation/selection

# Main comparison loop: Test every combination of model, transformation, and feature selection
for n_clf, clf in enumerate(classifiers): #for each classifier

    fp.write(""+str(strClf[n_clf])+":\n")

    for n_trans in range(0,5): #for each transformation

        fp.write("\t"+str(strTrans[n_trans])+":\n")

        # Copy features before applying transformation
        # We need a copy because transformations modify the data
        X = deepcopy(X_all)

        # Apply the transformation to the first 2 features (which are continuous)
        # np.finfo(float).eps adds a tiny value to avoid log(0) errors
        X[1:3] = trans[n_trans](X[1:3] + np.finfo(float).eps)

        # Test different numbers of features to remove
        # Removing less important features can improve performance and reduce overfitting
        for n_feat in [2500,3500,4500]: #for each number of least informative features to remove

            # Test with Chi-squared feature selection
            fp.write("\t\tChi_feat: "+str(n_feat)+"\n")

            # Remove the n_feat least important features (keep the rest)
            # chi_inds is sorted, so [n_feat:] gets the most important features
            X_selected = X[:,chi_inds[n_feat:]]

            # Get the parameter search space for this model type
            parameters = parameterSets[n_clf]

            # Perform grid search: Try all parameter combinations and find the best
            # n_jobs=-1 means use all CPU cores for faster processing
            GS = GridSearchCV(clf, parameters, n_jobs=-1)
            GS.fit(X_selected, Y)  # Train on the selected features

            # Write results: accuracy and best parameters found
            fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
            for (x, y) in GS.best_params_.items():
                fp.write(str(x)+":"+str(y)+" ")
            fp.write("\n")

            # Update best model if this one is better
            if GS.best_score_ > max_score:
                winnerString = strClf[n_clf]+" is the winner with "+str(n_feat)+" features removed using Chi_squared, "+strTrans[n_trans]+" feature transformation, and the following params:"
                winnerString += str(GS.best_params_)
                max_score = GS.best_score_

            #-------------repeat above for Mutual Information feature selection
            fp.write("\t\tMI_feat: "+str(n_feat)+"\n")

            # Remove the n_feat least important features using Mutual Information ranking
            X_selected = X[:,mi_inds[n_feat:]]

            # Get the parameter search space for this model type
            parameters = parameterSets[n_clf]

            # Perform grid search with MI-selected features
            GS = GridSearchCV(clf, parameters, n_jobs=-1)
            GS.fit(X_selected, Y)

            # Write results
            fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
            for (x, y) in GS.best_params_.items():
                fp.write(str(x)+":"+str(y)+" ")
            fp.write("\n")

            # Update best model if this one is better
            if GS.best_score_ > max_score:
                winnerString = strClf[n_clf]+" is the winner with "+str(n_feat)+" features removed using Mutual Importance, "+strTrans[n_trans]+" feature transformation, and the following params:"
                winnerString += str(GS.best_params_)
                max_score = GS.best_score_

    fp.write("\n\n")


# Also test using ALL features (no feature selection)
# Sometimes using all features works better than removing any
fp.write("All features\n")
for n_clf, clf in enumerate(classifiers):
    
    fp.write("\t"+str(strClf[n_clf])+":\n")

    for n_trans in range(0,5):

        fp.write("\t\t"+str(strTrans[n_trans])+":\n")
        
        # Copy features before applying transformation
        X = deepcopy(X_all)

        # Apply transformation
        X[1:3] = trans[n_trans](X[1:3] + np.finfo(float).eps)
        
        # Get parameter search space for this model
        parameters = parameterSets[n_clf]

        # Perform grid search using ALL features (no feature selection)
        GS = GridSearchCV(clf, parameters, n_jobs=-1)
        GS.fit(X, Y)

        # Write results
        fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
        for (x, y) in GS.best_params_.items():
            fp.write(str(x)+":"+str(y)+" ")
        fp.write("\n")

        # Update best model if this one is better
        if GS.best_score_ > max_score:
            winnerString = strClf[n_clf]+" is the winner using all features, "+strTrans[n_trans]+" feature transformation, and the following params:"
            winnerString += str(GS.best_params_)
            max_score = GS.best_score_

# Write summary: The best model found across all combinations
fp.write("\n\nBest Preforming Model:\n")
fp.write("Accuracy: "+str(max_score)+"\n")
fp.write(str(winnerString)+"\n")

fp.close()