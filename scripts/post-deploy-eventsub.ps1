
Write-Host "Creating/Updating Event Grid subscription for storage -> transcription app..." -ForegroundColor Cyan

# Fetch azd environment values
$envJson = azd env get-values --output json | ConvertFrom-Json

# Helper to resolve keys in both camelCase and UPPER_SNAKE
function Resolve-EnvValue {
  param(
    [Parameter(Mandatory=$true)][string]$Key,
    [Parameter()][string[]]$Fallbacks
  )
  $allKeys = @($Key) + ($Fallbacks | Where-Object { $_ })
  foreach ($k in $allKeys) {
    if ($envJson.PSObject.Properties.Name -contains $k) { return $envJson.$k }
    $upper = $k.ToUpperInvariant()
    if ($envJson.PSObject.Properties.Name -contains $upper) { return $envJson.$upper }
  }
  return $null
}

$subscriptionId          = Resolve-EnvValue -Key 'subscriptionId' -Fallbacks 'AZURE_SUBSCRIPTION_ID'
$resourceGroupName       = Resolve-EnvValue -Key 'resourceGroupName' -Fallbacks 'AZURE_RESOURCE_GROUP'
$location                = Resolve-EnvValue -Key 'location' -Fallbacks 'AZURE_LOCATION','LOCATION'
$storageAccountName      = Resolve-EnvValue -Key 'storageAccountName' -Fallbacks 'AZURE_STORAGE_ACCOUNT'
$audioContainerName      = Resolve-EnvValue -Key 'audioContainerName' -Fallbacks 'AUDIO_CONTAINER_NAME'
$transcriptionFqdn       = Resolve-EnvValue -Key 'transcriptionAppFqdn' -Fallbacks 'TRANSCRIPTIONAPPFQDN'
$EventSubscriptionName = 'transcriptionApp'
$SystemTopicName = $storageAccountName + '-events'

# Debug: print resolved values
Write-Host "subscriptionId: $subscriptionId" -ForegroundColor DarkGray
Write-Host "resourceGroupName: $resourceGroupName" -ForegroundColor DarkGray
Write-Host "location: $location" -ForegroundColor DarkGray
Write-Host "storageAccountName: $storageAccountName" -ForegroundColor DarkGray
Write-Host "audioContainerName: $audioContainerName" -ForegroundColor DarkGray
Write-Host "transcriptionFqdn: $transcriptionFqdn" -ForegroundColor DarkGray
Write-Host "EventSubscriptionName: $EventSubscriptionName" -ForegroundColor DarkGray
Write-Host "SystemTopicName: $SystemTopicName" -ForegroundColor DarkGray

if (-not $subscriptionId -or -not $resourceGroupName -or -not $storageAccountName -or -not $audioContainerName -or -not $transcriptionFqdn) {
  throw "Missing required environment values. Ensure azd outputs include storageAccountName, audioContainerName, transcriptionContainerFqdn, resourceGroupName, subscriptionId."
}

$SystemTopicName       = if ($SystemTopicName) { $SystemTopicName } else { "$storageAccountName-events" }
$EventSubscriptionName = if ($EventSubscriptionName) { $EventSubscriptionName } else { 'transcriptionApp' }

$storageResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Storage/storageAccounts/$storageAccountName"
$endpointUrl       = "https://$transcriptionFqdn/events"
$subjectPrefix     = "/blobServices/default/containers/$audioContainerName"

Write-Host "Subscription: $subscriptionId" -ForegroundColor DarkGray
Write-Host "Resource Group: $resourceGroupName" -ForegroundColor DarkGray
Write-Host "Storage Account: $storageAccountName" -ForegroundColor DarkGray
Write-Host "System Topic: $SystemTopicName" -ForegroundColor DarkGray
Write-Host "Event Sub: $EventSubscriptionName" -ForegroundColor DarkGray
Write-Host "Endpoint: $endpointUrl" -ForegroundColor DarkGray
Write-Host "Filter: $subjectPrefix" -ForegroundColor DarkGray

# Ensure system topic exists
az eventgrid system-topic create `
  --name $SystemTopicName `
  --location $location `
  --resource-group $resourceGroupName `
  --source $storageResourceId `
  --topic-type microsoft.storage.storageaccounts `
  --only-show-errors `
  --output none

# Create or update the subscription
$subExists = $false
try {
  az eventgrid system-topic event-subscription show `
    --name $EventSubscriptionName `
    --resource-group $resourceGroupName `
    --system-topic-name $SystemTopicName `
    --only-show-errors `
    --output none
  $subExists = $true
} catch {
  $subExists = $false
}

if ($subExists) {
  Write-Host "Updating event subscription $EventSubscriptionName..." -ForegroundColor Yellow
} else {
  Write-Host "Creating event subscription $EventSubscriptionName..." -ForegroundColor Yellow
}

az eventgrid system-topic event-subscription create `
  --name $EventSubscriptionName `
  --resource-group $resourceGroupName `
  --system-topic-name $SystemTopicName `
  --endpoint $endpointUrl `
  --included-event-types Microsoft.Storage.BlobCreated `
  --subject-begins-with $subjectPrefix `
  --max-events-per-batch 1 `
  --preferred-batch-size-in-kilobytes 64 `
  --max-delivery-attempts 30 `
  --event-ttl 1440 `
  --only-show-errors `
  --output none

Write-Host "Event subscription ensured." -ForegroundColor Green
