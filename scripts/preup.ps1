#!/usr/bin/env pwsh

# Get the current user's object ID
$currentUserObjectId = az ad signed-in-user show -o tsv --query id
if (-not $currentUserObjectId) {
    Write-Error "Failed to get current user object ID. Make sure you're logged into Azure CLI."
    exit 1
}

$envValues = azd env get-values --output json | ConvertFrom-Json
$AZURE_LOCATION = $envValues.AZURE_LOCATION
$envValues = azd env get-values --output json | ConvertFrom-Json
$envName = $envValues.AZURE_ENV_NAME
$safeEnvName = ($envName -replace '[^a-zA-Z0-9]', '').ToLower()


azd env set "AZURE_LOCATION" $AZURE_LOCATION
azd env set "AZURE_RESOURCE_GROUP" $envName-rg
azd env set "AZURE_STORAGE_ACCOUNT" "$($safeEnvName)storage"
azd env set "AZURE_AISERVICES_ACCOUNT_NAME" "$envName-aiservices"
azd env set "AZURE_FUNCTION_APP_NAME" "$envName-func"
azd env set "AZURE_AIFOUNDRY" "$envName-foundry"
azd env set "AZURE_COSMOS_ACCOUNT_NAME" "$safeEnvName-cosmos"
azd env set "AZURE_AISEARCH_NAME" "$safeEnvName-search"
azd env set "AZURE_CURRENT_USER_OBJECT_ID" $currentUserObjectId



azd env get-values

