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
    [Cmdlet(VerbsCommon.Remove, "Resource", DefaultParameterSetName="ObjectIDString")]
    public class RemoveResource : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ID")]
        public object[] ID { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "ResourceObject")]
        public RmaObject[] ResourceObjects { get; set; }
                
        protected override void ProcessRecord()
        {
            if (this.ID != null)
            {
                List<UniqueIdentifier> ids = new List<UniqueIdentifier>();

                foreach (object id in this.ID)
                {
                    string idString = id as string;
                    Guid? idGuid = id as Guid?;
                    UniqueIdentifier idUniqueID = id as UniqueIdentifier;

                    if (idString != null)
                    {
                        ids.Add(new UniqueIdentifier(idString));
                    }
                    else if (idGuid.HasValue)
                    {
                        ids.Add(new UniqueIdentifier(idGuid.Value));
                    }
                    else if (idUniqueID != null)
                    {
                        ids.Add(idUniqueID);
                    }
                }

                RmcWrapper.Client.DeleteResources(ids);
            }
            else if (this.ResourceObjects != null)
            {
                RmcWrapper.Client.DeleteResources(this.ResourceObjects.Select(t => t.InternalObject));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}