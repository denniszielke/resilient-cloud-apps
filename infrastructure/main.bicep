@description('Location resources.')
param location string = 'westeurope'

@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string

param registryOwner string

param imageTag string

targetScope = 'subscription'

var aiStorageContainerName = 'ai-data'

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
    cosmosdbDatabaseName: 'repair_parts'
    autoscaleMaxThroughput: 400
  }
}

module eh_storage 'storage.bicep' = {
  name: 'ehstorage'
  scope: rg
  params: {
    location: location
    storageAccountName: 'ehst${projectName}'
    containerNames: []
  }
}

module ai_storage 'storage.bicep' = {
  name: 'aistorage'
  scope: rg
  params: {
    location: location
    storageAccountName: 'aist${projectName}'
    containerNames: [
      aiStorageContainerName
    ]
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

module acareceiver 'acacontonancebackend.bicep' = {
  name: 'acacontonancebackend'
  scope: rg
  params: {
    containerAppEnvId: acaenv.outputs.containerAppEnvId
    location: location
    appInsightsName: logging.outputs.appInsightsName
    eventHubName: eventhub.outputs.eventHubName
    eventHubNamespaceName: eventhub.outputs.eventHubNamespaceName
    eventHubAuthRuleName: eventhub.outputs.authRuleName
    storageConnectionString: eh_storage.outputs.blobStorageConnectionString
    registryOwner: registryOwner
    imageTag: imageTag
    appConfigurationName: appconfig.outputs.appConfigurationName
  }
}

module acasink 'acawarehouse.bicep' = {
  name: 'acawarehouse'
  scope: rg
  params: {
    containerAppEnvId: acaenv.outputs.containerAppEnvId
    location: location
    appInsightsName: logging.outputs.appInsightsName
    registryOwner: registryOwner
    imageTag: imageTag
    cosmosDbName: cosmosdbsql.outputs.name
  }
}

module acawebportal 'acawebportal.bicep' = {
  name: 'acawebportal'
  scope: rg
  params: {
    containerAppEnvId: acaenv.outputs.containerAppEnvId
    location: location
    appInsightsName: logging.outputs.appInsightsName
    registryOwner: registryOwner
    imageTag: imageTag
    storageAccountName: ai_storage.outputs.storageAccountName
    containerName: aiStorageContainerName
    appConfigurationName: appconfig.outputs.appConfigurationName
    eventHubName: eventhub.outputs.eventHubName
    eventHubAuthRuleName: eventhub.outputs.authRuleName
    eventHubNamespaceName: eventhub.outputs.eventHubNamespaceName
  }
}
/*
module ai 'ai.bicep' = {
  name: 'ai'
  scope: rg
  params: {
    location: location
    openaiDeploymentName: 'openai-${projectName}'
    documentIntDeploymentName: 'documentInt-${projectName}'
    projectName: projectName
  }
}*/
