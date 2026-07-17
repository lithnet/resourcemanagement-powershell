<#
    Cmdlet equivalent of the client unit tests' Put*Tests.cs (one file per data type). Each of those
    C# files exercises, per attribute type, the same three-part pattern against an already-persisted
    object: (a) set a value and verify it round-trips, (b) modify the value and verify, (c) clear the
    value and verify it becomes null/empty - with single-valued (SV) and multi-valued (MV) variants.

    This suite ports those cases to what the cmdlet surface can observe. The C# tests assert on
    internal AttributeValue state (PendingChanges.Count, the client type's IsNull flag) that the
    cmdlets do not expose; the observable equivalent is simply "the re-read note-property equals the
    expected value / is null / is empty", so those internal-state assertions are dropped and only the
    round-tripped value is checked.

    Model (per the module): New-Resource builds an object, note-properties carry attribute values,
    Save-Resource persists, Get-Resource -ID re-reads. Setting an SV to $null or an MV to @() and
    saving clears it. References are set/read as ObjectIDs and compared by string form.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects

    # Second-value test data, mirroring the '...2' / '...3' constants in Constants.cs. These are the
    # values the modify cases change TO, and the two distinct strings the composite case writes. The
    # DateTime values are derived from the already second-truncated, UTC DateTime1/DateTime1MV so they
    # round-trip without rounding drift, exactly as Constants.cs derives its DateTime constants.
    $script:data2 = @{
        String2     = 'testString2'
        String3     = 'testString3'
        String2MV   = @('testString7', 'testString8', 'testString9')
        Integer2    = [long]5
        Integer2MV  = @([long]16, [long]17, [long]18)
        Text2       = 'testText2'
        Text2MV     = @('testText7', 'testText8', 'testText9')
        Binary2     = [byte[]]@(4, 5, 6, 7)
        Binary2MV   = @(, [byte[]]@(24, 25, 26, 27)) + @(, [byte[]]@(28, 29, 30, 31)) + @(, [byte[]]@(32, 33, 34, 35))
        DateTime2   = $script:data.DateTime1.AddDays(1)
        DateTime2MV = @($script:data.DateTime1MV | ForEach-Object { $_.AddDays(3) })
    }

    function New-PutResource
    {
        <# Creates a _unitTestObject with a unique, greppable account name, persists it, registers its
           ObjectID for AfterEach cleanup, and returns the saved object ready to be modified. #>
        $r = New-Resource -ObjectType $script:data.UnitTestObjectType
        $r.($script:data.Attr.AccountName) = "put-$([Guid]::NewGuid().ToString('N'))"
        Save-Resource $r
        $script:created.Add($r.ObjectID)
        return $r
    }

    function Save-AndReget
    {
        <# Persists pending changes on a resource then returns a freshly re-read copy, so assertions
           always run against what the service actually stored (mirrors the C# save + GetResource). #>
        param([Parameter(Mandatory)] $Resource)
        Save-Resource $Resource
        return Get-Resource -ID $Resource.ObjectID
    }

    function ConvertTo-UtcSecondsList
    {
        <# Normalises a collection of datetimes to UTC, whole-second strings for instant-based,
           order-sensitive comparison (the list form of ConvertTo-UtcSeconds). #>
        param($Values)
        return @($Values | ForEach-Object { ConvertTo-UtcSeconds $_ })
    }

    function ConvertTo-StringList
    {
        <# Projects a collection (e.g. reference ObjectIDs) to their string form for comparison. #>
        param($Values)
        return @($Values | ForEach-Object { "$_" })
    }

    function Assert-BinaryEqual
    {
        <# Byte-by-byte comparison of a single binary value. #>
        param($Actual, $Expected)
        $a = [byte[]] $Actual
        $e = [byte[]] $Expected
        $a.Length | Should -Be $e.Length -Because 'the binary values must have the same length'
        for ($i = 0; $i -lt $e.Length; $i++)
        {
            $a[$i] | Should -Be $e[$i] -Because "the byte at index $i must match"
        }
    }

    function Assert-BinaryCollection
    {
        <# Element-by-element byte comparison of a multi-valued binary attribute, order-sensitive. #>
        param($Actual, $Expected)
        $a = @($Actual)
        $e = @($Expected)
        $a.Count | Should -Be $e.Count -Because 'the binary collections must have the same number of values'
        for ($i = 0; $i -lt $e.Count; $i++)
        {
            Assert-BinaryEqual $a[$i] $e[$i]
        }
    }
}

Describe 'Attribute value round-trips (Put*Tests)' {

    BeforeEach {
        $script:created = [System.Collections.Generic.List[object]]::new()
    }

    AfterEach {
        foreach ($id in $script:created) { $id | Remove-TestResource }
    }

    # --- String -----------------------------------------------------------------------------------
    # Ports PutStringTests.cs. AddStringSV/ModifyStringSV/DeleteStringSV/DeleteAllValueStringSV and the
    # MV set AddFirst/AddSecond/Replace/DeleteFirstValue/DeleteAllValue. ModifyStringSVNoUpdate is
    # dropped: it only asserts PendingChanges.Count == 0, which is not observable through the cmdlets.
    # DeleteStringSV and DeleteAllValueStringSV collapse to one "clears" case (both null the SV).

    Context 'String (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svstring = $script:data.String1
            $r = Save-AndReget $r
            $r.ut_svstring | Should -Be $script:data.String1
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svstring = $script:data.String1
            $r = Save-AndReget $r
            $r.ut_svstring = $script:data2.String2
            $r = Save-AndReget $r
            $r.ut_svstring | Should -Be $script:data2.String2
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svstring = $script:data.String1
            $r = Save-AndReget $r
            $r.ut_svstring = $null
            $r = Save-AndReget $r
            $r.ut_svstring | Should -BeNullOrEmpty
        }
    }

    Context 'String (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvstring = $script:data.String1MV
            $r = Save-AndReget $r
            @($r.ut_mvstring) | Should -Be $script:data.String1MV
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvstring = $script:data.String1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvstring = $script:data.String1MV
            $r = Save-AndReget $r
            @($r.ut_mvstring) | Should -Be $script:data.String1MV
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvstring = $script:data.String1MV
            $r = Save-AndReget $r
            $r.ut_mvstring = $script:data2.String2MV
            $r = Save-AndReget $r
            @($r.ut_mvstring) | Should -Be $script:data2.String2MV
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvstring = $script:data.String1MV
            $r = Save-AndReget $r
            $r.ut_mvstring = $script:data.String1MV[1..2]
            $r = Save-AndReget $r
            @($r.ut_mvstring) | Should -Be $script:data.String1MV[1..2]
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvstring = $script:data.String1MV
            $r = Save-AndReget $r
            $r.ut_mvstring = @()
            $r = Save-AndReget $r
            @($r.ut_mvstring).Count | Should -Be 0
        }
    }

    # --- Integer ----------------------------------------------------------------------------------
    # Ports PutIntegerTests.cs.

    Context 'Integer (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svinteger = $script:data.Integer1
            $r = Save-AndReget $r
            $r.ut_svinteger | Should -Be $script:data.Integer1
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svinteger = $script:data.Integer1
            $r = Save-AndReget $r
            $r.ut_svinteger = $script:data2.Integer2
            $r = Save-AndReget $r
            $r.ut_svinteger | Should -Be $script:data2.Integer2
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svinteger = $script:data.Integer1
            $r = Save-AndReget $r
            $r.ut_svinteger = $null
            $r = Save-AndReget $r
            $r.ut_svinteger | Should -BeNullOrEmpty
        }
    }

    Context 'Integer (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvinteger = $script:data.Integer1MV
            $r = Save-AndReget $r
            @($r.ut_mvinteger) | Should -Be $script:data.Integer1MV
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvinteger = $script:data.Integer1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvinteger = $script:data.Integer1MV
            $r = Save-AndReget $r
            @($r.ut_mvinteger) | Should -Be $script:data.Integer1MV
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvinteger = $script:data.Integer1MV
            $r = Save-AndReget $r
            $r.ut_mvinteger = $script:data2.Integer2MV
            $r = Save-AndReget $r
            @($r.ut_mvinteger) | Should -Be $script:data2.Integer2MV
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvinteger = $script:data.Integer1MV
            $r = Save-AndReget $r
            $r.ut_mvinteger = $script:data.Integer1MV[1..2]
            $r = Save-AndReget $r
            @($r.ut_mvinteger) | Should -Be $script:data.Integer1MV[1..2]
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvinteger = $script:data.Integer1MV
            $r = Save-AndReget $r
            $r.ut_mvinteger = @()
            $r = Save-AndReget $r
            @($r.ut_mvinteger).Count | Should -Be 0
        }
    }

    # --- Text -------------------------------------------------------------------------------------
    # Ports PutTextTests.cs (the Text data type has both SV and MV cases in the client tests).

    Context 'Text (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svtext = $script:data.Text1
            $r = Save-AndReget $r
            $r.ut_svtext | Should -Be $script:data.Text1
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svtext = $script:data.Text1
            $r = Save-AndReget $r
            $r.ut_svtext = $script:data2.Text2
            $r = Save-AndReget $r
            $r.ut_svtext | Should -Be $script:data2.Text2
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svtext = $script:data.Text1
            $r = Save-AndReget $r
            $r.ut_svtext = $null
            $r = Save-AndReget $r
            $r.ut_svtext | Should -BeNullOrEmpty
        }
    }

    Context 'Text (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvtext = $script:data.Text1MV
            $r = Save-AndReget $r
            @($r.ut_mvtext) | Should -Be $script:data.Text1MV
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvtext = $script:data.Text1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvtext = $script:data.Text1MV
            $r = Save-AndReget $r
            @($r.ut_mvtext) | Should -Be $script:data.Text1MV
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvtext = $script:data.Text1MV
            $r = Save-AndReget $r
            $r.ut_mvtext = $script:data2.Text2MV
            $r = Save-AndReget $r
            @($r.ut_mvtext) | Should -Be $script:data2.Text2MV
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvtext = $script:data.Text1MV
            $r = Save-AndReget $r
            $r.ut_mvtext = $script:data.Text1MV[1..2]
            $r = Save-AndReget $r
            @($r.ut_mvtext) | Should -Be $script:data.Text1MV[1..2]
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvtext = $script:data.Text1MV
            $r = Save-AndReget $r
            $r.ut_mvtext = @()
            $r = Save-AndReget $r
            @($r.ut_mvtext).Count | Should -Be 0
        }
    }

    # --- Boolean ----------------------------------------------------------------------------------
    # Ports PutBooleanTests.cs (SV only). Modify goes true -> false. The "clears" case checks that a
    # cleared boolean re-reads as null, distinct from a stored $false.

    Context 'Boolean (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svboolean = $script:data.BooleanTrue
            $r = Save-AndReget $r
            $r.ut_svboolean | Should -Be $script:data.BooleanTrue
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svboolean = $true
            $r = Save-AndReget $r
            $r.ut_svboolean = $false
            $r = Save-AndReget $r
            $r.ut_svboolean | Should -Be $false
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svboolean = $true
            $r = Save-AndReget $r
            $r.ut_svboolean = $null
            $r = Save-AndReget $r
            $r.ut_svboolean | Should -BeNullOrEmpty
        }
    }

    # --- DateTime ---------------------------------------------------------------------------------
    # Ports PutDateTimeTests.cs. All datetimes compared instant-based via ConvertTo-UtcSeconds.

    Context 'DateTime (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svdatetime = $script:data.DateTime1
            $r = Save-AndReget $r
            (ConvertTo-UtcSeconds $r.ut_svdatetime) | Should -Be (ConvertTo-UtcSeconds $script:data.DateTime1)
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svdatetime = $script:data.DateTime1
            $r = Save-AndReget $r
            $r.ut_svdatetime = $script:data2.DateTime2
            $r = Save-AndReget $r
            (ConvertTo-UtcSeconds $r.ut_svdatetime) | Should -Be (ConvertTo-UtcSeconds $script:data2.DateTime2)
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svdatetime = $script:data.DateTime1
            $r = Save-AndReget $r
            $r.ut_svdatetime = $null
            $r = Save-AndReget $r
            $r.ut_svdatetime | Should -BeNullOrEmpty
        }
    }

    Context 'DateTime (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvdatetime = $script:data.DateTime1MV
            $r = Save-AndReget $r
            (ConvertTo-UtcSecondsList $r.ut_mvdatetime) | Should -Be (ConvertTo-UtcSecondsList $script:data.DateTime1MV)
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvdatetime = $script:data.DateTime1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvdatetime = $script:data.DateTime1MV
            $r = Save-AndReget $r
            (ConvertTo-UtcSecondsList $r.ut_mvdatetime) | Should -Be (ConvertTo-UtcSecondsList $script:data.DateTime1MV)
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvdatetime = $script:data.DateTime1MV
            $r = Save-AndReget $r
            $r.ut_mvdatetime = $script:data2.DateTime2MV
            $r = Save-AndReget $r
            (ConvertTo-UtcSecondsList $r.ut_mvdatetime) | Should -Be (ConvertTo-UtcSecondsList $script:data2.DateTime2MV)
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvdatetime = $script:data.DateTime1MV
            $r = Save-AndReget $r
            $r.ut_mvdatetime = $script:data.DateTime1MV[1..2]
            $r = Save-AndReget $r
            (ConvertTo-UtcSecondsList $r.ut_mvdatetime) | Should -Be (ConvertTo-UtcSecondsList $script:data.DateTime1MV[1..2])
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvdatetime = $script:data.DateTime1MV
            $r = Save-AndReget $r
            $r.ut_mvdatetime = @()
            $r = Save-AndReget $r
            @($r.ut_mvdatetime).Count | Should -Be 0
        }
    }

    # --- Binary -----------------------------------------------------------------------------------
    # Ports PutBinaryTests.cs. Binary values compared byte-by-byte (Assert-BinaryEqual /
    # Assert-BinaryCollection). MV states are built from array slices to keep each element an
    # intact byte[] (wrapping a single byte[] in @() would flatten it into individual bytes).

    Context 'Binary (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svbinary = $script:data.Binary1
            $r = Save-AndReget $r
            Assert-BinaryEqual $r.ut_svbinary $script:data.Binary1
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svbinary = $script:data.Binary1
            $r = Save-AndReget $r
            $r.ut_svbinary = $script:data2.Binary2
            $r = Save-AndReget $r
            Assert-BinaryEqual $r.ut_svbinary $script:data2.Binary2
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svbinary = $script:data.Binary1
            $r = Save-AndReget $r
            $r.ut_svbinary = $null
            $r = Save-AndReget $r
            $r.ut_svbinary | Should -BeNullOrEmpty
        }
    }

    Context 'Binary (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvbinary = $script:data.Binary1MV
            $r = Save-AndReget $r
            Assert-BinaryCollection $r.ut_mvbinary $script:data.Binary1MV
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvbinary = $script:data.Binary1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvbinary = $script:data.Binary1MV
            $r = Save-AndReget $r
            Assert-BinaryCollection $r.ut_mvbinary $script:data.Binary1MV
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvbinary = $script:data.Binary1MV
            $r = Save-AndReget $r
            $r.ut_mvbinary = $script:data2.Binary2MV
            $r = Save-AndReget $r
            Assert-BinaryCollection $r.ut_mvbinary $script:data2.Binary2MV
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvbinary = $script:data.Binary1MV
            $r = Save-AndReget $r
            $r.ut_mvbinary = $script:data.Binary1MV[1..2]
            $r = Save-AndReget $r
            Assert-BinaryCollection $r.ut_mvbinary $script:data.Binary1MV[1..2]
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvbinary = $script:data.Binary1MV
            $r = Save-AndReget $r
            $r.ut_mvbinary = @()
            $r = Save-AndReget $r
            @($r.ut_mvbinary).Count | Should -Be 0
        }
    }

    # --- Reference --------------------------------------------------------------------------------
    # Ports PutReferenceTests.cs. References are set from and compared to reftest1..6 ObjectIDs (never
    # deleted). Ref1/Ref2 are the two single references; Ref1MV = {1,2,3}, Ref2MV = {4,5,6}. Compared
    # by string form of the ObjectID.

    Context 'Reference (single-valued)' {

        It 'sets a value on an existing object' {
            $r = New-PutResource
            $r.ut_svreference = $script:refs.Ref1
            $r = Save-AndReget $r
            "$($r.ut_svreference)" | Should -Be "$($script:refs.Ref1)"
        }

        It 'modifies an existing value' {
            $r = New-PutResource
            $r.ut_svreference = $script:refs.Ref1
            $r = Save-AndReget $r
            $r.ut_svreference = $script:refs.Ref2
            $r = Save-AndReget $r
            "$($r.ut_svreference)" | Should -Be "$($script:refs.Ref2)"
        }

        It 'clears the value' {
            $r = New-PutResource
            $r.ut_svreference = $script:refs.Ref1
            $r = Save-AndReget $r
            $r.ut_svreference = $null
            $r = Save-AndReget $r
            $r.ut_svreference | Should -BeNullOrEmpty
        }
    }

    Context 'Reference (multi-valued)' {

        It 'sets values on an existing object' {
            $r = New-PutResource
            $r.ut_mvreference = $script:refs.Ref1MV
            $r = Save-AndReget $r
            (ConvertTo-StringList $r.ut_mvreference) | Should -Be (ConvertTo-StringList $script:refs.Ref1MV)
        }

        It 'grows the collection with a further value' {
            $r = New-PutResource
            $r.ut_mvreference = $script:refs.Ref1MV[0..1]
            $r = Save-AndReget $r
            $r.ut_mvreference = $script:refs.Ref1MV
            $r = Save-AndReget $r
            (ConvertTo-StringList $r.ut_mvreference) | Should -Be (ConvertTo-StringList $script:refs.Ref1MV)
        }

        It 'replaces all values' {
            $r = New-PutResource
            $r.ut_mvreference = $script:refs.Ref1MV
            $r = Save-AndReget $r
            $r.ut_mvreference = $script:refs.Ref2MV
            $r = Save-AndReget $r
            (ConvertTo-StringList $r.ut_mvreference) | Should -Be (ConvertTo-StringList $script:refs.Ref2MV)
        }

        It 'removes one value' {
            $r = New-PutResource
            $r.ut_mvreference = $script:refs.Ref1MV
            $r = Save-AndReget $r
            $r.ut_mvreference = $script:refs.Ref1MV[1..2]
            $r = Save-AndReget $r
            (ConvertTo-StringList $r.ut_mvreference) | Should -Be (ConvertTo-StringList $script:refs.Ref1MV[1..2])
        }

        It 'clears all values' {
            $r = New-PutResource
            $r.ut_mvreference = $script:refs.Ref1MV
            $r = Save-AndReget $r
            $r.ut_mvreference = @()
            $r = Save-AndReget $r
            @($r.ut_mvreference).Count | Should -Be 0
        }
    }

    # --- Composite --------------------------------------------------------------------------------
    # Ports PutCompositeTests.cs: populate two objects, then change one attribute on each and persist
    # both changes in a single Save-Resource of an array; verify each object round-trips its own value.

    Context 'Composite (multiple resources in one save)' {

        It 'saves changes to multiple resources in a single composite save' {
            $r1 = New-PutResource
            Set-TestUserData -Resource $r1 -Data $script:data -References $script:refs
            $r1 = Save-AndReget $r1

            $r2 = New-PutResource
            Set-TestUserData -Resource $r2 -Data $script:data -References $script:refs
            $r2 = Save-AndReget $r2

            $r1.ut_svstring = $script:data2.String2
            $r2.ut_svstring = $script:data2.String3

            Save-Resource @($r1, $r2)

            (Get-Resource -ID $r1.ObjectID).ut_svstring | Should -Be $script:data2.String2
            (Get-Resource -ID $r2.ObjectID).ut_svstring | Should -Be $script:data2.String3
        }
    }
}
