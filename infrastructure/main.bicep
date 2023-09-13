@description('Location resources.')
param location string = 'westeurope'

@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string

param registryOwner string

param imageTag string

targetScope = 'subscription'

resource rg 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: '${projectName}-rg'
  location: location
}

module logging 'logging.bicep' = {
  name: 'logging'
  scope: rg
  params: {
    location: location
    logAnalyticsWorkspaceName: 'log-${projectName}'
    applicationInsightsName: 'appi-${projectName}'
  }
}

module workbook 'workbook.bicep' = {
  name: 'workbook'
  scope: rg
  params: {
    location: location
    workbookId: '5caf5fbb-125c-4cfb-a3b3-de2c5a27ff08'
    workbookDisplayName: 'reliable-apps-new-${projectName}'
    workbookSourceId: logging.outputs.appInsightsId
  }
}

module eventhub 'eventhub.bicep' = {
  name: 'eventhub'
  scope: rg
  params: {
    location: location
    eventHubNamespaceName: 'evhns-${projectName}'
    eventHubName: 'events'
  }
}

module cosmosdbsql 'cosmosdb-sql.bicep' = {
  name: 'cosmosdbsql'
  scope: rg
  params: {
    location: location
    cosmosdbAccountName: 'dbs${projectName}'
    cosmosdbDatabaseName: 'messages'
    autoscaleMaxThroughput: 400
  }
}

module storage 'storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    storageAccountName: 'st${projectName}'
  }
}

module appconfig 'appconfig.bicep' = {
  name: 'appconfig'
  scope: rg
  params: {
    location: location
    appConfigStoreName: 'appcs-${projectName}'
  }
}

module acaenv 'acaenv.bicep' = {
  name: 'acaenv'
  scope: rg
  params: {
    containerAppEnvName: 'aca-${projectName}'
    location: location
    logAnalyticsWorkspaceName: logging.outputs.logAnalyticsWorkspaceName
  }
}

module acacreator 'acacreator.bicep' = {
  name: 'acacreator'
  scope: rg
  params: {
    containerAppEnvId: acaenv.outputs.containerAppEnvId
    location: location
    appInsightsName: logging.outputs.appInsightsName
    eventHubName: eventhub.outputs.eventHubName
    eventHubNamespaceName: eventhub.outputs.eventHubNamespaceName
    eventHubAuthRuleName: eventhub.outputs.authRuleName
    registryOwner: registryOwner
    imageTag: imageTag
  }
}

module acareceiver 'acareceiver.bicep' = {
  name: 'acareceiver'
  scope: rg
  params: {
    containerAppEnvId: acaenv.outputs.containerAppEnvId
    location: location
    appInsightsName: logging.outputs.appInsightsName
    eventHubName: eventhub.outputs.eventHubName
    eventHubNamespaceName: eventhub.outputs.eventHubNamespaceName
    eventHubAuthRuleName: eventhub.outputs.authRuleName
    storageConnectionString: storage.outputs.blobStorageConnectionString
    registryOwner: registryOwner
    imageTag: imageTag
  }
}
