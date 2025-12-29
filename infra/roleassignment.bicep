param storageAccountName string
param blobContainerName string
param aiServicesPrincipal string
param askPrincipal string
param transcriptionPrincipal string


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

var storageBlobDataContrib = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource ai_blob_contrib_role 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServicesPrincipal, storageBlobDataContrib, resourceGroup().id)
  scope: blobService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContrib)
    principalId: aiServicesPrincipal
  }
}

resource ask_blob_contrib_role 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(askPrincipal, storageBlobDataContrib, resourceGroup().id)
  scope: blobService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContrib)
    principalId: askPrincipal
  }
}

resource transcription_blob_contrib_role 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(transcriptionPrincipal, storageBlobDataContrib, resourceGroup().id)
  scope: blobService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContrib)
    principalId: transcriptionPrincipal
  }
}
