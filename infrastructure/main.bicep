@description('Location resources.')
param location string = resourceGroup().location

@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string

module logging 'logging.bicep' = {
  name: 'logging'
  params: {
    location: location
    logAnalyticsWorkspaceName: 'log-${projectName}'
    applicationInsightsName: 'appi-${projectName}'
  }
}

module workbook 'workbook.bicep' = {
  name: 'workbook'
  params: {
    location: location
    workbookDisplayName: 'reliable-apps-new'
    workbookSourceId: logging.outputs.appInsightsId
  }
}

module eventhub 'eventhub.bicep' = {
  name: 'eventhub'
  params: {
    location: location
    eventHubNamespaceName: 'evhns-${projectName}'
    eventHubName: 'events'
  }
}

module cosmosdbsql 'cosmosdb-sql.bicep' = {
  name: 'cosmosdbsql'
  params: {
    location: location
    cosmosdbAccountName: 'dbs${projectName}'
    cosmosdbDatabaseName: 'messages'
    autoscaleMaxThroughput: 400
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
    workspaceResourceId: logging.outputs.logAnalyticsWorkspaceId
  }
}
