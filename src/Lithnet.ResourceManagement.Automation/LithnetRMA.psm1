$depsPath = Join-Path $PSScriptRoot 'dependencies'

if ($PSEdition -eq 'Core') {
    $resolveHandler = [System.Func[System.Runtime.Loader.AssemblyLoadContext, System.Reflection.AssemblyName, System.Reflection.Assembly]]{
        param($context, $assemblyName)
        $path = [System.IO.Path]::Combine($depsPath, "$($assemblyName.Name).dll")
        if ([System.IO.File]::Exists($path)) {
            return $context.LoadFromAssemblyPath($path)
        }
        return $null
    }
    [System.Runtime.Loader.AssemblyLoadContext]::Default.add_Resolving($resolveHandler)
} else {
    $resolveHandler = [System.ResolveEventHandler]{
        param($sender, $eventArgs)
        $assemblyName = [System.Reflection.AssemblyName]::new($eventArgs.Name)

        # On .NET Framework, the netstandard2.0 builds of System.* and Microsoft.Win32.*
        # assemblies are stubs that throw PlatformNotSupportedException.
        # Let the framework resolve these from the GAC.
        $name = $assemblyName.Name
        if ($name.StartsWith('System.') -or $name.StartsWith('Microsoft.Win32.')) {
            return $null
        }

        $path = [System.IO.Path]::Combine($depsPath, "$($assemblyName.Name).dll")
        if ([System.IO.File]::Exists($path)) {
            return [System.Reflection.Assembly]::LoadFrom($path)
        }
        return $null
    }
    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
}

Import-Module (Join-Path $PSScriptRoot 'Lithnet.ResourceManagement.Automation.dll')
