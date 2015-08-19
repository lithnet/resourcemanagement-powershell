using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class RmaObject : PSObject
    {
        internal RmaObject (ResourceObject resource)
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
            foreach(AttributeValue value in this.InternalObject.Attributes.OrderBy(t => t.AttributeName))
            {
                PSNoteProperty prop = new PSNoteProperty(value.AttributeName, value.Value);

                this.Properties.Add(prop);
            }
        }

        internal ResourceObject GetResourceWithAppliedChanges()
        {
            foreach(var property in this.Properties)
            {
                if (this.InternalObject.Attributes[property.Name].Attribute.IsReadOnly)
                {
                    continue;
                }

                this.InternalObject.Attributes[property.Name].SetValue(property.Value);
            }

            return this.InternalObject;
        }
    }
}
