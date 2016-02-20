using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using Microsoft.ResourceManagement.WebServices;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.Search, "Resources", DefaultParameterSetName = "ConstrainedQueryByTypeRaw")]
    public class SearchResources : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public object XPath { get; set; }

        [Parameter(ParameterSetName = "ConstrainedQueryByAttributesRaw", Mandatory = false, Position = 2)]
        public string[] AttributesToGet { get; set; }

        [Parameter(ParameterSetName = "ConstrainedQueryByTypeRaw", Mandatory = false, Position = 2)]
        public string ExpectedObjectType { get; set; }

        [Parameter(ParameterSetName = "UnconstrainedQueryRaw", Mandatory = false, Position = 3)]
        public SwitchParameter Unconstrained { get; set; }

        [Parameter]
        public int MaxResults { get; set; }

        [Parameter]
        public int PageSize { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<string> attributes = null;
            string filter = this.GetQueryString();

            if (!this.Unconstrained.IsPresent)
            {
                if (this.AttributesToGet == null || this.AttributesToGet.Length == 0)
                {
                    if (string.IsNullOrWhiteSpace(this.ExpectedObjectType))
                    {
                        attributes = new List<string>() { "ObjectID" };
                    }
                    else
                    {
                        ObjectTypeDefinition objectType = ResourceManagementSchema.GetObjectType(this.ExpectedObjectType);
                        attributes = objectType.Attributes.Select(t => t.SystemName);
                    }
                }
                else
                {
                    attributes = this.AttributesToGet;
                }
            }

            int count = 0;
            int pageSize;

            if (this.PageSize > 0)
            {
                pageSize = this.PageSize;
            }
            else
            {
                pageSize = 200;
            }

            if (this.MaxResults > 0 && this.MaxResults < pageSize)
            {
                pageSize = this.MaxResults;
            }

            foreach (ResourceObject resource in RmcWrapper.Client.GetResources(filter, pageSize, attributes))
            {
                this.WriteObject(new RmaObject(resource));
                count++;

                if (this.MaxResults > 0 && count >= this.MaxResults)
                {
                    break;
                }
            }
        }

        private string GetQueryString()
        {
            XPathExpression expression = this.XPath as XPathExpression;

            if (expression != null)
            {
                return expression.ToString(false);
            }

            PSObject wrappedObject = this.XPath as PSObject;

            if (wrappedObject != null)
            {
                expression = wrappedObject.BaseObject as XPathExpression;

                if (expression != null)
                {
                    return expression.ToString(false);
                }

                throw new ArgumentException("The XPath parameter must be a string or XPathExpression object");
            }

            if (!(this.XPath is string))
            {
                throw new ArgumentException("The XPath parameter must be a string or XPathExpression object");
            }

            return (string)this.XPath;
        }
    }
}