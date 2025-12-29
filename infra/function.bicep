param functionAppName string
param openAIChatModel string = 'gpt-4-32k'
param openAIChatDeploymentName string = 'gpt-4-32k'
param openAIEmbeddingModel string = 'text-embedding-ada-002'
param openAIEmbeddingDeploymentName string = 'text-embedding-ada-002'
param location string = resourceGroup().location
param storageAccountName string
param aiServicesAccountName string
param aiServicesApiVersion string = 'v3.2-preview.1'
param cosmosDbName string
param cosmosDbContainerName string
param storageBlobServiceUri string
param storageQueueServiceUri string
param storageTableServiceUri string
param storageFileServiceUri string
param audioContainerUri string
param transcriptionContainerUri string
param cosmosAccountEndpoint string
param aiSearchEndpoint string
param openAiEndpoint string


resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: aiServicesAccountName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
}

resource appServicePlan 'Microsoft.Web/serverfarms@2021-01-15' = {
  name: '${functionAppName}-asp'
  location: location
  kind: 'app'
  sku:{
    name: 'Y1'
    tier: 'Dynamic'
  }
}


resource functionAppConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  name : 'web'
  kind: 'string'
  parent: functionApp
  properties: {
    cors: {
      allowedOrigins: [
        'https://portal.azure.com'
        'https://ms.portal.azure.com'
      ]
      supportCredentials: true
    }
  }
  
}
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'AzureWebJobsStorage__blobServiceUri'
          value: storageBlobServiceUri
        }
        {
          name: 'AzureWebJobsStorage__queueServiceUri'
          value: storageQueueServiceUri
        }
        {
          name: 'AzureWebJobsStorage__fileServiceUri'
          value: storageFileServiceUri
        }
        {
          name: 'AzureWebJobsStorage__tableServiceUri'
          value: storageTableServiceUri
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'AzureOpenAi:ChatModel'
          value: openAIChatModel
        }
        {
          name: 'AzureOpenAi:ChatDeploymentName'
          value: openAIChatDeploymentName
        }
        {
          name: 'AzureOpenAi:EmbeddingModel'
          value: openAIEmbeddingModel
        }
        {
          name: 'AzureOpenAi:EmbeddingDeploymentName'
          value: openAIEmbeddingDeploymentName
        }
        {
          name: 'AzureOpenAi:EndPoint'
          value: openAiEndpoint
        }
        {
          name : 'Storage:TargetContainerUrl'
          value: transcriptionContainerUri
        } 
        {
          name : 'Storage:SourceContainerUrl'
          value: audioContainerUri
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AiServices:Endpoint'
          value: aiServices.properties.endpoint
        }
        {
          name: 'AiServices:Region'
          value: aiServices.location
        }
        {
          name: 'AiServices:ApiVersion'
          value: aiServicesApiVersion
        }
        {
          name: 'CosmosDb:DatabaseName'
          value: cosmosDbName
        }
        {
          name: 'CosmosDb:ContainerName'
          value: cosmosDbContainerName
        }
        {
          name: 'CosmosDb:AccountEndpoint'
          value: cosmosAccountEndpoint
        }
        {
          name: 'CosmosDb:TenantId'
          value: subscription().tenantId
        }
        {
          name: 'AiSearch:Endpoint'
          value: aiSearchEndpoint
        }
      ]
    }
  }
}
resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: '${functionAppName}-insights'
  location: location
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
  tags: {
    // circular dependency means we can't reference functionApp directly  /subscriptions/<subscriptionId>/resourceGroups/<rg-name>/providers/Microsoft.Web/sites/<appName>"
     'hidden-link:/subscriptions/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/sites/${functionAppName}': 'Resource'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${functionAppName}-log'
  location: location
  properties: {
    retentionInDays: 30
  }
}






output functionPrincipalId string = functionApp.identity.principalId
