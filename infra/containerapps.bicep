param location string = resourceGroup().location
param environmentName string
param logAnalyticsName string
param askAppName string
param transcriptionAppName string
param containerRegistryServer string = ''
param containerRegistryUsername string = ''
@secure()
param containerRegistryPassword string = ''
param askImage string
param transcriptionImage string
param aiServicesEndpoint string
param aiServicesRegion string
param aiServicesApiVersion string
param aiSearchEndpoint string
param storageSourceContainerUrl string
param storageTargetContainerUrl string
param cosmosAccountEndpoint string
param cosmosDatabaseName string
param cosmosContainerName string
param openAiEndpoint string
param openAiChatModel string
param openAiChatDeploymentName string
param openAiEmbeddingModel string
param openAiEmbeddingDeploymentName string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    retentionInDays: 30
  }
}

var logAnalyticsSharedKeys = listKeys(logAnalytics.id, '2020-08-01')

resource managedEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalyticsSharedKeys.primarySharedKey
      }
    }
  }
}

var registrySecrets = empty(containerRegistryServer) ? [] : [
  {
    name: 'registry-password'
    value: containerRegistryPassword
  }
]

var registries = empty(containerRegistryServer) ? [] : [
  {
    server: containerRegistryServer
    username: containerRegistryUsername
    passwordSecretRef: 'registry-password'
  }
]

var commonEnvs = [
  {
    name: 'AzureOpenAi__EndPoint'
    value: openAiEndpoint
  }
  {
    name: 'AzureOpenAi__ChatModel'
    value: openAiChatModel
  }
  {
    name: 'AzureOpenAi__ChatDeploymentName'
    value: openAiChatDeploymentName
  }
  {
    name: 'AzureOpenAi__EmbeddingModel'
    value: openAiEmbeddingModel
  }
  {
    name: 'AzureOpenAi__EmbeddingDeploymentName'
    value: openAiEmbeddingDeploymentName
  }
  {
    name: 'AiServices__Endpoint'
    value: aiServicesEndpoint
  }
  {
    name: 'AiServices__Region'
    value: aiServicesRegion
  }
  {
    name: 'AiServices__ApiVersion'
    value: aiServicesApiVersion
  }
  {
    name: 'AiSearch__Endpoint'
    value: aiSearchEndpoint
  }
  {
    name: 'Storage__SourceContainerUrl'
    value: storageSourceContainerUrl
  }
  {
    name: 'Storage__TargetContainerUrl'
    value: storageTargetContainerUrl
  }
  {
    name: 'CosmosDb__AccountEndpoint'
    value: cosmosAccountEndpoint
  }
  {
    name: 'CosmosDb__DatabaseName'
    value: cosmosDatabaseName
  }
  {
    name: 'CosmosDb__ContainerName'
    value: cosmosContainerName
  }
  {
    name: 'CosmosDb__TenantId'
    value: tenant().tenantId
  }
]

resource askInsightsApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: askAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: registries
      secrets: registrySecrets
    }
    template: {
      containers: [
        {
          name: 'askinsights'
          image: askImage
          env: commonEnvs
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource transcriptionApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: transcriptionAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: registries
      secrets: registrySecrets
    }
    template: {
      containers: [
        {
          name: 'transcription'
          image: transcriptionImage
          env: commonEnvs
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output askPrincipalId string = askInsightsApp.identity.principalId
output transcriptionPrincipalId string = transcriptionApp.identity.principalId
output transcriptionFqdn string = reference(transcriptionApp.id, '2024-03-01', 'Full').properties.configuration.ingress.fqdn
output askFqdn string = reference(askInsightsApp.id, '2024-03-01', 'Full').properties.configuration.ingress.fqdn
