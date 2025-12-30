param location string
param storageAccountName string
param audioContainerName string
param transcriptionEndpoint string
@description('Optional override for the system topic name. Defaults to <storageAccountName>-events.')
param systemTopicName string = '${storageAccountName}-events'
@description('Optional override for the event subscription name. Defaults to transcriptionApp.')
param eventSubscriptionName string = 'transcriptionApp'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource storageSystemTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: systemTopicName
  location: location
  properties: {
    source: storageAccount.id
    topicType: 'microsoft.storage.storageaccounts'
  }
}

var endpointUrl = 'https://${transcriptionEndpoint}/events'
var containerSubjectPrefix = '/blobServices/default/containers/${audioContainerName}'

resource blobCreatedSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = {
  name: '${systemTopicName}/${eventSubscriptionName}'
  properties: {
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: endpointUrl
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
      subjectBeginsWith: containerSubjectPrefix
    }
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
  dependsOn: [
    storageSystemTopic
  ]
}

output systemTopicId string = storageSystemTopic.id
output eventSubscriptionId string = blobCreatedSubscription.id
output webhookEndpoint string = endpointUrl
