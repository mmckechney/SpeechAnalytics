param location string = resourceGroup().location
param environmentName string
param logAnalyticsName string
param askAppName string
param transcriptionAppName string
param containerRegistryServer string = ''
@secure()
param askImage string
param transcriptionImage string
param aiSpeechServicesEndpoint string
param aiSearchEndpoint string
param storageSourceContainerUrl string
param storageTargetContainerUrl string
param cosmosAccountEndpoint string
param cosmosDatabaseName string
param cosmosContainerName string
param openAiEndpoint string
param chatDeploymentName string
param embeddingDeploymentName string

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


var commonEnvs = [
  {
    name: 'AzureOpenAi__EndPoint'
    value: openAiEndpoint
  }
  {
    name: 'AzureOpenAi__ChatDeploymentName'
    value: chatDeploymentName
  }
  {
    name: 'AzureOpenAi__EmbeddingDeploymentName'
    value: embeddingDeploymentName
  }
  {
    name: 'AiServices__Endpoint'
    value: aiSpeechServicesEndpoint
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
      registries: [
        {
          server: containerRegistryServer
          identity: 'system'
        }
      ]
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
      registries:  [
        {
          server: containerRegistryServer
          identity: 'system'
        }
      ]
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
output transcriptionFqdn string = transcriptionApp.properties.configuration.ingress.fqdn
output askFqdn string = askInsightsApp.properties.configuration.ingress.fqdn
