using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [Flags]
    public enum ResourceOperationType
    {
        [XmlEnum("None")]
        None = 0,

        [XmlEnum("Add")]
        Add = 1,

        [XmlEnum("Update")]
        Update = 2,

        [XmlEnum("Add Update")]
        AddUpdate = 3,

        [XmlEnum("Delete")]
        Delete = 4
    }
}
