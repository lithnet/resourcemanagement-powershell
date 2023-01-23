﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "XPathQueryGroup")]
    public class NewXPathQueryGroup : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public GroupOperator Operator { get; set; }

        [Parameter(Mandatory = false, Position = 2, ValueFromPipeline = true)]
        public object[] Queries { get; set; }

        protected override void ProcessRecord()
        {
            this.WriteObject(new XPathQueryGroup((Client.GroupOperator)this.Operator, (IXPathQueryObject[])this.Queries));
        }
    }
}