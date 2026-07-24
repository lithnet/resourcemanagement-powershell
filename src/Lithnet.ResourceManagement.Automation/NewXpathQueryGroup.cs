using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Collections;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "XPathQueryGroup")]
    public class NewXPathQueryGroup : ModuleCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public GroupOperator Operator { get; set; }

        [Parameter(Mandatory = false, Position = 2, ValueFromPipeline = true)]
        public IXPathQueryObject[] Queries { get; set; }

        protected override void ProcessRecordInModuleContext()
        {
            this.WriteObject(new XPathQueryGroup(this.Operator, this.Queries));
        }
    }
}
