#!/bin/bash

# Azure Functions Deployment Script
# This script deploys the .NET 10 AOT Azure Functions app

set -e  # Exit on error

# Configuration
RESOURCE_GROUP="xlsx-grid-flow-rg"
FUNCTION_APP_NAME="xlsx-grid-flow-func-1768234154"
STORAGE_ACCOUNT="ebrdb"
LOCATION="canadacentral"

echo "🚀 Deploying Azure Functions with Native AOT..."

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed"
    echo "Install it from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    echo "❌ Not logged in to Azure"
    echo "Run: az login"
    exit 1
fi

# Build the AOT binary
echo "📦 Building AOT binary..."
dotnet publish -c Release

# Deploy to Azure
echo "☁️  Deploying to Azure Function App: $FUNCTION_APP_NAME..."
cd bin/Release/net10.0/osx-arm64/publish

# Use Azure Functions Core Tools to deploy
func azure functionapp publish $FUNCTION_APP_NAME --dotnet-isolated

echo "✅ Deployment complete!"
echo "🌐 Function App URL: https://$FUNCTION_APP_NAME.azurewebsites.net"
