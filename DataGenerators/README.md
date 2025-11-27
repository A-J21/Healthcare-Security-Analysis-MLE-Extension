# Data Generators

This directory contains scripts to generate synthetic but realistic data for testing the encrypted ML system in different use cases.

## Files

### `generate_financial_data.py`
Generates financial transaction data for fraud detection.

**Features Generated:**
1. Transaction amount (normalized)
2. Time of day (hour)
3. Day of week
4. Merchant category code
5. Transaction frequency (last 24h)
6. Average transaction amount (last 30 days)
7. Distance from home location
8. Device type (mobile/desktop/ATM)
9. Transaction velocity
10. Account age

**Usage:**
```bash
python generate_financial_data.py
```

**Output:**
- `financial_features.csv`: Feature matrix (no headers)
- `financial_labels.csv`: Labels (0 = legitimate, 1 = fraudulent)

### `generate_academic_data.py`
Generates student academic data for grade prediction.

**Features Generated:**
1. Study hours per week
2. Class attendance rate
3. Assignment average score
4. Midterm exam score
5. Participation score
6. Previous GPA
7. Number of courses
8. Homework hours per week
9. Library usage (visits per week)
10. Online resource usage (hours per week)

**Usage:**
```bash
python generate_academic_data.py
```

**Output:**
- `academic_features.csv`: Feature matrix (no headers)
- `academic_labels.csv`: Labels (0 = will pass, 1 = at risk)

## Requirements

- Python 3.7+
- numpy
- pandas

Install dependencies:
```bash
pip install numpy pandas
```

## Notes

- All features are normalized to 0-1 range (except where noted)
- Data is shuffled before saving
- Generated data is synthetic but follows realistic patterns
- For production use, replace with real data

