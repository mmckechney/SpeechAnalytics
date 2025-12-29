#!/usr/bin/env pwsh

# Create local settings file for Console app with the environment values
# Get updated values after potentially setting them above
$envValues = azd env get-values --output json | ConvertFrom-Json


Write-Host -ForegroundColor Green "Getting AI Search account account key"
$aiSearchKey = az search admin-key show --resource-group $envValues.AZURE_RESOURCEGROUP_NAME   --service-name $envValues.AZURE_AISEARCH_NAME -o tsv --query primaryKey
$docIntelligenceKey = az cognitiveservices account keys list --name $envValues.AZURE_DOCUMENTINTELLIGENCE_ACCOUNT_NAME --resource-group $envValues.AZURE_RESOURCEGROUP_NAME  -o tsv --query key1

#Write-Host $appSettings 

$appSettingsHash = @{}
$appSettings | ForEach-Object { $appSettingsHash[$_.name] = $_.value }

$json = Get-Content 'infra/constants.json' | ConvertFrom-Json
$localSettings = @{
        "$($json.OPENAI_CHAT_MODEL_NAME)"= $envValues.chatModelName
        "$($json.OPENAI_CHAT_DEPLOYMENT_NAME)" = $envValues.chatModelName
     
        "$($json.OPENAI_EMBEDDING_MODEL_NAME)" = $envValues.embeddingModelName
        "$($json.OPENAI_EMBEDDING_DEPLOYMENT_NAME)" =  $envValues.embeddingModelName

        "$($json.DOCUMENTINTELLIGENCE_ENDPOINT)" = $envValues.docIntelEndpoint
        "$($json.DOCUMENTINTELLIGENCE_KEY)" = $docIntelligenceKey
     
        "$($json.AISEARCH_ENDPOINT)"=   $envValues.aiSearchEndpoint
        "$($json.AISEARCH_KEY)" = $aiSearchKey

        "$($json.STORAGE_ACCOUNT_BLOB_URL.Replace("__", ":"))" =  $envValues.storageBlobEndpoint
        "$($json.STORAGE_ACCOUNT_QUEUE_URL.Replace("__", ":"))" = $envValues.storageQueueEndpoint
        "$($json.STORAGE_ACCOUNT_NAME)" = $envValues.storageAccountName
        "$($json.EXTRACTED_CONTAINER_NAME)" = $envValues.extractedContainerName
        "$($json.RAW_CONTAINER_NAME)" = $envValues.rawContainerName
        "$($json.AIFOUNDRY_PROJECT_ENDPOINT)" = $envValues.aiFoundryProjectEndpoint
        #"$($json.APPLICATIONINSIGHTS_CONNECTION_STRING)" = $envValues.appInsightsConnectionString
        
        "UseOpenAIKey" = $true
}
#Write-Host $localSettings | ConvertTo-Json -Depth 10

# Ensure directory exists
$consoleDirPath = "./DocumentQuestionsConsole"
if (Test-Path $consoleDirPath) {
    # Save local settings to file
    $localSettings | ConvertTo-Json -Depth 10 | Out-File -FilePath "$consoleDirPath/local.settings.json"
    Write-Host "Created $consoleDirPath/local.settings.json for DocumentQuestionsConsole"
} else {
    Write-Warning "Directory $consoleDirPath not found. Skipping local.settings.json creation."
}
