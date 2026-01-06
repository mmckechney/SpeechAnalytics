#!/usr/bin/env pwsh

azd env set "FIRST_PROVISION" $false

# Capture all environment values from the current azd environment
$envValues = azd env get-values --output json | ConvertFrom-Json

function Get-EnvValue {
	param(
		[Parameter(Mandatory = $true)]
		[string[]] $Keys,
		[string] $Default = ''
	)

	$properties = $envValues.PSObject.Properties

	foreach ($key in $Keys) {
		$prop = $properties | Where-Object { $_.Name -ieq $key }
		if ($prop) {
			$value = $prop.Value
			if ($null -ne $value -and $value -ne '') {
				return $value
			}
		}
	}

	foreach ($key in $Keys) {
		$value = [System.Environment]::GetEnvironmentVariable($key)
		if ($null -ne $value -and $value -ne '') {
			return $value
		}
	}

	return $Default
}

$localSettings = @{
	
	Storage = @{
		SourceContainerUrl = Get-EnvValue @('audioContainerUri', 'Storage__SourceContainerUrl')
		TargetContainerUrl = Get-EnvValue @('transcriptionContainerUri', 'Storage__TargetContainerUrl')
	}
	CosmosDb = @{
		AccountEndpoint = Get-EnvValue @('cosmosEndpoint', 'COSMOS_ENDPOINT')
		DatabaseName    = Get-EnvValue @('cosmosDataBaseName', 'COSMOS_DATABASE_NAME')
		TenantId        = Get-EnvValue @('cosmosTenantId', 'AZURE_TENANT_ID')
		ContainerName   = Get-EnvValue @('cosmosContainerName', 'COSMOS_CONTAINER_NAME')
	}
	AiSearch = @{
		Endpoint = Get-EnvValue @('aiSearchEndpoint', 'AI_SEARCH_ENDPOINT')
	}
	FoundryAgent = @{
		ProjectEndpoint             = Get-EnvValue @('foundryProjectEndpoint', 'FOUNDRY_PROJECT_ENDPOINT')
		ModelDeploymentName         = Get-EnvValue @('foundryModelDeploymentName', 'FOUNDRY_MODEL_DEPLOYMENT_NAME', 'chatModel', 'AZURE_OPENAI_CHAT_DEPLOYMENT_NAME')
		EmbeddingDeploymentName     = Get-EnvValue @('foundryEmbeddingDeploymentName', 'FOUNDRY_EMBEDDING_DEPLOYMENT_NAME', 'embeddingModel', 'AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME')
		#VoiceDiarizeModel           = Get-EnvValue @('foundryVoiceDiarizeModel', 'FOUNDRY_VOICE_DIARIZE_MODEL', 'voiceDiarizeModel')
		InsightsAgentName           = Get-EnvValue @('foundryInsightsAgentName', 'insightsAgentName')
		SpeakerAgentName            = Get-EnvValue @('foundrySpeakerAgentName', 'speakerAgentName')
		QueryAgentName              = Get-EnvValue @('foundryQueryAgentName', 'queryAgentName')
		AnswerAgentName             = Get-EnvValue @('foundryAnswerAgentName', 'answerAgentName')
		ResourceId                  = Get-EnvValue @('foundryResourceId', 'FOUNDRY_RESOURCE_ID')
		ResourceName                = Get-EnvValue @('foundryResourceName', 'FOUNDRY_RESOURCE_NAME')
		Region     = Get-EnvValue @('aiServicesRegion', 'AISERVICES_REGION', 'location', 'AZURE_LOCATION')
		}
	}
 $localSettingsJson = $localSettings | ConvertTo-Json -Depth 5

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
if (-not $repoRoot) {
	$repoRoot = (Get-Location).Path
}

$settingsPaths = @('SpeechAnalytics/local.settings.json', 'AskInsightsService/local.settings.json', 'TranscriptionService/local.settings.json')
foreach($settingsPath in $settingsPaths) {
    $fullPath = Join-Path -Path $repoRoot -ChildPath $settingsPath

    $settingsDirectory = Split-Path -Parent $fullPath
    if (-not (Test-Path -LiteralPath $settingsDirectory)) {
        New-Item -ItemType Directory -Path $settingsDirectory | Out-Null
    }
    $localSettingsJson | Set-Content -LiteralPath $fullPath -Encoding utf8

    Write-Host "local.settings.json updated at $fullPath"
}