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

@description('Deploy Aspire dashboard container apps')
param enableAspireDashboard bool = true

param firstProvision bool = true

var containerEnvironmentName = '${containerAppName}-env'
var appInsightsName = '${containerAppName}-insights'
var logAnalyticsWorkspaceName = '${containerAppName}-log'
var askContainerAppName = '${containerAppName}-ask'
var transcriptionContainerAppName = '${containerAppName}-transcription'
var aiFoundryResourceName = '${foundryProjectName}-resource'
var managedIdentityName = '${containerAppName}-mi'
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
    name: resourceGroupName
    location: location
}

module managedIdentity 'managed-identity.bicep' = {
	name: 'managedIdentity'
	scope: rg
	params: {
		name: managedIdentityName
		location: location
	}
}

module permissionsacr 'permissions-acr.bicep' = {
    scope: rg
    name: 'permissionsacr'
    params: {
        managedIdentityPrincipalId: managedIdentity.outputs.principalId
        userPrincipalId:userIdGuid
    }
}

module storage 'storage.bicep' = {
    name: 'storage'
    scope: rg
    params: {
        location: location
        storageAccountName:storageAccountName
        logAnalyticsWorkspaceResourceId: appInsights.outputs.logAnalyticsResourceId
    }
  
}

module aiFoundry 'aifoundryresource.bicep' = {
    scope: rg
    name: 'aifoundry'
    params: {
        location: location
        aiFoundryName: foundryProjectName
        aiFoundryResourceName: aiFoundryResourceName
        chatModel: chatDeploymentName
        embeddingModel: embeddingDeploymentName
        managedIdentityResourceId: managedIdentity.outputs.id
        logAnalyticsName: appInsights.outputs.logAnalyticsWorkspaceName

    }
 }

module appInsights 'appinsights.bicep' = {
     scope: rg
     params: {
        appInsightsName: appInsightsName
        logAnalyticsName : logAnalyticsWorkspaceName
     }
   
}
module aiSearch 'aisearch.bicep' = {
    scope: rg
    name: 'aisearch'
    params: {
        location: location
        aiSearchName:aiSearchName
        logAnalyticsWorkspaceResourceId: appInsights.outputs.logAnalyticsResourceId
    }
}


module cosmosDB 'cosmos.bicep' = {
    scope: rg
    name: 'cosmos'
    params: {
        location: location
        cosmosAccountName: cosmosAccountName
        cosmosContainerName: 'analyticscontainer'
        cosmosDataBaseName: 'speechanalyticsdb'
        logAnalyticsWorkspaceResourceId: appInsights.outputs.logAnalyticsResourceId
    }
}

module containerRegistry 'containerregistry.bicep' = {
    scope: rg
    name: 'containerregistry'
    params: {
        location: location
        registryName: containerRegistryName
        logAnalyticsWorkspaceResourceId: appInsights.outputs.logAnalyticsResourceId
    }
}
module containerApps 'containerapps.bicep' = {
    scope: rg
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
        aiSearchEndpoint: aiSearch.outputs.aiSearchEndpoint
        storageSourceContainerUrl: storage.outputs.audioContainerUri
        storageTargetContainerUrl: storage.outputs.transcriptionContainerUri
        cosmosAccountEndpoint: cosmosDB.outputs.cosmosAccountEndpoint
        cosmosDatabaseName: cosmosDB.outputs.cosmosDataBaseName
        cosmosContainerName: cosmosDB.outputs.cosmosContainerName
        appInsightsConnectionString : appInsights.outputs.connectionString
        usePlaceholderImage : firstProvision
        managedIdentityResourceId: managedIdentity.outputs.id
        foundryProjectEndpoint: aiFoundry.outputs.aiFoundryProjectEndpoint
        chatModelDeploymentName: chatDeploymentName
        embeddingModeDeploymentName: embeddingDeploymentName
        mangedIdentityClientId: managedIdentity.outputs.clientId
        foundryResourceId: aiFoundry.outputs.foundryResourceId
        foundryResourceName: aiFoundry.outputs.foundryResourceName
        enableAspireDashboard: enableAspireDashboard
    }
    dependsOn: [
        permissionsacr
    ]
}

module storageEvents 'eventgrid.bicep' = {
    scope: rg
    name: 'storageEvents'
    params: {
        location: location
        storageAccountName: storageAccountName
        audioContainerName: storage.outputs.audioContainerName
        transcriptionEndpoint: containerApps.outputs.transcriptionFqdn
        firstProvision: firstProvision
    }
}

module permissions 'permissions.bicep' = {
    scope: rg
    name: 'permissions'
    params: {
        aiFoundryProjectPrincipalId: aiFoundry.outputs.aiFoundryProjectPrincipalId
        aiFoundryResourcePrincipalId: aiFoundry.outputs.aiFoundryResourcePrincipalId
        cosmosAccountName: cosmosDB.outputs.cosmosAccountName
        userPrincipalId: userIdGuid
        managedIdentityPrincipalId: managedIdentity.outputs.principalId
    }
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
output askAppFqdn string = containerApps.outputs.askFqdn
output transcriptionAppFqdn string = containerApps.outputs.transcriptionFqdn
output foundryProjectEndpoint string = aiFoundry.outputs.aiFoundryProjectEndpoint
output chatModel string = aiFoundry.outputs.chatModelName
output embeddingModel string = aiFoundry.outputs.embeddingModelName
output speechToTextEndpoint string = aiFoundry.outputs.aiSpeechToTextStandardEndpoint
output insightsAgentName string = containerApps.outputs.insightsAgentNameOutput
output speakerAgentName string = containerApps.outputs.speakerAgentNameOutput 
output queryAgentName string = containerApps.outputs.queryAgentNameOutput
output answerAgentName string = containerApps.outputs.answerAgentNameOutput
output foundryResourceId string = aiFoundry.outputs.foundryResourceId
output foundryResourceName string = aiFoundryResourceName
