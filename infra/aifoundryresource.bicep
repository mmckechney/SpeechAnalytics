
param aiFoundryName string
param aiFoundryResourceName string
param location string= resourceGroup().location



var chatModel string = 'gpt-5-mini'
var embeddingModel string = 'text-embedding-3-large'

resource aiFoundryResource 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: aiFoundryResourceName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    apiProperties: {}
    customSubDomainName: aiFoundryResourceName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    allowProjectManagement: true
    defaultProject: aiFoundryName
    associatedProjects: [
      aiFoundryName
    ]
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}
resource gpt_5_mini_deployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: aiFoundryResource
  name: chatModel
  sku: {
    name: 'GlobalStandard'
    capacity: 150
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModel
      version: '2025-08-07'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    currentCapacity: 150
    raiPolicyName: 'Microsoft.DefaultV2'
    
  }
}

resource text_embedding_3_large_deployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: aiFoundryResource
  name: embeddingModel
  sku: {
    name: 'GlobalStandard'
    capacity: 150
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModel
      version: '1'
    }
    versionUpgradeOption: 'NoAutoUpgrade'
    currentCapacity: 150
    raiPolicyName: 'Microsoft.DefaultV2'
  }
  dependsOn: [
    gpt_5_mini_deployment
  ]
}


resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: aiFoundryResource
  name: aiFoundryName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

output aiFoundryPrincipalId string = foundryProject.identity.principalId
output embeddingModelName string = embeddingModel
output chatModelName string = chatModel
output aiFoundryProjectEndpoint string = foundryProject.properties.endpoints['AI Foundry API']
output aiSpeechToTextStandardEndpoint string = aiFoundryResource.properties.endpoints['Speech Services Speech to Text (Standard)']
output aiSpeechToTextEndpoint string = aiFoundryResource.properties.endpoints['Speech Services Speech to Text']
