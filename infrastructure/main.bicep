@description('Location resources.')
param location string = resourceGroup().location

@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string
param containerRegistryOwner string

module logging 'logging.bicep' = {
  name: 'logging'
  params: {
    location: location
    logAnalyticsWorkspaceName: 'log-${projectName}'
    applicationInsightsName: 'appi-${projectName}'
  }
}

module eventhub 'eventhub.bicep' = {
  name: 'eventhub'
  params: {
    location: location
    eventHubNamespaceName: 'evhns-${projectName}'
    eventHubName: 'evh-${projectName}'
  }
}

module cosmosdb 'cosmosdb.bicep' = {
  name: 'cosmosdb'
  params: {
    location: location
    cosmosdbAccountName: 'db${projectName}'
    cosmosdbTableName: 'messages'
    autoscaleMaxThroughput: 1000
  }
}

module storage 'storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: 'st${projectName}'
  }
}

module appconfig 'appconfig.bicep' = {
  name: 'appconfig'
  params: {
    location: location
    appConfigStoreName: 'appcs-${projectName}'
  }
}

module aks 'aks.bicep' = {
  name: 'aks'
  params: {
    location: location
    clusterName: projectName
  }
}
