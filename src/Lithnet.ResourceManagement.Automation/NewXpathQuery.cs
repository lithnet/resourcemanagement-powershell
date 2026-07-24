using System.Management.Automation;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "XPathQuery")]
    public class NewXPathQuery : ModulePSCmdlet//, IDynamicParameters
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string AttributeName { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public ComparisonOperator Operator { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public object Value { get; set; }

        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter Negate { get; set; }

        protected override void ProcessRecordInModuleContext()
        {
            object unwrappedValue = XPathObjectConverter.UnwrapPSObject(this.Value);

            var attribute = RmcWrapper.Client.GetAttributeDefinition(this.AttributeName);

            WriteObject(new XPathQuery(attribute, this.Operator, unwrappedValue, this.Negate.IsPresent));
        }

    }
}
