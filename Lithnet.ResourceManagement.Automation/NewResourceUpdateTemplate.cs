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
    [Cmdlet(VerbsCommon.New, "ResourceUpdateTemplate", DefaultParameterSetName = "ObjectIDString")]
    public class NewResourceUpdateTemplate : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName="ObjectIDString")]
        public string ObjectIDString { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ObjectIDGuid")]
        public Guid ObjectIDGuid { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ObjectID")]
        public UniqueIdentifier ObjectID { get; set; }

        protected override void ProcessRecord()
        {
            if (this.ObjectID != null)
            {
                this.WriteObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, this.ObjectID));
            }
            else if (this.ObjectIDString != null)
            {
                this.WriteObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, new UniqueIdentifier(this.ObjectIDString)));
            }
            else if (this.ObjectIDGuid != Guid.Empty)
            {
                this.WriteObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, new UniqueIdentifier(this.ObjectIDGuid)));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}