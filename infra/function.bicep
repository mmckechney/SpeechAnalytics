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
param keyVaultName string
param cosmosSecretName string
param audioSecretName string
param transcriptionSecretName string
param aiServicesSecretName string
param openAiKeySecretName string
param openAiEndpointSecretName string
param storgeConnectionSecretName string
param aiSearchEndpointSecretName string
param aiSearchAdminKeySecretName string


resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: aiServicesAccountName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
}
var storageAccountConnection = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

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
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value:storageAccountConnection
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnection
        }
        {
          name: 'StorageConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${storgeConnectionSecretName})'
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
          name: 'AzureOpenAi:Key'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${openAiKeySecretName})'
        }
        {
          name: 'AzureOpenAi:EndPoint'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${openAiEndpointSecretName})'
        }
        {
          name : 'Storage:TargetContainerUrl'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${transcriptionSecretName})'
        } 
        {
          name : 'Storage:SourceContainerUrl'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${audioSecretName})'
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
          name: 'AiServices:Key'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${aiServicesSecretName})'
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
          name: 'CosmosDb:ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${cosmosSecretName})'
        }
        {
          name: 'AiSearch:Key'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${aiSearchAdminKeySecretName})'
        }
        {
          name: 'AiSearch:Endpoint'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${aiSearchEndpointSecretName})'
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
  }
  tags: {
    // circular dependency means we can't reference functionApp directly  /subscriptions/<subscriptionId>/resourceGroups/<rg-name>/providers/Microsoft.Web/sites/<appName>"
     'hidden-link:/subscriptions/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/sites/${functionAppName}': 'Resource'
  }
}





output functionPrincipalId string = functionApp.identity.principalId
