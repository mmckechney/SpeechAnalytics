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

@description('Deploy Aspire dashboard apps')
param enableAspireDashboard bool = true

var insightsAgentName = 'speechanalytics-insights'
var speakerAgentName = 'speechanalytics-speaker-id'
var queryAgentName = 'speechanalytics-cosmos-query'
var answerAgentName = 'speechanalytics-qna'
var aspireDashboardAppName = 'speechanalytics-aspire'
var aspireDashboardOtlpAppName = 'speechanalytics-aspire-otlp'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsName

}
resource managedEnv 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
        dynamicJsonColumns : true
      }
    }
     appInsightsConfiguration: {
      connectionString: appInsightsConnectionString
    }
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: [
          'appInsights'
        ]
      }
      logsConfiguration: {
        destinations: [
          'appInsights'
        ]
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
        categoryGroup: 'allLogs'
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
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
]

var allEnvs = concat(commonEnvs)

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
          env: allEnvs          
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
           env: allEnvs          
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

// Aspire Dashboard UI (external)
resource aspireDashboard 'Microsoft.App/containerApps@2024-03-01' = if (enableAspireDashboard) {
  name: aspireDashboardAppName
  location: location
  tags: {
    'azd-service-name': 'aspire-dashboard'
    'workload-role': 'aspire-dashboard'
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 18888
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'aspire-dashboard'
          image: 'mcr.microsoft.com/dotnet/aspire-dashboard:latest'
          env: [
            {
              name: 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS_REMOTE_ACCESS'
              value: 'true'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:18888'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// Aspire Dashboard OTLP ingress (internal)
resource aspireDashboardOtlp 'Microsoft.App/containerApps@2024-03-01' = if (enableAspireDashboard) {
  name: aspireDashboardOtlpAppName
  location: location
  tags: {
    'azd-service-name': 'aspire-dashboard-otlp'
    'workload-role': 'aspire-dashboard-otlp'
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 18888
        transport: 'http2'
      }
    }
    template: {
      containers: [
        {
          name: 'aspire-dashboard-otlp'
          image: 'mcr.microsoft.com/dotnet/aspire-dashboard:latest'
          env: [
            {
              name: 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS_REMOTE_ACCESS'
              value: 'true'
            }
            {
              name: 'DOTNET_DASHBOARD_OTLP__ENABLED'
              value: 'true'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:18888'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// Portal-visible Aspire Dashboard component (creates dashboard managed component inside ACA env)
resource aspireDashboardComponent 'Microsoft.App/managedEnvironments/dotNetComponents@2025-10-02-preview' = if (enableAspireDashboard) {
  name: 'aspire-dashboard'
  parent: managedEnv
  properties: {
    componentType: 'AspireDashboard'
    // serviceBinds: [] // not required for dashboard
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

// Aspire dashboard outputs
@description('Aspire dashboard UI FQDN')
output aspireDashboardFqdn string = enableAspireDashboard ? aspireDashboard.properties.configuration.ingress.fqdn : ''
@description('Aspire dashboard OTLP ingress FQDN')
output aspireDashboardOtlpFqdn string = enableAspireDashboard ? aspireDashboardOtlp.properties.configuration.ingress.fqdn : ''
