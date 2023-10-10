@description('Location for the AppConfiguration account')
param location string = resourceGroup().location

param appConfigStoreName string

param featureFlags array = [
  {
    app: 'Contonance.WebPortal.Server'
    featureFlagKey: 'EnableRetryPolicy'
    featureFlagLabel: ''
    featureFlagDescription: 'Enable retry on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
  }
  {
    app: 'Contonance.WebPortal.Server'
    featureFlagKey: 'EnableCircuitBreakerPolicy'
    featureFlagLabel: ''
    featureFlagDescription: 'Enable circuit breaker on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
  }
  {
    app: 'Contonance.WebPortal.Server'
    featureFlagKey: 'InjectRateLimitingFaults'
    featureFlagLabel: ''
    featureFlagDescription: 'Inject rate limiting faults on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
  }
  {
    app: 'Contonance.WebPortal.Server'
    featureFlagKey: 'InjectLatencyFaults'
    featureFlagLabel: ''
    featureFlagDescription: 'Inject latency faults on Contonance.WebPortal.Server for HTTP calls to Contonance.Backend'
  }
  {
    app: 'Contonance.Backend'
    featureFlagKey: 'InjectRateLimitingFaults'
    featureFlagLabel: ''
    featureFlagDescription: 'Inject rate limiting faults on Contonance.Backend for HTTP calls to EnterpriseWarehouse.Backend'
  }
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

resource configStoreFeatureflag 'Microsoft.AppConfiguration/configurationStores/keyValues@2021-10-01-preview' = [for featureFlag in featureFlags: {
  parent: appConfigStore
  // Delimiter '/' not possible because of BCP170
  name: '.appconfig.featureflag~2F${featureFlag.app}:${featureFlag.featureFlagKey}$${featureFlag.featureFlagLabel}'
  properties: {
    value: string({
      // Delimiter ':' not possible because of Azure portal, but we ignore this for now
      id: '${featureFlag.app}:${featureFlag.featureFlagKey}'
      description: featureFlag.featureFlagDescription
      enabled: false
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

output appConfigurationName string = appConfigStore.name
