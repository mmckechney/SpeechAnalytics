param storageAccountName string 
param location string = resourceGroup().location
param sasStartDate string =  utcNow('u')


resource storageAccountResource  'Microsoft.Storage/storageAccounts@2023-01-01' = {
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
    allowSharedKeyAccess: true
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
  parent: storageAccountResource
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

resource storageContainer_incoming 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
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

var incoming_sas = listServiceSas(storageAccountResource.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccountResource.name}/${storageContainer_incoming.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'racwdl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken


var transcription_sas = listServiceSas(storageAccountResource.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccountResource.name}/${storageContainer_transcription.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'racwdl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken

output audiofile_url string = '${storageAccountResource.properties.primaryEndpoints.blob}${storageContainer_incoming.name}?${incoming_sas}'
output audiofile_container string = storageContainer_incoming.name
output transcription_url string = '${storageAccountResource.properties.primaryEndpoints.blob}${storageContainer_transcription.name}?${transcription_sas}'
