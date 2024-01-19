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
        storageAccountName: storageAccountName
        cosmosAccountName: cosmosDB.outputs.cosmosAccountName
        aiServicesAccountName: aiServicesAccountName
        cosmosDbContainerName: cosmosDB.outputs.cosmosContainerName
        cosmosDbName: cosmosDB.outputs.cosmosDataBaseName
        functionAppName: functionAppName
        openAiKey: azureOpenAiKey
        openEndpoint:azureOpenAiEndpoint
        sourceContainerSas: storage.outputs.audiofile_url
        targetContainerSas: storage.outputs.transcription_url
    }
    dependsOn: [
        rg
        aiServices
    ]
}

output aiServicesEndpoint string = aiServices.outputs.endpoint
output location string = aiServices.outputs.location
output transcriptfile_url string = storage.outputs.transcription_url
output audiofile_url string = storage.outputs.audiofile_url
output cosmosEndpoint string = cosmosDB.outputs.cosmosAccountEndpoint
output cosmosAccountName string = cosmosDB.outputs.cosmosAccountName
output cosmosContainerName string = cosmosDB.outputs.cosmosContainerName
output cosmosDataBaseName string = cosmosDB.outputs.cosmosDataBaseName

