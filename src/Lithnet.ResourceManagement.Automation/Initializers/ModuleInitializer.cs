using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.Loader;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class ModuleInitializer : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        public static bool IsFullFramework => typeof(object).Assembly.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase);

        private readonly Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private DependencyAssemblyLoadContext dependencyLoadContext;

        public void OnImport()
        {
            Trace.WriteLine($"Initializing PowerShell module loaded in {(IsFullFramework ? "netfx" : "netcore")}");

            PreloadAssemblies();
            HookResolvers();
            SetRmcHostPath();
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            UnhookResolvers();
        }

        private void PreloadAssemblies()
        {
            string moduleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string depsDir = Path.Combine(moduleDir, "dependencies");

            if (!Directory.Exists(depsDir))
            {
                Trace.TraceWarning("Dependencies directory not found: {0}", depsDir);
                return;
            }

            if (!IsFullFramework)
            {
                dependencyLoadContext = new DependencyAssemblyLoadContext(depsDir);
            }

            foreach (string dllPath in Directory.GetFiles(depsDir, "*.dll"))
            {
                string fileName = Path.GetFileName(dllPath);

                try
                {
                    Trace.WriteLine("Preloading assembly: " + fileName);

                    if (IsFullFramework)
                    {
                        assemblies.Add(fileName, Assembly.LoadFrom(dllPath));
                    }
                    else
                    {
                        assemblies.Add(fileName, dependencyLoadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath)));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to load {0}: {1}", fileName, ex.ToString());
                }
            }
        }

        private void HookResolvers()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            if (!IsFullFramework)
            {
                AssemblyLoadContext.Default.Resolving += ResolveAssemblyFromAlc;
            }
        }

        private void UnhookResolvers()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

            if (!IsFullFramework)
            {
                AssemblyLoadContext.Default.Resolving -= ResolveAssemblyFromAlc;
            }
        }

        private void SetRmcHostPath()
        {
            RmcConfiguration.FxHostPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            return FindCachedAssembly(assemblyName);
        }

        private Assembly ResolveAssemblyFromAlc(AssemblyLoadContext defaultAlc, AssemblyName assemblyName)
        {
            return FindCachedAssembly(assemblyName);
        }

        private Assembly FindCachedAssembly(AssemblyName assemblyName)
        {
            string key = assemblyName.Name + ".dll";

            if (assemblies.TryGetValue(key, out Assembly assembly))
            {
                return assembly;
            }

            return null;
        }
    }
}
