
param aiFoundryName string
param aiFoundryResourceName string
param location string= resourceGroup().location
param chatModel string = 'gpt-5-mini'
param embeddingModel string = 'text-embedding-3-large'
param voiceDiarizeModel string = 'gpt-4o-transcribe-diarize '
param managedIdentityResourceId string
param logAnalyticsName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsName
}

resource aiFoundryResource 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: aiFoundryResourceName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
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
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
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
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
   properties: {}
}

resource foundryResourceLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'resource-logs'
  scope: aiFoundryResource
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'Audit'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Trace'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'RequestResponse'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AzureOpenAIRequestUsage'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource foundryProjectLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'foundry-project-logs'
  scope: foundryProject
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'Audit'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Trace'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

output aiFoundryResourcePrincipalId string = aiFoundryResource.identity.principalId
output aiFoundryProjectPrincipalId string = foundryProject.identity.principalId
output embeddingModelName string = embeddingModel
output chatModelName string = chatModel
output voiceDiarizeModel string = voiceDiarizeModel
output aiFoundryProjectEndpoint string = foundryProject.properties.endpoints['AI Foundry API']
output aiSpeechToTextStandardEndpoint string = aiFoundryResource.properties.endpoints['Speech Services Speech to Text (Standard)']
output aiSpeechToTextEndpoint string = aiFoundryResource.properties.endpoints['Speech Services Speech to Text']
output foundryResourceId string = aiFoundryResource.id
output foundryResourceName string = aiFoundryResourceName 
