@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

param clusterName string

param vmSize string = 'standard_d2s_v3'

#disable-next-line BCP081
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

resource aksChaosMesh 'Microsoft.Chaos/targets@2021-09-15-preview' = {
  name: 'Microsoft-AzureKubernetesServiceChaosMesh'
  scope: aks
  location: location
  properties: {}
}

resource aksNetworkChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/NetworkChaos-2.1'
  scope: aks
}

resource aksPodChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/PodChaos-2.1'
  scope: aks
}

resource aksStressChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/StressChaos-2.1'
  scope: aks
}

resource aksIOChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/IOChaos-2.1'
  scope: aks
}

resource aksTimeChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/TimeChaos-2.1'
  scope: aks
}

resource aksKernelChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/KernelChaos-2.1'
  scope: aks
}

resource aksDNSChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/DNSChaos-2.1'
  scope: aks
}

resource aksHTTPChaos 'Microsoft.Chaos/targets/capabilities@2021-09-15-preview' = {
  name: '${aksChaosMesh.name}/HTTPChaos-2.1'
  scope: aks
}

resource aksChaosExperiment 'Microsoft.Chaos/experiments@2021-09-15-preview' = {
  name: 'appChaos'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    selectors: [
      {
        type: 'List'
        id: 'SelectorAKS'
        targets: [
          {
            type: 'ChaosTarget'
            id: aksChaosMesh.id
          }
        ]
      }
    ]
    steps: [
      {
        name: 'HTTP chaos at sink'
        branches: [
          {
            name: 'HTTP chaos at sink'
            actions: [
              {
                type: 'continuous'
                name: 'urn:csci:microsoft:azureKubernetesServiceChaosMesh:HTTPChaos/2.1'
                duration: 'PT2M'
                selectorId: 'SelectorAKS'
                parameters: [
                    {
                        key: 'jsonSpec'
                        value: '{"mode":"all","selector":{"labelSelectors":{"app":"message-sink"}},"target":"Request","port":80,"method":"GET","path":"/api","abort":true,"duration":"120s"}'
                    }
                ]
              }
            ]
          }
        ]
      }
      {
        name: 'Pod kill of sink'
        branches: [
          {
            name: 'Pod kill of sink'
            actions: [
              {
                type: 'continuous'
                name: 'urn:csci:microsoft:azureKubernetesServiceChaosMesh:podChaos/2.1'
                duration: 'PT2M'
                selectorId: 'SelectorAKS'
                parameters: [
                  {
                      key: 'jsonSpec'
                      value: '{"mode":"all","selector":{"labelSelectors":{"app":"message-sink"}},"action":"pod-failure","duration":"120s",}'
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  }
}

@description('This is the built-in Azure Kubernetes Service Cluster Admin Role role.')
resource aksClusterAdminRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8'
}

resource chaosRoleAssign 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(aks.id, aksClusterAdminRoleDefinition.id)
  scope: aks
  properties: {
    roleDefinitionId: aksClusterAdminRoleDefinition.id
    principalId: aksChaosExperiment.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
