
param aiFoundryName string
param location string= resourceGroup().location

// @secure()
// param appInsightsConnectionString string
// param appInsightsResourceId string
// param appInsightsResourceName string
var aiFoundryResourceName = '${aiFoundryName}-resource'


// var ingestionEndpointComponents = split(appInsightsConnectionString, 'IngestionEndpoint=')
// var ingestionEndpointValue = length(ingestionEndpointComponents) > 1 ? split(ingestionEndpointComponents[1], ';')[0] : ''
// var normalizedIngestionEndpoint = empty(ingestionEndpointValue) ? '' : (endsWith(ingestionEndpointValue, '/') ? ingestionEndpointValue : '${ingestionEndpointValue}/')
// var telemetryTargetUrl = empty(normalizedIngestionEndpoint) ? 'https://dc.services.visualstudio.com/v2/track' : '${normalizedIngestionEndpoint}v2/track'

var chatModel string = 'gpt-5-mini'
var embeddingModel string = 'text-embedding-3-large'

// resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
//   name: appInsightsResourceName
// }

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

// resource appInsightsConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-06-01' = {
//   parent: foundryProject
//   name: 'ApplicationInsights'
//   properties: {
//     category: 'AppInsights'
//     authType: 'ApiKey'
//     target: appInsightsResourceId
//     credentials: {
//       key: appInsightsConnectionString
//     }
     
//     metadata: {
//       connectionString: appInsightsConnectionString

//     }
//     useWorkspaceManagedIdentity: false
//     peRequirement: 'NotApplicable'
//   }
// }

output docIntelPrincipalId string = foundryProject.identity.principalId
output embeddingModelName string = embeddingModel
output chatModelName string = chatModel
output aiFoundryProjectEndpoint string = foundryProject.properties.endpoints['AI Foundry API']
