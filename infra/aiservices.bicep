param aiServicesAccountName string
param location string = resourceGroup().location
param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: aiServicesAccountName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'SpeechServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    
    customSubDomainName: aiServicesAccountName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}


resource aiServicesConnection 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'AiServicesKey'
  properties: {
    value:  aiServices.listKeys().key1
  }
}

output endpoint string = aiServices.properties.endpoints.token
output location string = aiServices.location
output aiServicesIdentityPrincipal string = aiServices.identity.principalId
output aiServicesSecretName string = aiServicesConnection.name

