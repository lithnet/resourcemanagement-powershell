<#
    Verifies the dual-edition module boundary and the PowerShell Core AssemblyLoadContext
    isolation. These tests run after the normal bootstrap has connected to the lab service, so
    the client dependency and XPath surface have both been exercised rather than merely reflected.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:manifestPath = $env:LITHNETRMA_MODULE_PATH
    $script:exported = @(Get-Command -Module LithnetRMA -CommandType Cmdlet)
}

Describe 'LithnetRMA module loading' {

    It 'exports the complete cmdlet surface' {
        $script:exported.Count | Should -Be 16
    }

    It 'uses module-owned types for every client-facing parameter' {
        (Get-Command Set-ResourceManagementClient).Parameters.ConnectionMode.ParameterType.FullName |
            Should -Be 'Lithnet.ResourceManagement.Automation.ConnectionMode'
        (Get-Command New-XPathExpression).Parameters.QueryObject.ParameterType.FullName |
            Should -Be 'Lithnet.ResourceManagement.Automation.IXPathQueryObject'
        (Get-Command New-XPathQueryGroup).Parameters.Queries.ParameterType.GetElementType().FullName |
            Should -Be 'Lithnet.ResourceManagement.Automation.IXPathQueryObject'
    }

    It 'returns module-owned XPath facade objects' {
        $query = New-XPathQuery -AttributeName AccountName -Operator Equals -Value 'someone'
        $group = New-XPathQueryGroup -Operator And -Queries $query
        $expression = New-XPathExpression -ObjectType Person -QueryObject $group

        $query.GetType().FullName | Should -Be 'Lithnet.ResourceManagement.Automation.XPathQuery'
        $group.GetType().FullName | Should -Be 'Lithnet.ResourceManagement.Automation.XPathQueryGroup'
        $expression.GetType().FullName | Should -Be 'Lithnet.ResourceManagement.Automation.XPathExpression'
        $expression.ToString() | Should -Be "/Person[(AccountName = 'someone')]"
    }

    It 'resolves module-owned facade types by name' {
        'Lithnet.ResourceManagement.Automation.ConnectionMode' -as [type] | Should -Not -BeNullOrEmpty
        'Lithnet.ResourceManagement.Automation.IXPathQueryObject' -as [type] | Should -Not -BeNullOrEmpty
        'Lithnet.ResourceManagement.Automation.XPathExpression' -as [type] | Should -Not -BeNullOrEmpty
    }

    Context 'PowerShell Core dependency isolation' -Skip:($PSEdition -ne 'Core') {

        BeforeAll {
            $script:alc = [System.Runtime.Loader.AssemblyLoadContext]::All |
                Where-Object { $_.Name -eq 'LithnetRMA' }
        }

        It 'loads the cmdlet assembly in the isolated context' {
            $script:alc | Should -Not -BeNullOrEmpty
            $cmdletAssembly = (Get-Command New-XPathQuery).ImplementingType.Assembly
            [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($cmdletAssembly) |
                Should -Be $script:alc
        }

        It 'loads the resource management client only in the isolated context' {
            @($script:alc.Assemblies | Where-Object { $_.GetName().Name -eq 'Lithnet.ResourceManagement.Client' }).Count |
                Should -Be 1
            @([System.Runtime.Loader.AssemblyLoadContext]::Default.Assemblies |
                Where-Object { $_.GetName().Name -eq 'Lithnet.ResourceManagement.Client' }).Count |
                Should -Be 0
        }

        It 'resolves bundled dependencies from the isolated context' {
            foreach ($name in 'Microsoft.Bcl.AsyncInterfaces', 'StreamJsonRpc', 'Newtonsoft.Json') {
                $assemblyName = [System.Reflection.AssemblyName]::new($name)
                $resolved = $script:alc.LoadFromAssemblyName($assemblyName)
                [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($resolved) |
                    Should -Be $script:alc -Because "$name must come from the module dependency closure"
            }
        }

        It 'keeps the loader as the single bridge assembly in the default context' {
            $loaders = @([System.Runtime.Loader.AssemblyLoadContext]::All |
                ForEach-Object { $_.Assemblies } |
                Where-Object { $_.GetName().Name -eq 'Lithnet.ResourceManagement.Automation.Loader' })

            $loaders.Count | Should -Be 1
            [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($loaders[0]) |
                Should -Be ([System.Runtime.Loader.AssemblyLoadContext]::Default)
        }

        It 'exposes no resource management client types on the cmdlet parameter surface' {
            $offenders = @()

            foreach ($cmdlet in $script:exported) {
                foreach ($parameter in $cmdlet.Parameters.Values) {
                    $parameterType = $parameter.ParameterType

                    while ($parameterType.HasElementType) {
                        $parameterType = $parameterType.GetElementType()
                    }

                    if ($parameterType.Assembly.GetName().Name -eq 'Lithnet.ResourceManagement.Client') {
                        $offenders += "$($cmdlet.Name) -$($parameter.Name) [$($parameterType.FullName)]"
                    }
                }
            }

            $offenders | Should -BeNullOrEmpty
        }

        It 'survives Import-Module -Force' {
            Import-Module $script:manifestPath -Force -ErrorAction Stop
            @(Get-Command -Module LithnetRMA -CommandType Cmdlet).Count | Should -Be 16
        }

        It 'survives Remove-Module followed by re-import' {
            Remove-Module LithnetRMA -Force -ErrorAction Stop
            Import-Module $script:manifestPath -ErrorAction Stop
            @(Get-Command -Module LithnetRMA -CommandType Cmdlet).Count | Should -Be 16
        }
    }
}
