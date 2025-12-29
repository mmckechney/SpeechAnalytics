function Get-LargestWingetVersion {
    <#
    .SYNOPSIS
        Extracts the largest version number from winget list command output
    
    .DESCRIPTION
        Parses the output from winget list command and returns the largest version number available.
        It can handle both installed and available versions.
    
    .PARAMETER PackageId
        The ID of the package to query
    
    .EXAMPLE
        Get-LargestWingetVersion -PackageId "Microsoft.DotNet.SDK.9"
        # Returns the largest version available for .NET SDK 9
    
    .NOTES
        This function requires the winget command-line tool to be installed.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    # Run winget list command and capture output
    $wingetOutput = winget list --id $PackageId

    # Define regex patterns to extract versions
    $versionPattern = "(\d+\.\d+\.\d+)"
    
    # Initialize collections to store versions
    $installedVersions = @()
    $availableVersions = @()

    # Process each line of the output
    $wingetOutput | ForEach-Object {
        # Skip header lines and empty lines
        if ($_ -match $PackageId) {
            # Extract versions using regex
            $line = $_
            $matches = [regex]::Matches($line, $versionPattern)
            
            if ($matches.Count -ge 1) {
                # First version is always the installed one
                $installedVersions += [version]$matches[0].Value
                
                # If there's a second version, it's the available one
                if ($matches.Count -ge 2) {
                    $availableVersions += [version]$matches[1].Value
                }
            }
        }
    }

    # Combine all versions
    $allVersions = $installedVersions + $availableVersions
    
    # Return the largest version if any were found
    if ($allVersions.Count -gt 0) {
        return ($allVersions | Sort-Object -Descending)[0].ToString()
    }
    else {
        Write-Warning "No versions found for package $PackageId"
        return $null
    }
}

# Example usage:
# $largestVersion = Get-LargestWingetVersion -PackageId "Microsoft.DotNet.SDK.9"
# Write-Host "The largest version available is: $largestVersion"
