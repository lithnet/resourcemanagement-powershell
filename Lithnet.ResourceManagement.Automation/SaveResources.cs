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
        public RmaObject[] Resources { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Parallel { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<RmaObject> creatingObjects = this.Resources.Where(t => t.InternalObject.ModificationType == OperationType.Create).ToList();

            if (this.Parallel.IsPresent)
            {
                RmcWrapper.Client.SaveResourcesParallel(this.Resources.Select(t => t.GetResourceWithAppliedChanges()));
            }
            else
            {
                RmcWrapper.Client.SaveResources(this.Resources.Select(t => t.GetResourceWithAppliedChanges()));
            }

            foreach(RmaObject resource in creatingObjects)
            {
                resource.ReloadProperties();
            }
        }
    }
}