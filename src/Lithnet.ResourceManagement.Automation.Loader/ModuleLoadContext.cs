using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Lithnet.ResourceManagement.Automation.Loader
{
    /// <summary>
    /// Hosts the PowerShell Core cmdlet assembly and its bundled dependency closure in a dedicated
    /// non-collectible load context. The loader itself remains in the default context and has no
    /// dependencies outside the shared framework. Managed and native resolution follows the
    /// cmdlet assembly's deps.json; unresolved assemblies return to the host's normal resolution
    /// path. A missing cmdlet assembly fails at initialization with its expected path. If neither
    /// the module layout nor the host can satisfy another request, the runtime's load exception is
    /// allowed to propagate rather than being swallowed.
    /// </summary>
    public sealed class ModuleLoadContext : AssemblyLoadContext
    {
        private const string ModuleAssemblyFileName = "Lithnet.ResourceManagement.Automation.dll";

        private static readonly object syncLock = new object();
        private static ModuleLoadContext instance;

        private readonly Assembly loaderAssembly;
        private readonly AssemblyName loaderAssemblyName;
        private readonly Assembly moduleAssembly;
        private readonly AssemblyDependencyResolver resolver;

        private ModuleLoadContext(string moduleAssemblyPath)
            : base(name: "LithnetRMA", isCollectible: false)
        {
            this.resolver = new AssemblyDependencyResolver(moduleAssemblyPath);
            this.loaderAssembly = typeof(ModuleLoadContext).Assembly;
            this.loaderAssemblyName = this.loaderAssembly.GetName();
            this.moduleAssembly = this.LoadFromAssemblyPath(moduleAssemblyPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (AssemblyName.ReferenceMatchesDefinition(this.loaderAssemblyName, assemblyName))
            {
                return this.loaderAssembly;
            }

            string path = this.resolver.ResolveAssemblyToPath(assemblyName);

            if (path != null)
            {
                return this.LoadFromAssemblyPath(path);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string path = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            if (path != null)
            {
                return this.LoadUnmanagedDllFromPath(path);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Returns the single cmdlet assembly instance loaded into the module context.
        /// </summary>
        public static Assembly Initialize()
        {
            if (instance != null)
            {
                return instance.moduleAssembly;
            }

            lock (syncLock)
            {
                if (instance != null)
                {
                    return instance.moduleAssembly;
                }

                string loaderPath = typeof(ModuleLoadContext).Assembly.Location;
                string loaderDirectory = Path.GetDirectoryName(loaderPath);
                string modulePath = Path.Combine(loaderDirectory, ModuleAssemblyFileName);

                if (!File.Exists(modulePath))
                {
                    throw new FileNotFoundException(
                        $"The module assembly '{modulePath}' does not exist. The module installation is incomplete.",
                        modulePath);
                }

                instance = new ModuleLoadContext(modulePath);
                return instance.moduleAssembly;
            }
        }
    }
}
