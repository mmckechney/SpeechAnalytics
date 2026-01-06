param aiSearchName string
param location string = resourceGroup().location
param logAnalyticsWorkspaceResourceId string



resource cognitiveSearchInstance 'Microsoft.Search/searchServices@2023-11-01' = {
  name: aiSearchName
  location: location
  sku: {
    name: 'basic'
  }
}

resource aiSearchDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'aisearch-logs'
  scope: cognitiveSearchInstance
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        category: 'OperationLogs'
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

output aiSearchEndpoint string = 'https://${aiSearchName}.search.windows.net'
