// App Service Bicep template
// Deploys Azure App Service with Standard S1 SKU to avoid cold start

@description('Location for the App Service')
param location string

@description('Name of the App Service')
param appServiceName string

@description('Resource ID of the User-Assigned Managed Identity')
param managedIdentityId string

@description('Client ID of the User-Assigned Managed Identity')
param managedIdentityClientId string

var appServicePlanName = 'asp-${appServiceName}'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    reserved: false // Windows
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedIdentityClientId
        }
      ]
    }
  }
}

output appServiceName string = appService.name
output defaultHostName string = appService.properties.defaultHostName
output managedIdentityPrincipalId string = reference(managedIdentityId, '2023-01-31').principalId
