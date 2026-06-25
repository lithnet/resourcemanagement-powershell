using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Lithnet.ResourceManagement.Automation
{
    internal class DependencyAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string dependenciesPath;

        public DependencyAssemblyLoadContext(string dependenciesPath)
        {
            this.dependenciesPath = dependenciesPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (IsRuntimeProvidedAssembly(assemblyName.Name))
            {
                return null;
            }

            string path = Path.Combine(dependenciesPath, assemblyName.Name + ".dll");

            if (File.Exists(path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }

        private static bool IsRuntimeProvidedAssembly(string name)
        {
            if (name.StartsWith("System.ServiceModel") || name == "System.Private.ServiceModel")
            {
                return false;
            }

            return name.StartsWith("System.") || name.StartsWith("Microsoft.Win32.");
        }
    }
}
