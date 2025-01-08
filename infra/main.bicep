targetScope = 'subscription'

@minLength(1)
@description('Primary location for all resources')
param location string
param resourceGroupName string
param storageAccountName string
param aiServicesAccountName string
param functionAppName string
param azureOpenAiKey string
param azureOpenAiEndpoint string
param cosmosAccountName string
param keyVaultName string
param aiSearchName string
param userIdGuid string


resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
    name: resourceGroupName
    location: location
}

module storage 'storage.bicep' = {
    name: 'storage'
    scope: resourceGroup(resourceGroupName)
    params: {
        location: location
        storageAccountName:storageAccountName
        keyVaultName: keyVaultName
      
    }
    dependsOn: [
        rg
    ]
}

module aiServices 'aiservices.bicep' = {
	scope: resourceGroup(resourceGroupName)
	name: 'aiservices'
	params: {
        location: location
		aiServicesAccountName: aiServicesAccountName
        keyVaultName: keyvault.name
        
	}
    dependsOn: [
        rg
        keyvault
    ]
}

module aiSearch 'aisearch.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'aisearch'
    params: {
        location: location
        aiSearchName:aiSearchName
        keyVaultName: keyVaultName
        
    }
    dependsOn: [
        rg
        keyvault
    ]
}
module roleassignment 'roleassignment.bicep' = {
	scope: resourceGroup(resourceGroupName)
	name: 'roleassignment'
	params: {
        storageAccountName: storageAccountName
        aiServicesPrincipal: aiServices.outputs.aiServicesIdentityPrincipal
        blobContainerName: storage.outputs.audiofile_container
        functionAppPrincipal: function.outputs.functionPrincipalId
        keyVaultName: keyVaultName
	}
    dependsOn: [
        rg
        keyvault
    ]
}

module cosmosDB 'cosmos.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'cosmos'
    params: {
        location: location
        cosmosAccountName: cosmosAccountName
        cosmosContainerName: 'analyticscontainer'
        cosmosDataBaseName: 'speechanalyticsdb'
        keyVaultName: keyVaultName
    }
    dependsOn: [
        rg
        keyvault
    ]
}

module function 'function.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'function'
    params: {
        location: location
        aiServicesAccountName: aiServicesAccountName
        cosmosDbContainerName: cosmosDB.outputs.cosmosContainerName
        cosmosDbName: cosmosDB.outputs.cosmosDataBaseName
        functionAppName: functionAppName
        keyVaultName: keyvault.name
        aiServicesSecretName: aiServices.outputs.aiServicesSecretName
        audioSecretName: storage.outputs.audioSecretname
        transcriptionSecretName: storage.outputs.transcriptionSecretname
        cosmosSecretName: cosmosDB.outputs.cosmosSecretName
        openAiEndpointSecretName: keyvault.outputs.openAiEndpointSecretName
        openAiKeySecretName: keyvault.outputs.openAiKeySecretName
        storgeConnectionSecretName: storage.outputs.storageConnectionSecretName
        storageAccountName: storageAccountName
        aiSearchAdminKeySecretName: aiSearch.outputs.aiSearchAdminKeySecretName
        aiSearchEndpointSecretName: aiSearch.outputs.aiSearchEndpointSecretName

    }
    dependsOn: [
        rg
    ]
}

module keyvault 'keyvault.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'keyvault'
    params:{
        keyVaultName: keyVaultName
        openAiEndpoint: azureOpenAiEndpoint 
        openAiKey:azureOpenAiKey
        location: location
    }
    dependsOn: [
        rg
    ]
}

module useridentity 'useridentity.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'useridentity'
    params:{
        userIdGuid: userIdGuid
        resourceGroupName: resourceGroupName
    }
    dependsOn: [
        rg
    ]
}

output aiServicesEndpoint string = aiServices.outputs.endpoint
output location string = aiServices.outputs.location
output cosmosEndpoint string = cosmosDB.outputs.cosmosAccountEndpoint
output cosmosAccountName string = cosmosDB.outputs.cosmosAccountName
output cosmosContainerName string = cosmosDB.outputs.cosmosContainerName
output cosmosDataBaseName string = cosmosDB.outputs.cosmosDataBaseName
output cosmosResourceId string = cosmosDB.outputs.cosmosResourceId
output audioContainerName string = storage.outputs.audioContainerName
output transcriptContainerName string = storage.outputs.transcriptContainerName

