param aiServicesAccountName string
param aiSearchName string
param cosmosAccountName string
param askPrincipalId string
param transcriptionPrincipalId string
param userPrincipalId string

resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: aiServicesAccountName
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: aiSearchName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' existing = {
  name: cosmosAccountName
}

var contributorRole = resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var cognitiveServicesUserRole = resourceId('Microsoft.Authorization/roleDefinitions', '15bb77dd-0749-4cdf-b593-16d7c3f01970')
var searchDataContributorRole = resourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4474-99ad-5a515542ac68')

resource askContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'ask-contributor', askPrincipalId)
  scope: resourceGroup()
  properties: {
    principalId: askPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: contributorRole
  }
}

resource transcriptionContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'transcription-contributor', transcriptionPrincipalId)
  scope: resourceGroup()
  properties: {
    principalId: transcriptionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: contributorRole
  }
}

resource userContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'user-contributor', userPrincipalId)
  scope: resourceGroup()
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: contributorRole
  }
}

resource askCognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, 'ask-cog-user', askPrincipalId)
  scope: aiServices
  properties: {
    principalId: askPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: cognitiveServicesUserRole
  }
}

resource transcriptionCognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, 'transcription-cog-user', transcriptionPrincipalId)
  scope: aiServices
  properties: {
    principalId: transcriptionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: cognitiveServicesUserRole
  }
}

resource userCognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, 'user-cog-user', userPrincipalId)
  scope: aiServices
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: cognitiveServicesUserRole
  }
}

resource askSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, 'ask-search', askPrincipalId)
  scope: searchService
  properties: {
    principalId: askPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchDataContributorRole
  }
}

resource transcriptionSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, 'transcription-search', transcriptionPrincipalId)
  scope: searchService
  properties: {
    principalId: transcriptionPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchDataContributorRole
  }
}

resource userSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, 'user-search', userPrincipalId)
  scope: searchService
  properties: {
    principalId: userPrincipalId
    principalType: 'User'
    roleDefinitionId: searchDataContributorRole
  }
}

resource cosmosAskContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid('cosmos-ask', askPrincipalId, cosmosAccountName)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: askPrincipalId
    scope: cosmosAccount.id
  }
}

resource cosmosTranscriptionContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid('cosmos-transcription', transcriptionPrincipalId, cosmosAccountName)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: transcriptionPrincipalId
    scope: cosmosAccount.id
  }
}

resource cosmosUserContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid('cosmos-user', userPrincipalId, cosmosAccountName)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: userPrincipalId
    scope: cosmosAccount.id
  }
}param aiServicesAccountName string
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
