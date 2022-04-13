
@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string

@description('Specifies the Azure location for all resources.')
param location string = resourceGroup().location

@description('Specifies the messaging tier for Event Hub Namespace.')
@allowed([
  'Basic'
  'Standard'
])
param eventHubSku string = 'Standard'

var eventHubNamespaceName = 'evhns-${projectName}'
var eventHubName = 'evh-${projectName}'

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: eventHubNamespaceName
  location: location
  sku: {
    name: eventHubSku
    tier: eventHubSku
    capacity: 1
  }
  properties: {
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: eventHubNamespace
  name: eventHubName
  properties: {
    messageRetentionInDays: 7
    partitionCount: 1
  }
}

resource eventHub_ListenSend 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-01-01-preview' = {
  parent: eventHub
  name: 'ListenSend'
  properties: {
    rights: [
      'Listen'
      'Send'
    ]
  }
}

var storageAccountName = '${projectName}st'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

var workspaceName = 'log-${projectName}'
var applicationInsightsName = 'appi-${projectName}'

resource workspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'Standard'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    DisableLocalAuth: false
    Flow_Type: 'Bluefield'
    ForceCustomerStorageForProfiler: false
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    Request_Source: 'rest'
    WorkspaceResourceId: workspace.id
  }
}

var appConfigStoreName = 'appcs-${projectName}'

resource appConfigStore 'Microsoft.AppConfiguration/configurationStores@2021-10-01-preview' = {
  name: appConfigStoreName
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    createMode: 'Default'
    disableLocalAuth: false
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
}

var eventHubNamespaceConnectionString = listKeys(eventHub_ListenSend.id, eventHub_ListenSend.apiVersion).primaryConnectionString
output eventHubNamespaceConnectionString string = eventHubNamespaceConnectionString
output eventHubName string = eventHubName

var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
output blobStorageConnectionString string = blobStorageConnectionString


output applicationInsightsId string = applicationInsights.id
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
