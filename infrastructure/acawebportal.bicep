param containerAppEnvId string

param location string = resourceGroup().location

param appName string = 'contonance-web-portal'

param appInsightsName string

param registryOwner string

param imageTag string

param storageAccountName string

param containerName string

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' existing = {
  name: storageAccountName
}

resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' existing = {
  name: containerName
}

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      containers: [
        {
          name: appName
          image: 'ghcr.io/${registryOwner}/reliable-apps/${appName}:${imageTag}'
          resources: {
            cpu: json('.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'PORT'
              value: '8080'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'AzureOpenAiServiceEndpoint'
              value: ''
            }
            {
              name: 'AzureOpenAiKey'
              value: ''
            }
            {
              name: 'AzureOpenAiDeployment'
              value: ''
            }
            {
              name: 'AzureCognitiveSearchServiceEndpoint'
              value: ''
            }
            {
              name: 'AzureCognitiveSearchKey'
              value: ''
            }
            {
              name: 'AzureCognitiveSearchIndexName'
              value: ''
            }
            {
              name: 'AzureBlobSasToken'
              value: storageAccount.listKeys().keys[0].value
            }
            {
              name: 'AzureBlobContainerUrl'
              value: 'https://${storageAccount.name}.blob.core.windows.net/${storageContainer.name}'
            }
          ]
          probes: [
            {
              httpGet: {
                path: '/ping'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
        rules: [
          {
            name: 'http-requests'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}
