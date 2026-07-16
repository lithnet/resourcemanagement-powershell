<#
    Shared setup for the LithnetRMA Pester suite. Dot-sourced from each *.Tests.ps1 BeforeAll.

    Imports the module staged by Invoke-Tests.ps1 (Build-TestModule.ps1 output) and connects to the
    test MIM service. The endpoint mirrors the resourcemanagement-client unit tests: the 'fimsvc'
    alias, SPN 'FIMService/fimsvc'. LocalProxy is the default connection mode because it matches the
    unit tests and does not depend on the RemoteProxy service address resolution. All values are
    overridable by environment variable so the same suite runs in CI against a different lab.
#>
$ErrorActionPreference = 'Stop'

if (-not $env:LITHNETRMA_MODULE_PATH)
{
    throw 'LITHNETRMA_MODULE_PATH is not set. Run the suite via Invoke-Tests.ps1, which builds and stages the module first.'
}

# Ensure a version installed on the machine (e.g. from PSGallery) cannot shadow the source build.
Get-Module LithnetRMA | Remove-Module -Force
Import-Module $env:LITHNETRMA_MODULE_PATH -Force

Import-Module (Join-Path $PSScriptRoot 'LithnetRMA.TestSupport.psm1') -Force

Connect-TestClient

# Once-per-process suite setup: ensure the _unitTestObject schema exists and start from a clean slate
# (delete all leftover test objects), mirroring the unit tests' Setup.cs / DeleteAllTestObjects.
Initialize-TestEnvironment
