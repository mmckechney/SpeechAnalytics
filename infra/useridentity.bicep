param userIdGuid string
param resourceGroupName string = resourceGroup().name
param cosmosAccountName string

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroupName, 'KeyVaultSecretsUser', userIdGuid)
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: userIdGuid
    principalType: 'User'


  }
}

resource keyVaultSecretsOfficer 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroupName, 'keyVaultSecretsOfficer', userIdGuid)
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
    principalId: userIdGuid
    principalType: 'User'


  }
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroupName, 'storageBlobDataContributor', userIdGuid)
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: userIdGuid
    principalType: 'User'

  }
}


resource contributor 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroupName, 'Contributor', userIdGuid)
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: userIdGuid
    principalType: 'User'

  }
}

