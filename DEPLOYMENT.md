# TCGHit Backend Deployment Guide

This guide covers deploying the TCGHit API to Azure Container Apps.

## Repository Structure

This backend is designed to be a **standalone Git repository**:

```
backend/                    ← Git root (this directory)
├── .github/
│   └── workflows/
│       └── azure-container-apps.yml
├── TCGHit.Api/
│   ├── Dockerfile
│   ├── Program.cs
│   └── ...
├── DEPLOYMENT.md           ← You are here
└── README.md
```

## Architecture Overview

```
GitHub (push to master)
    ↓
GitHub Actions
    ↓
Build .NET 8 → Docker Image → Azure Container Registry
                                        ↓
                              Azure Container Apps
                                        ↓
                              Azure SQL Database
```

## Prerequisites

- Azure subscription
- GitHub repository with Actions enabled
- Azure CLI installed locally (for initial setup)

## Step 1: Create Azure Resources

### 1.1 Login to Azure CLI

```bash
az login
az account set --subscription "<your-subscription-id>"
```

### 1.2 Create Resource Group

```bash
az group create \
  --name tcgh-rg \
  --location westus2
```

### 1.3 Create Azure Container Registry (ACR)

```bash
az acr create --resource-group tcgh-rg --name tcghitacr  --sku Basic --admin-enabled true
```

Get ACR credentials (you'll need these for GitHub Secrets):

```bash
az acr credential show --name tcghitacr
```

### 1.4 Create Container Apps Environment

```bash
az containerapp env create \
  --name tcghit-env \
  --resource-group tcgh-rg \
  --location westus2
```

### 1.5 Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name tcghit-sql \
  --resource-group tcgh-rg \
  --location westus2 \
  --admin-user tcghitadmin \
  --admin-password "<strong-password>"

# Create database (Serverless tier for cost optimization)
az sql db create \
  --resource-group tcgh-rg \
  --server tcghit-sql \
  --name tcghit-db \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --family Gen5 \
  --capacity 1 \
  --auto-pause-delay 60

# Allow Azure services to connect
az sql server firewall-rule create \
  --resource-group tcgh-rg \
  --server tcghit-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 1.6 Create Container App

```bash
az containerapp create \
  --name tcghit-api \
  --resource-group tcgh-rg \
  --environment tcghit-env \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 10 \
  --cpu 0.5 \
  --memory 1.0Gi
```

### 1.7 Configure Container App to use ACR

```bash
az containerapp registry set \
  --name tcghit-api \
  --resource-group tcgh-rg \
  --server tcghitacr.azurecr.io \
  --username tcghitacr \
  --password "<acr-password>"
```

### 1.8 Set Environment Variables

**PowerShell:**
```powershell
$SQL_CONNECTION = "Server=tcp:tcghit-sql.database.windows.net,1433;Database=tcghit-db;User ID=tcghitadmin;Password=<password>;Encrypt=True;TrustServerCertificate=False;"

az containerapp update --name tcghit-api --resource-group tcgh-rg --set-env-vars "ASPNETCORE_ENVIRONMENT=Production" "ConnectionStrings__DefaultConnection=$SQL_CONNECTION"
```

## Step 2: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add these secrets:

| Secret Name | Value | How to Get It |
|-------------|-------|---------------|
| `ACR_LOGIN_SERVER` | `tcghitacr.azurecr.io` | Your ACR login server |
| `ACR_USERNAME` | `tcghitacr` | From `az acr credential show` |
| `ACR_PASSWORD` | `<password>` | From `az acr credential show` |
| `AZURE_CREDENTIALS` | `<json>` | See below |
| `AZURE_RESOURCE_GROUP` | `tcgh-rg` | Your resource group name |
| `AZURE_CONTAINER_APP_NAME` | `tcghit-api` | Your container app name |

### Getting AZURE_CREDENTIALS

Create a service principal:

```bash
az ad sp create-for-rbac \
  --name "tcghit-github-actions" \
  --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/tcgh-rg \
  --sdk-auth
```

Copy the entire JSON output and paste it as the `AZURE_CREDENTIALS` secret.

## Step 3: Run EF Core Migrations

Before the first deployment, run migrations against the production database.

### 3.1 Add your IP to Azure SQL firewall

Azure SQL only allows connections from whitelisted IPs. Add your current IP temporarily:

```powershell
# Get your current public IP and add firewall rule
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az sql server firewall-rule create --resource-group tcgh-rg --server tcghit-sql --name MyLocalIP --start-ip-address $myIp --end-ip-address $myIp
```

### 3.2 Run migrations

```powershell
cd C:\path\to\backend\TCGHit.Api
dotnet ef database update --connection "Server=tcp:tcghit-sql.database.windows.net,1433;Database=tcghit-db;User ID=tcghitadmin;Password=<your-password>;Encrypt=True;TrustServerCertificate=False;"
```

### 3.3 Remove firewall rule (optional)

After migrations complete, you can remove the rule since you rarely need local access to production DB:

```powershell
az sql server firewall-rule delete --resource-group tcgh-rg --server tcghit-sql --name MyLocalIP
```

> **Note:** If you have a dynamic IP, you'll need to repeat step 3.1 whenever your IP changes. Future migrations can be run from GitHub Actions or Azure Cloud Shell instead.

## Step 4: Deploy

Push to the `master` branch:

```bash
git add .
git commit -m "Deploy backend to Azure Container Apps"
git push origin master
```

The GitHub Actions workflow will:
1. Build and test the .NET code
2. Build the Docker image
3. Push to Azure Container Registry
4. Deploy to Azure Container Apps

## Monitoring & Troubleshooting

### View Container App Logs

```bash
az containerapp logs show \
  --name tcghit-api \
  --resource-group tcgh-rg \
  --follow
```

### Check Container App Status

```bash
az containerapp show \
  --name tcghit-api \
  --resource-group tcgh-rg \
  --query "{status:properties.runningStatus, url:properties.configuration.ingress.fqdn}"
```

### Health Check Endpoints

- **Liveness:** `https://<app-url>/health` - Is the app running?
- **Readiness:** `https://<app-url>/health/ready` - Is the database connected?
- **Swagger:** `https://<app-url>/swagger` - API documentation

## Cost Optimization

The current configuration is optimized for cost:

| Resource | Configuration | Monthly Cost (Est.) |
|----------|---------------|---------------------|
| Container Apps | Scale 0-10, Consumption | ~$0-30 based on usage |
| Container Registry | Basic | ~$5 |
| SQL Database | Serverless, auto-pause | ~$5-15 based on usage |
| **Total (idle)** | | **~$10/month** |
| **Total (moderate use)** | | **~$30-50/month** |

### Tips

1. **Scale-to-zero**: Container Apps scales to 0 when no traffic
2. **SQL auto-pause**: Database pauses after 60 min of inactivity
3. **ACR Basic tier**: Sufficient for small projects

## Updating CORS for Production

Once deployed, update `Program.cs` to include your Container App URL in CORS:

```csharp
.WithOrigins(
    "http://localhost:5173",
    "https://tcghit.com",
    "https://www.tcghit.com",
    "https://<your-container-app>.azurecontainerapps.io"  // Add this
)
```
