@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param appConfigStoreName string

param serviceNames array = [
  'Message.Creator'
  'Message.Receiver'
]

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
    softDeleteRetentionInDays: 1
  }
}
resource EnableRetryFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for serviceName in serviceNames: {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F${serviceName}:EnableRetry'
  properties: {
    value: string({
      id: '${serviceName}:EnableRetry'
      description: 'Enable retry on ${serviceName} for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

resource EnableBreakerFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for serviceName in serviceNames: {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F${serviceName}:EnableBreaker'
  properties: {
    value: string({
      id: '${serviceName}:EnableBreaker'
      description: 'Enable circuit breaker on ${serviceName} for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

resource EnableRateLimitingFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for serviceName in serviceNames: {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F${serviceName}:EnableRateLimiting'
  properties: {
    value: string({
      id: '${serviceName}:EnableRateLimiting'
      description: 'Enable rate limiting on ${serviceName}'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]


output appConfigurationName string = appConfigStore.name


