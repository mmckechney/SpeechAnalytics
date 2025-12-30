param userPrincipalId string = ''
param managedIdentityPrincipalId string = ''

type principalInfo = {
  Id: string
  Type: string
}

var managedIdentityInfo principalInfo = {
  Id: managedIdentityPrincipalId
  Type: 'ServicePrincipal'
}
var userIdentityInfo principalInfo = {
  Id: userPrincipalId
  Type: 'User'
}
var principals = [
  managedIdentityInfo
  userIdentityInfo
]

var deploymentEntropy = '3F2504E0-4F89-11D3-9A0C-0305E82C3301'

var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions','7f951dda-4ed3-4680-a7ca-43fe172d538d')
var acrPushRole = resourceId('Microsoft.Authorization/roleDefinitions','8311e382-0749-4cb8-b61a-304f252e45ec')

resource acrPush 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for prin in principals: {
  name: guid(resourceGroup().id, prin.Id, 'acrPushRole', deploymentEntropy )
  scope: resourceGroup()
  properties: {
    roleDefinitionId: acrPushRole
    principalId: prin.Id
    description: 'acrPush for ${prin.Id}'
    principalType: prin.Type
  }
}]


resource acrPull 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for prin in principals: {
  name: guid(resourceGroup().id, prin.Id, 'acrPullRole', deploymentEntropy )
  scope: resourceGroup()
  properties: {
    roleDefinitionId: acrPullRole
    principalId: prin.Id
    description: 'acrPull for ${prin.Id}'
    principalType: prin.Type
  }
}]





