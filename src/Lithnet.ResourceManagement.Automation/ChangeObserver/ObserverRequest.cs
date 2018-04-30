using Microsoft.ResourceManagement.WebServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.ChangeObserver
{
    internal class ObserverRequest
    {
        public UniqueIdentifier TargetObjectID { get; set; }
        public string TargetObjectType { get; set; }
        public string RequestOperationType { get; set; }

        internal ObserverRequest(UniqueIdentifier TargetObjectID, string TargetObjectType, string RequestOperationType)
        {
            this.TargetObjectID = TargetObjectID;
            this.TargetObjectType = TargetObjectType;
            this.RequestOperationType = RequestOperationType;
        }
    }
}
