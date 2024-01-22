param keyVaultName string
param openAiEndpoint string
param openAiKey string
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enableSoftDelete: true
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 90
  }
  tags: {}
}


resource aiKey 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'OpenAiKey'
  properties: {
    value:  openAiKey
  }
}

resource aiEndPoint 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'OpenAIEndpoint'
  properties: {
    value:  openAiEndpoint
  }
}

output openAiKeySecretName string = aiKey.name
output openAiEndpointSecretName string = aiEndPoint.name







