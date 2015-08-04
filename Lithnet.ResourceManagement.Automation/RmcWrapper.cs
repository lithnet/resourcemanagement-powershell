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
    }
}