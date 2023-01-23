using System.Management.Automation;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.Get, "ApprovalRequest")]
    public class GetApprovalRequest : PSCmdlet
    {
        [Parameter(Mandatory = false, Position = 1)]
        public ApprovalStatus Status { get; set; }

        protected override void ProcessRecord()
        {
            ISearchResultCollection results = RmcWrapper.Client.GetApprovals((Client.ApprovalStatus)Status);

            foreach (ResourceObject result in results)
            {
                this.WriteObject(new RmaObject(result));
            }
        }
    }
}