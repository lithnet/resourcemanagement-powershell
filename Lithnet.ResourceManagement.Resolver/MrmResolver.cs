using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Lithnet.ResourceManagement.Resolver
{
    public static class MrmResolver
    {
        private static int loadAttempts;

        public static void RegisterResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Microsoft.ResourceManagement"))
            {
                loadAttempts++;

                if (loadAttempts > 1)
                {
                    return null;
                }

                try
                {
#pragma warning disable 618
                    return Assembly.LoadWithPartialName("Microsoft.ResourceManagement");
#pragma warning restore 618
                }
                catch
                {
                    try
                    {
                        return Assembly.Load("Microsoft.ResourceManagement.dll");
                    }
                    catch
                    {
                    }
                }

            }

            return null;
        }
    }
}