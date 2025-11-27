# Setup and Run Guide

This guide walks you through setting up and running the extended MLE system step-by-step.

## Overview

The system still has two parts:
1. **Ubuntu Server**: Runs the ML inference service (never sees unencrypted data)
2. **Windows Client**: Encrypts data, sends to server, decrypts results (GUI or can still use console)

## Part 1: Ubuntu Server Setup

### Step 1: Install Dependencies

On your Ubuntu VM, run:

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET Core 3.1 SDK
# Get the Microsoft repo for Ubuntu 22.04 (jammy), not 20.04
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package lists
sudo apt update

# Install current .NET SDK (8.0)
sudo apt install -y dotnet-sdk-8.0


# Install nginx
sudo apt install -y nginx

# Install MongoDB
wget -qO - https://www.mongodb.org/static/pgp/server-7.0.asc | sudo apt-key add -
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list
sudo apt update
sudo apt install -y mongodb-org


# Install Python dependencies
sudo apt install -y python3-pip
pip3 install pandas numpy pymongo scikit-learn
```

### Step 2: Start MongoDB

```bash
sudo systemctl start mongod
sudo systemctl enable mongod
sudo systemctl status mongod  # Should show "active (running)"
```

### Step 3: Copy Project Files to Ubuntu

Copy the entire project to your Ubuntu VM (or clone from your repo):

```bash
cd ~
# If using git:
git clone <YOUR_REPO_URL>
cd Healthcare-Security-Analysis-MLE-Extension

# Or copy files via SCP/Shared Folder
```

### Step 4: Train and Load Models

You need to load at least one model into MongoDB. Choose which use case you want to start with:

#### Option A: Healthcare (Original - 1835 features)
```bash
cd ~/Healthcare-Security-Analysis-MLE-Extension/SystemArchitecture/configDB
# Copy the original coefs.csv here (from ModelTraining/ if you have it)
# Edit loadModelFromCsv.py and set: modelName = "LogisticRegression"
python3 loadModelFromCsv.py
```

#### Option B: Financial Fraud Detection (10 features)
```bash
# First, generate and train the model
cd ~/Healthcare-Security-Analysis-MLE-Extension/DataGenerators
python3 generate_financial_data.py

cd ../ModelTraining
python3 train_financial_model.py

# Now load it into MongoDB
cd ../SystemArchitecture/configDB
cp ../../ModelTraining/coefs.csv .
# Edit loadModelFromCsv.py and set: modelName = "FinancialFraud"
python3 loadModelFromCsv.py
```

#### Option C: Academic Grade Prediction (10 features)
```bash
# First, generate and train the model
cd ~/Healthcare-Security-Analysis-MLE-Extension/DataGenerators
python3 generate_academic_data.py

cd ../ModelTraining
python3 train_academic_model.py

# Now load it into MongoDB
cd ../SystemArchitecture/configDB
cp ../../ModelTraining/coefs.csv .
# Edit loadModelFromCsv.py and set: modelName = "AcademicGrade"
python3 loadModelFromCsv.py
```

**You can load multiple models!** Just repeat the process with different model names.

### Step 5: Build and Deploy Server

```bash
cd ~/Healthcare-Security-Analysis-MLE-Extension/SystemArchitecture/Server

# Build the server
dotnet publish --configuration Release

# Deploy to web server directory
sudo mkdir -p /var/www/MLE/
sudo cp -r ./bin/Release/netcoreapp3.1/publish/* /var/www/MLE/
```

### Step 6: Create systemd Service

```bash
sudo nano /etc/systemd/system/MLE.service
```

Paste this configuration:

```ini
[Unit]
Description=MLE .NET Service
After=network.target

[Service]
WorkingDirectory=/var/www/MLE
ExecStart=/usr/bin/dotnet /var/www/MLE/CDTS_PROJECT.dll --urls http://0.0.0.0:5000
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Save and exit (Ctrl+X, Y, Enter).

### Step 7: Configure nginx

```bash
sudo nano /etc/nginx/nginx.conf
```

Add this line inside the `http` block:
```nginx
client_max_body_size 100M;
```

Then configure the site:
```bash
sudo nano /etc/nginx/sites-available/default
```

Replace contents with:
```nginx
server {
    listen 80;
    server_name _;
    
    client_max_body_size 100M;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Step 8: Start Services

```bash
# Test nginx configuration
sudo nginx -t

# Reload systemd and start services
sudo systemctl daemon-reload
sudo systemctl start MLE.service
sudo systemctl enable MLE.service
sudo systemctl restart nginx

# Check service status
sudo systemctl status MLE.service
sudo systemctl status nginx
```

Both should show "active (running)".

### Step 9: Get VM IP Address

```bash
ip addr show
```

Note the IP address (typically `10.0.2.15` for NAT or `192.168.x.x` for Bridged).

### Step 10: Configure Port Forwarding (If Using VirtualBox NAT)

**On your Windows host machine:**

1. Open VirtualBox Manager
2. With VM powered **OFF**, go to: Settings → Network → Adapter 1
3. Click "Advanced" → "Port Forwarding"
4. Add a new rule:
   - Name: `MLE`
   - Protocol: `TCP`
   - Host Port: `8080`
   - Guest Port: `80`
5. Start the VM

**OR use command line (Windows):**
```cmd
cd "C:\Program Files\Oracle\VirtualBox"
VBoxManage list vms
VBoxManage modifyvm "YourVMName" --natpf1 "MLE,tcp,,8080,,80"
```

---

## Part 2: Windows Client Setup

### Option A: GUI Application (Recommended)

#### Step 1: Build the GUI

On your Windows machine:

```cmd
cd "C:\Users\Chris\OneDrive\Desktop\Healthcare-Security-Analysis-MLE-Extension\SystemArchitecture\ClientGUI"
dotnet build
```

#### Step 2: Update Server URL (if needed)

If your server is not at `localhost:8080`, edit `MainForm.cs` and find:
```csharp
client.BaseAddress = new Uri("http://localhost:8080/");
```

Change to your server's address (e.g., `http://192.168.1.100:8080/` if using bridged networking).

#### Step 3: Run the GUI

```cmd
dotnet run
```

Or build a standalone executable:
```cmd
dotnet publish --configuration Release -r win-x64 --self-contained true
```

Then run the `.exe` from `bin\Release\netcoreapp3.1\win-x64\publish\`

#### Step 4: Use the GUI

1. Select mode: Healthcare, Business (Financial), or Academic
2. Select model: LogisticRegression
3. Click "Browse..." and select your CSV file
4. Click "Process Encrypted ML"
5. Results will be saved to `Result.csv` in the same directory as your input file

### Option B: Console Client (Original)

#### Step 1: Build the Console Client

```cmd
cd "C:\Users\Chris\OneDrive\Desktop\Healthcare-Security-Analysis-MLE-Extension\SystemArchitecture\Client"
dotnet build
```

#### Step 2: Update Server URL (if needed)

Edit `Program.cs` and find:
```csharp
client.BaseAddress = new Uri("http://localhost:8080/");
```

Change to your server's address if needed.

#### Step 3: Run the Console Client

```cmd
dotnet run
```

When prompted, enter the name of your CSV file.

---

## Part 3: Preparing Test Data

### For Healthcare (1835 features)
You need genomic data with 1835 features per sample. Use the original data or generate test data.

### For Financial Fraud Detection (10 features)

On Windows or Ubuntu:

```bash
cd DataGenerators
python generate_financial_data.py
```

This creates `financial_features.csv` with 10 features per transaction.

**To create a test file for inference** (not training):
```python
import pandas as pd
import numpy as np

# Generate a few test samples
test_data = np.random.rand(5, 10)  # 5 samples, 10 features
pd.DataFrame(test_data).to_csv("test_financial.csv", header=False, index=False)
```

### For Academic Grade Prediction (10 features)

```bash
cd DataGenerators
python generate_academic_data.py
```

This creates `academic_features.csv` with 10 features per student.

**To create a test file for inference**:
```python
import pandas as pd
import numpy as np

# Generate a few test samples
test_data = np.random.rand(5, 10)  # 5 samples, 10 features
pd.DataFrame(test_data).to_csv("test_academic.csv", header=False, index=False)
```

---

## Part 4: Running the System

### Complete Workflow Example: Financial Fraud Detection

1. **On Ubuntu Server:**
   ```bash
   # Generate data
   cd ~/Healthcare-Security-Analysis-MLE-Extension/DataGenerators
   python3 generate_financial_data.py
   
   # Train model
   cd ../ModelTraining
   python3 train_financial_model.py
   
   # Load model
   cd ../SystemArchitecture/configDB
   cp ../../ModelTraining/coefs.csv .
   # Edit loadModelFromCsv.py: modelName = "FinancialFraud"
   python3 loadModelFromCsv.py
   
   # Verify model is loaded
   mongo models --eval "db.models.find({}, {Type:1, NWeights:1}).pretty()"
   ```

2. **On Windows Client:**
   ```cmd
   # Create test data (or use existing)
   cd DataGenerators
   python generate_financial_data.py
   
   # Run GUI
   cd ..\SystemArchitecture\ClientGUI
   dotnet run
   ```

3. **In the GUI:**
   - Select mode: "Business (Financial)"
   - Select model: "LogisticRegression"
   - Browse to: `DataGenerators\financial_features.csv`
   - Click "Process Encrypted ML"
   - Check `Result.csv` for predictions

---

## Troubleshooting

### Server Issues

**Check if server is running:**
```bash
sudo systemctl status MLE.service
```

**Check server logs:**
```bash
sudo journalctl -u MLE.service -n 50 --no-pager
```

**Check MongoDB:**
```bash
sudo systemctl status mongod
mongo models --eval "db.models.find().pretty()"
```

**Restart services:**
```bash
sudo systemctl restart MLE.service
sudo systemctl restart nginx
```

### Client Issues

**"Connection failed" Error:**
- Verify server is running: `curl http://localhost:8080` (on Ubuntu)
- Check port forwarding is configured (if using NAT)
- Verify firewall allows port 8080
- Check server URL in client code matches your setup

**"Model not found" Error:**
- Ensure model is loaded in MongoDB
- Check model name matches exactly (case-sensitive)
- Verify you're using the correct model name in the client

**"Feature count mismatch" Error:**
- Check your CSV has the correct number of features
- Healthcare: 1835 features
- Financial: 10 features
- Academic: 10 features

**GUI Won't Start:**
- Ensure .NET Core 3.1 SDK is installed on Windows
- Try building from command line: `dotnet build`
- Check for error messages in the console

---

## Quick Reference

### Server Commands (Ubuntu)
```bash
# Start services
sudo systemctl start mongod
sudo systemctl start MLE.service
sudo systemctl start nginx

# Stop services
sudo systemctl stop MLE.service
sudo systemctl stop mongod

# Check status
sudo systemctl status MLE.service
sudo systemctl status mongod

# View logs
sudo journalctl -u MLE.service -f
```

### Client Commands (Windows)
```cmd
# Build GUI
cd SystemArchitecture\ClientGUI
dotnet build
dotnet run

# Build Console
cd SystemArchitecture\Client
dotnet build
dotnet run
```

### Model Management
```bash
# List all models in MongoDB
mongo models --eval "db.models.find({}, {Type:1, NWeights:1}).pretty()"

# Remove a model
mongo models --eval "db.models.deleteOne({Type: 'FinancialFraud'})"

# Load a new model
cd SystemArchitecture/configDB
# Edit loadModelFromCsv.py with correct modelName
python3 loadModelFromCsv.py
```

---

## Summary

**Server (Ubuntu):**
1. Install dependencies (.NET, MongoDB, nginx)
2. Train models (optional - can use pre-trained)
3. Load models into MongoDB
4. Build and deploy server
5. Start services

**Client (Windows):**
1. Build GUI or console client
2. Update server URL if needed
3. Prepare test CSV files
4. Run and process data

The system is now ready to perform encrypted machine learning inference!

