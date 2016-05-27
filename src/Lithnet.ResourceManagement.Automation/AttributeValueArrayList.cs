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
        
        public override int Add(object value)
        {
            RmaObject rmaObject = value as RmaObject;
            if (rmaObject != null)
            {
                // obj is an existing object
                return base.Add(rmaObject.InternalObject.ObjectID);
            }
            else
            {
                return base.Add(value);
            }
        }

        public override void Remove(object obj)
        {
            if (base.Contains(obj))
            { 
                // obj is already a unique identifier
                base.Remove(obj);
                return;
            }

            RmaObject rmaObject = obj as RmaObject;
            if (rmaObject != null)
            {
                // obj is an existing object
                base.Remove(rmaObject.InternalObject.ObjectID);
                return;
            }

            if (obj is Guid)
            {
                // obj is a guid
                base.Remove(new UniqueIdentifier((Guid)obj));
                return;
            }

            if (obj is string)
            {
                Guid guid;
                if (Guid.TryParse((string)obj, out guid))
                {
                    // obj is a string in GUID format
                    base.Remove(new UniqueIdentifier(guid));
                    return;
                }
            }
        } 
    }
}
