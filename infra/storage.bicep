param storageAccountName string 
param location string = resourceGroup().location
param sasStartDate string =  utcNow('u')
param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

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
var audio_sas = listServiceSas(storageAccount.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccount.name}/${storageContainer_audio.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'racwdl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken

var audioSasSecretName = 'AudioSasUrl'

resource audioSas 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
        parent: keyVault
        name: audioSasSecretName
        properties: {
          value:  'https://${storageAccountName}.blob.${environment().suffixes.storage}/${storageContainer_audio.name}?${audio_sas}'
        }
}
      
var transcription_sas = listServiceSas(storageAccount.name, '2021-04-01',
      {
        canonicalizedResource: '/blob/${storageAccount.name}/${storageContainer_transcription.name}'
        signedResource: 'c'
        signedProtocol: 'https'
        signedPermission: 'racwdl'
        signedServices: 'b'
        signedExpiry: dateTimeAdd(sasStartDate, 'P1Y')
      }).serviceSasToken


var transcriptionSasSecretName = 'TranscriptionSasUrl'

resource transcriptionSas 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: transcriptionSasSecretName
  properties: {
    value:  'https://${storageAccountName}.blob.${environment().suffixes.storage}/${storageContainer_transcription.name}?${transcription_sas}'
  }
}


var storageAccountConnection = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

resource storageConnection 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'StorageAccountConnection'
  properties: {
    value:  storageAccountConnection
  }
}
output audiofile_container string = storageContainer_audio.name

output storageConnectionSecretName string = storageConnection.name
output transcriptionSecretname string = transcriptionSasSecretName
output audioSecretname string = audioSasSecretName
output audioContainerName string = storageContainer_audio.name
output transcriptContainerName string = storageContainer_transcription.name
