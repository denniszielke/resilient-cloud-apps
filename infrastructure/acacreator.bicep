param containerAppEnvId string

param location string = resourceGroup().location

param creatorAppName string = 'message-creator'

param eventHubNamespaceName string

param eventHubName string

param eventHubAuthRuleName string

param appInsightsName string

resource rule 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2022-01-01-preview' existing = {
  name: '${eventHubNamespaceName}/${eventHubName}/${eventHubAuthRuleName}'
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: creatorAppName
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
          name: creatorAppName
          image: 'ghcr.io/jplck/reliable-apps/message-creator:main'
          resources: {
            cpu: json('.5')
            memory: '0.3Gi'
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
