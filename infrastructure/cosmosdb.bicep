
@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

@description('Maximum autoscale throughput for the table')
@minValue(400)
@maxValue(1000000)
param autoscaleMaxThroughput int = 1000

@description('The name for the cosmosdb')
param cosmosdbAccountName string

@description('The name for the table')
param cosmosdbTableName string

resource cosmosDB 'Microsoft.DocumentDB/databaseAccounts@2021-04-15' = {
  name: cosmosdbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    isVirtualNetworkFilterEnabled: false
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableTable'
      }
    ]
    enableFreeTier: false
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
  }
}

resource cosmosDBTable 'Microsoft.DocumentDB/databaseAccounts/tables@2021-04-15' = {
  parent: cosmosDB
  name: cosmosdbTableName
  properties: {
    resource: {
      id: cosmosdbTableName
    }
    options: {
      autoscaleSettings: {
        maxThroughput: autoscaleMaxThroughput
      }
    }
  }
}
