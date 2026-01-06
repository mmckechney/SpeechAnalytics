param location string = resourceGroup().location
param environmentName string
param logAnalyticsName string
param askAppName string
param transcriptionAppName string
param containerRegistryServer string = ''
@secure()
param askImage string
param transcriptionImage string
param foundryResourceId string
param foundryResourceName string
param aiSearchEndpoint string
param storageSourceContainerUrl string
param storageTargetContainerUrl string
param cosmosAccountEndpoint string
param cosmosDatabaseName string
param cosmosContainerName string
param managedIdentityResourceId string
param mangedIdentityClientId string
@secure()
param appInsightsConnectionString string
@description('Use placeholder image for initial deployment before images are pushed to ACR')
param usePlaceholderImage bool = false 
param foundryProjectEndpoint string
param chatModelDeploymentName string
param embeddingModeDeploymentName string

var insightsAgentName = 'speechanalytics-insights'
var speakerAgentName = 'speechanalytics-speaker-id'
var queryAgentName = 'speechanalytics-cosmos-query'
var answerAgentName = 'speechanalytics-qna'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsName

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

resource managedEnvDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'managedenv-logs'
  scope: managedEnv
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'ContainerAppConsoleLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'ContainerAppSystemLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        timeGrain: 'PT1M'
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}


var commonEnvs = [
  {
    name: 'AZURE_CLIENT_ID'
    value: mangedIdentityClientId
  }
  {
    name: 'FoundryAgent__ProjectEndpoint'
    value: foundryProjectEndpoint
  }
    {
    name: 'FoundryAgent__ModelDeploymentName'
    value: chatModelDeploymentName
  }
  {
    name: 'FoundryAgent__EmbeddingDeploymentName'
    value: embeddingModeDeploymentName
  }
  {
    name: 'FoundryAgent__InsightsAgentName'
    value: insightsAgentName
  }
  {
    name: 'FoundryAgent__SpeakerAgentName'
    value: speakerAgentName
  }
  {
    name: 'FoundryAgent__QueryAgentName'
    value: queryAgentName
  }
  {
    name: 'FoundryAgent__AnswerAgentName'
    value: answerAgentName
  }
  {
    name: 'FoundryAgent__ResourceId'
    value: foundryResourceId
  }
  {
    name: 'FoundryAgent__ResourceName'
    value: foundryResourceName
  }
  {
    name: 'FoundryAgent__Region'
    value: location
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
  {
    name: 'APPINSIGHTS_CONNECTIONSTRING'
    value: appInsightsConnectionString
  }
  
]

resource askInsightsApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: askAppName
  location: location
   tags: {
    'azd-service-name': 'askinsights'
    'workload-role': 'askinsights'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
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
          identity: managedIdentityResourceId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'askinsights'
          image: usePlaceholderImage ? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest' : '${containerRegistryServer}/${askImage}'
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

resource askInsightsDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'askinsights-metrics'
  scope: askInsightsApp
  properties: {
    workspaceId: logAnalytics.id
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        timeGrain: 'PT1M'
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource transcriptionApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: transcriptionAppName
  location: location
  tags: {
    'azd-service-name': 'transcription'
    'workload-role': 'transcription'
  }
   identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
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
          identity: managedIdentityResourceId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'transcription' 
          image: usePlaceholderImage ? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest' : '${containerRegistryServer}/${transcriptionImage}'
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

resource transcriptionDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'transcription-metrics'
  scope: transcriptionApp
  properties: {
    workspaceId: logAnalytics.id
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        timeGrain: 'PT1M'
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// output askPrincipalId string = askInsightsApp.identity.principalId
// output transcriptionPrincipalId string = transcriptionApp.identity.principalId
output transcriptionFqdn string = transcriptionApp.properties.configuration.ingress.fqdn
output askFqdn string = askInsightsApp.properties.configuration.ingress.fqdn

output insightsAgentNameOutput string = insightsAgentName
output speakerAgentNameOutput string = speakerAgentName 
output queryAgentNameOutput string = queryAgentName
output answerAgentNameOutput string = answerAgentName
