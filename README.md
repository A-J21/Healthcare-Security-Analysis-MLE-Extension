# Healthcare Security Analysis - Machine Learning with Encryption (MLE)

## Overview
This project implements a Machine Learning with Encryption (MLE) framework for privacy-preserving predictions on sensitive data. Originally designed for cancer genomics prediction, this system uses homomorphic encryption to perform machine learning inference without exposing raw patient data.

**Research Paper:** [Machine Learning in Precision Medicine to Preserve Privacy via Encryption](https://arxiv.org/abs/2102.03412)

## System Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────┐
│  Windows Client │────────▶│  Ubuntu Server   │────────▶│   MongoDB   │
│   (MLE.exe)     │  HTTP   │  (.NET + nginx)  │         │   (Models)  │
└─────────────────┘         └──────────────────┘         └─────────────┘
     Encrypts Data           Processes Encrypted          Stores Model
                            Data (never sees raw)         Weights
```

## Prerequisites

### Ubuntu Server (Linux VM)
- Ubuntu 20.04 or later
- .NET Core 3.1 SDK
- MongoDB 4.4+
- nginx web server
- Python 3.8+ with pip

### Windows Client
- Windows 10/11
- No additional software needed (client is self-contained)

## Installation Guide

### Part 1: Ubuntu Server Setup

#### 1.1 Install Dependencies

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET Core 3.1 SDK
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-3.1

# Install nginx
sudo apt install -y nginx

# Install MongoDB
wget -qO - https://www.mongodb.org/static/pgp/server-4.4.asc | sudo apt-key add -
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/4.4 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-4.4.list
sudo apt update
sudo apt install -y mongodb-org

# Install Python dependencies
sudo apt install -y python3-pip
pip3 install pandas numpy pymongo
```

#### 1.2 Start MongoDB

```bash
sudo systemctl start mongod
sudo systemctl enable mongod
sudo systemctl status mongod  # Should show "active (running)"
```

#### 1.3 Clone the Repository

```bash
cd ~
git clone <YOUR_REPO_URL>
cd Healthcare-Security-Analysis-MLE
```

#### 1.4 Load the ML Model into MongoDB

```bash
cd SystemArchitecture/configDB
python3 loadModelFromCsv.py

# Verify model is loaded
mongo models --eval "db.models.find().pretty()"
```

You should see the LogisticRegression model with weights.

#### 1.5 Configure and Build the Server

```bash
cd ~/Healthcare-Security-Analysis-MLE/SystemArchitecture/Server

# Build the server
dotnet publish --configuration Release

# Deploy to web server directory
sudo mkdir -p /var/www/MLE/
sudo cp -r ./bin/Release/netcoreapp3.1/publish/* /var/www/MLE/
```

#### 1.6 Create the systemd Service

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

#### 1.7 Configure nginx

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

Save and exit.

#### 1.8 Test and Start Services

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

Both should show "active (running)" in green.

#### 1.9 Get Your VM's IP Address

```bash
ip addr show
```

Look for the IP address (typically `10.0.2.15` for NAT or `192.168.x.x` for Bridged).

### Part 2: VirtualBox Port Forwarding (If Using NAT)

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

### Part 3: Windows Client Setup

#### 3.1 Build the Client (On Ubuntu VM)

```bash
cd ~/Healthcare-Security-Analysis-MLE/SystemArchitecture/Server

# Edit Program.cs to set server URL
nano ~/Healthcare-Security-Analysis-MLE/Client/Program.cs
```

Find line 134 and change:
```csharp
client.BaseAddress = new Uri("https://mle.isot.ca/");
```

To:
```csharp
client.BaseAddress = new Uri("http://localhost:8080/");
```

Save and compile:

```bash
cd ~/Healthcare-Security-Analysis-MLE/Client
dotnet publish --configuration Release -r win-x64 --self-contained true -p:PublishSingleFile=false

# Copy SEAL native library
cp /home/$USER/.nuget/packages/microsoft.research.sealnet/3.5.8/runtimes/win10-x64/sealc.dll ./bin/Release/netcoreapp3.1/win-x64/publish/

# Create zip package
cd ./bin/Release/netcoreapp3.1/win-x64/publish/
zip -r MLE_Complete.zip .

# Copy to web server for download
sudo cp MLE_Complete.zip /var/www/MLE/wwwroot/DownloadableFiles/
```

#### 3.2 Download Client on Windows

From your Windows browser, go to:
```
http://localhost:8080/DownloadableFiles/MLE_Complete.zip
```

Extract to a folder like `C:\MLE_Testing\`

Rename `CDTS_PROJECT.exe` to `MLE.exe`

## Usage

### 1. Prepare Your CSV Data

Create a CSV file with **no headers**, where:
- Each row = one sample
- Each column = one feature
- Features must match the model's expected count (check with MongoDB)

Example for the genomic model (1835 features):
```
0.123,0.456,0.789,...(1835 values total)
0.234,0.567,0.890,...(1835 values total)
```

### 2. Run the Client

**On Windows:**

```cmd
cd C:\MLE_Testing
MLE.exe
```

When prompted:
```
Enter the name of the CSV containing the samples to be encrypted: yourdata.csv
```

Results will be saved to `Result.csv` in the same directory.

## Troubleshooting

### Server Issues

**Check service logs:**
```bash
sudo journalctl -u MLE.service -n 50 --no-pager
```

**Check nginx logs:**
```bash
sudo tail -f /var/log/nginx/error.log
```

**Restart services:**
```bash
sudo systemctl restart MLE.service
sudo systemctl restart nginx
```

### MongoDB Issues

**Check MongoDB status:**
```bash
sudo systemctl status mongod
```

**View models in database:**
```bash
mongo models --eval "db.models.find().pretty()"
```

**Reload model:**
```bash
cd ~/Healthcare-Security-Analysis-MLE/SystemArchitecture/configDB
python3 loadModelFromCsv.py
```

### Client Issues

**Error: "Unable to load DLL 'sealc'"**
- Ensure `sealc.dll` is in the same directory as `MLE.exe`

**Error: "Connection failed"**
- Verify server is running: `curl http://localhost:8080`
- Check port forwarding is configured
- Verify firewall allows port 8080

**Error: "Request Entity Too Large"**
- Server nginx config has `client_max_body_size 100M;`
- Reduce dataset size or increase limit further

**Error: "InternalServerError"**
- Check server logs for details
- Verify MongoDB is running and has model loaded
- Ensure CSV feature count matches model's `NWeights`

## Testing with Financial Data

To test with financial fraud detection data instead of genomics:

1. **Generate test data** using the provided dataset generator
2. **Train a new model** with 10 financial features
3. **Load the financial model** into MongoDB
4. **Run predictions** on financial CSV data

See `docs/FINANCIAL_MODEL.md` for detailed instructions (if available).

## Project Structure

```
Healthcare-Security-Analysis-MLE/
├── SystemArchitecture/
│   ├── Server/              # .NET Core server application
│   ├── Client/              # Windows client application
│   └── configDB/            # MongoDB model loading scripts
├── ModelTraining/           # ML model training scripts
│   ├── feature_table.csv   # Genomic training data
│   ├── Y.csv               # Labels
│   └── compareModels.py    # Model training script
└── README.md               # This file
```

## Security Considerations

- Data is encrypted client-side using homomorphic encryption
- Server never sees unencrypted data
- Suitable for HIPAA, GDPR, PCI-DSS compliance scenarios
- Network traffic should use HTTPS in production (not implemented in this demo)

## Citation

If you use this framework in your research, please cite:

```bibtex
@Article{Briguglio2021MachineLearningPrecisionMedicine-arxiv,
  author = {William Briguglio and Parisa Moghaddam and Waleed A. Yousef and Issa Traore and Mohammad Mamun},
  title = {Machine Learning in Precision Medicine to Preserve Privacy via Encryption},
  journal = {arXiv Preprint, arXiv:2102.03412},
  year = 2021,
  url = {https://github.com/isotlaboratory/Healthcare-Security-Analysis-MLE},
  primaryclass = {cs.LG}
}
```

## License

GPL-3.0 License - See LICENSE file for details

## Contributors

- Original Research Team (see paper)
- [Your Name] - Setup and Documentation

## Support

For issues or questions:
1. Check the Troubleshooting section above
2. Review server logs
3. Open an issue on GitHub
4. Contact: [your-email@example.com]
