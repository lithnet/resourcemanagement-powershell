using System.Management.Automation;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "XPathQuery")]
    public class NewXPathQuery : PSCmdlet//, IDynamicParameters
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string AttributeName { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public ComparisonOperator Operator { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public object Value { get; set; }

        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter Negate { get; set; }

        protected override void ProcessRecord()
        {
            object unwrappedValue;

            PSObject wrappedObject = Value as PSObject;

            if (wrappedObject != null)
            {
                unwrappedValue = wrappedObject.BaseObject;
            }
            else
            {
                unwrappedValue = Value;
            }

            var attribute = RmcWrapper.Client.GetAttributeDefinition(this.AttributeName);

            WriteObject(new XPathQuery(attribute, (Client.ComparisonOperator)Operator, unwrappedValue, Negate.IsPresent));
        }

    }
}