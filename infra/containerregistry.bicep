param registryName string
param location string = resourceGroup().location
param logAnalyticsWorkspaceResourceId string
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param registrySku string = 'Basic'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: registrySku
  }
  properties: {
    adminUserEnabled: false
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
    }
  }
}

resource containerRegistryDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'acr-logs'
  scope: containerRegistry
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        category: 'ContainerRegistryRepositoryEvents'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'ContainerRegistryLoginEvents'
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

output id string = containerRegistry.id
output name string = containerRegistry.name
output loginServer string = containerRegistry.properties.loginServer
