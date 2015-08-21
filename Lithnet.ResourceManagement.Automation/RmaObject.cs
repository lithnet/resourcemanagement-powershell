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

                RmaObject resourceValue = property.Value as RmaObject;

                if (resourceValue != null)
                {
                    this.InternalObject.Attributes[property.Name].SetValue(resourceValue.InternalObject.ObjectID);
                    continue;
                }

                IEnumerable<RmaObject> resourceValues = property.Value as IEnumerable<RmaObject>;

                if (resourceValues != null)
                {
                    this.InternalObject.Attributes[property.Name].SetValue(resourceValues.Select(t => t.InternalObject.ObjectID));
                    continue;
                }
                else
                {
                    this.InternalObject.Attributes[property.Name].SetValue(property.Value);
                }
            }

            return this.InternalObject;
        }
    }
}
