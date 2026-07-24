using System.Management.Automation;

namespace Lithnet.ResourceManagement.Automation
{
    public abstract class ModulePSCmdlet : PSCmdlet
    {
        protected sealed override void BeginProcessing()
        {
            ModuleExecutionContext.Run(this.BeginProcessingInModuleContext);
        }

        protected virtual void BeginProcessingInModuleContext()
        {
            base.BeginProcessing();
        }

        protected sealed override void ProcessRecord()
        {
            ModuleExecutionContext.Run(this.ProcessRecordInModuleContext);
        }

        protected virtual void ProcessRecordInModuleContext()
        {
            base.ProcessRecord();
        }

        protected sealed override void EndProcessing()
        {
            ModuleExecutionContext.Run(this.EndProcessingInModuleContext);
        }

        protected virtual void EndProcessingInModuleContext()
        {
            base.EndProcessing();
        }

        protected sealed override void StopProcessing()
        {
            ModuleExecutionContext.Run(this.StopProcessingInModuleContext);
        }

        protected virtual void StopProcessingInModuleContext()
        {
            base.StopProcessing();
        }
    }
}
