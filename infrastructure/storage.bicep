@description('Specifies the Azure location for all resources.')
param location string = resourceGroup().location

param storageAccountName string

param containerNames array

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2019-06-01' = {
  name: 'default'
  parent: storageAccount
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = [for i in range(0, length(containerNames)): {
  name: containerNames[i]
  parent: blobServices
  properties: {
    publicAccess: 'None'
    metadata: {}
  }
}]

var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
output blobStorageConnectionString string = blobStorageConnectionString
output storageAccountName string = storageAccount.name
