param cosmosAccountName string
param cosmosDataBaseName string
param cosmosContainerName string
param location string = resourceGroup().location

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
   name:cosmosAccountName
   location: location
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

output cosmosAccountEndpoint string = cosmosAccount.properties.documentEndpoint
output cosmosAccountName string = cosmosAccount.name
output cosmosContainerName string = cosmosContainer.name
output cosmosDataBaseName string = cosmosDatabase.name
