if ($PSEdition -eq 'Core') {
    $subDir = 'coreclr'
} else {
    $subDir = 'desktop'
}

Import-Module ([System.IO.Path]::Combine($PSScriptRoot, $subDir, 'Lithnet.ResourceManagement.Automation.dll'))
