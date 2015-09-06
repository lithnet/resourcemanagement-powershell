using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class RmaObject : PSObject
    {
        internal RmaObject(ResourceObject resource)
        {
            this.InternalObject = resource;
            this.LoadProperties();
        }

        internal ResourceObject InternalObject { get; set; }

        internal void ReloadProperties()
        {
            foreach (var property in this.Properties)
            {
                this.Properties.Remove(property.Name);
            }

            this.LoadProperties();
        }

        private void LoadProperties()
        {
            foreach (AttributeValue value in this.InternalObject.Attributes.OrderBy(t => t.AttributeName))
            {
                PSNoteProperty prop;
                if (value.Attribute.IsMultivalued)
                {
                    prop = new PSNoteProperty(value.AttributeName, new ArrayList((ICollection)value.Value));
                }
                else
                {
                    prop = new PSNoteProperty(value.AttributeName, value.Value);
                }

                this.Properties.Add(prop);
            }
        }

        internal ResourceObject GetResourceWithAppliedChanges()
        {
            foreach (var property in this.Properties)
            {
                if (this.InternalObject.Attributes[property.Name].Attribute.IsReadOnly)
                {
                    continue;
                }

                IEnumerable<object> resourceValues = property.Value as IEnumerable<object>;

                if (resourceValues != null)
                {
                    this.SetMultivaluedAttribute(property, resourceValues);
                }
                else
                {
                    this.SetSingleValuedAttribute(property, property.Value);
                }
            }

            return this.InternalObject;
        }

        private void SetMultivaluedAttribute(PSPropertyInfo property, IEnumerable<object> resourceValues)
        {
            List<object> newValues = new List<object>();

            foreach (object value in resourceValues)
            {
                RmaObject resourceValue = value as RmaObject;

                if (resourceValue != null)
                {
                    newValues.Add(resourceValue.InternalObject.ObjectID);
                }
                else
                {
                    newValues.Add(this.UnwrapPSObject(value));
                }
            }

            this.InternalObject.Attributes[property.Name].SetValue(newValues);
        }

        private void SetSingleValuedAttribute(PSPropertyInfo property, object value)
        {
            RmaObject resourceValue = property.Value as RmaObject;

            if (resourceValue != null)
            {
                this.InternalObject.Attributes[property.Name].SetValue(resourceValue.InternalObject.ObjectID);
            }
            else
            {
                this.InternalObject.Attributes[property.Name].SetValue(this.UnwrapPSObject(property.Value));
            }
        }

        private object UnwrapPSObject(object value)
        {
            PSObject psObject = value as PSObject;

            if (psObject != null)
            {
                return psObject.BaseObject;
            }
            else
            {
                return value;
            }
        }
    }
}
