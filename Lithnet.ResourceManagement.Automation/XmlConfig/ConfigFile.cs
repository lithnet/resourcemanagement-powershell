using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [XmlRoot(ElementName = "Lithnet.ResourceManagement.ConfigSync")]
    public class ConfigFile
    {
        private Variables variables;
        private List<ResourceOperation> operations;

        //[XmlArray(ElementName = "Variables")]
        //[XmlArrayItem(ElementName = "Variable", Type = typeof(Variable))]
        public Variables Variables
        {
            get
            {
                if (this.variables == null)
                {
                    this.variables = new Variables();
                }

                return this.variables;
            }
            set
            {
                this.variables = value;
            }
        }

        [XmlArray(ElementName = "Operations")]
        [XmlArrayItem(ElementName = "ResourceOperation", Type = typeof(ResourceOperation))]
        public List<ResourceOperation> Operations
        {
            get
            {
                if (this.operations == null)
                {
                    this.operations = new List<ResourceOperation>();
                }

                return this.operations;
            }
            set
            {
                this.operations = value;
            }
        }
    }
}
