param storageAccountName string 
param location string = resourceGroup().location
param logAnalyticsWorkspaceResourceId string

resource storageAccount  'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      requireInfrastructureEncryption: false
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource storageAccount_blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    changeFeed: {
      enabled: false
    }
    restorePolicy: {
      enabled: false
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: false
      enabled: true
      days: 7
    }
    isVersioningEnabled: false
  }
}

resource storageContainer_audio 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: storageAccount_blobService
  name: 'audio'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageContainer_transcription 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: storageAccount_blobService
  name: 'transcription'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
 
}

  resource storageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
    name: 'storage-blob-logs'
    scope: storageAccount_blobService
    properties: {
      workspaceId: logAnalyticsWorkspaceResourceId
      logs: [
        {
          category: 'StorageRead'
          enabled: true
          retentionPolicy: {
            enabled: false
            days: 0
          }
        }
        {
          category: 'StorageWrite'
          enabled: true
          retentionPolicy: {
            enabled: false
            days: 0
          }
        }
        {
          category: 'StorageDelete'
          enabled: true
          retentionPolicy: {
            enabled: false
            days: 0
          }
        }
      ]
      metrics: [
        {
          category: 'Transaction'
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
output audiofile_container string = storageContainer_audio.name
output blobServiceUri string = 'https://${storageAccountName}.blob.${environment().suffixes.storage}'
output queueServiceUri string = 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
output tableServiceUri string = 'https://${storageAccountName}.table.${environment().suffixes.storage}'
output fileServiceUri string = 'https://${storageAccountName}.file.${environment().suffixes.storage}'
output audioContainerUri string = 'https://${storageAccountName}.blob.${environment().suffixes.storage}/${storageContainer_audio.name}'
output transcriptionContainerUri string = 'https://${storageAccountName}.blob.${environment().suffixes.storage}/${storageContainer_transcription.name}'
output audioContainerName string = storageContainer_audio.name
output transcriptContainerName string = storageContainer_transcription.name
