param aiSearchName string
param keyVaultName string
param location string = resourceGroup().location



resource cognitiveSearchInstance 'Microsoft.Search/searchServices@2023-11-01' = {
  name: aiSearchName
  location: location
  sku: {
    name: 'basic'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

var aiSearchAdminKeySecretName = 'AiSearchAdminKey'
resource adminKey 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: aiSearchAdminKeySecretName
  properties: {
    value:  cognitiveSearchInstance.listAdminKeys().primaryKey
  }
}

var aiSearchEndpointSecretName = 'AiSearchEndpoint'
resource endPoint 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: aiSearchEndpointSecretName
  properties: {
    value: 'https://${aiSearchName}.search.windows.net'
  }
}

output aiSearchEndpointSecretName string = aiSearchEndpointSecretName
output aiSearchAdminKeySecretName string = aiSearchAdminKeySecretName
