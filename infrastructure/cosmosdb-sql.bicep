
@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

@description('Maximum autoscale throughput for the table')
@minValue(400)
@maxValue(1000000)
param autoscaleMaxThroughput int = 400

@description('The name for the cosmosdb')
param cosmosdbAccountName string

@description('The name for the databse')
param cosmosdbDatabaseName string

@description('The container for the database')
param containerNameName string = 'data'


resource cosmosDB 'Microsoft.DocumentDB/databaseAccounts@2021-01-15' = {
  name: cosmosdbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    isVirtualNetworkFilterEnabled: false
    enableMultipleWriteLocations: false
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

resource cosmosDBSql 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-01-15' = {
  parent: cosmosDB
  name: cosmosdbDatabaseName
  properties: {
    resource: {
      id: cosmosdbDatabaseName
    }
    options: {
      throughput: autoscaleMaxThroughput
      // autoscaleSettings: {
      //   maxThroughput: autoscaleMaxThroughput
      // }
    }
  }
}
