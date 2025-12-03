// Azure SQL Database Bicep template
// Deploys Azure SQL Server with Azure AD-only authentication and Northwind database

@description('Location for the SQL Server')
param location string

@description('Name of the SQL Server')
param sqlServerName string

@description('Azure AD Object ID of the SQL Administrator')
param adminObjectId string

@description('Azure AD User Principal Name of the SQL Administrator')
param adminLogin string

@description('Principal ID of the Managed Identity for database access. Note: Database role assignment is handled post-deployment via Python scripts.')
#disable-next-line no-unused-params
param managedIdentityPrincipalId string

var databaseName = 'Northwind'

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLogin
      sid: adminObjectId
      principalType: 'User'
      tenantId: subscription().tenantId
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Allow Azure services to access the SQL Server
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output databaseName string = sqlDatabase.name
