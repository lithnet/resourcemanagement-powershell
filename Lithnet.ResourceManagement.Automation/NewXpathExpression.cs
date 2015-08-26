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
    [Cmdlet(VerbsCommon.New, "XpathExpression")]
    public class NewXpathExpression : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(Mandatory = true, Position = 2, ValueFromPipeline=true)]
        public IXPathQueryObject QueryObject { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public string DereferenceAttribute { get; set; }

        protected override void ProcessRecord()
        {
            if (this.DereferenceAttribute == null)
            {
                this.WriteObject(new XPathExpression(this.ObjectType, this.QueryObject));
            }
            else
            {
                this.WriteObject(new XPathDereferencedExpression(this.ObjectType, this.DereferenceAttribute, this.QueryObject));
            }
        }
    }
}