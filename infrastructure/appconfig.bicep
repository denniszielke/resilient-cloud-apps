@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param appConfigStoreName string

param serviceNames array = [
  'Contonance.WebPortal.Server'
  'Contonance.Backend'
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
  name: '.appconfig.featureflag~2F${serviceName}:EnableRetryPolicy'
  properties: {
    value: string({
      id: '${serviceName}:EnableRetryPolicy'
      description: 'Enable retry on ${serviceName} for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

resource EnableBreakerFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for serviceName in serviceNames: {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F${serviceName}:EnableCircuitBreakerPolicy'
  properties: {
    value: string({
      id: '${serviceName}:EnableCircuitBreakerPolicy'
      description: 'Enable circuit breaker on ${serviceName} for HttpClient'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

resource EnableRateLimitingFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for serviceName in serviceNames: {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F${serviceName}:InjectRateLimitingFaults'
  properties: {
    value: string({
      id: '${serviceName}:InjectRateLimitingFaults'
      description: 'Inject rate limiting faults on ${serviceName}'
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

output appConfigurationName string = appConfigStore.name
