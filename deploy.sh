#!/bin/bash
# deploy.sh - Deploy Expense Management System to Azure
# This script deploys App Service, Azure SQL, and Managed Identity (without GenAI resources)
# 
# Prerequisites:
# 1. Azure CLI installed and logged in (az login)
# 2. Appropriate Azure subscription selected
#
# Usage: bash deploy.sh
#
# After deployment, access the app at: https://<app-name>.azurewebsites.net/Index

set -e

echo "=========================================="
echo "Expense Management System Deployment"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Get current user info for SQL admin
echo "Getting current user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"

# Create resource group if it doesn't exist
echo "Creating resource group $RESOURCE_GROUP..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure
echo "Deploying infrastructure (this may take several minutes)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
    --query "properties.outputs" \
    --output json)

# Extract deployment outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
APP_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceDefaultHostName.value')

echo ""
echo "Deployment outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server FQDN: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"

# Wait for SQL Server to be fully ready
echo ""
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none 2>/dev/null || echo "Azure services rule may already exist"

# Add deployment IP
echo "Adding deployment IP ($MY_IP) to firewall..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none 2>/dev/null || echo "Deployment IP rule may already exist"

echo "Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Update Python scripts with actual values
echo "Configuring Python scripts with deployment values..."
sed -i.bak "s/example.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/database_name/$DATABASE_NAME/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/example.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/database_name/$DATABASE_NAME/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

sed -i.bak "s/example.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/database_name/$DATABASE_NAME/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Install required Python packages
echo "Installing required Python packages..."
pip3 install --quiet pyodbc azure-identity

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo "Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

# Create stored procedures
echo "Creating stored procedures..."
python3 run-sql-stored-procs.py

# Configure App Service connection string
echo "Configuring App Service connection string..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --connection-string-type SQLAzure \
    --settings "DefaultConnection=$CONNECTION_STRING" \
    --output none

# Configure App Service settings
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --settings \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    --output none

# Deploy application code
echo "Deploying application code..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "App URL: https://$APP_URL/Index"
echo ""
echo "Note: Access the app at /Index (not the root URL)"
echo ""
echo "To run locally, update appsettings.json connection string to use:"
echo "Authentication=Active Directory Default"
echo "Then run 'az login' and start the application."
