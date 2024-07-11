param containerAppEnvId string

param location string = resourceGroup().location

param appName string = 'contonance-backend'

param eventHubNamespaceName string

param eventHubName string

param eventHubAuthRuleName string

param appInsightsName string

param storageConnectionString string

param registryOwner string

param imageTag string

param appConfigurationName string

var EHConnectionStringSecretName = 'eventhub-connection-string'
var StorageConnectionStringSecretName = 'storage-connection-string'
var StorageLeaseBlobName = 'keda-blob-lease'

resource rule 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2022-01-01-preview' existing = {
  name: '${eventHubNamespaceName}/${eventHubName}/${eventHubAuthRuleName}'
}

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2021-10-01-preview' existing = {
  name: appConfigurationName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
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
      secrets: [
        {
          name: EHConnectionStringSecretName
          value: rule.listKeys().primaryConnectionString
        }
        {
          name: StorageConnectionStringSecretName
          value: storageConnectionString
        }
      ]
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
              name: 'ENTERPRISE_WAREHOUSE_BACKEND_URL'
              value: 'http://enterprise-warehouse-backend/api/message/receive'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'EventHub__EventHubName'
              value: eventHubName
            }
            {
              name: 'EventHub__EventHubConnectionString'
              value: rule.listKeys().primaryConnectionString
            }
            {
              name: 'EventHub__BlobConnectionString'
              value: storageConnectionString
            }
            {
              name: 'AppConfiguration__ConnectionString'
              value: appConfiguration.listKeys().value[0].connectionString
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
            name: 'sb-keda-scale'
            custom: {
              type: 'azure-eventhub'
              metadata: {
                consumerGroup: '$Default'
                unprocessedEventThreshold: '64'
                blobContainer: StorageLeaseBlobName
                checkpointStrategy: 'blobMetadata'
              }
              auth: [
                {
                  secretRef: EHConnectionStringSecretName
                  triggerParameter: 'connection'
                }
                {
                  secretRef: StorageConnectionStringSecretName
                  triggerParameter: 'storageConnection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}
