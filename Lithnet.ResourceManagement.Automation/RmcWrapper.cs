using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lithnet.ResourceManagement.Client;

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
