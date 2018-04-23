using Lithnet.ResourceManagement.Automation.Enums;
using Lithnet.ResourceManagement.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.ConvertTo, "RMConfig")]
    public class ConvertToRMConfig : PSCmdlet
    {

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        [Parameter(ParameterSetName = "ConvertResourceReferenceResolution", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public RmaObject[] Resources { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = true, Position = 2)]
        public string[] AnchorAttributes { get; set; }

        [Parameter(ParameterSetName = "ConvertResourceReferenceResolution", Mandatory = true, ValueFromPipeline = false, Position = 3)]
        public ConfigSyncController ConfigSyncConfigurationController { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = false, Position = 3)]
        public string IDPrefix { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = false, Position = 4)]
        public ObjectExclusion[] ObjectExclusion { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 5)]
        public string[] AttributExclusions { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 6)]
        public bool IncludeDefaultAttributes { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 7)]
        public bool IncludeEmptyAttributeValues { get; set; }


        private string[] defaultAttributes = new string[] {
            "CreatedTime",
            "ObjectID",
            "Creator",
            "ComputedMember",
            "MVObjectID",
            "ResourceTime" };

        private ConfigFile config;

        private List<ResourceObject> resolvedObject = new List<ResourceObject>();

        protected override void BeginProcessing()
        {
            config = new ConfigFile();
            if (ConfigSyncConfigurationController == null)
            {
                ConfigSyncConfiguration c = new ConfigSyncConfiguration()
                {
                    AnchorAttributes = AnchorAttributes.ToList(),
                    AttributExclusions = AttributExclusions.ToList(),
                    IDPrefix = IDPrefix,
                    IncludeDefaultAttributes = IncludeDefaultAttributes,
                    IncludeEmptyAttributeValues = IncludeEmptyAttributeValues,
                    ObjectSpecificExlusions = ObjectExclusion.ToList(),
                    ObjectType = Resources[0].InternalObject.ObjectTypeName
                };

                ConfigSyncConfigurationController = new ConfigSyncController()
                {
                    Configurations = new List<ConfigSyncConfiguration>() { c }
                };
            }
        }

        protected override void ProcessRecord()
        {
            int count = 0;

            foreach (ResourceObject r in Resources.Select(t => t.InternalObject))
            {
                ProgressRecord p = new ProgressRecord(0, string.Format("Processing converting...")
                    , string.Format("Processing {0}", r.ObjectID));
                p.PercentComplete = (count / Resources.Length) * 100;

                ConfigSyncConfiguration syncConfiguration = ConfigSyncConfigurationController.Configurations
                    .Where(c => c.ObjectType == r.ObjectTypeName)
                    .First();

                bool objectExcluded = r.ProcessObjectExclusion(syncConfiguration);

                if (objectExcluded)
                {
                    this.WriteWarning(String.Format(
                        "Ressource Object {0} is filtered by ObjectExclusion",
                        r.ObjectID));
                }
                else
                {
                    config.Operations.Add(
                       new ResourceOperation()
                       {
                           Operation = ResourceOperationType.AddUpdate,
                           ResourceType = r.ObjectType.SystemName,
                           ID = GetID(r, true),
                           AnchorAttributes = syncConfiguration.AnchorAttributes.ToList(),
                           AttributeOperations = GetAttributeOperations(r, syncConfiguration)
                       });


                    this.WriteProgress(p);
                }

            }
        }
        protected override void EndProcessing()
        {
            this.WriteObject(AddXMLReferencedObjects(config));
        }

        private ConfigFile AddXMLReferencedObjects(ConfigFile configFile)
        {
            ConfigFile file = new ConfigFile();
            List<ResourceObject> refObjectsToAdd = resolvedObject.ToList();
            do
            {
                foreach (ResourceObject r in refObjectsToAdd)
                {
                    ConfigSyncConfiguration syncConfiguration = ConfigSyncConfigurationController.Configurations
                        .Where(c => c.ObjectType == r.ObjectTypeName)
                        .First();

                    file.Operations.Add(
                           new ResourceOperation()
                           {
                               Operation = ResourceOperationType.None,
                               ResourceType = r.ObjectType.SystemName,
                               ID = GetID(r, true),
                               AnchorAttributes = syncConfiguration.AnchorAttributes.ToList(),
                               AttributeOperations = GetAttributeOperations(r, syncConfiguration)
                           });
                }

                if (refObjectsToAdd.Count != resolvedObject.Count)
                {
                    List<ResourceObject> newToResolve = new List<ResourceObject>();

                    foreach (ResourceObject r in resolvedObject)
                    {
                        if (!refObjectsToAdd.Exists(x => x.ObjectID == r.ObjectID))
                            newToResolve.Add(r);
                    }
                    resolvedObject = newToResolve;
                }
                else
                    resolvedObject.Clear();

            } while (resolvedObject.Count == 0);            

            file.Operations.AddRange(configFile.Operations);

            return file;
        }

        private List<AttributeOperation> GetAttributeOperations(ResourceObject r, ConfigSyncConfiguration configuration)
        {
            List<AttributeOperation> operations = new List<AttributeOperation>();

            foreach (var a in r.Attributes)
            {
                // Processing AttributExclusions
                if (configuration.AttributExclusions != null)
                {
                    if (configuration.AttributExclusions.Contains(a.AttributeName))
                        if (configuration.AnchorAttributes.Contains(a.AttributeName))
                            this.WriteWarning(
                                String.Format(
                                    "Attribut {0} was skipped, because it is configured as AnchorAttribut.",
                                    a.AttributeName
                                    ));
                        else
                            continue;
                }

                // Processing Default Attribute Exclusions
                if (!configuration.IncludeDefaultAttributes)
                {
                    if (this.defaultAttributes.Contains(a.AttributeName))
                        continue;
                }


                // Skipping Empty Attribute Exclusions
                if (!configuration.IncludeEmptyAttributeValues)
                {
                    if (a.IsNull)
                        continue;
                }

                // Adding empty Add in case of a multiValue attribute
                if (a.Attribute.IsMultivalued)
                {
                    operations.Add(
                        new AttributeOperation()
                        {
                            Operation = AttributeOperationType.Replace,
                            Name = a.AttributeName,
                        });
                }

                // Processing AttributOperation Conversation if needed
                if (configuration.ReferenceResolution)
                    operations.AddRange(ResolveReferenceAttribute(a));
                else
                    operations.AddRange(GetAttributeOperations(a));
            }

            return operations.OrderBy(a => a.Name).ToList();
        }

        private List<AttributeOperation> GetAttributeOperations(AttributeValue attributeValue)
        {
            List<AttributeOperation> operations = new List<AttributeOperation>();

            foreach (string val in attributeValue.ValuesAsString)
            {
                AttributeOperation op = new AttributeOperation();
                op.Operation = (attributeValue.Attribute.IsMultivalued) ? AttributeOperationType.Add : AttributeOperationType.Replace;
                op.Name = attributeValue.AttributeName;
                op.Value = (attributeValue.IsFilter()) ? GetFilterValue(val) : val;
                op.ValueType = (attributeValue.IsFilter()) ? AttributeValueType.XPathFilter : AttributeValueType.Value;
                operations.Add(op);
            }

            return operations;
        }

        private List<AttributeOperation> ResolveReferenceAttribute(AttributeValue attributeValue)
        {
            if (attributeValue.Attribute.Type == AttributeType.Binary ||
                attributeValue.Attribute.Type == AttributeType.Boolean ||
                attributeValue.Attribute.Type == AttributeType.DateTime ||
                attributeValue.Attribute.Type == AttributeType.Integer)
            {
                return GetAttributeOperations(attributeValue);
            }
            else
            {
                List<AttributeOperation> operations = new List<AttributeOperation>();
                List<string> values = new List<string>();

                var guidRegex = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}",
                                              RegexOptions.IgnoreCase);

                if (attributeValue.Attribute.IsMultivalued)
                    values.AddRange(attributeValue.StringValues);
                else values.Add(attributeValue.StringValue);

                foreach (string val in values)
                {
                    string resolved = val;
                    bool inlineReferenceResolution = false;

                    foreach (Match match in guidRegex.Matches(val))
                    {
                        if (!WellKnownGuids.Contains(match.Value))
                        {
                            var refObj = resolvedObject
                                            .Where(r => r.ObjectID.Value == match.Value)
                                            .FirstOrDefault();

                            if (refObj == null)
                            {
                                // Get Reference Object if not is resolved previously
                                refObj = RmcWrapper.Client.GetResource(match.Value);

                                if (refObj == null)
                                    throw new Exception(String.Format(
                                        "Could not resolve reference attribut ({0}) with ID ({1})",
                                        attributeValue.Attribute.SystemName,
                                        match.Value));
                                else
                                    resolvedObject.Add(refObj);
                            }

                            var refAnchorAttributes = ConfigSyncConfigurationController.Configurations
                                    .Where(c => c.ObjectType == refObj.ObjectType.SystemName)
                                    .First()
                                    .AnchorAttributes;


                            if (val == match.Value)
                            {
                                if (refAnchorAttributes.Count == 1)
                                {
                                    // Single Text or string Value resolution
                                    string refID = GetID(refObj, false);

                                    operations.Add(new AttributeOperation()
                                    {
                                        Name = attributeValue.AttributeName,
                                        Operation = attributeValue.Attribute.IsMultivalued ? AttributeOperationType.Add : AttributeOperationType.Replace,
                                        Value = string.Format("{0}|{1}|{2}",
                                                                    refObj.ObjectTypeName,
                                                                    attributeValue.AttributeName,
                                                                    refID),
                                        ValueType = AttributeValueType.Reference
                                    });
                                }
                                else
                                {
                                    // Single Text or string Value resolution
                                    string refID = GetID(refObj, true);

                                    operations.Add(new AttributeOperation()
                                    {
                                        Name = attributeValue.AttributeName,
                                        Operation = attributeValue.Attribute.IsMultivalued ? AttributeOperationType.Add : AttributeOperationType.Replace,
                                        Value = refID,
                                        ValueType = AttributeValueType.XmlReference
                                    });
                                }
                            }
                            else
                            {
                                string refID = GetID(refObj, true);
                                resolved = resolved.Replace(match.Value, String.Format("##xmlref:{0}:ObjectID##", refID));
                                inlineReferenceResolution = true;
                            }
                        }
                    }

                    if(!guidRegex.IsMatch(val))
                        operations.Add(new AttributeOperation()
                        {
                            Name = attributeValue.AttributeName,
                            Operation = attributeValue.Attribute.IsMultivalued ? AttributeOperationType.Add : AttributeOperationType.Replace,
                            Value = (attributeValue.IsFilter()) ? GetFilterValue(val) : val,
                            ValueType = attributeValue.IsFilter() ? AttributeValueType.XPathFilter : AttributeValueType.Value
                        });

                    if (inlineReferenceResolution)
                    {
                        operations.Add(new AttributeOperation()
                        {
                            Name = attributeValue.AttributeName,
                            Operation = attributeValue.Attribute.IsMultivalued ? AttributeOperationType.Add : AttributeOperationType.Replace,
                            Value = (attributeValue.IsFilter()) ? GetFilterValue(resolved) : resolved,
                            ValueType = attributeValue.IsFilter() ? AttributeValueType.XPathFilter : AttributeValueType.Value
                        });
                    }
                }

                return operations;
            }
        }

        public string GetFilterValue(string Filter)
        {
            int startindex = Filter.IndexOf(@">") + 1;
            return Filter.Substring(startindex).Replace("</Filter>", "");
        }

        private string GetID(ResourceObject r, bool AddIdSuffix)
        {
            var config = ConfigSyncConfigurationController.Configurations
                .Where(c => c.ObjectType == r.ObjectType.SystemName)
                .First();

            string id = (!string.IsNullOrEmpty(config.IDPrefix) && AddIdSuffix) ? config.IDPrefix : "";
            foreach (string s in config.AnchorAttributes)
                id += (string.IsNullOrEmpty(id)) ? r.Attributes[s].StringValue : "-" + r.Attributes[s].StringValue;
            return id;

        }



        private void ResourceOperation_LogEvent(object sender, string e)
        {
            this.builder.AppendLine(e);
        }

        private StringBuilder builder = new StringBuilder();

    }
}
