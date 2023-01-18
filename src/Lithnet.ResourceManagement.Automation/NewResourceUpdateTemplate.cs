using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "ResourceUpdateTemplate")]
    public class NewResourceUpdateTemplate : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public object ID { get; set; }

        protected override void ProcessRecord()
        {
            UniqueIdentifier uniqueID = this.ID as UniqueIdentifier;

            if (uniqueID != null)
            {
                this.WriteObject(new RmaObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, uniqueID)));
                return;
            }

            string stringID = this.ID as string;

            if (stringID != null)
            {
                uniqueID = new UniqueIdentifier(stringID);
                this.WriteObject(new RmaObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, uniqueID)));
                return;
            }

            Guid? guidID = this.ID as Guid?;

            if (guidID != null)
            {
                uniqueID = new UniqueIdentifier(guidID.Value);
                this.WriteObject(new RmaObject(RmcWrapper.Client.CreateResourceTemplateForUpdate(this.ObjectType, uniqueID)));
                return;
            }
        }
    }
}