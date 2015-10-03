using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Management.Automation;
using Lithnet.ResourceManagement.Client;
using Microsoft.ResourceManagement.WebServices;

namespace Lithnet.ResourceManagement.Automation
{
    public class AttributeValueArrayList : ArrayList
    {
        public AttributeValueArrayList ()
            : base()
        {
        }

        public AttributeValueArrayList (ICollection c)
            : base(c)
        {
        }

        public override void Remove(object obj)
        {
            if (base.Contains(obj))
            {
                base.Remove(obj);
                return;
            }

            RmaObject rmaObject = obj as RmaObject;

            if (rmaObject != null)
            {
                base.Remove(rmaObject.InternalObject.ObjectID);
            }

            if (obj is Guid)
            {
                base.Remove(new UniqueIdentifier((Guid)obj));
            }
        } 
    }
}
