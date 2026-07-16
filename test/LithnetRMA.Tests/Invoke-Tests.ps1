<#
.SYNOPSIS
    Builds and stages the LithnetRMA module, then runs the Pester suite against the test MIM.

.DESCRIPTION
    The single entry point for the suite. Builds the module from source (so fixes are what gets
    tested), stages it into the dual-edition layout, points the suite at it via environment
    variable, and runs Pester 5. Run under both pwsh (PowerShell 7) and powershell (Windows
    PowerShell 5.1) to cover both editions - the #45 defects are edition-sensitive.

.EXAMPLE
    pwsh -File ./test/LithnetRMA.Tests/Invoke-Tests.ps1
    powershell -File ./test/LithnetRMA.Tests/Invoke-Tests.ps1 -OutputXml ./test-results-desktop.xml
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string] $BaseAddress = 'fimsvc',

    [string] $ServicePrincipalName = 'FIMService/fimsvc',

    [ValidateSet('Auto', 'LocalProxy', 'RemoteProxy', 'DirectNetTcp', 'DirectWsHttp')]
    [string] $ConnectionMode = 'LocalProxy',

    [string] $OutputXml
)

$ErrorActionPreference = 'Stop'

$env:LITHNETRMA_MODULE_PATH = & (Join-Path $PSScriptRoot 'Build-TestModule.ps1') -Configuration $Configuration
$env:LITHNETRMA_TEST_BASEADDRESS = $BaseAddress
$env:LITHNETRMA_TEST_SPN = $ServicePrincipalName
$env:LITHNETRMA_TEST_MODE = $ConnectionMode

Import-Module Pester -MinimumVersion 5.0

$config = New-PesterConfiguration
$config.Run.Path = $PSScriptRoot
$config.Output.Verbosity = 'Detailed'

if ($OutputXml)
{
    $config.TestResult.Enabled = $true
    $config.TestResult.OutputFormat = 'NUnitXml'
    $config.TestResult.OutputPath = $OutputXml
}

Invoke-Pester -Configuration $config
