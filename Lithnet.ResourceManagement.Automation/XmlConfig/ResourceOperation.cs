using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class ResourceOperation
    {
        public delegate void LogMessageEventHander(object sender, string message);

        internal static event LogMessageEventHander LogEvent;

        private List<string> anchorAttributes;

        private List<AttributeOperation> attributeOperations;

        [XmlArray(ElementName = "AnchorAttributes", IsNullable = true)]
        [XmlArrayItem(ElementName = "AnchorAttribute", IsNullable = false)]
        public List<string> AnchorAttributes
        {
            get
            {
                if (this.anchorAttributes == null)
                {
                    this.anchorAttributes = new List<string>();
                }

                return this.anchorAttributes;
            }
            set
            {
                this.anchorAttributes = value;
            }
        }

        [XmlArray(ElementName = "AttributeOperations", IsNullable = true)]
        [XmlArrayItem(ElementName = "AttributeOperation", IsNullable = false)]
        public List<AttributeOperation> AttributeOperations
        {
            get
            {
                if (this.attributeOperations == null)
                {
                    this.attributeOperations = new List<AttributeOperation>();
                }

                return this.attributeOperations;
            }
            set
            {
                this.attributeOperations = value;
            }
        }

        [XmlAttribute(AttributeName = "operation")]
        public ResourceOperationType Operation { get; set; }

        [XmlAttribute(AttributeName = "resourceType")]
        public string ResourceType { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string ID { get; set; }

        [XmlAttribute(AttributeName = "refresh-schema")]
        public SchemaRefreshEvent SchemaRefresh { get; set; }

        [XmlIgnore]
        public string ObjectID
        {
            get
            {
                if (this.Resource != null && this.Resource.ObjectID != null)
                {
                    return this.Resource.ObjectID.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        [XmlIgnore]
        internal bool HasProcessed
        {
            get
            {
                return this.Resource != null;
            }
        }

        [XmlIgnore]
        internal ResourceObject Resource { get; private set; }

        internal void ExecuteOperation()
        {
            if (this.SchemaRefresh == SchemaRefreshEvent.BeforeOperation)
            {
                this.RaiseLogEvent("Refreshing schema");
                RmcWrapper.Client.RefreshSchema();
            }

            switch (this.Operation)
            {
                case ResourceOperationType.None:
                    break;

                case ResourceOperationType.Add:
                    this.ProcessResourceAdd();
                    break;

                case ResourceOperationType.Update:
                    this.ProcessResourceUpdate();
                    break;

                case ResourceOperationType.Delete:
                    this.ProcessResourceDelete();
                    break;

                case ResourceOperationType.Add | ResourceOperationType.Update:
                    this.ProcessResourceAddUpdate();
                    break;

                default:
                    throw new ArgumentException("Unknown or unsupported operation type: " + this.Operation.ToString());
            }

            if (this.SchemaRefresh == SchemaRefreshEvent.AfterOperation)
            {
                this.RaiseLogEvent("Refreshing schema");
                RmcWrapper.Client.RefreshSchema();
            }
        }

        private void ProcessResourceAdd()
        {
            this.Resource = RmcWrapper.Client.CreateResource(this.ResourceType);
            this.ExecuteAttributeOperations();

            this.RaiseLogEvent(string.Format("Creating resource {0} with the following attribute values", this.ID));

            foreach (AttributeValue attribute in this.Resource.Attributes)
            {
                this.RaiseLogEvent(string.Format("{0}:{1}", attribute.AttributeName, attribute.ToString()));
            }

            if (!ConfigSyncControl.Preview)
            {
                this.Resource.Save();
            }
        }

        private void ProcessResourceUpdate()
        {
            if (this.Resource == null)
            {
                this.Resource = RmcWrapper.Client.GetResourceByKey(this.ResourceType, this.GetAnchorValues(), this.AttributesToGet);
                
                if (this.Resource == null)
                {
                    throw new InvalidOperationException(string.Format("An update operation is not valid for the resource operation with ID {0} as the resource does not exist in the FIM service. Consider changing the operation to an 'Add Update' type", this.ID));
                }
            }

            this.ExecuteAttributeOperations();

            Dictionary<string, List<AttributeValueChange>> attributeChanges = this.Resource.PendingChanges;

            if (attributeChanges.Count == 0)
            {
                this.RaiseLogEvent(string.Format("Object {0} was up to date", this.ID));
            }
            else
            {
                this.RaiseLogEvent(string.Format("Updating resource {0} with the following attribute values", this.ID));

                foreach (KeyValuePair<string, List<AttributeValueChange>> attributeChange in attributeChanges)
                {
                    foreach (AttributeValueChange valueChange in attributeChange.Value)
                    {
                        this.RaiseLogEvent(string.Format("{0}:{1}:{2}", attributeChange.Key, valueChange.ChangeType, valueChange.Value == null ? string.Empty : valueChange.Value.ToString()));
                    }
                }

                if (!ConfigSyncControl.Preview)
                {
                    this.Resource.Save();
                }
            }
        }

        private void ProcessResourceDelete()
        {
            if (this.Resource == null)
            {
                this.Resource = RmcWrapper.Client.GetResourceByKey(this.ResourceType, this.GetAnchorValues(), new List<string>() { "ObjectID" });

                if (this.Resource == null)
                {
                    this.RaiseLogEvent(string.Format("The object {0} was not found in the FIM service", this.ID));
                    return;
                }
            }

            this.RaiseLogEvent(string.Format("Deleting object {0}-{1}", this.ID, this.ObjectID));

            if (!ConfigSyncControl.Preview)
            {
                RmcWrapper.Client.DeleteResource(this.Resource);
            }
        }

        private void ProcessResourceAddUpdate()
        {
            this.Resource = RmcWrapper.Client.GetResourceByKey(this.ResourceType, this.GetAnchorValues(), this.AttributesToGet);

            if (this.Resource == null)
            {
                this.ProcessResourceAdd();
            }
            else
            {
                this.ProcessResourceUpdate();
            }
        }

        internal Dictionary<string, object> GetAnchorValues()
        {
            Dictionary<string, object> anchors = new Dictionary<string, object>();

            foreach (string anchor in this.AnchorAttributes)
            {
                if (string.IsNullOrWhiteSpace(anchor))
                {
                    throw new ArgumentException(string.Format("The resource operation with ID {0} specified a modification type that requires an anchor, but no anchor attributes were present"));
                }

                AttributeOperation attributeOp = this.AttributeOperations.FirstOrDefault(t => t.Name == anchor);

                if (attributeOp == null)
                {
                    throw new ArgumentException(string.Format("The resource operation with ID {0} specified a modification type that requires an anchor, but the defined anchor attribute was not present in the list of AttributeOperations"));
                }

                anchors.Add(anchor, attributeOp.ExpandedValue.ToString());
            }

            return anchors;
        }

        internal IEnumerable<string> AttributesToGet
        {
            get
            {
                List<string> attributesToGet = new List<string>();

                foreach (string anchor in this.AnchorAttributes)
                {
                    attributesToGet.Add(anchor);
                }

                foreach (AttributeOperation op in this.AttributeOperations)
                {
                    attributesToGet.Add(op.Name);
                }

                return attributesToGet.Distinct();
            }
        }

        private void ExecuteAttributeOperations()
        {
            foreach (AttributeOperation op in this.AttributeOperations)
            {
                op.ExecuteOperation(this.Resource, this.Operation);
            }
        }

        private void RaiseLogEvent(string message)
        {
            if (ResourceOperation.LogEvent != null)
            {
                ResourceOperation.LogEvent(this, message);
            }
        }
    }
}