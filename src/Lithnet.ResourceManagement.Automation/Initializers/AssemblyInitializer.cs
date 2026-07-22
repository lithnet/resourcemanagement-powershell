using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lithnet.ResourceManagement.Automation
{
    internal static class AssemblyInitializer
    {
        private static readonly Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        [ModuleInitializer]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255")]
        internal static void Initialize()
        {
            string moduleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (string dllPath in Directory.GetFiles(moduleDir, "*.dll"))
            {
                string fileName = Path.GetFileName(dllPath);
                if (fileName.Equals("Lithnet.ResourceManagement.Automation.dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    loadedAssemblies[Path.GetFileNameWithoutExtension(dllPath)] = Assembly.LoadFrom(dllPath);
                }
                catch
                {
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveForFramework;
        }

        private static Assembly FindCachedAssembly(string name)
        {
            if (loadedAssemblies.TryGetValue(name, out Assembly assembly))
            {
                return assembly;
            }

            return null;
        }

        private static Assembly ResolveForFramework(object sender, ResolveEventArgs e)
        {
            return FindCachedAssembly(new AssemblyName(e.Name).Name);
        }
    }
}
