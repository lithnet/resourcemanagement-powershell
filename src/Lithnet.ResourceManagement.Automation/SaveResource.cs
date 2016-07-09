using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using Microsoft.ResourceManagement.WebServices;
using System.Collections;
using Lithnet.ResourceManagement.Client;
using System.Globalization;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.Save, "Resource")]
    public class SaveResource : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public RmaObject[] Resources { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Parallel { get; set; }

        [Parameter(Mandatory = false)]
        public string Locale { get; set; }
        
        protected override void ProcessRecord()
        {
            IEnumerable<RmaObject> creatingObjects = this.Resources.Where(t => t.InternalObject.ModificationType == OperationType.Create).ToList();

            CultureInfo locale = null;

            if (this.Locale != null)
            {
                locale = new CultureInfo(this.Locale);
            }

            if (this.Parallel.IsPresent)
            {
                RmcWrapper.Client.SaveResourcesParallel(this.Resources.Select(t => t.GetResourceWithAppliedChanges()), -1, locale);
            }
            else
            {
                RmcWrapper.Client.SaveResources(this.Resources.Select(t => t.GetResourceWithAppliedChanges()), locale);
            }

            foreach(RmaObject resource in creatingObjects)
            {
                resource.ReloadProperties();
            }
        }
    }
}