<#
    Cmdlet equivalent of the client unit tests' ApprovalTests.cs. Exercises Get-ApprovalRequest +
    Set-PendingApprovalRequest against a real MIM approval flow and asserts what the cmdlet surface
    exposes: a membership change that requires approval raises a pending ApprovalRequest that the
    approver can retrieve, approve, or reject, and the request then leaves the Pending set and appears
    in the Approved / Rejected set.

    ApprovalTests.cs has two logical scenarios - TestApprovalForCurrentUser and
    TestRejectionForCurrentUser - each run once per connection mode (ConnectionModeSourcesApprovals
    yields LocalProxy and RemoteProxy on .NET/PowerShell 7). That is the four cases ported here:
    approve x {LocalProxy, RemoteProxy} and reject x {LocalProxy, RemoteProxy}. The transport
    difference is pinned too: retrieving approvals is a plain search and works everywhere
    (asserted over DirectNetTcp in a dedicated case), but ACTIONING one uses the approval
    endpoint, which net.tcp lacks (the client wires NotImplementedApprovalClient into its NetTcp
    transport) - the approve scenario asserts that failure against a genuine pending approval on
    both PowerShell editions.

    Honest runnability - the real approval flow cannot be manufactured from the cmdlets alone. It has
    two preconditions that live in lab MIM policy, not in the module:

      1. A SECOND requestor credential. The current user (who runs the suite) is the group Owner and
         therefore the approver; a request they raise against their own group is auto-authorized and
         produces no approval. A DIFFERENT user must raise the membership request so the approval is
         routed to the owner. In the client unit tests this is the 'mimuser' account from App.config
         (approvalClientUser / approvalClientUserDomain / approvalClientUserPassword). That credential
         cannot be created through the module's cmdlets and is not supplied by _Bootstrap, so it is
         read from the environment (LITHNETRMA_TEST_APPROVAL_USER / _DOMAIN / _PASSWORD). When it is
         absent the case is Skipped rather than faked.

      2. An approval workflow + MPR bound to the target object type so the membership change generates
         a pending ApprovalRequest (the client tests use the built-in Group 'Owner Approval'
         workflow). If the lab has no such policy the membership change raises no request; the case is
         then Skipped.

    Where both preconditions are present the case runs for real and asserts the cmdlet-observable
    state change. Note that Get-ApprovalRequest exposes only the status-only overload, which filters
    to the connected (approver) identity - so the C# retrievals "by owner ObjectID" and "by current
    context" both map to Get-ApprovalRequest -Status Pending here.

    Save-Resource does not throw when a change requires authorization: the cmdlet catches the
    AuthorizationRequiredException and writes the pending request reference to the pipeline, so the
    request id is simply Save-Resource's output.
#>

BeforeAll {
    . $PSScriptRoot/_Bootstrap.ps1
    $script:data = Get-TestData
    $script:refs = Initialize-ReferenceObjects

    # Second requestor credential (mirrors ApprovalTests.cs' standardUserCredentials / App.config
    # approvalClientUser). Absent by default; supply via environment to enable the real flow.
    $approvalUser     = $env:LITHNETRMA_TEST_APPROVAL_USER
    $approvalDomain   = $env:LITHNETRMA_TEST_APPROVAL_DOMAIN
    $approvalPassword = $env:LITHNETRMA_TEST_APPROVAL_PASSWORD

    if ($approvalUser -and $approvalPassword)
    {
        $userNameForLogon = if ($approvalDomain) { "$approvalDomain\$approvalUser" } else { $approvalUser }
        $securePassword = ConvertTo-SecureString $approvalPassword -AsPlainText -Force
        $script:approvalCred = New-Object System.Management.Automation.PSCredential($userNameForLogon, $securePassword)
        $script:approvalUser = $approvalUser
    }
    else
    {
        $script:approvalCred = $null
        $script:approvalUser = $null
    }

    # In a pipeline run (TF_BUILD is set by Azure DevOps) the approval tests are mandatory: they
    # exercise the most complex flow in the product, so a missing credential must fail the gate
    # loudly rather than silently skipping them. Locally, skipping remains fine.
    function Assert-ApprovalCredentialOrSkip
    {
        param([Parameter(Mandatory)] [string] $Because)

        if ($script:approvalCred)
        {
            return $true
        }

        if ($env:TF_BUILD)
        {
            throw 'The approval test credential (LITHNETRMA_TEST_APPROVAL_USER / _DOMAIN / _PASSWORD) is not available to this pipeline run. The approval tests are a mandatory part of the release gate; check that the ''MIM lab test credentials'' variable group is linked and the secret is mapped into the step environment.'
        }

        Set-ItResult -Skipped -Because $Because
        return $false
    }

    # Same contract for the scenario preconditions (resolvable approver/requestor Persons, an
    # approval workflow bound in the lab): on the gate these are lab regressions and must fail
    # loudly, because approvals are mandatory gate coverage; locally they skip so the suite stays
    # runnable against arbitrary labs.
    function Assert-ApprovalPreconditionOrSkip
    {
        param([Parameter(Mandatory)] [string] $Because)

        if ($env:TF_BUILD)
        {
            throw "Approval test precondition failed on the pipeline gate: $Because"
        }

        Set-ItResult -Skipped -Because $Because
    }

    # Connects the module's shared client as an explicit user, reusing the same endpoint and
    # mode-aware SPN defaults as Connect-TestClient so only the identity differs. Used to raise the
    # membership request as the second user; the approver connection is restored afterwards with
    # Connect-TestClient.
    function Connect-AsUser
    {
        param(
            [Parameter(Mandatory)] [pscredential] $Credential,
            [string] $ConnectionMode = 'LocalProxy'
        )
        Connect-TestClient -ConnectionMode $ConnectionMode -Credential $Credential
    }

    # Normalises any reference value (UniqueIdentifier, 'urn:uuid:...' text, or bare guid) to a
    # canonical lowercase guid string so the pending-request reference and an approval's Request
    # attribute can be compared regardless of representation.
    function Get-GuidText
    {
        param($Value)
        if ($null -eq $Value) { return $null }
        $match = [regex]::Match([string]$Value, '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}')
        if ($match.Success) { return $match.Value.ToLowerInvariant() }
        return $null
    }
}

Describe 'Get-ApprovalRequest / Set-PendingApprovalRequest' {

    BeforeEach {
        $script:created = [System.Collections.Generic.List[object]]::new()
    }

    AfterEach {
        foreach ($id in $script:created) { $id | Remove-TestResource }
    }

    It 'approves a pending membership request for the current user (<Mode>)' -Tag 'RequiresApprovalWorkflow', 'RequiresSecondUser' -ForEach @(
        @{ Mode = 'LocalProxy' }
        @{ Mode = 'RemoteProxy' }
    ) {
        if (-not (Assert-ApprovalCredentialOrSkip -Because 'needs a second requestor credential to raise the membership request the current user then approves. Set LITHNETRMA_TEST_APPROVAL_USER / LITHNETRMA_TEST_APPROVAL_DOMAIN / LITHNETRMA_TEST_APPROVAL_PASSWORD to enable.'))
        {
            return
        }

        try
        {
            # Approver == current user, in this connection mode.
            Connect-TestClient -ConnectionMode $Mode

            $owner  = Get-Resource -ObjectType Person -AttributeName AccountName -AttributeValue $env:USERNAME
            $member = Get-Resource -ObjectType Person -AttributeName AccountName -AttributeValue $script:approvalUser

            if (-not $owner -or -not $member)
            {
                Assert-ApprovalPreconditionOrSkip -Because 'the approver (current user) and/or the second requestor could not be resolved as Person objects in this lab, so the approval scenario cannot be established'
                return
            }

            $groupName = [Guid]::NewGuid().ToString()
            $group = New-Resource -ObjectType Group
            $group.AccountName           = $groupName
            $group.DisplayName           = "Unit test approval group $groupName"
            $group.Owner                 = $owner.ObjectID
            $group.DisplayedOwner        = $owner.ObjectID
            $group.Scope                 = 'Global'
            $group.Type                  = 'Security'
            $group.Domain                = $env:USERDOMAIN
            $group.MembershipLocked      = $false
            $group.MembershipAddWorkflow = 'Owner Approval'
            Save-Resource $group
            $script:created.Add($group.ObjectID)

            # Raise the membership request as the SECOND user. Save-Resource emits the pending request
            # reference when the change requires authorization.
            Connect-AsUser -Credential $script:approvalCred -ConnectionMode $Mode
            $group2 = Get-Resource -ID $group.ObjectID
            $group2.ExplicitMember = @($member.ObjectID)
            $requestRef = Save-Resource $group2

            # Back to the approver to retrieve and act on the request.
            Connect-TestClient -ConnectionMode $Mode

            $target = Get-GuidText $requestRef
            if (-not $target)
            {
                Assert-ApprovalPreconditionOrSkip -Because 'the membership change raised no pending ApprovalRequest - this lab has no approval workflow + MPR (e.g. the built-in ''Owner Approval'' workflow) bound to the target object type, so there is nothing for Get-ApprovalRequest / Set-PendingApprovalRequest to act on'
                return
            }

            $pending = @(Get-ApprovalRequest -Status Pending)
            $ours = $pending | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $ours | Should -Not -BeNullOrEmpty -Because 'the pending request must be retrievable via Get-ApprovalRequest -Status Pending'

            # Approvals cannot be ACTIONED over net.tcp: the client wires a
            # NotImplementedApprovalClient into its NetTcp transport because the FIM approval
            # endpoint has no net.tcp binding. Pin the intended failure here, against a genuine
            # pending approval, so it can never silently turn into a hang or a no-op. The client
            # is a single netstandard assembly, so this holds on both PowerShell editions.
            Connect-TestClient -ConnectionMode DirectNetTcp
            $overNetTcp = @(Get-ApprovalRequest -Status Pending) | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $overNetTcp | Should -Not -BeNullOrEmpty -Because 'retrieving approvals is a plain search and works on every transport'
            { $overNetTcp | Set-PendingApprovalRequest -Decision Approve -Reason 'Must fail over net.tcp' } | Should -Throw
            Connect-TestClient -ConnectionMode $Mode

            $ours | Set-PendingApprovalRequest -Decision Approve -Reason 'Test reason for approval'

            # The approval decision is processed asynchronously by the MIM workflow (mirrors the
            # client test's post-approve wait).
            Start-Sleep -Seconds 5

            $stillPending = @(Get-ApprovalRequest -Status Pending) | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $stillPending | Should -BeNullOrEmpty -Because 'an approved request must no longer appear in the Pending set'

            $approved = @(Get-ApprovalRequest -Status Approved) | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $approved | Should -Not -BeNullOrEmpty -Because 'the request must move to the Approved set once approved'
        }
        finally
        {
            # Restore a known-good approver connection so cleanup (and the next test) run as the
            # current user regardless of where the flow left off.
            Connect-TestClient
        }
    }

    It 'rejects a pending membership request for the current user (<Mode>)' -Tag 'RequiresApprovalWorkflow', 'RequiresSecondUser' -ForEach @(
        @{ Mode = 'LocalProxy' }
        @{ Mode = 'RemoteProxy' }
    ) {
        if (-not (Assert-ApprovalCredentialOrSkip -Because 'needs a second requestor credential to raise the membership request the current user then rejects. Set LITHNETRMA_TEST_APPROVAL_USER / LITHNETRMA_TEST_APPROVAL_DOMAIN / LITHNETRMA_TEST_APPROVAL_PASSWORD to enable.'))
        {
            return
        }

        try
        {
            Connect-TestClient -ConnectionMode $Mode

            $owner  = Get-Resource -ObjectType Person -AttributeName AccountName -AttributeValue $env:USERNAME
            $member = Get-Resource -ObjectType Person -AttributeName AccountName -AttributeValue $script:approvalUser

            if (-not $owner -or -not $member)
            {
                Assert-ApprovalPreconditionOrSkip -Because 'the approver (current user) and/or the second requestor could not be resolved as Person objects in this lab, so the approval scenario cannot be established'
                return
            }

            $groupName = [Guid]::NewGuid().ToString()
            $group = New-Resource -ObjectType Group
            $group.AccountName           = $groupName
            $group.DisplayName           = "Unit test approval group $groupName"
            $group.Owner                 = $owner.ObjectID
            $group.DisplayedOwner        = $owner.ObjectID
            $group.Scope                 = 'Global'
            $group.Type                  = 'Security'
            $group.Domain                = $env:USERDOMAIN
            $group.MembershipLocked      = $false
            $group.MembershipAddWorkflow = 'Owner Approval'
            Save-Resource $group
            $script:created.Add($group.ObjectID)

            Connect-AsUser -Credential $script:approvalCred -ConnectionMode $Mode
            $group2 = Get-Resource -ID $group.ObjectID
            $group2.ExplicitMember = @($member.ObjectID)
            $requestRef = Save-Resource $group2

            Connect-TestClient -ConnectionMode $Mode

            $target = Get-GuidText $requestRef
            if (-not $target)
            {
                Assert-ApprovalPreconditionOrSkip -Because 'the membership change raised no pending ApprovalRequest - this lab has no approval workflow + MPR (e.g. the built-in ''Owner Approval'' workflow) bound to the target object type, so there is nothing for Get-ApprovalRequest / Set-PendingApprovalRequest to act on'
                return
            }

            $pending = @(Get-ApprovalRequest -Status Pending)
            $ours = $pending | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $ours | Should -Not -BeNullOrEmpty -Because 'the pending request must be retrievable via Get-ApprovalRequest -Status Pending'

            $ours | Set-PendingApprovalRequest -Decision Reject -Reason 'Test reason for rejection'

            Start-Sleep -Seconds 5

            $stillPending = @(Get-ApprovalRequest -Status Pending) | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $stillPending | Should -BeNullOrEmpty -Because 'a rejected request must no longer appear in the Pending set'

            $rejected = @(Get-ApprovalRequest -Status Rejected) | Where-Object { (Get-GuidText $_.Request) -eq $target }
            $rejected | Should -Not -BeNullOrEmpty -Because 'the request must move to the Rejected set once rejected'
        }
        finally
        {
            Connect-TestClient
        }
    }

    It 'retrieves approval requests over DirectNetTcp' {
        # Retrieving approvals is a plain XPath search over /Approval
        # (ResourceManagementClient.GetApprovalsAsync), so it works on every transport, including
        # net.tcp. Only ACTING on an approval uses the approval endpoint, which net.tcp lacks -
        # that intended failure is pinned inside the approve scenario, where a genuine pending
        # approval exists to attempt.
        try
        {
            Connect-TestClient -ConnectionMode DirectNetTcp
            { Get-ApprovalRequest -Status Pending } | Should -Not -Throw
        }
        finally
        {
            Connect-TestClient
        }
    }
}
