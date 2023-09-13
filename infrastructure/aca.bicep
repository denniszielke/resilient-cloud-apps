@description('Specifies the name of the container app environment.')
param containerAppEnvName string

@description('Specifies the location for all resources.')
param location string = resourceGroup().location

param creatorAppName string = 'message-creator'

param eventHubNamespaceName string

param eventHubName string

param appInsightsName string

param logAnalyticsWorkspaceName string

param ehAuthRuleName string

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' existing = {
  name: eventHubNamespaceName
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource rule 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2022-01-01-preview' existing = {
  name: '${eventHubNamespace}/${eventHubName}/${ehAuthRuleName}'
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2022-06-01-preview' = {
  name: containerAppEnvName
  location: location
  sku: {
    name: 'Consumption'
  }
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: creatorAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
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
          name: creatorAppName
          image: 'ghcr.io/[handle]/reliable-apps/message-creator:main'
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
              name: 'RECEIVER_URL'
              value: 'http://message-receiver/api/message/receive'
            }
            {
              name: 'HttpClient__EnableRetry'
              value: 'false'
            }
            {
              name: 'HttpClient__EnableBreaker'
              value: 'false'
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
        minReplicas: 0
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
