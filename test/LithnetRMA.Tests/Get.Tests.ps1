<#
    Cmdlet equivalent of the client unit tests' GetTests.cs. Exercises Get-Resource across its three
    parameter sets - by ID (-ID), by a single key (-ObjectType/-AttributeName/-AttributeValue) and by
    multiple keys (-ObjectType/-AttributeValuePairs) - plus attribute projection (-AttributesToGet)
    and the not-found / too-many-results failure paths.

    Only cmdlet-observable behaviour is asserted. The C# tests' internal-state checks (IsPlaceHolder
    in the finally blocks) are replaced by AfterEach cleanup that deletes every object created by ID.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects
}

Describe 'Get-Resource' {

    BeforeEach {
        $script:created = [System.Collections.Generic.List[object]]::new()
    }

    AfterEach {
        foreach ($id in $script:created) { $id | Remove-TestResource }
    }

    Context 'By ID' {

        It 'gets a resource by its ObjectID and round-trips all attributes' {
            $resource = New-TestResource
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ID $resource.ObjectID
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }

        It 'gets a resource by its ObjectID in string form' {
            $resource = New-TestResource
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ID $resource.ObjectID.Value
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }

        It 'gets a resource by its ObjectID in Guid form' {
            $resource = New-TestResource
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ID $resource.ObjectID.GetGuid()
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }

        It 'returns only the requested attributes when -AttributesToGet is used' {
            $a = $script:data.Attr

            $resource = New-TestResource
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ID $resource.ObjectID -AttributesToGet @($a.BooleanSV, $a.StringSV, $a.ReferenceMV)

            # requested attributes are present with the expected values
            $fetched.($a.BooleanSV) | Should -Be $script:data.BooleanTrue
            $fetched.($a.StringSV)  | Should -Be $script:data.String1
            (@($fetched.($a.ReferenceMV)) | ForEach-Object { "$_" } | Sort-Object) |
                Should -Be (@($script:refs.Ref1MV) | ForEach-Object { "$_" } | Sort-Object)

            # attributes that were not requested come back null
            $fetched.($a.IntegerSV) | Should -BeNullOrEmpty
            $fetched.($a.TextSV)    | Should -BeNullOrEmpty
        }
    }

    Context 'By a single key attribute' {

        It 'gets a resource by AccountName and round-trips all attributes' {
            $a = $script:data.Attr
            $account = "get-$([Guid]::NewGuid().ToString('N'))"

            $resource = New-TestResource -AccountName $account
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeName $a.AccountName -AttributeValue $account
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }

        It 'gets a resource by AccountName with only the requested attributes' {
            $a = $script:data.Attr
            $account = "get-$([Guid]::NewGuid().ToString('N'))"

            $resource = New-TestResource -AccountName $account
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $fetched = Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeName $a.AccountName -AttributeValue $account `
                -AttributesToGet @($a.AccountName, $a.BooleanSV, $a.StringSV, $a.ReferenceMV)

            $fetched.($a.AccountName) | Should -Be $account
            $fetched.($a.BooleanSV)   | Should -Be $script:data.BooleanTrue
            $fetched.($a.StringSV)    | Should -Be $script:data.String1
            (@($fetched.($a.ReferenceMV)) | ForEach-Object { "$_" } | Sort-Object) |
                Should -Be (@($script:refs.Ref1MV) | ForEach-Object { "$_" } | Sort-Object)

            $fetched.($a.IntegerSV) | Should -BeNullOrEmpty
            $fetched.($a.TextSV)    | Should -BeNullOrEmpty
        }

        It 'throws when a single-key lookup matches more than one resource' {
            $a = $script:data.Attr

            $r1 = New-TestResource
            Set-TestUserData -Resource $r1 -Data $script:data -References $script:refs
            Save-Resource $r1
            $script:created.Add($r1.ObjectID)

            $r2 = New-TestResource
            Set-TestUserData -Resource $r2 -Data $script:data -References $script:refs
            Save-Resource $r2
            $script:created.Add($r2.ObjectID)

            # both resources share the same ut_svstring value, so a lookup on it is ambiguous
            { Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeName $a.StringSV -AttributeValue $script:data.String1 } |
                Should -Throw
        }

        It 'throws when a single-key lookup finds no matching resource' {
            $a = $script:data.Attr

            # the client returns null on no match; the cmdlet turns that into a not-found terminating error
            { Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeName $a.StringSV -AttributeValue ([Guid]::NewGuid().ToString()) } |
                Should -Throw -ExpectedMessage '*not found*'
        }
    }

    Context 'By multiple key attributes' {

        It 'gets a resource by AccountName + string key and round-trips all attributes' {
            $a = $script:data.Attr
            $account = "get-$([Guid]::NewGuid().ToString('N'))"

            $resource = New-TestResource -AccountName $account
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $keys = @{}
            $keys[$a.AccountName] = $account
            $keys[$a.StringSV] = $script:data.String1

            $fetched = Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeValuePairs $keys
            Assert-TestUserData -Resource $fetched -Data $script:data -References $script:refs
        }

        It 'gets a resource by multiple keys with only the requested attributes' {
            $a = $script:data.Attr
            $account = "get-$([Guid]::NewGuid().ToString('N'))"

            $resource = New-TestResource -AccountName $account
            Set-TestUserData -Resource $resource -Data $script:data -References $script:refs
            Save-Resource $resource
            $script:created.Add($resource.ObjectID)

            $keys = @{}
            $keys[$a.AccountName] = $account
            $keys[$a.StringSV] = $script:data.String1

            $fetched = Get-Resource -ObjectType $script:data.UnitTestObjectType -AttributeValuePairs $keys `
                -AttributesToGet @($a.AccountName, $a.BooleanSV, $a.StringSV, $a.ReferenceMV)

            $fetched.($a.AccountName) | Should -Be $account
            $fetched.($a.BooleanSV)   | Should -Be $script:data.BooleanTrue
            $fetched.($a.StringSV)    | Should -Be $script:data.String1
            (@($fetched.($a.ReferenceMV)) | ForEach-Object { "$_" } | Sort-Object) |
                Should -Be (@($script:refs.Ref1MV) | ForEach-Object { "$_" } | Sort-Object)

            $fetched.($a.IntegerSV) | Should -BeNullOrEmpty
            $fetched.($a.TextSV)    | Should -BeNullOrEmpty
        }
    }

    Context 'Not found' {

        It 'throws when getting a non-existent object by ID' {
            { Get-Resource -ID ([Guid]::NewGuid()) } | Should -Throw -ExpectedMessage '*not found*'
        }
    }
}
