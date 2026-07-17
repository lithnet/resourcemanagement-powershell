<#
    Shared test support for the LithnetRMA Pester suite. This is the PowerShell equivalent of the
    resourcemanagement-client unit tests' Constants.cs, UnitTestHelper.cs and the schema/reference
    bootstrap in Setup.cs. It drives the same _unitTestObject schema and the same test data, so the
    Pester suite mirrors the client unit tests case-for-case at the cmdlet surface.

    Like the client unit tests' Setup.cs, this module does NOT assume the _unitTestObject schema
    exists: Initialize-TestSchema checks for the object type, its ut_* attributes and their bindings
    and creates any that are missing (create rights are assumed), then refreshes the client schema.
#>

# --- Attribute name constants (mirror Constants.cs) -----------------------------------------------

$script:UnitTestObjectType = '_unitTestObject'
$script:Attr = @{
    StringSV    = 'ut_svstring'
    StringMV    = 'ut_mvstring'
    IntegerSV   = 'ut_svinteger'
    IntegerMV   = 'ut_mvinteger'
    ReferenceSV = 'ut_svreference'
    ReferenceMV = 'ut_mvreference'
    TextSV      = 'ut_svtext'
    TextMV      = 'ut_mvtext'
    DateTimeSV  = 'ut_svdatetime'
    DateTimeMV  = 'ut_mvdatetime'
    BinarySV    = 'ut_svbinary'
    BinaryMV    = 'ut_mvbinary'
    BooleanSV   = 'ut_svboolean'
    AccountName = 'AccountName'
}

function ConvertTo-UtcSeconds
{
    <# Normalises a datetime returned by MIM (which may come back as local time with an offset) to a
       UTC, second-precision string so comparisons are instant-based, not representation-based. #>
    param([Parameter(Mandatory)] $Value)
    return ([datetime]$Value).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss')
}

function Get-TestData
{
    <# Returns a hashtable of round-trippable test data mirroring Constants.cs. DateTimes are
       truncated to whole seconds because the MIM service stores second precision. #>
    $now = [DateTime]::UtcNow
    $trunc = { param($d) [DateTime]::new($d.Year, $d.Month, $d.Day, $d.Hour, $d.Minute, $d.Second, [DateTimeKind]::Utc) }

    return @{
        Attr = $script:Attr
        UnitTestObjectType = $script:UnitTestObjectType

        String1   = 'testString1'
        String1MV = @('testString4', 'testString5', 'testString6')
        Integer1  = [long]4
        Integer1MV = @([long]13, [long]14, [long]15)
        Text1     = 'testText1'
        Text1MV   = @('testText4', 'testText5', 'testText6')
        BooleanTrue = $true
        Binary1   = [byte[]]@(0, 1, 2, 3)
        Binary1MV = @(, [byte[]]@(12, 13, 14, 15)) + @(, [byte[]]@(16, 17, 18, 19)) + @(, [byte[]]@(20, 21, 22, 23))
        DateTime1 = & $trunc $now
        DateTime1MV = @((& $trunc $now.AddDays(3)), (& $trunc $now.AddDays(4)), (& $trunc $now.AddDays(5)))
    }
}

# --- Connection (mirrors the unit tests' appsettings.json / App.config) ---------------------------

function Connect-TestClient
{
    <#
        The SPN differs by connection mode because it authenticates a different service. Direct and
        LocalProxy connections authenticate the MIM Service itself (FIMService/..., held by the MIM
        service account). A RemoteProxy connection authenticates the proxy service, which runs as
        NT AUTHORITY\NetworkService, so its SPN belongs to the host computer account (host/...).
        For RemoteProxy, LITHNETRMA_TEST_PROXY_SPN overrides; when unset, no SPN is passed so the
        client's own default applies - the same configuration a customer gets out of the box.
    #>
    param(
        [string] $BaseAddress = $(if ($env:LITHNETRMA_TEST_BASEADDRESS) { $env:LITHNETRMA_TEST_BASEADDRESS } else { 'fimsvc' }),
        [string] $ServicePrincipalName = $(if ($env:LITHNETRMA_TEST_SPN) { $env:LITHNETRMA_TEST_SPN } else { 'FIMService/fimsvc' }),
        [string] $ConnectionMode = $(if ($env:LITHNETRMA_TEST_MODE) { $env:LITHNETRMA_TEST_MODE } else { 'LocalProxy' }),
        [pscredential] $Credential
    )

    $arguments = @{
        BaseAddress = $BaseAddress
        ConnectionMode = $ConnectionMode
    }

    if ($ConnectionMode -eq 'RemoteProxy')
    {
        if ($env:LITHNETRMA_TEST_PROXY_SPN)
        {
            $arguments.ServicePrincipalName = $env:LITHNETRMA_TEST_PROXY_SPN
        }
    }
    else
    {
        $arguments.ServicePrincipalName = $ServicePrincipalName
    }

    if ($Credential)
    {
        $arguments.Credentials = $Credential
    }

    Set-ResourceManagementClient @arguments
}

# --- Schema bootstrap (mirror Setup.cs PrepareRMSForUnitTests) -------------------------------------

$script:SchemaInitialized = $false

function Get-ResourceByKeyOrNull
{
    param([string] $ObjectType, [string] $AttributeName, $AttributeValue)
    try
    {
        return Get-Resource -ObjectType $ObjectType -AttributeName $AttributeName -AttributeValue $AttributeValue
    }
    catch
    {
        return $null
    }
}

function New-SchemaObjectTypeIfMissing
{
    $existing = Get-ResourceByKeyOrNull -ObjectType ObjectTypeDescription -AttributeName Name -AttributeValue $script:UnitTestObjectType
    if ($existing) { return $existing }

    $r = New-Resource -ObjectType ObjectTypeDescription
    $r.Name = $script:UnitTestObjectType
    $r.DisplayName = $script:UnitTestObjectType
    Save-Resource $r
    $script:SchemaChanged = $true
    return $r
}

function New-SchemaAttributeIfMissing
{
    param([string] $Name, [string] $DataType, [bool] $Multivalued)

    $existing = Get-ResourceByKeyOrNull -ObjectType AttributeTypeDescription -AttributeName Name -AttributeValue $Name
    if ($existing) { return $existing }

    $r = New-Resource -ObjectType AttributeTypeDescription
    $r.Name = $Name
    $r.DisplayName = $Name
    $r.Multivalued = $Multivalued
    $r.DataType = $DataType
    Save-Resource $r
    $script:SchemaChanged = $true
    return $r
}

function New-SchemaBindingIfMissing
{
    param($ObjectTypeResource, $AttributeResource)

    $existing = $null
    try
    {
        $existing = Get-Resource -ObjectType BindingDescription -AttributeValuePairs @{
            BoundObjectType    = $ObjectTypeResource.ObjectID
            BoundAttributeType = $AttributeResource.ObjectID
        }
    }
    catch
    {
        $existing = $null
    }

    if ($existing) { return }

    $r = New-Resource -ObjectType BindingDescription
    $r.BoundObjectType = $ObjectTypeResource.ObjectID
    $r.BoundAttributeType = $AttributeResource.ObjectID
    $r.Required = $false
    Save-Resource $r
    $script:SchemaChanged = $true
}

function Initialize-TestSchema
{
    <# Ensures the _unitTestObject type, its ut_* attributes and their bindings exist, creating any
       that are missing and refreshing the client schema only if something was created. Runs once per
       process (guarded), mirroring the client unit tests' OneTimeSetUp. #>
    if ($script:SchemaInitialized) { return }
    $script:SchemaChanged = $false

    $objectType = New-SchemaObjectTypeIfMissing

    $definitions = @(
        @{ Name = $script:Attr.StringSV;    DataType = 'String';    Multivalued = $false }
        @{ Name = $script:Attr.StringMV;    DataType = 'String';    Multivalued = $true  }
        @{ Name = $script:Attr.IntegerSV;   DataType = 'Integer';   Multivalued = $false }
        @{ Name = $script:Attr.IntegerMV;   DataType = 'Integer';   Multivalued = $true  }
        @{ Name = $script:Attr.ReferenceSV; DataType = 'Reference'; Multivalued = $false }
        @{ Name = $script:Attr.ReferenceMV; DataType = 'Reference'; Multivalued = $true  }
        @{ Name = $script:Attr.TextSV;      DataType = 'Text';      Multivalued = $false }
        @{ Name = $script:Attr.TextMV;      DataType = 'Text';      Multivalued = $true  }
        @{ Name = $script:Attr.DateTimeSV;  DataType = 'DateTime';  Multivalued = $false }
        @{ Name = $script:Attr.DateTimeMV;  DataType = 'DateTime';  Multivalued = $true  }
        @{ Name = $script:Attr.BinarySV;    DataType = 'Binary';    Multivalued = $false }
        @{ Name = $script:Attr.BinaryMV;    DataType = 'Binary';    Multivalued = $true  }
        @{ Name = $script:Attr.BooleanSV;   DataType = 'Boolean';   Multivalued = $false }
    )

    $attributes = foreach ($d in $definitions)
    {
        New-SchemaAttributeIfMissing -Name $d.Name -DataType $d.DataType -Multivalued $d.Multivalued
    }

    # AccountName is a built-in MIM attribute; ensure it and bind it, matching Setup.cs.
    $accountName = New-SchemaAttributeIfMissing -Name $script:Attr.AccountName -DataType 'String' -Multivalued $false

    foreach ($attribute in @($attributes) + @($accountName))
    {
        New-SchemaBindingIfMissing -ObjectTypeResource $objectType -AttributeResource $attribute
    }

    if ($script:SchemaChanged)
    {
        Update-ResourceManagementClientSchema
    }

    $script:SchemaInitialized = $true
}

# --- Clean slate (mirror UnitTestHelper.DeleteAllTestObjects) ---------------------------------------

function Clear-AllTestObjects
{
    <# Deletes every _unitTestObject instance so each run starts from a clean, deterministic slate.
       Mirrors the client unit tests' reliance on an isolated set of test objects. Returns the count
       deleted. #>
    $all = @(Search-Resources -XPath "/$($script:UnitTestObjectType)" -AttributesToGet ObjectID)
    if ($all.Count -eq 0)
    {
        return 0
    }

    $ids = @($all | ForEach-Object { $_.ObjectID })
    $batchSize = 200
    for ($i = 0; $i -lt $ids.Count; $i += $batchSize)
    {
        $chunk = $ids[$i..([Math]::Min($i + $batchSize - 1, $ids.Count - 1))]
        Remove-Resource -ID $chunk
    }

    return $ids.Count
}

$script:EnvironmentInitialized = $false

function Initialize-TestEnvironment
{
    <# Once-per-process suite setup: ensure the schema exists, then clear all _unitTestObject
       instances so the run starts clean. Reference objects are (re)created by the tests that need
       them, after this clean slate. #>
    if ($script:EnvironmentInitialized)
    {
        return
    }

    Initialize-TestSchema
    [void](Clear-AllTestObjects)
    $script:EnvironmentInitialized = $true
}

# --- Reference objects (mirror Setup.cs CreateReferenceTestObjects) --------------------------------

function Initialize-ReferenceObjects
{
    <# Ensures reftest1..reftest6 exist and returns their ObjectIDs plus the two MV groupings used
       by the reference-attribute tests. Idempotent: reuses existing objects. #>
    $ids = @{}
    foreach ($n in 1..6)
    {
        $name = "reftest$n"
        $existing = $null
        try { $existing = Get-Resource -ObjectType $script:UnitTestObjectType -AttributeName AccountName -AttributeValue $name -AttributesToGet ObjectID } catch { }

        if ($existing)
        {
            $ids["Ref$n"] = $existing.ObjectID
        }
        else
        {
            $r = New-Resource -ObjectType $script:UnitTestObjectType
            $r.AccountName = $name
            Save-Resource $r
            $ids["Ref$n"] = $r.ObjectID
        }
    }

    $ids['Ref1MV'] = @($ids['Ref1'], $ids['Ref2'], $ids['Ref3'])
    $ids['Ref2MV'] = @($ids['Ref4'], $ids['Ref5'], $ids['Ref6'])
    return $ids
}

# --- Resource helpers (mirror UnitTestHelper) -----------------------------------------------------

function New-TestResource
{
    param([string] $AccountName)
    $r = New-Resource -ObjectType $script:UnitTestObjectType
    if ($AccountName)
    {
        $r.($script:Attr.AccountName) = $AccountName
    }
    return $r
}

function Set-TestUserData
{
    <# Mirrors UnitTestHelper.PopulateTestUserData: sets every attribute type to its test value. #>
    param(
        [Parameter(Mandatory)] $Resource,
        [Parameter(Mandatory)] [hashtable] $Data,
        [Parameter(Mandatory)] [hashtable] $References
    )
    $a = $Data.Attr
    $Resource.($a.StringSV)    = $Data.String1
    $Resource.($a.StringMV)    = $Data.String1MV
    $Resource.($a.IntegerSV)   = $Data.Integer1
    $Resource.($a.IntegerMV)   = $Data.Integer1MV
    $Resource.($a.TextSV)      = $Data.Text1
    $Resource.($a.TextMV)      = $Data.Text1MV
    $Resource.($a.BooleanSV)   = $Data.BooleanTrue
    $Resource.($a.BinarySV)    = $Data.Binary1
    $Resource.($a.BinaryMV)    = $Data.Binary1MV
    $Resource.($a.DateTimeSV)  = $Data.DateTime1
    $Resource.($a.DateTimeMV)  = $Data.DateTime1MV
    $Resource.($a.ReferenceSV) = $References.Ref1
    $Resource.($a.ReferenceMV) = $References.Ref1MV
}

function Assert-TestUserData
{
    <# Mirrors UnitTestHelper.ValidateTestUserData: asserts every attribute round-tripped. #>
    param(
        [Parameter(Mandatory)] $Resource,
        [Parameter(Mandatory)] [hashtable] $Data,
        [Parameter(Mandatory)] [hashtable] $References
    )
    $a = $Data.Attr
    $Resource.($a.StringSV)   | Should -Be $Data.String1
    $Resource.($a.IntegerSV)  | Should -Be $Data.Integer1
    $Resource.($a.TextSV)     | Should -Be $Data.Text1
    $Resource.($a.BooleanSV)  | Should -Be $Data.BooleanTrue
    (ConvertTo-UtcSeconds $Resource.($a.DateTimeSV)) | Should -Be (ConvertTo-UtcSeconds $Data.DateTime1)

    @($Resource.($a.StringMV))  | Should -Be $Data.String1MV
    @($Resource.($a.IntegerMV)) | Should -Be $Data.Integer1MV
    @($Resource.($a.TextMV))    | Should -Be $Data.Text1MV
    @($Resource.($a.DateTimeMV) | ForEach-Object { ConvertTo-UtcSeconds $_ }) | Should -Be @($Data.DateTime1MV | ForEach-Object { ConvertTo-UtcSeconds $_ })

    "$($Resource.($a.ReferenceSV))" | Should -Be "$($References.Ref1)"
}

# --- Cleanup --------------------------------------------------------------------------------------

function Remove-TestResource
{
    <# Deletes by ObjectID, ignoring 'not found' so cleanup is safe to call unconditionally. #>
    param([Parameter(ValueFromPipeline)] $Id)
    process
    {
        if (-not $Id) { return }
        try { Remove-Resource -ID $Id } catch { }
    }
}

Export-ModuleMember -Function ConvertTo-UtcSeconds, Get-TestData, Connect-TestClient, Initialize-TestSchema,
    Clear-AllTestObjects, Initialize-TestEnvironment, Initialize-ReferenceObjects, New-TestResource,
    Set-TestUserData, Assert-TestUserData, Remove-TestResource
