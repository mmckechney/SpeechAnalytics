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
$result = az deployment sub create --location $location --template-file .\infra\main.bicep --parameters resourceGroupName=$resourceGroup storageAccountName=$storageAcctName aiServicesAccountName=$aiServicesAcctName location=$location | ConvertFrom-Json -Depth 10

Write-Host -ForegroundColor Green "Deployment Result"
Write-Host -ForegroundColor Yellow ($result.properties.outputs | ConvertTo-Json)

if(!$?){ exit }
if($null -eq $result) {exit}


Write-Host -ForegroundColor Green "Getting translation account account key"
$aiServicesKey = az cognitiveservices account keys list --resource-group $resourceGroup  --name $aiServicesAcctName -o tsv --query key1

Write-Host -ForegroundColor Green "Getting translation account account key"
$cosmosConnection = az cosmosdb keys list --name $result.properties.outputs.cosmosAccountName.value --resource-group $resourceGroup --type connection-strings -o tsv --query connectionStrings[0].connectionString

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
       

Write-Host -ForegroundColor Green "Creating local.settings.json"
$localsettingsJson = ConvertTo-Json $localsettings -Depth 100
$localsettingsJson | Out-File -FilePath ".\local.settings.json"

Write-Host -ForegroundColor Green "Running app..."
dotnet build --no-incremental .\SpeechAnalytics.csproj -o .\bin\demo

.\bin\demo\SpeechAnalytics.exe





