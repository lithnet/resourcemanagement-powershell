using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using Microsoft.ResourceManagement.WebServices;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.Set, "PendingApprovalRequest")]
    public class SetPendingApprovalRequest : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public RmaObject ApprovalObject { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public ApprovalDecision Decision { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 3)]
        public string Reason { get; set; }

        protected override void ProcessRecord()
        {
            ResourceObject r = this.ApprovalObject.InternalObject;
            RmcWrapper.Client.Approve(r, this.Decision == ApprovalDecision.Approve, this.Reason);
        }
    }
}