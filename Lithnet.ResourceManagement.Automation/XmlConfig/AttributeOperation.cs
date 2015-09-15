using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Lithnet.ResourceManagement.Client;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.ResourceManagement.WebServices;

namespace Lithnet.ResourceManagement.Automation
{
    public class AttributeOperation
    {
        private const string filterTextFormat = "<Filter xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" Dialect=\"http://schemas.microsoft.com/2006/11/XPathFilterDialect\" xmlns=\"http://schemas.xmlsoap.org/ws/2004/09/enumeration\">{0}</Filter>";

        [XmlAttribute(AttributeName = "operation")]
        public AttributeOperationType Operation { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public AttributeValueType ValueType { get; set; }

        [XmlText()]
        public string Value { get; set; }

        [XmlIgnore]
        public object ExpandedValue
        {
            get
            {
                return this.GetValue();
            }
        }

        internal void ExecuteOperation(ResourceObject resource, ResourceOperationType resourceOpType)
        {
            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            this.ValidateResourceOperationType(resourceOpType);

            switch (this.Operation)
            {
                case AttributeOperationType.None:
                    return;

                case AttributeOperationType.Add:
                    this.PerformAttributeAdd(resource);
                    break;

                case AttributeOperationType.Replace:
                    this.PerformAttributeReplace(resource);
                    break;

                case AttributeOperationType.Delete:
                    this.PerformAttributeDelete(resource);
                    break;

                default:
                    throw new ArgumentException("Unknown or unsupported operation type");
            }
        }

        private void PerformAttributeAdd(ResourceObject resource)
        {
            if (!resource.Attributes[this.Name].Attribute.IsMultivalued)
            {
                this.PerformAttributeReplace(resource);
            }
            else
            {
                object newValue = this.ExpandedValue;

                resource.Attributes[this.Name].AddValue(newValue);
            }
        }

        private void PerformAttributeReplace(ResourceObject resource)
        {
            object newValue = this.ExpandedValue;

            if (newValue is string)
            {
                if (string.IsNullOrEmpty((string)newValue))
                {
                    if (resource.Attributes[this.Name].IsNull)
                    {
                        return;
                    }
                }
            }

            resource.Attributes[this.Name].SetValue(newValue);
        }

        private void PerformAttributeDelete(ResourceObject resource)
        {
            object newValue = this.ExpandedValue;

            resource.Attributes[this.Name].RemoveValue(newValue);
        }

        private void ValidateResourceOperationType(ResourceOperationType resourceOpType)
        {
            switch (this.Operation)
            {
                case AttributeOperationType.None:
                    break;

                case AttributeOperationType.Replace:
                case AttributeOperationType.Delete:
                case AttributeOperationType.Add:
                    if (resourceOpType == ResourceOperationType.Delete)
                    {
                        throw new ArgumentException("Cannot perform an attribute change on a delete operation");
                    }

                    break;

                default:
                    throw new ArgumentException("Unknown or unsupported AttributeOperationType: " + this.Operation.ToString());
            }
        }

        private object GetValue()
        {
            if (this.Value == null)
            {
                return null;
            }

            switch (this.ValueType)
            {
                case AttributeValueType.Value:
                    return this.ExpandVariables(this.Value);

                case AttributeValueType.File:
                    return this.GetFileContent(this.ExpandVariables(this.Value));

                case AttributeValueType.Reference:
                    return this.GetReference(this.Value);

                case AttributeValueType.XmlReference:
                    return this.GetXmlReference(this.Value);

                case AttributeValueType.XPathFilter:
                    return this.BuildFilterAttribute(this.Value);

                default:
                    throw new ArgumentException("Unknown value type " + this.Value.ToString());
            }
        }

        private string ExpandVariables(string input)
        {
            string expandedInput = input;

            if ( ConfigSyncControl.CurrentConfig.Variables != null && ConfigSyncControl.CurrentConfig.Variables.Items != null)
            {
                foreach (Variable variable in ConfigSyncControl.CurrentConfig.Variables.Items)
                {
                    expandedInput = expandedInput.Replace(variable.Name, variable.ExpandedValue);
                }
            }

            foreach (Match match in Regex.Matches(expandedInput, @"\#\#xmlref:(.+?):(.*?)\#\#"))
            {
                if (match.Groups.Count >= 2)
                {
                    string xmlRefName = match.Groups[1].Value;
                    ResourceObject xmlRef = this.GetXmlReferenceResource(xmlRefName);
                    if (xmlRef == null)
                    {
                        throw new ArgumentException("The input string contained the xmlref pattern '{0}', but the object could not be resolved");
                    }

                    string xmlDeRefAttributeName = "ObjectID";
                    if (match.Groups.Count > 2)
                    {
                        string groupValue = match.Groups[2].Value;

                        if (!string.IsNullOrWhiteSpace(groupValue))
                        {
                            xmlDeRefAttributeName = match.Groups[2].Value;
                        }
                    }

                    object xmlDeRefValue = xmlRef.Attributes[xmlDeRefAttributeName].Value;

                    UniqueIdentifier xmlDerefID = xmlDeRefValue as UniqueIdentifier;
                    if (xmlDerefID != null)
                    {
                        xmlDeRefValue = xmlDerefID.Value;
                    }

                    expandedInput = expandedInput.Replace(match.Value, xmlDeRefValue == null ? null : xmlDeRefValue.ToString());
                }
            }

            return expandedInput;
        }

        private string GetFileContent(string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(ConfigSyncControl.CurrentPath, filename);
            }

            return this.ExpandVariables(File.ReadAllText(filename));
        }

        private string BuildFilterAttribute(string xpath)
        {
            return this.ExpandVariables(string.Format(filterTextFormat, xpath));
        }

        private string GetReference(string value)
        {
            string[] split = value.Split('|');
            if (split.Length != 3)
            {
                throw new ArgumentException(string.Format("The attribute operation of {0} on attribute {1} specifies a reference type, but does not have a string in the value of ObjectType|AttributeName|AttributeValue. The invalid value was {2}", this.Name, this.Operation, this.Value));
            }

            ResourceObject resource = RmcWrapper.Client.GetResourceByKey(split[0], split[1], split[2],  ResourceManagementSchema.ObjectTypes[split[0]].Attributes.Select(t => t.SystemName).Except(ResourceManagementSchema.ComputedAttributes));

            if (resource == null)
            {
                if (ConfigSyncControl.Preview)
                {
                    return Guid.NewGuid().ToString();
                }
                else
                {
                    throw new ArgumentException(string.Format("The attribute operation of {1} on attribute {0} specifies a reference to {2}, but the object was not found in the FIM service", this.Name, this.Operation, value));
                }
            }

            return resource.ObjectID.Value;
        }

        private string GetXmlReference(string id)
        {
            ResourceObject resource = this.GetXmlReferenceResource(id);

            return resource.ObjectID == null ? Guid.Empty.ToString() : resource.ObjectID.Value;
        }

        private ResourceObject GetXmlReferenceResource(string id)
        {
            ResourceOperation op =  ConfigSyncControl.CurrentConfig.Operations.FirstOrDefault(t => t.ID == id);

            if (op == null)
            {
                throw new ArgumentException(string.Format("The attribute operation of {1} on attribute {0} specifies a reference to another operation with ID {2}, but the operation was not found in the XML file", this.Name, this.Operation, id));
            }

            if (op.HasProcessed)
            {
                return op.Resource;
            }

            Dictionary<string, object> anchorValues;

            try
            {
                anchorValues = op.GetAnchorValues();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("The attribute operation of {1} on attribute {0} specifies a reference to another operation with ID {2}, but resource failed to resolve its anchor", this.Name, this.Operation, id), ex);
            }

            ResourceObject resource = RmcWrapper.Client.GetResourceByKey(op.ResourceType, anchorValues, op.AttributesToGet);

            if (resource == null)
            {
                throw new ArgumentException(string.Format("The attribute operation of {1} on attribute {0} specifies a reference to another operation with ID {2}, but the object was not found in the FIM service", this.Name, this.Operation, id));
            }

            return resource;
        }
    }
}
