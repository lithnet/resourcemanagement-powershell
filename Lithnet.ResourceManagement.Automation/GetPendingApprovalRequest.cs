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
    [Cmdlet(VerbsCommon.Get, "PendingApprovalRequest")]
    public class GetPendingApprovalRequest : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            ISearchResultCollection results = RmcWrapper.Client.GetPendingApprovals();

            foreach (ResourceObject result in results)
            {
                this.WriteObject(new RmaObject(result));
            }
        }
    }
}