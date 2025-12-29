param aiServicesAccountName string
param aiSearchName string
param cosmosAccountName string
param functionPrincipalId string
param userPrincipalId string

var contributorRoleDefinitionId = resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var cognitiveServicesUserRoleId = resourceId('Microsoft.Authorization/roleDefinitions', '15bb77dd-0749-4cdf-b593-16d7c3f01970')
var searchIndexDataContributorRoleId = resourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4474-99ad-5a515542ac68')

resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: aiServicesAccountName
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: aiSearchName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' existing = {
  name: cosmosAccountName
}

resource functionContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'function-contributor', functionPrincipalId)
  scope: resourceGroup()
  properties: {
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: contributorRoleDefinitionId
  }
}

resource userContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'user-contributor', userPrincipalId)
  scope: resourceGroup()
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: contributorRoleDefinitionId
  }
}

resource functionCognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, 'function-cog-user', functionPrincipalId)
  scope: aiServices
  properties: {
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: cognitiveServicesUserRoleId
  }
}

resource userCognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, 'user-cog-user', userPrincipalId)
  scope: aiServices
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: cognitiveServicesUserRoleId
  }
}

resource functionSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, 'function-search', functionPrincipalId)
  scope: searchService
  properties: {
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchIndexDataContributorRoleId
  }
}

resource userSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, 'user-search', userPrincipalId)
  scope: searchService
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: searchIndexDataContributorRoleId
  }
}

resource cosmosFunctionDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid('cosmos-func-contributor', functionPrincipalId, cosmosAccountName)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: functionPrincipalId
    scope: cosmosAccount.id
  }
}

resource cosmosUserDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid('cosmos-user-contributor', userPrincipalId, cosmosAccountName)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: userPrincipalId
    scope: cosmosAccount.id
  }
}
