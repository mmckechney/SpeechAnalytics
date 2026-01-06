
# hooks/prebuild-set-tag.ps1

param(
  [string] $SolutionName = "speechanalytics",
  [string]$ServiceName = $env:AZD_SERVICE_NAME
)


if (-not $ServiceName) { throw "AZD_SERVICE_NAME not set. Ensure this hook is declared under a service in azure.yaml." }

Write-Host "Running prebuild for service: $ServiceName (env: $EnvName, location: $Location)"
Write-Host "Service path: $ServicePath"
$environment = (azd env get-value "AZURE_ENV_NAME").ToLowerInvariant()
# Write-Host "Environment: $environment"
# # Example: compute a tag and persist with service-specific keys
# $gitSha   = (git rev-parse --short HEAD)
# $timestamp = Get-Date -Format "yyyyMMddHHmmss"
# $tag       = "$timestamp-$gitSha"
$tag = (azd env get-value "CONTAINER_TAGS")
# Compose keys using the service name to keep the script generic across services
$upper = $ServiceName.ToUpperInvariant()
$imageName = "${SolutionName}/${ServiceName}-${environment}:${tag}"

azd env set "AZD_SERVICE_${upper}_IMAGE" "$imageName"

