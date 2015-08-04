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
    [Cmdlet(VerbsCommon.Search, "Resources", DefaultParameterSetName = "ConstrainedQueryByClass")]
    public class SearchResources : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public string XPath { get; set; }

        [Parameter(ParameterSetName = "ConstrainedQueryByAttributes", Mandatory = false, Position = 2)]
        public string[] AttributesToGet { get; set; }

        [Parameter(ParameterSetName = "ConstrainedQueryByClass", Mandatory = false, Position = 2)]
        public string ExpectedObjectClass { get; set; }

        [Parameter(ParameterSetName = "UnconstrainedQuery", Mandatory = false, Position = 3)]
        public SwitchParameter Unconstrained { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<string> attributes = null;

            if (!this.Unconstrained.IsPresent)
            {
                if (this.AttributesToGet == null || this.AttributesToGet.Length == 0)
                {
                    if (string.IsNullOrWhiteSpace(this.ExpectedObjectClass))
                    {
                        attributes = new List<string>() { "ObjectID" };
                    }
                    else
                    {
                        ObjectTypeDefinition objectType = ResourceManagementSchema.GetObjectType(this.ExpectedObjectClass);
                        attributes = objectType.Attributes.Select(t => t.SystemName);
                    }
                }
                else
                {
                    attributes = this.AttributesToGet;
                }
            }

            foreach (ResourceObject resource in RmcWrapper.Client.GetResources(this.XPath, attributes))
            {
                this.WriteObject(resource);
            }
        }
    }
}