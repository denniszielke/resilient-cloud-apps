@description('Location for the AppConfiguration account')
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
    softDeleteRetentionInDays: 1
  }
}

resource ContonanceWebPortalServerEnableRetryPolicyFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F$Contonance.WebPortal.Server:EnableRetryPolicy'
  properties: {
    value: string({
      id: 'Contonance.WebPortal.Server:EnableRetryPolicy'
      description: 'Enable retry on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
      enabled: false
      labels: [
        'Contonance.WebPortal.Server'
      ]
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ContonanceWebPortalServerEnableCircuitBreakerPolicyFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F$Contonance.WebPortal.Server:EnableCircuitBreakerPolicy'
  properties: {
    value: string({
      id: 'Contonance.WebPortal.Server:EnableCircuitBreakerPolicy'
      description: 'Enable circuit breaker on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
      enabled: false
      labels: [
        'Contonance.WebPortal.Server'
      ]
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ContonanceWebPortalServerInjectRateLimitingFaultsFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F$Contonance.WebPortal.Server:InjectRateLimitingFaults'
  properties: {
    value: string({
      id: 'Contonance.WebPortal.Server:InjectRateLimitingFaults'
      description: 'Inject rate limiting faults on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
      enabled: false
      labels: [
        'Contonance.WebPortal.Server'
      ]
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ContonanceWebPortalServerInjectLatencyFaultsFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F$Contonance.WebPortal.Server:InjectLatencyFaults'
  properties: {
    value: string({
      id: 'Contonance.WebPortal.Server:InjectLatencyFaults'
      description: 'Inject latency faults on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
      enabled: false
      labels: [
        'Contonance.WebPortal.Server'
      ]
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ContonanceBackendInjectRateLimitingFaultsFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = {
  parent: appConfigStore
  name: '.appconfig.featureflag~2F$Contonance.Backend:InjectRateLimitingFaults'
  properties: {
    value: string({
      id: 'Contonance.Backend:InjectRateLimitingFaults'
      description: 'Inject rate limiting faults on Contonance.Backend for HTTP calls to EnterpriseWarehouse.Backend'
      enabled: false
      labels: [
        'Contonance.Backend'
      ]
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

output appConfigurationName string = appConfigStore.name
