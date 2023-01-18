using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
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
            try
            {
                IEnumerable<RmaObject> creatingObjects = this.Resources.Where(t => t.InternalObject.ModificationType == OperationType.Create).ToList();

                if (this.Locale != null || this.Resources.Any(t => t.InternalObject.Locale != null))
                {
                    CultureInfo locale = null;

                    if (this.Locale != null)
                    {
                        locale = new CultureInfo(this.Locale);
                    }

                    if (this.Resources.Length > 1)
                    {
                        this.WriteWarning("Composite save disabled as locale parameter has been specified or at least one resource has been localized");
                    }

                    foreach (ResourceObject r in this.Resources.Select(t => t.GetResourceWithAppliedChanges()))
                    {
                        RmcWrapper.Client.SaveResource(r, locale);
                    }
                }
                else
                {
                    if (this.Parallel.IsPresent)
                    {
                        RmcWrapper.Client.SaveResourcesParallel(this.Resources.Select(t => t.GetResourceWithAppliedChanges()), -1);
                    }
                    else
                    {
                        RmcWrapper.Client.SaveResources(this.Resources.Select(t => t.GetResourceWithAppliedChanges()));
                    }
                }

                foreach (RmaObject resource in creatingObjects)
                {
                    resource.ReloadProperties();
                }
            }
            catch (AuthorizationRequiredException ex)
            {
                this.WriteVerbose("Authorization required: " + ex.ResourceReference);
                this.WriteObject(ex.ResourceReference);
            }
        }
    }
}