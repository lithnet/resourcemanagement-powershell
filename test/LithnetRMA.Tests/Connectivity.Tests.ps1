<#
    Proves the test platform itself works: the freshly built module imports, connects to the test
    MIM service at the same endpoint the resourcemanagement-client unit tests use, and can read.
    If this fails, a red result elsewhere is a platform problem, not a code defect.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
}

Describe 'Test platform' {

    It 'exports the expected cmdlet surface' {
        (Get-Command -Module LithnetRMA).Count | Should -BeGreaterThan 0
    }

    It 'connects to the MIM service and reads a Person' {
        $result = Search-Resources -XPath '/Person' -AttributesToGet DisplayName -MaxResults 1
        @($result).Count | Should -BeGreaterThan 0
    }
}
