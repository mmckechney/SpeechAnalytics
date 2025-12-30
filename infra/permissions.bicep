param aiFoundryResourceName string
param aiSearchName string
param cosmosAccountName string
param askPrincipalId string
param transcriptionPrincipalId string
param userPrincipalId string
param aiFoundryPrincipal string
param storageAccountName string
param blobContainerName string

var principals = [
  askPrincipalId
  transcriptionPrincipalId
  userPrincipalId
  aiFoundryPrincipal
]

var deploymentEntropy = '3F2504E0-4F89-11D3-9A0C-0305E82C3301'

resource aiFoundry 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: aiFoundryResourceName
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: aiSearchName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' existing = {
  name: cosmosAccountName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
}
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-06-01' existing = {
  name: 'default'
  parent: storageAccount
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-06-01' = {
  name: blobContainerName
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

var contributorRole = resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var cognitiveServicesUserRole = resourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
var searchDataContributorRole = resourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
var storageBlobDataContribRole = resourceId('Microsoft.Authorization/roleDefinitions','ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions','7f951dda-4ed3-4680-a7ca-43fe172d538d')
var acrPushRole = resourceId('Microsoft.Authorization/roleDefinitions','8311e382-0749-4cb8-b61a-304f252e45ec')

resource contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(resourceGroup().id, 'contributor', id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id
    roleDefinitionId: contributorRole
    description: 'Contributor role assignment for ${id}'
  }
}
]

resource cognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(aiFoundry.id, 'foundry', id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id
    roleDefinitionId: cognitiveServicesUserRole
    description: 'Cognitive Services User role assignment for ${id}'
  }
}
]

resource searchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' =  [for id in principals:{
  name: guid(searchService.id, 'ask-search', id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id
    roleDefinitionId: searchDataContributorRole
    description: 'Search Data Contributor role assignment for ${id}'
  }
}
]


resource blobContribRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for id in principals:{
  name: guid(storageAccount.id, 'blobcontrib', id, deploymentEntropy)
  scope: resourceGroup()
  properties: {
    principalId: id
    roleDefinitionId: storageBlobDataContribRole
    description: 'Storage Blob Data Contributor role assignment for ${id}'

  }
}
]


resource cosmosAskContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = [for id in principals:{
  name: guid('cosmos-ask', id, cosmosAccountName, deploymentEntropy)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: id
    scope: cosmosAccount.id
  }
}
]

resource acrPush 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for id in principals: {
  name: guid(id, 'acrPushRole', deploymentEntropy )
  scope: resourceGroup()
  properties: {
    roleDefinitionId: acrPushRole
    principalId: id
    description: 'acrPush for ${id}'
  }
}]


resource acrPull 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for id in principals: {
  name: guid(id, 'acrPullRole', deploymentEntropy )
  scope: resourceGroup()
  properties: {
    roleDefinitionId: acrPullRole
    principalId: id
    description: 'acrPull for ${id}'
  }
}]






