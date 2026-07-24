<#
    Verifies that runtime-generated WCF channel proxies remain inside the PowerShell module's
    isolated AssemblyLoadContext. The endpoint is deliberately unreachable: proxy generation
    happens before the connection attempt, so the expected transport failure proves execution
    progressed beyond the TypeLoadException that occurs when the proxy is emitted in the default
    context. This test requires no MIM service.
#>

BeforeAll {
    $ErrorActionPreference = 'Stop'

    if (-not $env:LITHNETRMA_MODULE_PATH)
    {
        throw 'LITHNETRMA_MODULE_PATH is not set.'
    }

    Get-Module LithnetRMA | Remove-Module -Force
    Import-Module $env:LITHNETRMA_MODULE_PATH -Force
}

Describe 'LithnetRMA module execution context' {

    Context 'PowerShell Core dynamic proxy isolation' -Skip:($PSEdition -ne 'Core') {

        It 'generates WCF channel proxies in the module load context' {
            $caught = $null

            try
            {
                Set-ResourceManagementClient `
                    -BaseAddress 'net.tcp://127.0.0.1:1' `
                    -ConnectionMode DirectNetTcp `
                    -ConnectTimeoutSeconds 1 `
                    -ReceiveTimeoutSeconds 1 `
                    -SendTimeoutSeconds 1 `
                    -RefreshSchema `
                    -ErrorAction Stop
            }
            catch
            {
                $caught = $_.Exception
            }

            $caught | Should -Not -BeNullOrEmpty -Because 'the deliberately unreachable endpoint must produce a transport error'

            $exceptionTypes = @()
            $current = $caught

            while ($null -ne $current)
            {
                $exceptionTypes += $current.GetType().FullName
                $current = $current.InnerException
            }

            $exceptionTypes | Should -Not -Contain 'System.TypeLoadException'

            $moduleContext = [System.Runtime.Loader.AssemblyLoadContext]::All |
                Where-Object { $_.Name -eq 'LithnetRMA' }
            $proxyAssemblies = [AppDomain]::CurrentDomain.GetAssemblies() |
                Where-Object { $_.GetName().Name -eq 'DispatchProxyTypes.LithnetRMA' }

            $moduleContext | Should -Not -BeNullOrEmpty
            $proxyAssemblies | Should -Not -BeNullOrEmpty

            foreach ($proxyAssembly in $proxyAssemblies)
            {
                [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($proxyAssembly) |
                    Should -Be $moduleContext
            }
        }
    }
}
