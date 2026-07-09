using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Management.Automation;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class AttributeValueArrayList : ArrayList
    {
        public AttributeValueArrayList()
            : base()
        {
        }

        public AttributeValueArrayList(ICollection c)
            : base(c)
        {
        }

        public override void AddRange(ICollection c)
        {
            foreach (object item in c)
            {
                if (item is RmaObject rmaObject)
                {
                    base.Add(rmaObject.InternalObject.ObjectID);
                }
                else
                {
                    base.Add(item);
                }
            }
        }

        public override int Add(object value)
        {
            if (value is RmaObject rmaObject)
            {
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

            switch (obj)
            {
                case RmaObject rmaObject:
                    // obj is an existing object
                    base.Remove(rmaObject.InternalObject.ObjectID);
                    return;

                case Guid guid1:
                    // obj is a guid
                    base.Remove(new UniqueIdentifier(guid1));
                    return;

                case string s when Guid.TryParse(s, out Guid guid):
                    // obj is a string in GUID format
                    base.Remove(new UniqueIdentifier(guid));
                    return;
            }
        }
    }
}
