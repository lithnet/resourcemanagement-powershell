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
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ID")]
        public object ID { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "ResourceObject")]
        public RmaObject[] ResourceObjects { get; set; }
                
        protected override void ProcessRecord()
        {
            string idString = this.ID as string;
            Guid? idGuid = this.ID as Guid?;
            UniqueIdentifier idUniqueID = this.ID as UniqueIdentifier;

            if (idString != null)
            {
                RmcWrapper.Client.DeleteResource(idString);
            }
            else if (idGuid.HasValue)
            {
                RmcWrapper.Client.DeleteResource(idGuid.Value);
            }
            else if (this.ResourceObjects != null)
            {
                RmcWrapper.Client.DeleteResources(this.ResourceObjects.Select(t => t.InternalObject));
            }
            else if (idUniqueID != null)
            {
                RmcWrapper.Client.DeleteResource(idUniqueID);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}