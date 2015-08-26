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
    [Cmdlet(VerbsCommon.New, "XpathQuery")]
    public class NewXpathQuery : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string AttributeName { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public ComparisonOperator Operator { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public object Value { get; set; }
        
        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter Negate { get; set; }
        
        protected override void ProcessRecord()
        {
            this.WriteObject( new XPathQuery(this.AttributeName, this.Operator, this.Value, this.Negate.IsPresent));
        }
    }
}