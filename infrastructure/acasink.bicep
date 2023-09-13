param containerAppEnvId string

param location string = resourceGroup().location

param appName string = 'message-sink'

param appInsightsName string

param registryOwner string

param imageTag string

param cdbConnectionString string

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
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
              name: 'SINK_URL'
              value: 'http://message-sink/api/message/receive'
            }
            {
              name: 'IpRateLimiting__EnableEndpointRateLimiting'
              value: 'false'
            }
            {
              name: 'ConnectionStrings__CosmosApi'
              value: cdbConnectionString
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsights.properties.ConnectionString
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
