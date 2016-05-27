using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lithnet.ResourceManagement.Client;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Management.Automation;

namespace Lithnet.ResourceManagement.Automation
{
    internal static class RmcWrapper
    {
        private static ResourceManagementClient client;

        public static ResourceManagementClient Client
        {
            get
            {
                if (RmcWrapper.client == null)
                {
                    RmcWrapper.client = new ResourceManagementClient();
                }

                return RmcWrapper.client;
            }

            internal set
            {
                RmcWrapper.client = value;
            }
        }

        public static RuntimeDefinedParameter GetObjectTypeParameter(string paramName, bool mandatory, int position, bool allowWildcard, string parameterSetName)
        {
            RuntimeDefinedParameter parameter = new RuntimeDefinedParameter();
            parameter.Name = paramName;
            parameter.ParameterType = typeof(string);

            if (ResourceManagementSchema.ObjectTypes != null && ResourceManagementSchema.ObjectTypes.Count > 0)
            {
                List<string> objectTypeNames = ResourceManagementSchema.ObjectTypes.OrderBy(t => t.Key).Select(t => t.Key).ToList();
                if (allowWildcard)
                {
                    objectTypeNames.Add("*");
                }

                ValidateSetAttribute setAttribute = new ValidateSetAttribute(objectTypeNames.ToArray());
                parameter.Attributes.Add(setAttribute);
            }

            ParameterAttribute paramAttribute = new ParameterAttribute();
            paramAttribute.Mandatory = mandatory;
            paramAttribute.Position = position;
            paramAttribute.ParameterSetName = parameterSetName;
            parameter.Attributes.Add(paramAttribute);
            return parameter;
        }

        public static RuntimeDefinedParameter GetAttributeNameParameter(string paramName, bool mandatory, int position, string objectType, string parameterSetName)
        {
            RuntimeDefinedParameter parameter = new RuntimeDefinedParameter();
            parameter.Name = paramName;
            parameter.ParameterType = typeof(string);

            if (objectType == null)
            {
                List<string> attributeNames = new List<string>();

                if (ResourceManagementSchema.ObjectTypes != null)
                {
                    foreach (ObjectTypeDefinition type in ResourceManagementSchema.ObjectTypes.Values)
                    {
                        attributeNames.AddRange(type.Attributes.Select(t => t.SystemName));
                    }

                    if (attributeNames.Count > 0)
                    {
                        ValidateSetAttribute setAttribute = new ValidateSetAttribute(attributeNames.Distinct().OrderBy(t => t).ToArray());
                        parameter.Attributes.Add(setAttribute);
                    }
                }
            }
            else
            {
                if (ResourceManagementSchema.ObjectTypes != null &&
                    ResourceManagementSchema.ObjectTypes.ContainsKey(objectType))
                {
                    ValidateSetAttribute setAttribute = new ValidateSetAttribute(ResourceManagementSchema.ObjectTypes[objectType].Attributes.OrderBy(t => t.SystemName).Select(t => t.SystemName).ToArray());
                    parameter.Attributes.Add(setAttribute);
                }
            }

            ParameterAttribute paramAttribute = new ParameterAttribute();
            paramAttribute.Mandatory = mandatory;
            paramAttribute.Position = position;
            paramAttribute.ParameterSetName = parameterSetName;
            parameter.Attributes.Add(paramAttribute);
            return parameter;
        }
    }
}