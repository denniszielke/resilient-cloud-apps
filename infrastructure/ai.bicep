param location string = 'westeurope'
param openaiDeploymentName string
param documentIntDeploymentName string
param openAISku string = 'S0'
param searchSku string = 'standard'
param docIntSku string = 'S0'
param projectName string


resource openAIAccount 'Microsoft.CognitiveServices/accounts@2022-03-01' = {
  name: openaiDeploymentName
  location: location
  kind: 'OpenAI'
  sku: {
    name: openAISku
  }
  properties: {
    customSubDomainName: ''
    publicNetworkAccess: 'Enabled'
  }
}

resource documentIntAccount 'Microsoft.CognitiveServices/accounts@2022-03-01' = {
  name: documentIntDeploymentName
  location: location
  kind: 'FormRecognizer'
  sku: {
    name: docIntSku
  }
  properties: {
    customSubDomainName: ''
    publicNetworkAccess: 'Enabled'
  }
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2022-10-01' = {
  parent: openAIAccount
  name: 'model1'
  properties: {
    model: {
      name: 'gpt-35-turbo'
      version: '0301'
      format: 'OpenAI'
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

resource search 'Microsoft.Search/searchServices@2021-04-01-preview' = {
  name: 'search-${projectName}'
  location: location
  sku: {
    name: searchSku
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    semanticSearch: 'free'
  }
}

output openaiDeploymentEndpoint string = openAIAccount.properties.endpoint
