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
    [Cmdlet(VerbsCommon.Get, "ResourceCount")]
    public class GetResourceCount : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public object XPath { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<string> attributes = null;
            string filter = this.GetQueryString();
            attributes = new List<string>() { "ObjectID" };

            ISearchResultCollection results = RmcWrapper.Client.GetResources(filter, 0, attributes);
            this.WriteObject(results.Count);
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