using Lithnet.ResourceManagement.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.ChangeObserver
{
    public class ObserverObjectSetting
    {
        public string ObjectType { get; set; }
        public List<string> AttributeSeparations { get; set; }
        
        public string XPathExpression { get; set; }
    }

    [Cmdlet(VerbsCommon.New, "RMObserverObjectSetting")]
    public class NewObserverObjectSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public string ExportDirectory { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 3)]
        public string[] AttributeSeparations { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 4)]
        public string XPathExpression { get; set; }

        protected override void BeginProcessing()
        {
            this.WriteObject(
                new ObserverObjectSetting()
                {
                    ObjectType = ObjectType,                    
                    AttributeSeparations = AttributeSeparations?.ToList(),
                    XPathExpression = XPathExpression
                }
                );
        }
    }
}
