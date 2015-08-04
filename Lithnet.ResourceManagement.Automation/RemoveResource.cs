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
    [Cmdlet(VerbsCommon.Remove, "Resource", DefaultParameterSetName="ObjectIDString")]
    public class RemoveResource : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ObjectIDString")]
        public string ObjectIDString { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ObjectID")]
        public UniqueIdentifier ObjectID { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ObjectIDGuid")]
        public Guid ObjectIDGuid { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "ResourceObject")]
        public ResourceObject ResourceObject { get; set; }
                
        protected override void ProcessRecord()
        {
            if (this.ObjectIDString != null)
            {
                RmcWrapper.Client.DeleteResource(this.ObjectIDString);
            }
            else if (this.ObjectID != null)
            {
                RmcWrapper.Client.DeleteResource(this.ObjectID);
            }
            else if (this.ResourceObject != null)
            {
                RmcWrapper.Client.DeleteResource(this.ResourceObject);
            }
            else if (this.ObjectIDGuid != Guid.Empty)
            {
                RmcWrapper.Client.DeleteResource(this.ObjectIDGuid);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}