<#
.SYNOPSIS
    Builds the LithnetRMA binary module from source and stages it into the dual-edition
    folder layout that LithnetRMA.psm1 expects (coreclr\ for PowerShell 7, desktop\ for
    Windows PowerShell 5.1), then returns the path to the staged module manifest.

.DESCRIPTION
    The Pester suite must exercise the module as it actually ships - a script module
    (LithnetRMA.psm1) that loads the binary from coreclr\ or desktop\ depending on the
    running edition. The flat dotnet build output does not have that shape, so this helper
    assembles it. Building from source (not the published package) is deliberate: the whole
    point of the suite is to validate fixes to this source tree.

.OUTPUTS
    The full path to the staged LithnetRMA.psd1.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path ([System.IO.Path]::Combine($PSScriptRoot, '..', '..'))
$project = Join-Path $repoRoot 'src\Lithnet.ResourceManagement.Automation\Lithnet.ResourceManagement.Automation.csproj'
$projectDir = Split-Path $project -Parent
$loaderProject = Join-Path $repoRoot 'src\Lithnet.ResourceManagement.Automation.Loader\Lithnet.ResourceManagement.Automation.Loader.csproj'
$loaderProjectDir = Split-Path $loaderProject -Parent
$stageRoot = Join-Path $PSScriptRoot '.module'
$moduleDir = Join-Path $stageRoot 'LithnetRMA'

# Map each target framework to the module subfolder the .psm1 loads it from.
$editions = @(
    [pscustomobject]@{ Tfm = 'net8.0'; SubDir = 'coreclr' }
    [pscustomobject]@{ Tfm = 'net48';  SubDir = 'desktop' }
)

foreach ($edition in $editions)
{
    Write-Verbose "Building $($edition.Tfm)"
    & dotnet build $project -c $Configuration -f $edition.Tfm -v quiet | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet build failed for $($edition.Tfm)"
    }
}

Write-Verbose 'Building the PowerShell Core dependency loader'
& dotnet build $loaderProject -c $Configuration -v quiet | Out-Null
if ($LASTEXITCODE -ne 0)
{
    throw 'dotnet build failed for the PowerShell Core dependency loader'
}

if (Test-Path $moduleDir)
{
    Remove-Item $moduleDir -Recurse -Force
}
New-Item -ItemType Directory -Path $moduleDir -Force | Out-Null

# The manifest and script module live at the module root.
Copy-Item (Join-Path $projectDir 'LithnetRMA.psd1') $moduleDir -Force
Copy-Item (Join-Path $projectDir 'LithnetRMA.psm1') $moduleDir -Force

foreach ($edition in $editions)
{
    $source = Join-Path $projectDir "bin\$Configuration\$($edition.Tfm)"
    $target = Join-Path $moduleDir $edition.SubDir
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Copy-Item (Join-Path $source '*') $target -Recurse -Force
}

$loaderSource = Join-Path $loaderProjectDir "bin\$Configuration\net8.0\Lithnet.ResourceManagement.Automation.Loader.dll"
$coreTarget = Join-Path $moduleDir 'coreclr'
Copy-Item $loaderSource $coreTarget -Force

Join-Path $moduleDir 'LithnetRMA.psd1'
