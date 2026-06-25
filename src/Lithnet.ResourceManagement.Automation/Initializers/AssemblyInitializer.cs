using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Loader;
#endif

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
#if NET
                    if (alc == null)
                    {
                        alc = new DependencyAssemblyLoadContext(moduleDir);
                    }

                    loadedAssemblies[Path.GetFileNameWithoutExtension(dllPath)] = alc.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
#else
                    loadedAssemblies[Path.GetFileNameWithoutExtension(dllPath)] = Assembly.LoadFrom(dllPath);
#endif
                }
                catch
                {
                }
            }

#if NET
            AssemblyLoadContext.Default.Resolving += ResolveForAlc;
#else
            AppDomain.CurrentDomain.AssemblyResolve += ResolveForFramework;
#endif
        }

        internal static Assembly FindCachedAssembly(string name)
        {
            if (loadedAssemblies.TryGetValue(name, out Assembly assembly))
            {
                return assembly;
            }

            return null;
        }

#if NET
        private static DependencyAssemblyLoadContext alc;

        internal static void UnhookResolver()
        {
            AssemblyLoadContext.Default.Resolving -= ResolveForAlc;
        }

        private static Assembly ResolveForAlc(AssemblyLoadContext defaultAlc, AssemblyName assemblyName)
        {
            return FindCachedAssembly(assemblyName.Name);
        }
#else
        private static Assembly ResolveForFramework(object sender, ResolveEventArgs e)
        {
            return FindCachedAssembly(new AssemblyName(e.Name).Name);
        }
#endif
    }
}
