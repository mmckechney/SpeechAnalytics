param (
    [Parameter(Mandatory = $True)]
    [string]
    $resourceGroup, 
    [Parameter(Mandatory = $True)]
    [string]
    $location,
    [Parameter(Mandatory = $True)]
    [string]
    $aiServicesAcctName,
    [Parameter(Mandatory = $True)]
    [string]
    $functionAppName,
    [Parameter(Mandatory = $True)]
    [string]
    $cosmosAccountName,
    [Parameter(Mandatory = $True)]
    [string]
    $storageAcctName,
    [Parameter(Mandatory = $True)]
    [string]
    $azureOpenAiEndpoint,
    [Parameter(Mandatory = $True)]
    [string]
    $azureOpenAiKey,
    [string]
    $chatModel = "gpt-4-32k",
    [string]
    $chatDeploymentName = "gpt-4-32k",
    [string]
    $embeddingModel = "text-embedding-ada-002",
    [string]
    $embeddingDeploymentName = "text-embedding-ada-002"
 

)
$error.Clear()
$ErrorActionPreference = 'Stop'


Write-Host -ForegroundColor Green "Creating resource group $resourceGroup and required resources in $location"
$result = az deployment sub create --location $location --template-file .\infra\main.bicep --parameters resourceGroupName=$resourceGroup storageAccountName=$storageAcctName `
    aiServicesAccountName=$aiServicesAcctName location=$location `
    azureOpenAiEndpoint=$azureOpenAiEndpoint azureOpenAiKey=$azureOpenAiKey `
    functionAppName=$functionAppName cosmosAccountName=$cosmosAccountName | ConvertFrom-Json -Depth 10

Write-Host -ForegroundColor Green "Deployment Result"
Write-Host -ForegroundColor Yellow ($result.properties.outputs | ConvertTo-Json)

if(!$?){ exit }
if($null -eq $result) {exit}


Write-Host -ForegroundColor Green "Getting translation account account key"
$aiServicesKey = az cognitiveservices account keys list --resource-group $resourceGroup  --name $aiServicesAcctName -o tsv --query key1

Write-Host -ForegroundColor Green "Getting Cosmos Connection String"
$cosmosConnection = az cosmosdb keys list --name $result.properties.outputs.cosmosAccountName.value --resource-group $resourceGroup --type connection-strings -o tsv --query connectionStrings[0].connectionString

Write-Host -ForegroundColor Green "Getting Storage Connection String"
$storageKey = az storage account keys list -n $storageAcctName -g $resourceGroup --query [0].value -o tsv
$storageConnection = "DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=$($storageKey)};EndpointSuffix=core.windows.net"

 $localsettings = @{
    "AiServices" = @{
        "Endpoint" = $result.properties.outputs.aiServicesEndpoint.value
        "Key" = $aiServicesKey
        "ApiVersion" = "v3.2-preview.1"
        "Region" = $location
    }
    "Storage" = @{
        "SourceContainerUrl" = $result.properties.outputs.audiofile_url.value
        "TargetContainerUrl" = $result.properties.outputs.transcriptfile_url.value
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
        "Endpoint" = ""
        "Key" = ""
    }
    "CosmosDb" = @{
        "ConnectionString" = $cosmosConnection
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
        "AiServices:ApiVersion" = "v3.2-preview.1"
        "AiServices:Region" = $location

        "Storage:SourceContainerUrl" = $result.properties.outputs.audiofile_url.value
        "Storage:TargetContainerUrl" = $result.properties.outputs.transcriptfile_url.value
    
        "AzureOpenAi:EndPoint" = $azureOpenAiEndpoint
        "AzureOpenAi:Key" = $azureOpenAiKey
        "AzureOpenAi:ChatModel" = $chatModel
        "AzureOpenAi:ChatDeploymentName" = $chatDeploymentName
        "AzureOpenAi:EmbeddingModel" = $embeddingModel
        "AzureOpenAi:EmbeddingDeploymentName" = $embeddingDeploymentName

        "AiSearch:Endpoint" = ""
        "AiSearch:Key" = ""

        "CosmosDb:ConnectionString" = $cosmosConnection
        "CosmosDb:ContainerName" = $result.properties.outputs.cosmosContainerName.value
        "CosmosDb:DatabaseName" = $result.properties.outputs.cosmosDataBaseName.value
    }
}

 Write-Host -ForegroundColor Green "Creating functionapp  local.settings.json"
 $localsettingsJson = ConvertTo-Json $functionSettings -Depth 100
 $localsettingsJson | Out-File -FilePath ".\CallCenterFunction\local.settings.json"

 if(!$?){ exit }

 Push-Location .\CallCenterFunction
 Write-Host -ForegroundColor Green "Deploying function app..."
 func azure functionapp publish $functionAppName 
 Pop-Location

if(!$?){ exit }


Write-Host -ForegroundColor Green "Building console app..."
dotnet build --no-incremental .\SpeechAnalytics\SpeechAnalytics.csproj -o .\SpeechAnalytics\bin\demo

.\SpeechAnalytics\bin\demo\SpeechAnalytics.exe





