targetScope = 'subscription'

@minLength(1)
@description('Primary location for all resources')
param location string
param resourceGroupName string
param storageAccountName string
param aiServicesAccountName string


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
	}
    dependsOn: [
        rg
    ]
}

output aiServicesEndpoint string = aiServices.outputs.endpoint
output location string = aiServices.outputs.location
output transcriptfile_url string = storage.outputs.transcription_url
output audiofile_url string = storage.outputs.audiofile_url
