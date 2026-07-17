<#
    Cmdlet equivalent of the client unit tests' DeleteTests.cs. Exercises Remove-Resource across both
    of its parameter sets: -ID (single value or array, accepting the UniqueIdentifier / Guid / string
    forms the cmdlet switches on) and -ResourceObjects (pipeline). The only cmdlet-observable outcome
    of a delete is asserted: the object has a real ObjectID after Save-Resource, and once removed a
    Get-Resource -ID for it throws "not found".

    Two unit tests are intentionally not ported: DeleteEmptyListResource and
    DeleteEmptyListUniqueIdentifier. They assert the client library tolerates being handed an empty
    collection to delete, which is a client-API concern. Remove-Resource's -ID and -ResourceObjects
    parameters are both Mandatory, so "delete nothing" has no well-defined, observable cmdlet contract
    to assert against.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects
}

Describe 'Remove-Resource' {

    BeforeEach {
        $script:created = [System.Collections.Generic.List[object]]::new()
    }

    AfterEach {
        # Safety net: these tests delete their own objects, but best-effort remove any that survived
        # a failed assertion. Remove-TestResource swallows 'not found', so re-deleting is harmless.
        foreach ($id in $script:created) { $id | Remove-TestResource }
    }

    It 'deletes a resource by its ObjectID (UniqueIdentifier form)' {
        # Mirrors DeleteByID: delete using the ObjectID value as returned after save.
        $resource = New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))"
        Save-Resource $resource
        $resource.ObjectID | Should -Not -BeNullOrEmpty
        $id = $resource.ObjectID
        $script:created.Add($id)

        Remove-Resource -ID $id

        { Get-Resource -ID $id } | Should -Throw
    }

    It 'deletes a resource by its ObjectID given as a Guid' {
        # Mirrors DeleteByGuid: the cmdlet's -ID switch accepts a raw Guid.
        $resource = New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))"
        Save-Resource $resource
        $resource.ObjectID | Should -Not -BeNullOrEmpty
        $script:created.Add($resource.ObjectID)

        Remove-Resource -ID $resource.ObjectID.GetGuid()

        { Get-Resource -ID $resource.ObjectID } | Should -Throw
    }

    It 'deletes a resource by its ObjectID given as a string' {
        # Mirrors DeleteByString: the cmdlet's -ID switch accepts the string form of the identifier.
        $resource = New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))"
        Save-Resource $resource
        $resource.ObjectID | Should -Not -BeNullOrEmpty
        $script:created.Add($resource.ObjectID)

        Remove-Resource -ID $resource.ObjectID.Value

        { Get-Resource -ID $resource.ObjectID } | Should -Throw
    }

    It 'deletes a resource piped as a resource object' {
        # Mirrors DeleteByObject: pipe the RmaObject to bind the -ResourceObjects parameter set.
        $resource = New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))"
        Save-Resource $resource
        $resource.ObjectID | Should -Not -BeNullOrEmpty
        $script:created.Add($resource.ObjectID)

        $resource | Remove-Resource

        { Get-Resource -ID $resource.ObjectID } | Should -Throw
    }

    It 'throws when deleting a resource that does not exist' {
        # Mirrors DeleteByStringNonExistant: deleting an id that resolves to nothing raises an error
        # (the service returns permission-denied for a non-existent object). The observable contract
        # at the cmdlet surface is simply that Remove-Resource throws.
        $missing = 'f970bdf5-7b41-4618-82e6-ff16d34d2e41'

        { Remove-Resource -ID $missing } | Should -Throw
    }

    It 'deletes multiple resources in a single call by an array of IDs' {
        # Mirrors CompositeDeleteByIDTest: pass an array of identifiers to -ID.
        $resources = 1..3 | ForEach-Object { New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))" }
        Save-Resource $resources

        $ids = foreach ($r in $resources)
        {
            $r.ObjectID | Should -Not -BeNullOrEmpty
            $script:created.Add($r.ObjectID)
            $r.ObjectID
        }

        Remove-Resource -ID $ids

        foreach ($id in $ids)
        {
            { Get-Resource -ID $id } | Should -Throw
        }
    }

    It 'deletes multiple resources piped as resource objects' {
        # Mirrors CompositeDeleteByObjectTest: pipe several RmaObjects through the -ResourceObjects set.
        $resources = 1..3 | ForEach-Object { New-TestResource -AccountName "delete-$([Guid]::NewGuid().ToString('N'))" }
        Save-Resource $resources

        foreach ($r in $resources)
        {
            $r.ObjectID | Should -Not -BeNullOrEmpty
            $script:created.Add($r.ObjectID)
        }

        $resources | Remove-Resource

        foreach ($r in $resources)
        {
            { Get-Resource -ID $r.ObjectID } | Should -Throw
        }
    }
}
