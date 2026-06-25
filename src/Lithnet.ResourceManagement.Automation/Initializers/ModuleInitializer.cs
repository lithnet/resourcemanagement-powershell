using System.IO;
using System.Management.Automation;
using System.Reflection;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class ModuleInitializer : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        public void OnImport()
        {
            RmcConfiguration.FxHostPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
        }
    }
}
