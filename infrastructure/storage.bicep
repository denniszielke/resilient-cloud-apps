@description('Specifies the Azure location for all resources.')
param location string = resourceGroup().location

@description('Specifies a project name that is used to generate the Event Hub name and the Namespace name.')
param projectName string

var storageAccountName = '${projectName}st'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
output blobStorageConnectionString string = blobStorageConnectionString
