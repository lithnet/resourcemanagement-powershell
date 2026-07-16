<#
    Cmdlet equivalent of the client unit tests' SearchTests.cs. Exercises Search-Resources,
    Search-ResourcesPaged and Get-ResourceCount and asserts what the cmdlet surface exposes:
    a filter returns exactly the expected objects, results can be sorted, -AttributesToGet
    controls which attribute values come back, a paged search reports a total and yields pages
    that can be navigated (including index/page-size jumps), a non-matching filter yields nothing,
    and an unparseable filter throws.

    The lab MIM holds unrelated objects, so every query is scoped to objects this file created,
    tagged with a per-run marker stored in ut_svstring. That mirrors the source tests' reliance on
    the reftest1..6 fixture (which had a known, fixed membership) without depending on shared data.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects
}

Describe 'Search-Resources / Search-ResourcesPaged' {

    BeforeAll {
        # A set of six objects that all share one marker (for filtering) and carry sortable, unique
        # AccountNames "<marker>-1".."<marker>-6". These back the search / sort / paged / count cases,
        # standing in for the source tests' reftest1..6 fixture.
        $script:sortMarker = "search-$([Guid]::NewGuid().ToString('N'))"
        $script:sortFilter = "/_unitTestObject[ut_svstring = '$($script:sortMarker)']"
        $script:sortNames  = 1..6 | ForEach-Object { "$($script:sortMarker)-$_" }
        $script:sortIds    = [System.Collections.Generic.List[object]]::new()

        foreach ($name in $script:sortNames)
        {
            $r = New-Resource -ObjectType _unitTestObject
            $r.AccountName = $name
            $r.ut_svstring = $script:sortMarker
            Save-Resource $r
            $script:sortIds.Add($r.ObjectID)
        }

        # A single object with two populated attributes, used to prove -AttributesToGet limits which
        # attribute values are returned: ut_svstring is the filter/marker, ut_svinteger is the payload
        # that should only come back when explicitly requested.
        $script:attrMarker = "search-$([Guid]::NewGuid().ToString('N'))"
        $script:attrFilter = "/_unitTestObject[ut_svstring = '$($script:attrMarker)']"
        $script:attrInteger = [long]42

        $a = New-Resource -ObjectType _unitTestObject
        $a.ut_svstring = $script:attrMarker
        $a.ut_svinteger = $script:attrInteger
        Save-Resource $a
        $script:attrId = $a.ObjectID
    }

    AfterAll {
        foreach ($id in $script:sortIds) { $id | Remove-TestResource }
        $script:attrId | Remove-TestResource
    }

    Context 'basic search' {

        It 'returns exactly the objects matched by the filter' {
            # Ports SearchTest(Sync|Async): a query returns the expected result set. The source only
            # checked that the enumerated count equalled the reported count (an internal-consistency
            # assertion, dropped here); at the cmdlet surface we assert the returned ObjectIDs are
            # precisely the ones we created. ObjectID is always returned, even with default attributes.
            $results = Search-Resources -XPath $script:sortFilter
            $returnedIds = @($results | ForEach-Object { "$($_.ObjectID)" }) | Sort-Object
            $expectedIds = @($script:sortIds | ForEach-Object { "$_" }) | Sort-Object

            $returnedIds.Count | Should -Be 6
            $returnedIds | Should -Be $expectedIds
        }

        It 'returns nothing for a filter that matches no objects' {
            # Ports SearchTest(Sync|Async)NoResults: an empty result set for a non-matching filter.
            $noMatch = "no-such-$([Guid]::NewGuid().ToString('N'))"
            $results = @(Search-Resources -XPath "/_unitTestObject[ut_svstring = '$noMatch']")
            $results.Count | Should -Be 0
        }

        It 'throws when the XPath filter cannot be processed' {
            # Ports SearchBadFilter: an unparseable filter surfaces as a terminating error
            # (CannotProcessFilterException in the client) rather than silently returning nothing.
            { Search-Resources -XPath '!not a filter!' } | Should -Throw
        }
    }

    Context 'sorted search' {

        It 'sorts results ascending by the requested attribute' {
            # Ports SearchTestSyncSortedAsc / SearchTestAsyncSortedAscAsync (the sync/async pair
            # collapses to one cmdlet call). -AttributesToGet AccountName is required for AccountName
            # to be returned so the order can be observed.
            $results = Search-Resources -XPath $script:sortFilter -AttributesToGet AccountName -SortAttributes AccountName
            $names = @($results | ForEach-Object { $_.AccountName })

            $names.Count | Should -Be 6
            $names | Should -Be $script:sortNames
        }

        It 'sorts results descending by the requested attribute' {
            # Ports SearchTestSyncSortedDesc / SearchTestAsyncSortedDescAsync.
            $results = Search-Resources -XPath $script:sortFilter -AttributesToGet AccountName -SortAttributes AccountName -Descending
            $names = @($results | ForEach-Object { $_.AccountName })

            $names.Count | Should -Be 6
            $names | Should -Be (@($script:sortNames)[5, 4, 3, 2, 1, 0])
        }
    }

    Context 'attribute restriction (-AttributesToGet)' {

        It 'returns only the requested attribute values' {
            # Ports the intent of SearchTestSyncRestrictedAttributeList. The source only re-checked the
            # count; here we assert the observable effect: a requested attribute carries its value while
            # an unrequested (but populated) attribute comes back null.
            $results = @(Search-Resources -XPath $script:attrFilter -AttributesToGet ut_svstring)
            $results.Count | Should -Be 1
            $results[0].ut_svstring | Should -Be $script:attrMarker
            $results[0].ut_svinteger | Should -BeNullOrEmpty
        }

        It 'returns an attribute value when it is included in the requested list' {
            $results = @(Search-Resources -XPath $script:attrFilter -AttributesToGet ut_svstring, ut_svinteger)
            $results.Count | Should -Be 1
            $results[0].ut_svstring | Should -Be $script:attrMarker
            $results[0].ut_svinteger | Should -Be $script:attrInteger
        }
    }

    Context 'result count (Get-ResourceCount)' {

        It 'reports the number of matching objects' {
            # Ports SearchTestResultCount.
            Get-ResourceCount -XPath $script:sortFilter | Should -Be 6
        }
    }

    Context 'paged search (Search-ResourcesPaged)' {

        It 'reports the total and pages forward in ascending order, then supports an index/page-size jump' {
            # Ports SearchTestPagedResultsSortAscAsync (folds in SearchTestPagedSortedAscendingAsync,
            # which is just the index-jump portion of this same scenario).
            $pager = Search-ResourcesPaged -XPath $script:sortFilter -AttributesToGet AccountName -SortAttributes AccountName -PageSize 2
            $pager.TotalCount | Should -Be 6

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeTrue
            $page[0].AccountName | Should -Be $script:sortNames[0]
            $page[1].AccountName | Should -Be $script:sortNames[1]

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeTrue
            $page[0].AccountName | Should -Be $script:sortNames[2]
            $page[1].AccountName | Should -Be $script:sortNames[3]

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeFalse
            $page[0].AccountName | Should -Be $script:sortNames[4]
            $page[1].AccountName | Should -Be $script:sortNames[5]

            # Jump back into the result set and change the page size.
            $pager.CurrentIndex = 2
            $pager.PageSize = 4
            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 4
            $pager.HasMoreItems | Should -BeFalse
            $page[0].AccountName | Should -Be $script:sortNames[2]
            $page[1].AccountName | Should -Be $script:sortNames[3]
            $page[2].AccountName | Should -Be $script:sortNames[4]
            $page[3].AccountName | Should -Be $script:sortNames[5]
        }

        It 'reports the total and pages forward in descending order, then supports an index/page-size jump' {
            # Ports SearchTestPagedResultsSortDescAsync (folds in SearchTestPagedSortedDescendingAsync).
            $desc = @($script:sortNames)[5, 4, 3, 2, 1, 0]

            $pager = Search-ResourcesPaged -XPath $script:sortFilter -AttributesToGet AccountName -SortAttributes AccountName -Descending -PageSize 2
            $pager.TotalCount | Should -Be 6

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeTrue
            $page[0].AccountName | Should -Be $desc[0]
            $page[1].AccountName | Should -Be $desc[1]

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeTrue
            $page[0].AccountName | Should -Be $desc[2]
            $page[1].AccountName | Should -Be $desc[3]

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 2
            $pager.HasMoreItems | Should -BeFalse
            $page[0].AccountName | Should -Be $desc[4]
            $page[1].AccountName | Should -Be $desc[5]

            $pager.CurrentIndex = 2
            $pager.PageSize = 4
            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 4
            $pager.HasMoreItems | Should -BeFalse
            $page[0].AccountName | Should -Be $desc[2]
            $page[1].AccountName | Should -Be $desc[3]
            $page[2].AccountName | Should -Be $desc[4]
            $page[3].AccountName | Should -Be $desc[5]
        }

        It 'returns the whole result set in a single page when the page size exceeds the total' {
            # Ports SearchTestPagedDefaultPageAndSizeSortedDescendingAsync.
            $desc = @($script:sortNames)[5, 4, 3, 2, 1, 0]

            $pager = Search-ResourcesPaged -XPath $script:sortFilter -AttributesToGet AccountName -SortAttributes AccountName -Descending -PageSize 100
            $pager.TotalCount | Should -Be 6

            $page = @($pager.GetNextPageAsync().GetAwaiter().GetResult())
            $page.Count | Should -Be 6
            @($page | ForEach-Object { $_.AccountName }) | Should -Be $desc
        }
    }
}
