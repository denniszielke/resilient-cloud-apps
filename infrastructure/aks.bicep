@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param clusterName string

param vmSize string = 'standard_d2s_v3'

resource aks 'Microsoft.ContainerService/managedClusters@2022-02-01' = {
  name: clusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Paid'
  }
  properties: {
    dnsPrefix: clusterName
    enableRBAC: true
    agentPoolProfiles: [
      {
        name: 'default'
        enableAutoScaling: true
        count: 3
        minCount: 3
        maxCount: 10
        vmSize: vmSize
        mode: 'System'
      }
    ]
  }
}
