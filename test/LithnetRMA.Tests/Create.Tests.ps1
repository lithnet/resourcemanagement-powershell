<#
    Cmdlet equivalent of the client unit tests' CreateTests.cs. Exercises New-Resource + Save-Resource
    and asserts what the cmdlet surface exposes: a created resource gets a real ObjectID, its data
    round-trips through Get-Resource, and a composite (multi-object) save works.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects
}

Describe 'New-Resource / Save-Resource' {

    BeforeEach {
        $script:created = [System.Collections.Generic.List[object]]::new()
    }

    AfterEach {
        foreach ($id in $script:created) { $id | Remove-TestResource }
    }

    It 'creates a resource with populated data and round-trips it through Get-Resource' {
        $resource = New-TestResource
        Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
        Save-Resource $resource

        $resource.ObjectID | Should -Not -BeNullOrEmpty
        $script:created.Add($resource.ObjectID)

        $fetched = Get-Resource -ID $resource.ObjectID
        Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
    }

    It 'creates a resource carrying only an account name' {
        $account = "create-$([Guid]::NewGuid().ToString('N'))"
        $resource = New-TestResource -AccountName $account
        Save-Resource $resource
        $script:created.Add($resource.ObjectID)

        $fetched = Get-Resource -ObjectType _unitTestObject -AttributeName AccountName -AttributeValue $account
        $fetched | Should -Not -BeNullOrEmpty
        $fetched.ObjectID | Should -Be $resource.ObjectID
    }

    It 'creates multiple resources in a single composite save' {
        $accounts = 1..5 | ForEach-Object { "composite-$([Guid]::NewGuid().ToString('N'))" }
        $resources = foreach ($account in $accounts)
        {
            $r = New-TestResource -AccountName $account
            Set-TestUserData -Resource $r -Data $script:data -References $script:refs
            $r
        }

        Save-Resource $resources

        foreach ($r in $resources)
        {
            $r.ObjectID | Should -Not -BeNullOrEmpty
            $script:created.Add($r.ObjectID)
            $fetched = Get-Resource -ID $r.ObjectID
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }
    }
}
