# Azure Functions Deployment Guide

## Prerequisites

1. **Install Azure CLI**
   ```bash
   brew install azure-cli
   ```

2. **Login to Azure**
   ```bash
   az login
   ```

3. **Verify your subscription**
   ```bash
   az account show
   ```

## Deployment Steps

### Option 1: Using the deployment script (Recommended)

```bash
chmod +x deploy.sh
./deploy.sh
```

### Option 2: Manual deployment

1. **Build the AOT binary**
   ```bash
   dotnet publish -c Release
   ```

2. **Deploy using Azure Functions Core Tools**
   ```bash
   cd bin/Release/net10.0/osx-arm64/publish
   func azure functionapp publish <YOUR_FUNCTION_APP_NAME> --dotnet-isolated
   ```

## Azure Resources Required

You mentioned you already created Azure resources. Make sure you have:

1. **Resource Group** (e.g., `xlsx-grid-flow-rg`)
2. **Storage Account** (e.g., `xlsxgridflowstorage`)
3. **Function App** with:
   - Runtime: .NET (isolated)
   - Version: 10
   - OS: Linux (for AOT) or Windows
   - Plan: Flex Consumption (for Linux) or Consumption/Premium

## Configuration

After deployment, configure these settings in Azure Portal:

1. **Application Settings**:
   - `AzureWebJobsStorage`: Connection string to your storage account
   - `FUNCTIONS_WORKER_RUNTIME`: `dotnet-isolated`

2. **CORS** (if needed):
   - Add your frontend URL (e.g., `https://yourusername.github.io`)

## Verify Deployment

1. Check function app status:
   ```bash
   az functionapp show --name <YOUR_FUNCTION_APP_NAME> --resource-group <YOUR_RESOURCE_GROUP>
   ```

2. Test an endpoint:
   ```bash
   curl -X POST https://<YOUR_FUNCTION_APP_NAME>.azurewebsites.net/api/template/init \
     -H "Content-Type: application/json" \
     -d '{"filename":"test.xlsx","columnDefs":[],"rowData":[],"mergedCells":[]}'
   ```

## Update Frontend

After deployment, update your frontend's `environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://<YOUR_FUNCTION_APP_NAME>.azurewebsites.net/api'
};
```

## Troubleshooting

- **Deployment fails**: Check Azure Functions Core Tools version (`func --version` should be 4.x)
- **Function not starting**: Check Application Insights logs in Azure Portal
- **CORS errors**: Add your frontend domain to CORS settings
