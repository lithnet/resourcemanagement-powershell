<#
    Parameter-binding tests for the cmdlets reported in GitHub issue #45 and the two latent
    cmdlets with the same defect.

    Root cause: these cmdlets declare parameters as [object] / [object[]] and then either hard-cast
    to a client type or type-test with 'as'. PowerShell wraps a value passed to an [object] parameter
    in a PSObject (there is no target type to coerce to), so the hard cast throws and the 'as' test
    silently misses. A test that calls the cmdlet class directly in C# cannot see this - the wrapper
    only exists when the real PowerShell binder runs, which is why these tests are in Pester.

    These are written to FAIL against the current code and PASS once the cmdlets declare real
    parameter types / unwrap the PSObject. They connect to the test MIM (via _Bootstrap) because
    New-XPathQuery validates the attribute name against the live schema.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
}

Describe 'Cmdlet parameter binding (issue #45)' {

    Context 'Reported: hard cast to a client type throws InvalidCastException' {

        It 'New-XPathExpression accepts an XPathQuery held in a variable' {
            $query = New-XPathQuery -AttributeName AccountName -Operator Equals -Value 'someone'
            { New-XPathExpression -ObjectType Person -QueryObject $query } |
                Should -Not -Throw
        }

        It 'New-XPathExpression accepts an XPathQuery from the pipeline' {
            {
                New-XPathQuery -AttributeName AccountName -Operator Equals -Value 'someone' |
                    New-XPathExpression -ObjectType Person
            } | Should -Not -Throw
        }

        It 'New-XPathQueryGroup accepts an untyped array of queries' {
            $q1 = New-XPathQuery -AttributeName AccountName -Operator Equals -Value 'a'
            $q2 = New-XPathQuery -AttributeName AccountName -Operator Equals -Value 'b'
            { New-XPathQueryGroup -Operator Or -Queries ($q1, $q2) } |
                Should -Not -Throw
        }
    }

    Context 'Latent: PSObject-wrapped value is silently dropped (no error, wrong result)' {

        It 'New-ResourceUpdateTemplate returns a template when the ID is PSObject-wrapped' {
            $person = Search-Resources -XPath '/Person' -AttributesToGet ObjectID -MaxResults 1
            $person | Should -Not -BeNullOrEmpty -Because 'the lab MIM must contain at least one Person'

            $wrappedId = [psobject]::AsPSObject($person.ObjectID)
            $template = New-ResourceUpdateTemplate -ObjectType Person -ID $wrappedId

            $template | Should -Not -BeNullOrEmpty -Because 'a wrapped ID must be unwrapped, not silently dropped into a null result'
        }

        It 'Remove-Resource does not silently skip a PSObject-wrapped ID' -Tag 'RequiresDelete' {
            # The bug: a wrapped GUID fails every 'as' cast and is dropped, so DeleteResources is
            # called with an empty list and nothing happens - no error. Proving the positive
            # (the delete actually occurs) needs an account with delete rights and a throwaway
            # object, which belongs in the functional tier (C5). Skipped here so the read-only
            # binding run stays deterministic, but kept visible so the gap is not forgotten.
            Set-ItResult -Skipped -Because 'needs an account with delete rights + a throwaway object; covered by the functional tier'
        }
    }
}
