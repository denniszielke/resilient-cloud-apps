@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param appConfigStoreName string

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
    softDeleteRetentionInDays: 0
  }
}

resource HttpClient__EnableRetry 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2FHttpClient__EnableRetry'
  properties: {
    value: string({
      id: 'HttpClient__EnableRetry'
      description: 'Enable retry for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource HttpClient__EnableBreaker 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2FHttpClient__EnableBreaker'
  properties: {
    value: string({
      id: 'HttpClient__EnableBreaker'
      description: 'Enable circuit breaker for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource IpRateLimiting__EnableEndpointRateLimiting 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2FIpRateLimiting__EnableEndpointRateLimiting'
  properties: {
    value: string({
      id: 'IpRateLimiting__EnableEndpointRateLimiting'
      description: 'Set to true to enable endpoint rate limiting'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}


