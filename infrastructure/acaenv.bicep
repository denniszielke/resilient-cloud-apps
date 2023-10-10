@description('Specifies the name of the container app environment.')
param containerAppEnvName string

@description('Specifies the location for all resources.')
param location string = resourceGroup().location

param logAnalyticsWorkspaceName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2022-06-01-preview' = {
  name: containerAppEnvName
  location: location
  sku: {
    name: 'Consumption'
  }
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

output containerAppEnvId string = containerAppEnv.id
