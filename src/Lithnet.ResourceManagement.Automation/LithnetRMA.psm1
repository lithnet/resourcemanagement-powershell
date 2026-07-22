# Root module loader. The module ships two builds of the same cmdlets: 'desktop' (.NET
# Framework) for Windows PowerShell 5.1, and 'coreclr' (.NET) for PowerShell 7 and later.
#
# On PowerShell Core, the dependency-free loader is the only module assembly loaded into the
# default AssemblyLoadContext. The cmdlet assembly and its bundled dependency closure load into
# a dedicated context resolved from the module's deps.json.

$importModule = Get-Command -Name Import-Module -Module Microsoft.PowerShell.Core

if ($PSEdition -eq 'Core') {
    $isReload = $true

    if (-not ('Lithnet.ResourceManagement.Automation.Loader.ModuleLoadContext' -as [type])) {
        $isReload = $false
        Add-Type -Path (Join-Path $PSScriptRoot 'coreclr\Lithnet.ResourceManagement.Automation.Loader.dll') -ErrorAction Stop
    }

    $moduleAssembly = [Lithnet.ResourceManagement.Automation.Loader.ModuleLoadContext]::Initialize()
    $innerModule = & $importModule -Assembly $moduleAssembly -PassThru:$isReload -ErrorAction Stop
}
else {
    $modulePath = Join-Path $PSScriptRoot 'desktop\Lithnet.ResourceManagement.Automation.dll'
    $innerModule = & $importModule -Name $modulePath -ErrorAction Stop -PassThru
}

if ($innerModule -and $PSEdition -eq 'Core') {
    # PowerShell issue #20710 causes a repeated Import-Module -Force to reuse the cached binary
    # module without rebinding its cmdlets into this script module's scope. Reattach the cached
    # cmdlet metadata so forced and remove/re-import scenarios retain the exported command set.
    $addExportedCmdlet = [System.Management.Automation.PSModuleInfo].GetMethod(
        'AddExportedCmdlet',
        [System.Reflection.BindingFlags]'Instance, NonPublic')

    if ($null -eq $addExportedCmdlet) {
        throw 'PowerShell reused the binary module but its AddExportedCmdlet method is unavailable. The cmdlets cannot be rebound after re-import.'
    }

    foreach ($cmdlet in $innerModule.ExportedCmdlets.Values) {
        $addExportedCmdlet.Invoke($ExecutionContext.SessionState.Module, @(, $cmdlet))
    }
}
