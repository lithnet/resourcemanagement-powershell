
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.Update, "ResourceManagementClientSchema")]
    public class UpdateResourceManagementClientSchema : Cmdlet
    {
        protected override void ProcessRecord()
        {
            RmcWrapper.Client.RefreshSchema();
        }
    }
}