using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.Loader;
using Lithnet.ResourceManagement.Client;
namespace Lithnet.ResourceManagement.Automation
{
    public class ModuleInitializer : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        public static bool IsFullFramework => typeof(object).Assembly.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase);
        private Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

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
            var executingAssembly = Assembly.GetExecutingAssembly();

            foreach (string resource in executingAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll")))
            {
                using (var stream = executingAssembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                    {
                        continue;
                    }

                    try
                    {
                        Trace.WriteLine("Preloading assembly: " + resource);

                        if (IsFullFramework)
                        {
                            var bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);
                            assemblies.Add(resource, Assembly.Load(bytes));
                        }
                        else
                        {
                            assemblies.Add(resource, AssemblyContextResourceLoader.LoadIntoAlc(stream));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Failed to load: {0}\r\n", resource, ex.ToString());
                    }
                }
            }
        }

        private void HookResolvers()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            if (!IsFullFramework)
            {
                HookAssemblyLoadContextResolver();
            }
        }

        private void UnhookResolvers()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

            if (!IsFullFramework)
            {
                UnhookAssemblyLoadContextResolver();
            }
        }

        private void SetRmcHostPath()
        {
            RmcConfiguration.FxHostPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private void HookAssemblyLoadContextResolver()
        {
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
        }

        private void UnhookAssemblyLoadContextResolver()
        {
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
        }

        private Assembly ResolveAssembly(object s, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            return ResolveAssemblyFromCache(assemblyName);
        }

        private Assembly ResolveAssembly(AssemblyLoadContext defaultAlc, AssemblyName assemblyName)
        {
            return ResolveAssemblyFromCache(assemblyName);
        }

        private Assembly ResolveAssemblyFromCache(AssemblyName assemblyName)
        {
            var path = string.Format("{0}.dll", assemblyName.Name);

            if (assemblies.ContainsKey(path))
            {
                return assemblies[path];
            }

            return null;
        }
    }
}
