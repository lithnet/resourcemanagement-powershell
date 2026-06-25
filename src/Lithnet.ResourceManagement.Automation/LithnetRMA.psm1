$depsPath = Join-Path $PSScriptRoot 'dependencies'

$resolveHandler = [System.ResolveEventHandler]{
    param($sender, $eventArgs)
    $assemblyName = [System.Reflection.AssemblyName]::new($eventArgs.Name)
    $path = Join-Path $depsPath "$($assemblyName.Name).dll"
    if (Test-Path $path) {
        return [System.Reflection.Assembly]::LoadFrom($path)
    }
    return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)

Import-Module (Join-Path $PSScriptRoot 'Lithnet.ResourceManagement.Automation.dll')
