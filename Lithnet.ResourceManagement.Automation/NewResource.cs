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
    [Cmdlet(VerbsCommon.New, "Resource")]
    public class NewResource : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        protected override void ProcessRecord()
        {
            this.WriteObject(RmcWrapper.Client.CreateResource(this.ObjectType));
        }
    }
}