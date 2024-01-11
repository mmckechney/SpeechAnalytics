param aiServicesAccountName string
param location string = resourceGroup().location

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

output endpoint string = aiServices.properties.endpoints.token
output location string = aiServices.location
output aiServicesIdentityPrincipal string = aiServices.identity.principalId
