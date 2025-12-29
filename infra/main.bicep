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
param openAIChatModel string = 'gpt-4-32k'
param openAIChatDeploymentName string = 'gpt-4-32k'
param openAIEmbeddingModel string = 'text-embedding-ada-002'
param openAIEmbeddingDeploymentName string = 'text-embedding-ada-002'
param aiServicesApiVersion string = 'v3.2-preview.1'
param askContainerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param transcriptionContainerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param containerRegistryServer string = ''
param containerRegistryUsername string = ''
@secure()
param containerRegistryPassword string = ''


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

var containerEnvironmentName = '${functionAppName}-env'
var logAnalyticsWorkspaceName = '${functionAppName}-log'
var askContainerAppName = '${functionAppName}-ask'
var transcriptionContainerAppName = '${functionAppName}-transcription'
module roleassignment 'roleassignment.bicep' = {
	scope: resourceGroup(resourceGroupName)
	name: 'roleassignment'
	params: {
        storageAccountName: storageAccountName
        aiServicesPrincipal: aiServices.outputs.aiServicesIdentityPrincipal
        blobContainerName: storage.outputs.audiofile_container
        askPrincipal: containerApps.outputs.askPrincipalId
        transcriptionPrincipal: containerApps.outputs.transcriptionPrincipalId
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

module containerApps 'containerapps.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'containerapps'
    params: {
        location: location
        environmentName: containerEnvironmentName
        logAnalyticsName: logAnalyticsWorkspaceName
        askAppName: askContainerAppName
        transcriptionAppName: transcriptionContainerAppName
        containerRegistryServer: containerRegistryServer
        containerRegistryUsername: containerRegistryUsername
        containerRegistryPassword: containerRegistryPassword
        askImage: askContainerImage
        transcriptionImage: transcriptionContainerImage
        aiServicesEndpoint: aiServices.outputs.endpoint
        aiServicesRegion: aiServices.outputs.location
        aiServicesApiVersion: aiServicesApiVersion
        aiSearchEndpoint: aiSearch.outputs.aiSearchEndpoint
        storageSourceContainerUrl: storage.outputs.audioContainerUri
        storageTargetContainerUrl: storage.outputs.transcriptionContainerUri
        cosmosAccountEndpoint: cosmosDB.outputs.cosmosAccountEndpoint
        cosmosDatabaseName: cosmosDB.outputs.cosmosDataBaseName
        cosmosContainerName: cosmosDB.outputs.cosmosContainerName
        openAiEndpoint: aiFoundry.outputs.aiFoundryProjectEndpoint
        openAiChatModel: openAIChatModel
        openAiChatDeploymentName: openAIChatDeploymentName
        openAiEmbeddingModel: openAIEmbeddingModel
        openAiEmbeddingDeploymentName: openAIEmbeddingDeploymentName
    }
    dependsOn: [
        storage
        aiServices
        aiSearch
        cosmosDB
        aiFoundry
    ]
}

module permissions 'permissions.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'permissions'
    params: {
        aiServicesAccountName: aiServicesAccountName
        aiSearchName: aiSearchName
        cosmosAccountName: cosmosDB.outputs.cosmosAccountName
        askPrincipalId: containerApps.outputs.askPrincipalId
        transcriptionPrincipalId: containerApps.outputs.transcriptionPrincipalId
        userPrincipalId: userIdGuid
    }
    dependsOn: [
        aiServices
        containerApps
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
output askContainerFqdn string = containerApps.outputs.askFqdn
output transcriptionContainerFqdn string = containerApps.outputs.transcriptionFqdn

