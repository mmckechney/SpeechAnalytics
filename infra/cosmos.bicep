param cosmosAccountName string
param cosmosDataBaseName string
param cosmosContainerName string
param location string = resourceGroup().location
param logAnalyticsWorkspaceResourceId string
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
   name:cosmosAccountName
   location: location
   kind: 'GlobalDocumentDB'
   properties:{
     databaseAccountOfferType: 'Standard'
     locations:[{
       locationName: location
       failoverPriority: 0
     }]
     capabilities: [
         {
            name: 'EnableServerless'
         }
      ]
   }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  name: cosmosDataBaseName
  parent: cosmosAccount
  properties: {
      resource: {
         id: cosmosDataBaseName
      }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  name: cosmosContainerName
  parent: cosmosDatabase
  properties: {
      resource: {
         id: cosmosContainerName
         partitionKey: {
            paths: [
               '/id'
            ]
            kind: 'Hash'
         }
         indexingPolicy: {
            automatic: true
            indexingMode: 'Consistent'
            includedPaths: [
               {
                  path: '/*'
                  indexes: [
                     {
                        kind: 'Range'
                        dataType: 'Number'
                        precision: -1
                     }
                     {
                        kind: 'Range'
                        dataType: 'String'
                        precision: -1
                     }
                  ]
               }
            ]
            excludedPaths: [
               {
                  path: '/"_etag"/?'
               }
            ]
         }
         uniqueKeyPolicy: {
            uniqueKeys: [
               {
                  paths: [
                     '/CallId'
                  ]
               }
            ]
         }
      }
  }
}

resource cosmosDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
   name: 'cosmos-logs'
   scope: cosmosAccount
   properties: {
      workspaceId: logAnalyticsWorkspaceResourceId
      logs: [
         {
            category: 'DataPlaneRequests'
            enabled: true
            retentionPolicy: {
               enabled: false
               days: 0
            }
         }
         {
            category: 'ControlPlaneRequests'
            enabled: true
            retentionPolicy: {
               enabled: false
               days: 0
            }
         }
         {
            category: 'QueryRuntimeStatistics'
            enabled: true
            retentionPolicy: {
               enabled: false
               days: 0
            }
         }
         {
            category: 'PartitionKeyRUConsumption'
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

output cosmosAccountEndpoint string = cosmosAccount.properties.documentEndpoint
output cosmosAccountName string = cosmosAccount.name
output cosmosContainerName string = cosmosContainer.name
output cosmosDataBaseName string = cosmosDatabase.name
output cosmosResourceId string = cosmosAccount.id
