targetScope = 'subscription'

@minLength(1)
@description('Primary location for all resources')
param location string
param resourceGroupName string
param storageAccountName string
param aiServicesAccountName string
param functionAppName string
param cosmosAccountName string
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
        
	}
    dependsOn: [
        rg
    ]
}

module aiFoundry 'aifoundryresource.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'aifoundry'
    params: {
        location: location
        aiFoundryName: aiServicesAccountName
    }
    dependsOn: [
        rg
    ]
}
module aiSearch 'aisearch.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'aisearch'
    params: {
        location: location
        aiSearchName:aiSearchName
        
    }
    dependsOn: [
        rg
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
	}
    dependsOn: [
        rg
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
    }
    dependsOn: [
        rg
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
        storageAccountName: storageAccountName
        storageBlobServiceUri: storage.outputs.blobServiceUri
        storageQueueServiceUri: storage.outputs.queueServiceUri
        storageTableServiceUri: storage.outputs.tableServiceUri
        storageFileServiceUri: storage.outputs.fileServiceUri
        audioContainerUri: storage.outputs.audioContainerUri
        transcriptionContainerUri: storage.outputs.transcriptionContainerUri
        cosmosAccountEndpoint: cosmosDB.outputs.cosmosAccountEndpoint
        aiSearchEndpoint: aiSearch.outputs.aiSearchEndpoint
        openAiEndpoint: aiFoundry.outputs.aiFoundryProjectEndpoint

    }
    dependsOn: [
        rg
    ]
}

module permissions 'permissions.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'permissions'
    params: {
        aiServicesAccountName: aiServicesAccountName
        aiSearchName: aiSearchName
        cosmosAccountName: cosmosDB.outputs.cosmosAccountName
        functionPrincipalId: function.outputs.functionPrincipalId
        userPrincipalId: userIdGuid
    }
    dependsOn: [
        aiServices
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
output storageBlobServiceUri string = storage.outputs.blobServiceUri
output storageQueueServiceUri string = storage.outputs.queueServiceUri
output storageTableServiceUri string = storage.outputs.tableServiceUri
output storageFileServiceUri string = storage.outputs.fileServiceUri
output audioContainerUri string = storage.outputs.audioContainerUri
output transcriptionContainerUri string = storage.outputs.transcriptionContainerUri
output aiSearchEndpoint string = aiSearch.outputs.aiSearchEndpoint
output functionPrincipalId string = function.outputs.functionPrincipalId

