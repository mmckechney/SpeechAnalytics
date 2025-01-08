param (
    [Parameter(Mandatory = $True)]
    [string]
    $resourceGroup, 
    [Parameter(Mandatory = $True)]
    [string]
    $location,
    [Parameter(Mandatory = $True)]
    [string]
    $functionAppName,
    [Parameter(Mandatory = $True)]
    [string]
    $azureOpenAiEndpoint,
    [Parameter(Mandatory = $True)]
    [string]
    $azureOpenAiKey,
    [string]
    $chatModel = "gpt-4o",
    [string]
    $chatDeploymentName = "gpt-4o",
    [string]
    $embeddingModel = "text-embedding-ada-002",
    [string]
    $embeddingDeploymentName = "text-embedding-ada-002"
 

)
$error.Clear()
$ErrorActionPreference = 'Stop'
$userIdGuid = az ad signed-in-user show -o tsv --query id

$safeFuncAppName = $functionAppName.Substring(0, [math]::Min(13, ($functionAppName.ToLower() -replace '[-_]', '').Length))
$aiServicesAcctName = "${functionAppName}-aiservices"
$cosmosAccountName = "${functionAppName}-cosmos"
$storageAcctName = "${safeFuncAppName}storage"
$aiSearchName = "${safeFuncAppName}-search"
$keyVaultName = "${functionAppName}-kv"

Write-Host "Service Names:" -ForegroundColor Cyan
Write-Host "Function App: $functionAppName" -ForegroundColor Green
Write-Host "AI Services Account: $aiServicesAcctName" -ForegroundColor Green
Write-Host "Cosmos Account: $cosmosAccountName" -ForegroundColor Green
Write-Host "Storage Account: $storageAcctName" -ForegroundColor Green
Write-Host "Search Account: $aiSearchName" -ForegroundColor Green
Write-Host "Key Vault: $keyVaultName" -ForegroundColor Green

Write-Host "Azure OpenAI Settings:" -ForegroundColor Cyan
Write-Host "Azure Open AI Endpoint: $azureOpenAiEndpoint" -ForegroundColor Green
Write-Host "Chat Model: $chatModel" -ForegroundColor Green
Write-Host "Chat Deployment Name: $chatDeploymentName" -ForegroundColor Green
Write-Host "Embedding Model: $embeddingModel" -ForegroundColor Green
Write-Host "Embedding Deployment Name: $embeddingDeploymentName" -ForegroundColor Green



Write-Host -ForegroundColor Cyan "Creating resource group $resourceGroup and required resources in $location" 
$result = az deployment sub create --name $functionAppName --location $location --template-file .\infra\main.bicep --parameters resourceGroupName=$resourceGroup storageAccountName=$storageAcctName `
    aiServicesAccountName=$aiServicesAcctName location=$location `
    azureOpenAiEndpoint=$azureOpenAiEndpoint azureOpenAiKey=$azureOpenAiKey `
    functionAppName=$functionAppName cosmosAccountName=$cosmosAccountName `
    keyVaultName=$keyVaultName aiSearchName=$aiSearchName userIdGuid=$userIdGuid| ConvertFrom-Json -Depth 10


Write-Host -ForegroundColor Green "Deployment Result"
Write-Host -ForegroundColor Yellow ($result.properties.outputs | ConvertTo-Json)

if(!$?){ exit }
if($null -eq $result) {exit}


Write-Host -ForegroundColor Green "Getting translation account account key"
$aiServicesKey = az cognitiveservices account keys list --resource-group $resourceGroup  --name $aiServicesAcctName -o tsv --query key1

Write-Host -ForegroundColor Green "Getting AI Search account account key"
$aiSearchKey = az search admin-key show --resource-group $resourceGroup  --service-name $aiSearchName -o tsv --query primaryKey

Write-Host -ForegroundColor Green "Getting Cosmos Connection String"
$cosmosAccountEndpoint = az cosmosdb show --name --name $result.properties.outputs.cosmosAccountName.value --resource-group $resourceGroup -o tsv --query readLocations[0].documentEndpoint

Write-Host -ForegroundColor Green "Getting Storage Connection  and SAS Urls"
$storageKey = az storage account keys list -n $storageAcctName -g $resourceGroup --query [0].value -o tsv
$storageConnection = "DefaultEndpointsProtocol=https;AccountName=${storageAcctName};AccountKey=$($storageKey);EndpointSuffix=core.windows.net"

$expiry = (Get-Date).ToUniversalTime().AddYears(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
$start =  (Get-Date).ToUniversalTime().AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")
$audioContainerName = $result.properties.outputs.audioContainerName.value
$audioSasTmp = az storage container generate-sas --account-name $storageAcctName --name $audioContainerName   --account-key $storageKey --permissions racwdl --expiry $expiry --start $start --https-only -o tsv
$audioSas = "https://$storageAcctName.blob.core.windows.net/$($audioContainerName)?$audioSasTmp"

$transcriptContainerName = $result.properties.outputs.transcriptContainerName.value
$transcriptSasTmp = az storage container generate-sas --account-name $storageAcctName --name $transcriptContainerName --account-key $storageKey --permissions racwdl --expiry $expiry --start $start --https-only -o tsv
$transcriptSas = "https://$storageAcctName.blob.core.windows.net/$($transcriptContainerName)?$transcriptSasTmp"


 $localsettings = @{
    "AiServices" = @{
        "Endpoint" = $result.properties.outputs.aiServicesEndpoint.value
        "Key" = $aiServicesKey
        "ApiVersion" = "v3.2"
        "Region" = $location
    }
    "Storage" = @{
       "SourceContainerUrl" = $audioSas
        "TargetContainerUrl" = $transcriptSas
    }
    "AzureOpenAi" = @{
     
        "EndPoint" = $azureOpenAiEndpoint
        "Key" = $azureOpenAiKey
        "ChatModel" = $chatModel
        "ChatDeploymentName" = $chatDeploymentName
        "EmbeddingModel" = $embeddingModel
        "EmbeddingDeploymentName" = $embeddingDeploymentName
    }
    "AiSearch" = @{
        "Endpoint" = "https://$($aiSearchName).search.windows.net"
        "Key" = $aiSearchKey
    }
    "CosmosDb" = @{
        "AccountEndpoint" = $cosmosAccountEndpoint
        "ContainerName" = $result.properties.outputs.cosmosContainerName.value
        "DatabaseName" = $result.properties.outputs.cosmosDataBaseName.value
    }

}

Write-Host -ForegroundColor Green "Creating console app local.settings.json"
$localsettingsJson = ConvertTo-Json $localsettings -Depth 100
$localsettingsJson | Out-File -FilePath ".\SpeechAnalytics\local.settings.json"

if(!$?){ exit }


$functionSettings = @{
    "IsEncrypted" = $false
    "Values" = @{
        "AzureWebJobsStorage" = "UseDevelopmentStorage=true"
        "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
        "StorageConnectionString" = $storageConnection
    
        "AiServices:Endpoint" = $result.properties.outputs.aiServicesEndpoint.value
        "AiServices:Key" = $aiServicesKey
        "AiServices:ApiVersion" = "v3.2"
        "AiServices:Region" = $location

        "Storage:SourceContainerUrl" = $audioSas
        "Storage:TargetContainerUrl" = $transcriptSas
    
        "AzureOpenAi:EndPoint" = $azureOpenAiEndpoint
        "AzureOpenAi:Key" = $azureOpenAiKey
        "AzureOpenAi:ChatModel" = $chatModel
        "AzureOpenAi:ChatDeploymentName" = $chatDeploymentName
        "AzureOpenAi:EmbeddingModel" = $embeddingModel
        "AzureOpenAi:EmbeddingDeploymentName" = $embeddingDeploymentName

        "AiSearch:Endpoint" = "https://$($aiSearchName).search.windows.net"
        "AiSearch:Key" = $aiSearchKey

        "CosmosDb:AccountEndpoint" = $cosmosAccountEndpoint
        "CosmosDb:ContainerName" = $result.properties.outputs.cosmosContainerName.value
        "CosmosDb:DatabaseName" = $result.properties.outputs.cosmosDataBaseName.value
    }
}

 Write-Host -ForegroundColor Green "Creating functionapp  local.settings.json"
 $localsettingsJson = ConvertTo-Json $functionSettings -Depth 100
 $localsettingsJson | Out-File -FilePath ".\CallCenterFunction\local.settings.json"

 if(!$?){ exit }

#  Push-Location .\CallCenterFunction
#  Write-Host -ForegroundColor Green "Deploying function app..."
#  func azure functionapp publish $functionAppName 
#  Pop-Location

if(!$?){ exit }

$cid = az cosmosdb show --resource-group $resourceGroup --name $cosmosAccountName -o tsv --query id
Write-Output "CosmosDB Resource ID $cid"
$ids = az cosmosdb sql role definition list --resource-group $resourceGroup --account-name $cosmosAccountName -o tsv --query "[].id" 
Write-Output "CosmosDB Role Definitions ID $ids"
foreach($id in $ids){
    az cosmosdb sql role assignment create `
    --resource-group $resourceGroup `
    --account-name $cosmosAccountName `
    --role-definition-id $id `
    --principal-id $userIdGuid `
    --scope $cid
}


if(!$?){ exit }

Write-Host -ForegroundColor Green "Building console app..."
dotnet build --no-incremental .\SpeechAnalytics\SpeechAnalytics.csproj -o .\SpeechAnalytics\bin\demo

.\SpeechAnalytics\bin\demo\SpeechAnalytics.exe





