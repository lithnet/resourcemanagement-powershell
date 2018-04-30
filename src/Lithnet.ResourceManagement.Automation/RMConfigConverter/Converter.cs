using Lithnet.ResourceManagement.Automation.Enums;
using Lithnet.ResourceManagement.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation.RMConfigConverter
{
    internal class Converter
    {
        private ConfigFile config;
        private List<ResourceObject> resolvedObject = new List<ResourceObject>();
        private ConverterSetting settings;

        private static Regex illegalInFileName = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), RegexOptions.Compiled);

        internal static ConfigFile ConvertToRMConfig(ResourceObject Resource, ConverterSetting config)
        {
            return ConvertToRMConfig(new List<ResourceObject>() { Resource }, config);
        }

        internal static ConfigFile ConvertToRMConfig(List<ResourceObject> Resources, ConverterSetting config)
        {
            Converter converter = new Converter(config);

            foreach (ResourceObject r in Resources)
                converter.TryAddResourceOperation(r);

            return converter.GetConfigFile();
        }

        /// <summary>
        /// Serialize a ConfigFile to the file system
        /// </summary>
        /// <param name="RMConfig"></param>
        /// <param name="AttributeSeparations"></param>
        /// <param name="FilePath"></param>
        /// <returns>A list of exported files</returns>
        internal static List<string> SerializeConfigFile(ConfigFile RMConfig, List<string> AttributeSeparations, string FilePath)
        {
            List<string> fileExportList = new List<string>();

            if (RMConfig.Operations.Count > 0)
            {
                ConfigSyncControl.CurrentConfig = RMConfig;
                ConfigSyncControl.CurrentPath = FilePath;

                string exportDirectory = Path.GetDirectoryName(FilePath);

                if (!Directory.Exists(exportDirectory))
                    Directory.CreateDirectory(exportDirectory);

                string fileName = Path.GetFileName(FilePath);

                XmlSerializer s = new XmlSerializer(typeof(ConfigFile));

                if (AttributeSeparations != null)
                {
                    foreach (ResourceOperation operations in RMConfig.Operations)
                    {
                        foreach (string a in AttributeSeparations)
                        {
                            var attributeOperations = operations.AttributeOperations.FindAll(o => o.Name == a);

                            if (attributeOperations.Count == 1)
                            {
                                string attributDirectory = Path.Combine(exportDirectory, attributeOperations[0].Name);
                                string attributeFilePath = Path.Combine(attributDirectory, fileName);

                                if (!Directory.Exists(attributDirectory))
                                    Directory.CreateDirectory(attributDirectory);


                                File.WriteAllText(
                                    attributeFilePath,
                                    attributeOperations[0].Value);

                                fileExportList.Add(attributeFilePath);

                                attributeOperations[0].ValueType = AttributeValueType.File;
                                attributeOperations[0].Value = Path.Combine(
                                        @".\",
                                        attributeOperations[0].Name,
                                        fileName);
                            }
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(FilePath))
                {
                    s.Serialize(sw, RMConfig);
                }

                fileExportList.Insert(0, FilePath);
            }

            return fileExportList;
        }

        internal Converter(ConverterSetting converterSetting)
        {
            this.settings = converterSetting;
            config = new ConfigFile();
        }

        internal bool TryAddResourceOperation(ResourceObject Resource)
        {
            ObjectSetting objectSetting = settings.Configurations
                    .Where(c => c.ObjectType == Resource.ObjectTypeName)
                    .First();

            bool objectExcluded = ProcessingObjectExclusion(ref Resource, objectSetting);

            if (objectExcluded)
            {
                return false;
            }
            else
            {
                config.Operations.Add(
                   new ResourceOperation()
                   {
                       Operation = ResourceOperationType.Add | ResourceOperationType.Update,
                       ResourceType = Resource.ObjectType.SystemName,
                       ID = GetID(Resource, true),
                       AnchorAttributes = objectSetting.AnchorAttributes.ToList(),
                       AttributeOperations = GetAttributeOperations(Resource, objectSetting)
                   });
                return true;
            }
        }

        internal ConfigFile GetConfigFile()
        {
            return AddXMLReferencedObjects();
        }





        private ConfigFile AddXMLReferencedObjects()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFile));
            string configFile;

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, this.config);
                configFile = textWriter.ToString();
            }

            ConfigFile file = new ConfigFile();

            List<ResourceObject> objRefResolutionDone = new List<ResourceObject>();
            List<ResourceObject> objRefNeedResolution = resolvedObject.ToList();

            do
            {
                foreach (ResourceObject r in objRefNeedResolution)
                {
                    ObjectSetting objectSetting = settings.Configurations
                        .Where(c => c.ObjectType == r.ObjectTypeName)
                        .FirstOrDefault();

                    string refID = GetID(r, true);

                    if (configFile.Contains(String.Format("##xmlref:{0}:ObjectID##", refID)))
                    {
                        file.Operations.Add(
                               new ResourceOperation()
                               {
                                   Operation = ResourceOperationType.None,
                                   ResourceType = r.ObjectType.SystemName,
                                   ID = GetID(r, true),
                                   AnchorAttributes = objectSetting.AnchorAttributes.ToList(),
                                   AttributeOperations = GetAttributeOperations(r, objectSetting, false)
                               });
                    }
                    objRefResolutionDone.Add(r);
                }
                objRefNeedResolution.Clear();

                foreach (ResourceObject r in resolvedObject)
                {
                    if (!objRefResolutionDone.Exists(x => x.ObjectID == r.ObjectID))
                        objRefNeedResolution.Add(r);
                }
            } while (objRefNeedResolution.Count != 0);

            file.Operations.AddRange(config.Operations);

            return file;
        }

        private List<AttributeOperation> GetAttributeOperations(ResourceObject r, ObjectSetting objectSetting, bool ReferenceResolution)
        {
            List<AttributeOperation> operations = new List<AttributeOperation>();

            if (objectSetting == null)
            {
                objectSetting = ObjectSetting.GetDefaultObjectSetting(r.ObjectTypeName);
                /* Export minimum attributes
                minimumAttributes.ForEach(item =>
                {
                    operations.Add(new AttributeOperation()
                    {
                        Operation = AttributeOperationType.Replace,
                        Name = item,
                        Value = r.Attributes[item].StringValue
                    });
                });
                
            }
            else
            {*/
            }
            foreach (var a in r.Attributes)
            {
                // Processing AttributExclusions
                if (!objectSetting.AnchorAttributes.Contains(a.AttributeName))
                {
                    if (objectSetting.AttributExclusions != null)
                    {
                        if (objectSetting.AttributExclusions.Contains(a.AttributeName))
                            continue;
                    }

                    // Processing Default Attribute Exclusions
                    if (!objectSetting.IncludeDefaultAttributes)
                    {
                        if (ObjectSetting.DefaultAttributes.Contains(a.AttributeName))
                            continue;
                    }


                    // Skipping Empty Attribute Exclusions
                    if (!objectSetting.IncludeEmptyAttributeValues)
                    {
                        if (a.IsNull)
                            continue;
                    }
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
                if (ReferenceResolution)
                {
                    if (objectSetting.ReferenceResolutionAttributExclusions != null)
                    {
                        if (objectSetting.ReferenceResolutionAttributExclusions.Contains(a.AttributeName))
                            operations.AddRange(GetAttributeOperations(a));
                        else
                            operations.AddRange(ResolveReferenceAttribute(a));
                    }
                    else
                    {
                        operations.AddRange(ResolveReferenceAttribute(a));
                    }
                }
                else
                    operations.AddRange(GetAttributeOperations(a));
            }


            return operations.OrderBy(a => a.Name).ToList();
        }

        private List<AttributeOperation> GetAttributeOperations(ResourceObject r, ObjectSetting objectSetting)
        {
            return GetAttributeOperations(r, objectSetting, objectSetting.ReferenceResolution);
        }

        internal static string GetFileName(ResourceObject resourceObject, ObjectSetting objectSetting)
        {
            string fileName = String.Empty;
            foreach (string s in GetAnchorAttributeValues(resourceObject, objectSetting))
            {
                if (!string.IsNullOrEmpty(fileName))
                    fileName += "-";

                fileName += s;
            }

            return illegalInFileName.Replace(fileName + ".xml", " ");
        }

        private static List<string> GetAnchorAttributeValues(ResourceObject resourceObject, ObjectSetting objectSetting)
        {
            return GetAnchorAttributeValues(resourceObject, objectSetting, null);
        }

        private static List<string> GetAnchorAttributeValues(ResourceObject resourceObject, ObjectSetting objectSetting, List<ResourceObject> ResolvedResourceObjects)
        {
            List<string> anchorValues = new List<string>();
            foreach (string s in objectSetting.AnchorAttributes)
            {
                if (resourceObject.Attributes[s].Attribute.Type == AttributeType.Reference)
                {
                    ResourceObject refObj = null;
                    if (ResolvedResourceObjects != null)
                    {
                        var ob = ResolvedResourceObjects.Where(ro => ro.ObjectID.Value == resourceObject.Attributes[s].ReferenceValue.Value).FirstOrDefault();
                        if (ob != null)
                            refObj = ob;
                    }

                    if (refObj == null)
                        refObj = RmcWrapper.Client.GetResource(resourceObject.Attributes[s].ReferenceValue);

                    var nameAttribute = refObj.Attributes.FirstOrDefault(a => a.AttributeName == "Name");

                    if (nameAttribute != null)
                    {
                        anchorValues.Add(nameAttribute.StringValue);
                    }
                    else if (!String.IsNullOrEmpty(refObj.DisplayName))
                    {
                        anchorValues.Add(refObj.DisplayName);
                    }
                    else
                    {
                        anchorValues.Add(resourceObject.Attributes[s].StringValue);
                    }
                }
                else
                    anchorValues.Add(resourceObject.Attributes[s].StringValue);
            }
            return anchorValues;
        }

        internal static string GetFilePath(ResourceObject resourceObject, ObjectSetting objectSetting, string exportDirectory)
        {
            return Path.Combine(
                exportDirectory,
                resourceObject.ObjectTypeName,
                GetFileName(resourceObject, objectSetting)
                );

        }

        internal static List<string> GetSerializedFiles(ResourceObject resourceObject, ObjectSetting objectSetting, string exportDirectory, List<string> attributeSeparations)
        {
            List<string> list = new List<string>();

            string objFileName = GetFilePath(resourceObject, objectSetting, exportDirectory);
            string objectExportDirectory = Path.GetDirectoryName(objFileName);
            list.Add(objFileName);


            attributeSeparations?.ForEach(item =>
            {
                if (resourceObject.Attributes[item].IsNull)
                    list.Add(Path.Combine(exportDirectory, item, objFileName));
            });


            return list;
        }

        private List<AttributeOperation> GetAttributeOperations(AttributeValue attributeValue)
        {
            List<AttributeOperation> operations = new List<AttributeOperation>();

            foreach (string val in attributeValue.ValuesAsString)
            {
                AttributeOperation op = new AttributeOperation
                {
                    Operation = (attributeValue.Attribute.IsMultivalued) ? AttributeOperationType.Add : AttributeOperationType.Replace,
                    Name = attributeValue.AttributeName,
                    Value = (attributeValue.IsFilter()) ? GetFilterValue(val) : val,
                    ValueType = (attributeValue.IsFilter()) ? AttributeValueType.XPathFilter : AttributeValueType.Value
                };
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
                        if (!WellKnownGuids.Contains(match.Value) && match.Value != "00000000-0000-0000-0000-000000000000") //
                        {
                            var refObj = resolvedObject
                                            .Where(r => r.ObjectID.Value == match.Value)
                                            .FirstOrDefault();

                            if (refObj == null)
                            {
                                // Get Reference Object if not is resolved previously
                                try
                                {
                                    refObj = RmcWrapper.Client.GetResource(match.Value);
                                }
                                catch (ResourceNotFoundException) { continue; }


                                resolvedObject.Add(refObj);
                            }

                            if (val == match.Value)
                            {
                                var objectSetting = settings.Configurations
                                    .FirstOrDefault(c => c.ObjectType == refObj.ObjectType.SystemName);

                                if (objectSetting == null || objectSetting?.AnchorAttributes == null)
                                {
                                    operations.Add(new AttributeOperation()
                                    {
                                        Name = attributeValue.AttributeName,
                                        Operation = attributeValue.Attribute.IsMultivalued ? AttributeOperationType.Add : AttributeOperationType.Replace,
                                        Value = val,
                                        ValueType = AttributeValueType.Value
                                    });
                                }
                                else if (objectSetting.AnchorAttributes.Count == 1)
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

                    if (!guidRegex.IsMatch(val))
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

        private bool ProcessingObjectExclusion(ref ResourceObject resource, ObjectSetting objectSetting)
        {
            bool objectExcluded = false;

            // Processing ObjectSpecificExlusions
            if (objectSetting.ObjectSpecificExlusions != null)
            {
                foreach (var objexclusion in objectSetting.ObjectSpecificExlusions)
                {
                    foreach (var k in objexclusion.AnchorKeyValueList)
                    {
                        if (k.Value == resource.Attributes[k.Key].StringValue)
                        {
                            if (objexclusion.AttributExclusions.Contains("*"))
                            {
                                //EXCLUDE OBJECT
                                objectExcluded = true;
                                break;
                            }
                            else
                            {
                                foreach (string ea in objexclusion.AttributExclusions)
                                {
                                    if (!objectSetting.AnchorAttributes.Contains(ea))
                                        resource.Attributes[k.Key].Value = null;
                                }
                            }
                        }
                    }
                    if (objectExcluded)
                        break;
                }
            }

            return objectExcluded;
        }

        private string GetFilterValue(string Filter)
        {
            int startindex = Filter.IndexOf(@">") + 1;
            return Filter.Substring(startindex).Replace("</Filter>", "");
        }

        private string GetID(ResourceObject r, bool AddIdSuffix)
        {
            var config = settings.Configurations
                .Where(c => c.ObjectType == r.ObjectType.SystemName)
                .FirstOrDefault();

            if (config == null)
            {
                return r.ObjectID.GetGuid().ToString();
            }

            string id = (!string.IsNullOrEmpty(config.IDPrefix) && AddIdSuffix) ? config.IDPrefix : "";
            foreach (string s in GetAnchorAttributeValues(r, config, resolvedObject))
                id += (string.IsNullOrEmpty(id)) ? s : "-" + s;
            return id;

        }
    }
}
