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
    [Cmdlet(VerbsData.Save, "Resource")]
    public class SaveResources : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public PSObject[] Resources { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Parallel { get; set; }

        protected override void ProcessRecord()
        {
            if (this.Resources.Any(t => !(t.BaseObject is ResourceObject)))
            {
                this.WriteError(new ErrorRecord(new ArgumentException("Resources"), "InvalidObjectType", ErrorCategory.InvalidArgument, this.Resources));
            }

            if (this.Parallel.IsPresent)
            {
                RmcWrapper.Client.SaveResourcesParallel(this.Resources.Select(t => (ResourceObject)t.BaseObject));
            }
            else
            {
                RmcWrapper.Client.SaveResources(this.Resources.Select(t => (ResourceObject)t.BaseObject));
            }
        }
    }
}