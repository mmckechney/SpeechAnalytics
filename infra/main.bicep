targetScope = 'subscription'

@minLength(1)
@description('Primary location for all resources')
param location string
param resourceGroupName string
param storageAccountName string
param foundryProjectName string
param containerAppName string
param cosmosAccountName string
param aiSearchName string
param containerRegistryName string
param userIdGuid string
param chatDeploymentName string
param embeddingDeploymentName string
param askContainerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param transcriptionContainerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'


var containerEnvironmentName = '${containerAppName}-env'
var logAnalyticsWorkspaceName = '${containerAppName}-log'
var askContainerAppName = '${containerAppName}-ask'
var transcriptionContainerAppName = '${containerAppName}-transcription'
var aiFoundryResourceName = '${foundryProjectName}-resource'

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

module aiFoundry 'aifoundryresource.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'aifoundry'
    params: {
        location: location
        aiFoundryName: foundryProjectName
        aiFoundryResourceName: aiFoundryResourceName
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

module containerRegistry 'containerregistry.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'containerregistry'
    params: {
        location: location
        registryName: containerRegistryName
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
        containerRegistryServer: containerRegistry.outputs.loginServer
        askImage: askContainerImage
        transcriptionImage: transcriptionContainerImage
        aiSpeechServicesEndpoint: aiFoundry.outputs.aiSpeechToTextStandardEndpoint
        aiSearchEndpoint: aiSearch.outputs.aiSearchEndpoint
        storageSourceContainerUrl: storage.outputs.audioContainerUri
        storageTargetContainerUrl: storage.outputs.transcriptionContainerUri
        cosmosAccountEndpoint: cosmosDB.outputs.cosmosAccountEndpoint
        cosmosDatabaseName: cosmosDB.outputs.cosmosDataBaseName
        cosmosContainerName: cosmosDB.outputs.cosmosContainerName
        openAiEndpoint: aiFoundry.outputs.aiFoundryProjectEndpoint
        chatDeploymentName: chatDeploymentName
        embeddingDeploymentName: embeddingDeploymentName
    }
}

module permissions 'permissions.bicep' = {
    scope: resourceGroup(resourceGroupName)
    name: 'permissions'
    params: {
        aiFoundryResourceName: aiFoundryResourceName   
        aiFoundryPrincipal: aiFoundry.outputs.aiFoundryPrincipalId
        blobContainerName: storage.outputs.audiofile_container
        storageAccountName: storageAccountName
        aiSearchName: aiSearchName
        cosmosAccountName: cosmosDB.outputs.cosmosAccountName
        askPrincipalId: containerApps.outputs.askPrincipalId
        transcriptionPrincipalId: containerApps.outputs.transcriptionPrincipalId
        userPrincipalId: userIdGuid
    }
    dependsOn: [
        rg
    ]
}

output aiServicesEndpoint string = aiFoundry.outputs.aiSpeechToTextEndpoint
output location string = location
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

