<#
    Cmdlet port of the resourcemanagement-client XPath unit tests (XPathExpressionTests.cs,
    XpathPredicate*Tests.cs). Instead of driving the client's XPathQuery/XPathFilterBuilder
    types directly, these tests build predicates and groups through the cmdlet surface
    (New-XPathQuery, New-XPathQueryGroup, New-XPathExpression) and execute them with
    Search-Resources, then assert that the search returns exactly the objects whose attributes
    match the predicate under test.

    Scoping: every predicate is combined (via an And group) with a unique per-test marker so a
    search only ever matches the objects this test created, never the shared _unitTestObject
    pool. The marker lives in an attribute the predicate under test does not touch: string tests
    mark with ut_svinteger, every other type marks with ut_svstring.

    These tests assert intended behaviour. New-XPathExpression and New-XPathQueryGroup currently
    throw an InvalidCastException because of the parameter-binding defect in GitHub issue #45, so
    every test that routes through them fails until that defect is fixed. That is deliberate: the
    tests describe the correct behaviour and become the regression guard once #45 is resolved.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects

    $script:UnitTestType = '_unitTestObject'
    $script:created = [System.Collections.Generic.List[object]]::new()

    function New-XPathTestObject
    {
        # Creates and saves a _unitTestObject instance, tracks its ObjectID for AfterAll cleanup,
        # and returns the saved object. Attributes is a map of attribute system name to value; an
        # empty map creates a bare object.
        param([Parameter(Mandatory)] [hashtable] $Attributes)

        $resource = New-Resource -ObjectType $script:UnitTestType
        foreach ($name in $Attributes.Keys)
        {
            $resource.$name = $Attributes[$name]
        }
        Save-Resource $resource
        $script:created.Add($resource.ObjectID)
        return $resource
    }

    function Clear-TargetAttribute
    {
        # Re-reads an object from the service and explicitly clears one attribute. This mirrors the
        # refresh/set-null/save dance in XpathPredicateBooleanTests.TestSVBooleanIsNotPresent. The
        # FIM service stores an omitted single-valued boolean as false at create time even though
        # the create request carries no value for it (verified against the service's own Request
        # log), so the only way to make a boolean genuinely absent is to clear it with an update
        # after creation. Other attribute types remain absent when omitted from a create.
        param([Parameter(Mandatory)] $Resource, [Parameter(Mandatory)] [string] $Attribute)

        $fresh = Get-Resource -ID $Resource.ObjectID
        $fresh.$Attribute = $null
        Save-Resource $fresh
        return $fresh
    }

    function Invoke-XPathSearch
    {
        # Wraps a query object or group in an expression against _unitTestObject and returns the
        # search results as an array.
        param([Parameter(Mandatory)] $QueryObject)

        $expression = New-XPathExpression -ObjectType $script:UnitTestType -QueryObject $QueryObject
        return @(Search-Resources -XPath $expression)
    }

    function Assert-OnlyMatches
    {
        # Asserts the result set contains exactly the expected objects (by ObjectID), no more and
        # no fewer.
        param($Results, [object[]] $Expected)

        $resultIds = @($Results | ForEach-Object { "$($_.ObjectID)" })
        $expectedIds = @($Expected | ForEach-Object { "$($_.ObjectID)" })

        $resultIds.Count | Should -Be $expectedIds.Count
        foreach ($id in $expectedIds)
        {
            $resultIds | Should -Contain $id
        }
    }

    function Test-ScopedPredicate
    {
        # Creates a matching and a non-matching object (both carrying the same unique marker),
        # builds an And group of [marker predicate, predicate under test], searches, and asserts
        # only the matching object is returned.
        #
        # A $null MatchValue/NonMatchValue means the target attribute is left unset on that object
        # (used by the presence tests). For IsPresent/IsNotPresent no query value is supplied.
        param(
            [Parameter(Mandatory)] [string] $MarkerAttr,
            [Parameter(Mandatory)] [string] $TargetAttr,
            [Parameter(Mandatory)] [string] $Operator,
            $QueryValue,
            $MatchValue,
            $NonMatchValue,
            [switch] $Sleep,
            [switch] $ClearAbsent
        )

        if ($MarkerAttr -eq 'ut_svinteger')
        {
            $markerValue = [long](Get-Random -Minimum 100000000 -Maximum 2000000000)
        }
        else
        {
            $markerValue = "xpath-$([Guid]::NewGuid().ToString('N'))"
        }

        $markerQuery = New-XPathQuery -AttributeName $MarkerAttr -Operator Equals -Value $markerValue

        $matchSet = @{}
        $matchSet[$MarkerAttr] = $markerValue
        if ($null -ne $MatchValue)
        {
            $matchSet[$TargetAttr] = $MatchValue
        }
        $match = New-XPathTestObject $matchSet

        $nonSet = @{}
        $nonSet[$MarkerAttr] = $markerValue
        if ($null -ne $NonMatchValue)
        {
            $nonSet[$TargetAttr] = $NonMatchValue
        }
        $nonMatch = New-XPathTestObject $nonSet

        if ($ClearAbsent)
        {
            # Single-valued booleans omitted at create time are stored as false by the FIM service,
            # so presence predicates need the absent-side object explicitly cleared after creation
            # (see Clear-TargetAttribute).
            if ($null -eq $MatchValue)
            {
                $match = Clear-TargetAttribute -Resource $match -Attribute $TargetAttr
            }
            if ($null -eq $NonMatchValue)
            {
                $nonMatch = Clear-TargetAttribute -Resource $nonMatch -Attribute $TargetAttr
            }
        }

        if ($Operator -eq 'IsPresent' -or $Operator -eq 'IsNotPresent')
        {
            $predicate = New-XPathQuery -AttributeName $TargetAttr -Operator $Operator
        }
        else
        {
            $predicate = New-XPathQuery -AttributeName $TargetAttr -Operator $Operator -Value $QueryValue
        }

        $scoped = New-XPathQueryGroup -Operator And -Queries $markerQuery, $predicate

        if ($Sleep)
        {
            # The MIM full-text index lags creation for the contains() operator; the client unit
            # tests sleep for the same reason before running a contains query.
            Start-Sleep -Seconds 10
        }

        $results = Invoke-XPathSearch $scoped
        Assert-OnlyMatches -Results $results -Expected @($match)
    }
}

AfterAll {
    foreach ($id in $script:created)
    {
        $id | Remove-TestResource
    }
}

Describe 'LithnetRMA XPath queries' {

    Context 'String predicates' {

        It 'single-valued string <Name>' -ForEach @(
            @{ Name = 'equals';                          Op = 'Equals';     Q = 'user0001';  Match = 'user0001';  Non = 'user0002' }
            @{ Name = 'equals a value with a single quote'; Op = 'Equals';  Q = "user'0001"; Match = "user'0001"; Non = 'user0002' }
            @{ Name = 'equals a value with a double quote'; Op = 'Equals';  Q = 'user"0001'; Match = 'user"0001'; Non = 'user0002' }
            @{ Name = 'not equals';                      Op = 'NotEquals';  Q = 'user0001';  Match = 'user0002';  Non = 'user0001' }
            @{ Name = 'ends with';                       Op = 'EndsWith';   Q = '0001';      Match = 'user0001';  Non = 'user0002' }
            @{ Name = 'starts with';                     Op = 'StartsWith'; Q = 'y';         Match = 'yuser0001'; Non = 'xuser0002' }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_svstring -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'single-valued string contains a term after a word break' {
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_svstring -Operator Contains -QueryValue 'abc' -MatchValue 'user abc' -NonMatchValue 'user def' -Sleep
        }

        It 'single-valued string <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = 'user0001'
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_svstring -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }

        It 'rejects an equals value that contains both single and double quotes' {
            $predicate = New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value 'user"''0001'
            {
                $expression = New-XPathExpression -ObjectType $script:UnitTestType -QueryObject $predicate
                Search-Resources -XPath $expression | Out-Null
            } | Should -Throw -ExpectedMessage '*both single and double quotes*'
        }

        It 'multi-valued string <Name>' -ForEach @(
            @{ Name = 'equals';      Op = 'Equals';     Q = 'user0001'; Match = @('user0001', 'user0002');  Non = @('user0003', 'user0004') }
            @{ Name = 'not equals';  Op = 'NotEquals';  Q = 'user0001'; Match = @('user0003', 'user0004');  Non = @('user0001', 'user0002') }
            @{ Name = 'ends with';   Op = 'EndsWith';   Q = '0001';     Match = @('user0002', 'user0001');  Non = @('user0004', 'user0003') }
            @{ Name = 'starts with'; Op = 'StartsWith'; Q = 'y';        Match = @('yuser0002', 'yuser0001'); Non = @('xuser0004', 'xuser0003') }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_mvstring -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'multi-valued string contains a term after a word break' {
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_mvstring -Operator Contains -QueryValue 'abc' -MatchValue @('sdf abc', '1011 ghi') -NonMatchValue @('123 def', '456 def') -Sleep
        }

        It 'multi-valued string <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = @('user0001', 'user0002')
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svinteger -TargetAttr ut_mvstring -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }
    }

    Context 'Integer predicates' {

        It 'single-valued integer <Name>' -ForEach @(
            @{ Name = 'equals';                Op = 'Equals';               Q = [long]1;  Match = [long]1;  Non = [long]2 }
            @{ Name = 'not equals';            Op = 'NotEquals';            Q = [long]1;  Match = [long]2;  Non = [long]1 }
            @{ Name = 'greater than';          Op = 'GreaterThan';          Q = [long]10; Match = [long]11; Non = [long]5 }
            @{ Name = 'greater than or equal'; Op = 'GreaterThanOrEquals';  Q = [long]10; Match = [long]10; Non = [long]5 }
            @{ Name = 'less than';             Op = 'LessThan';             Q = [long]10; Match = [long]9;  Non = [long]15 }
            @{ Name = 'less than or equal';    Op = 'LessThanOrEquals';     Q = [long]10; Match = [long]10; Non = [long]15 }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svinteger -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'single-valued integer <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = [long]1
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svinteger -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }

        It 'multi-valued integer <Name>' -ForEach @(
            @{ Name = 'equals';                Op = 'Equals';              Q = [long]1;  Match = @([long]1, [long]4);  Non = @([long]2, [long]3) }
            @{ Name = 'not equals';            Op = 'NotEquals';           Q = [long]1;  Match = @([long]3, [long]4);  Non = @([long]1, [long]3) }
            @{ Name = 'greater than';          Op = 'GreaterThan';         Q = [long]10; Match = @([long]9, [long]11); Non = @([long]9, [long]8) }
            @{ Name = 'greater than or equal'; Op = 'GreaterThanOrEquals'; Q = [long]10; Match = @([long]9, [long]10); Non = @([long]9, [long]8) }
            @{ Name = 'less than';             Op = 'LessThan';            Q = [long]10; Match = @([long]9, [long]20); Non = @([long]15, [long]20) }
            @{ Name = 'less than or equal';    Op = 'LessThanOrEquals';    Q = [long]10; Match = @([long]10, [long]20); Non = @([long]15, [long]20) }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvinteger -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'multi-valued integer <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = @([long]1, [long]2)
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvinteger -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }
    }

    Context 'Boolean predicates' {

        It 'single-valued boolean <Name>' -ForEach @(
            @{ Name = 'equals true';     Op = 'Equals';    Q = $true; Match = $true;  Non = $false }
            @{ Name = 'not equals true'; Op = 'NotEquals'; Q = $true; Match = $false; Non = $true }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svboolean -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'single-valued boolean <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            # Ports TestSVBooleanIsPresent/IsNotPresent. The NUnit suite tolerates a created-unset
            # boolean matching a presence query because its result-count assertion is commented out;
            # this suite asserts exact result sets, so the absent-side object is explicitly cleared
            # after creation — the same dance TestSVBooleanIsNotPresent performs on its match object.
            $value = $true
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svboolean -Operator $Op -MatchValue $mv -NonMatchValue $nv -ClearAbsent
        }
    }

    Context 'DateTime predicates' {

        It 'single-valued datetime <Name>' -ForEach @(
            @{ Name = 'equals';                                  Op = 'Equals';              Q = '2000-01-01T00:00:00.000'; Match = '2000-01-01T00:00:00.000'; Non = '3000-01-01T00:00:00.000' }
            @{ Name = 'not equals';                              Op = 'NotEquals';           Q = '2000-01-01T00:00:00.000'; Match = '3000-01-01T00:00:00.000'; Non = '2000-01-01T00:00:00.000' }
            @{ Name = 'greater than';                            Op = 'GreaterThan';         Q = '3000-01-01T00:00:00.000'; Match = '3100-01-01T00:00:00.000'; Non = '2000-01-01T00:00:00.000' }
            @{ Name = 'greater than a function value';           Op = 'GreaterThan';         Q = 'current-dateTime()';      Match = '3000-01-01T00:00:00.000'; Non = '2000-01-01T00:00:00.000' }
            @{ Name = 'greater than or equal';                   Op = 'GreaterThanOrEquals'; Q = '3000-01-01T00:00:00.000'; Match = '3000-01-01T00:00:00.000'; Non = '2000-01-01T00:00:00.000' }
            @{ Name = 'greater than or equal to a function value'; Op = 'GreaterThanOrEquals'; Q = 'current-dateTime()';    Match = '3000-01-01T00:00:00.000'; Non = '2000-01-01T00:00:00.000' }
            @{ Name = 'less than';                               Op = 'LessThan';            Q = '2000-01-01T00:00:00.000'; Match = '1900-01-01T00:00:00.000'; Non = '2100-01-01T00:00:00.000' }
            @{ Name = 'less than a function value';              Op = 'LessThan';            Q = 'current-dateTime()';      Match = '2000-01-01T00:00:00.000'; Non = '3000-01-01T00:00:00.000' }
            @{ Name = 'less than or equal';                      Op = 'LessThanOrEquals';    Q = '2000-01-01T00:00:00.000'; Match = '2000-01-01T00:00:00.000'; Non = '2100-01-01T00:00:00.000' }
            @{ Name = 'less than or equal to a function value';  Op = 'LessThanOrEquals';    Q = 'current-dateTime()';      Match = '2000-01-01T00:00:00.000'; Non = '3000-01-01T00:00:00.000' }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svdatetime -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'single-valued datetime <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = '2000-01-01T00:00:00.000'
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svdatetime -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }

        It 'multi-valued datetime <Name>' -ForEach @(
            @{ Name = 'equals';                                  Op = 'Equals';              Q = '2000-01-01T00:00:00.000'; Match = @('2300-01-01T00:00:00.000', '2000-01-01T00:00:00.000'); Non = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000') }
            @{ Name = 'not equals';                              Op = 'NotEquals';           Q = '2000-01-01T00:00:00.000'; Match = @('2200-01-01T00:00:00.000', '2300-01-01T00:00:00.000'); Non = @('2000-01-01T00:00:00.000', '2100-01-01T00:00:00.000') }
            @{ Name = 'greater than';                            Op = 'GreaterThan';         Q = '2000-01-01T00:00:00.000'; Match = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000'); Non = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000') }
            @{ Name = 'greater than a function value';           Op = 'GreaterThan';         Q = 'current-dateTime()';      Match = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000'); Non = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000') }
            @{ Name = 'greater than or equal';                   Op = 'GreaterThanOrEquals'; Q = '2000-01-01T00:00:00.000'; Match = @('2000-01-01T00:00:00.000', '2100-01-01T00:00:00.000'); Non = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000') }
            @{ Name = 'greater than or equal to a function value'; Op = 'GreaterThanOrEquals'; Q = 'current-dateTime()';    Match = @('2000-01-01T00:00:00.000', '2100-01-01T00:00:00.000'); Non = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000') }
            @{ Name = 'less than';                               Op = 'LessThan';            Q = '2000-01-01T00:00:00.000'; Match = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000'); Non = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000') }
            @{ Name = 'less than a function value';              Op = 'LessThan';            Q = 'current-dateTime()';      Match = @('1900-01-01T00:00:00.000', '1800-01-01T00:00:00.000'); Non = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000') }
            @{ Name = 'less than or equal';                      Op = 'LessThanOrEquals';    Q = '2000-01-01T00:00:00.000'; Match = @('2000-01-01T00:00:00.000', '2200-01-01T00:00:00.000'); Non = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000') }
            @{ Name = 'less than or equal to a function value';  Op = 'LessThanOrEquals';    Q = 'current-dateTime()';      Match = @('2000-01-01T00:00:00.000', '2200-01-01T00:00:00.000'); Non = @('2100-01-01T00:00:00.000', '2200-01-01T00:00:00.000') }
        ) {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvdatetime -Operator $Op -QueryValue $Q -MatchValue $Match -NonMatchValue $Non
        }

        It 'multi-valued datetime <Name>' -ForEach @(
            @{ Name = 'is present';     Op = 'IsPresent';    Side = 'match' }
            @{ Name = 'is not present'; Op = 'IsNotPresent'; Side = 'non' }
        ) {
            $value = @('2000-01-01T00:00:00.000', '2100-01-01T00:00:00.000')
            if ($Side -eq 'match') { $mv = $value; $nv = $null } else { $mv = $null; $nv = $value }
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvdatetime -Operator $Op -MatchValue $mv -NonMatchValue $nv
        }
    }

    Context 'Reference predicates' {

        It 'single-valued reference equals the query value' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svreference -Operator Equals -QueryValue $script:refs.Ref1 -MatchValue $script:refs.Ref1 -NonMatchValue $script:refs.Ref2
        }

        It 'single-valued reference not equals the query value' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svreference -Operator NotEquals -QueryValue $script:refs.Ref1 -MatchValue $script:refs.Ref2 -NonMatchValue $script:refs.Ref1
        }

        It 'single-valued reference is present' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svreference -Operator IsPresent -MatchValue $script:refs.Ref1 -NonMatchValue $null
        }

        It 'single-valued reference is not present' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_svreference -Operator IsNotPresent -MatchValue $null -NonMatchValue $script:refs.Ref1
        }

        It 'multi-valued reference equals the query value' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvreference -Operator Equals -QueryValue $script:refs.Ref1 -MatchValue $script:refs.Ref1MV -NonMatchValue $script:refs.Ref2MV
        }

        It 'multi-valued reference not equals the query value' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvreference -Operator NotEquals -QueryValue $script:refs.Ref1 -MatchValue $script:refs.Ref2MV -NonMatchValue $script:refs.Ref1MV
        }

        It 'multi-valued reference is present' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvreference -Operator IsPresent -MatchValue $script:refs.Ref1MV -NonMatchValue $null
        }

        It 'multi-valued reference is not present' {
            Test-ScopedPredicate -MarkerAttr ut_svstring -TargetAttr ut_mvreference -Operator IsNotPresent -MatchValue $null -NonMatchValue $script:refs.Ref1MV
        }
    }

    Context 'Query groups' {

        It 'returns matches for a group containing a single predicate' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $match = New-XPathTestObject @{ ut_svstring = $marker }
            $nonMatch = New-XPathTestObject @{ ut_svstring = "xpath-$([Guid]::NewGuid().ToString('N'))" }

            $group = New-XPathQueryGroup -Operator And -Queries (New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker)

            $results = Invoke-XPathSearch $group
            Assert-OnlyMatches -Results $results -Expected @($match)
        }

        It 'returns only objects satisfying every predicate in an And group' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $match = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]100 }
            $wrongInteger = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]999 }
            $wrongMarker = New-XPathTestObject @{ ut_svstring = "xpath-$([Guid]::NewGuid().ToString('N'))"; ut_svinteger = [long]100 }

            $markerQuery = New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker
            $integerQuery = New-XPathQuery -AttributeName ut_svinteger -Operator Equals -Value ([long]100)
            $group = New-XPathQueryGroup -Operator And -Queries $markerQuery, $integerQuery

            $results = Invoke-XPathSearch $group
            Assert-OnlyMatches -Results $results -Expected @($match)
        }

        It 'returns objects satisfying any predicate in an Or group' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $matchInteger = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]200 }
            $matchString = New-XPathTestObject @{ ut_svstring = $marker; ut_mvstring = @('grpor term', 'other') }
            $neither = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]1 }

            $markerQuery = New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker
            $orInteger = New-XPathQuery -AttributeName ut_svinteger -Operator Equals -Value ([long]200)
            $orString = New-XPathQuery -AttributeName ut_mvstring -Operator Equals -Value 'grpor term'
            $orGroup = New-XPathQueryGroup -Operator Or -Queries $orInteger, $orString
            $group = New-XPathQueryGroup -Operator And -Queries $markerQuery, $orGroup

            $results = Invoke-XPathSearch $group
            Assert-OnlyMatches -Results $results -Expected @($matchInteger, $matchString)
        }

        It 'evaluates a nested group (an And of a predicate and an Or group)' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $match = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]55; ut_mvstring = @('nested term', 'x') }
            $wrongInteger = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]56; ut_mvstring = @('nested term') }
            $wrongOr = New-XPathTestObject @{ ut_svstring = $marker; ut_svinteger = [long]55; ut_mvstring = @('no term') }

            $markerQuery = New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker
            $integerQuery = New-XPathQuery -AttributeName ut_svinteger -Operator Equals -Value ([long]55)
            $orString = New-XPathQuery -AttributeName ut_mvstring -Operator Equals -Value 'nested term'
            $orBoolean = New-XPathQuery -AttributeName ut_svboolean -Operator Equals -Value $true
            $childOrGroup = New-XPathQueryGroup -Operator Or -Queries $orString, $orBoolean
            $group = New-XPathQueryGroup -Operator And -Queries $markerQuery, $integerQuery, $childOrGroup

            $results = Invoke-XPathSearch $group
            Assert-OnlyMatches -Results $results -Expected @($match)
        }
    }

    Context 'Expression building' {

        It 'resolves a reference predicate whose value is a child expression' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $filterTarget = New-XPathTestObject @{ ut_svstring = $marker }
            $children = 1..3 | ForEach-Object {
                New-XPathTestObject @{ ut_svreference = $filterTarget.ObjectID }
            }

            $childExpression = New-XPathExpression -ObjectType $script:UnitTestType -QueryObject (
                New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker)
            $referencePredicate = New-XPathQuery -AttributeName ut_svreference -Operator Equals -Value $childExpression
            $outerExpression = New-XPathExpression -ObjectType $script:UnitTestType -QueryObject $referencePredicate

            $results = @(Search-Resources -XPath $outerExpression)
            Assert-OnlyMatches -Results $results -Expected $children
        }

        It 'dereferences a reference attribute to return the parent objects' {
            $marker = "xpath-$([Guid]::NewGuid().ToString('N'))"
            $parent = New-XPathTestObject @{}
            $filterTarget = New-XPathTestObject @{ ut_svstring = $marker; ut_svreference = $parent.ObjectID }

            $expression = New-XPathExpression -ObjectType $script:UnitTestType -DereferenceAttribute ut_svreference -QueryObject (
                New-XPathQuery -AttributeName ut_svstring -Operator Equals -Value $marker)

            $results = @(Search-Resources -XPath $expression)
            Assert-OnlyMatches -Results $results -Expected @($parent)
        }
    }

    Context 'Unsupported operator and type combinations' {

        It 'rejects <Op> on <Attr>' -ForEach @(
            @{ Attr = 'ut_svstring';    Op = 'GreaterThan' }
            @{ Attr = 'ut_svstring';    Op = 'GreaterThanOrEquals' }
            @{ Attr = 'ut_svstring';    Op = 'LessThan' }
            @{ Attr = 'ut_svstring';    Op = 'LessThanOrEquals' }
            @{ Attr = 'ut_svinteger';   Op = 'Contains' }
            @{ Attr = 'ut_svinteger';   Op = 'StartsWith' }
            @{ Attr = 'ut_svinteger';   Op = 'EndsWith' }
            @{ Attr = 'ut_mvinteger';   Op = 'Contains' }
            @{ Attr = 'ut_mvinteger';   Op = 'StartsWith' }
            @{ Attr = 'ut_mvinteger';   Op = 'EndsWith' }
            @{ Attr = 'ut_svdatetime';  Op = 'Contains' }
            @{ Attr = 'ut_svdatetime';  Op = 'StartsWith' }
            @{ Attr = 'ut_svdatetime';  Op = 'EndsWith' }
            @{ Attr = 'ut_mvdatetime';  Op = 'Contains' }
            @{ Attr = 'ut_mvdatetime';  Op = 'StartsWith' }
            @{ Attr = 'ut_mvdatetime';  Op = 'EndsWith' }
            @{ Attr = 'ut_svboolean';   Op = 'GreaterThan' }
            @{ Attr = 'ut_svboolean';   Op = 'GreaterThanOrEquals' }
            @{ Attr = 'ut_svboolean';   Op = 'LessThan' }
            @{ Attr = 'ut_svboolean';   Op = 'LessThanOrEquals' }
            @{ Attr = 'ut_svboolean';   Op = 'Contains' }
            @{ Attr = 'ut_svboolean';   Op = 'StartsWith' }
            @{ Attr = 'ut_svboolean';   Op = 'EndsWith' }
            @{ Attr = 'ut_svreference'; Op = 'GreaterThan' }
            @{ Attr = 'ut_svreference'; Op = 'GreaterThanOrEquals' }
            @{ Attr = 'ut_svreference'; Op = 'LessThan' }
            @{ Attr = 'ut_svreference'; Op = 'LessThanOrEquals' }
            @{ Attr = 'ut_svreference'; Op = 'Contains' }
            @{ Attr = 'ut_svreference'; Op = 'StartsWith' }
            @{ Attr = 'ut_svreference'; Op = 'EndsWith' }
            @{ Attr = 'ut_mvreference'; Op = 'GreaterThan' }
            @{ Attr = 'ut_mvreference'; Op = 'GreaterThanOrEquals' }
            @{ Attr = 'ut_mvreference'; Op = 'LessThan' }
            @{ Attr = 'ut_mvreference'; Op = 'LessThanOrEquals' }
            @{ Attr = 'ut_mvreference'; Op = 'Contains' }
            @{ Attr = 'ut_mvreference'; Op = 'StartsWith' }
            @{ Attr = 'ut_mvreference'; Op = 'EndsWith' }
        ) {
            { New-XPathQuery -AttributeName $Attr -Operator $Op -Value 'x' } | Should -Throw
        }
    }
}
