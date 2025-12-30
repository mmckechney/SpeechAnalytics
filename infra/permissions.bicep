param cosmosAccountName string
param userPrincipalId string
param aiFoundryProjectPrincipalId string
param aiFoundryResourcePrincipalId string
param managedIdentityPrincipalId string

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
var principals = concat([
  managedIdentityInfo
  userIdentityInfo
], empty(aiFoundryProjectPrincipalId) ? [] : [
  {
    Id: aiFoundryProjectPrincipalId
    Type: 'ServicePrincipal'
  }
], empty(aiFoundryResourcePrincipalId) ? [] : [
  {
    Id: aiFoundryResourcePrincipalId
    Type: 'ServicePrincipal'
  }
])


var deploymentEntropy = '3F2504E0-4F89-11D3-9A0C-0305E82C3301'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' existing = {
  name: cosmosAccountName
}


var contributorRole = resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var cognitiveServicesUserRole = resourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
var speechContribRole = resourceId('Microsoft.Authorization/roleDefinitions','0e75ca1e-0464-4b4d-8b93-68208a576181')
var searchDataContributorRole = resourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
var storageBlobDataContribRole = resourceId('Microsoft.Authorization/roleDefinitions','ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var storageBlobDataReaderRole = resourceId('Microsoft.Authorization/roleDefinitions','2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')

resource contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id, 'contributor', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: contributorRole
    description: 'Contributor role assignment for ${id.Id}'
    principalType: id.Type
  }
}
]

resource cognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id,'foundry', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: cognitiveServicesUserRole
    description: 'Cognitive Services User role assignment for ${id.Id}'
    principalType: id.Type
  }
}
]

resource speechContrib 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id,'speech', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: speechContribRole
    description: 'Speech Contributor role assignment for ${id.Id}'
    principalType: id.Type
  }
}
]


resource searchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' =  [for id in principals:{
  name: guid(resourceGroup().id,'ask-search', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: searchDataContributorRole
    description: 'Search Data Contributor role assignment for ${id.Id}'
    principalType: id.Type
  }
}
]


resource blobContribRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id,'blobcontrib', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: storageBlobDataContribRole
    description: 'Storage Blob Data Contributor role assignment for ${id.Id}'
    principalType: id.Type

  }
}
]

resource blobReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id,'blobreader', id.Id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id.Id
    roleDefinitionId: storageBlobDataReaderRole
    description: 'Storage Blob Data Reader role assignment for ${id.Id}'
    principalType: id.Type

  }
}
]

resource cosmosAskContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = [for id in principals:{
  name: guid(resourceGroup().id,'cosmos-ask', id.Id, cosmosAccountName, deploymentEntropy)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: id.Id
    scope: cosmosAccount.id
  }
}
]








