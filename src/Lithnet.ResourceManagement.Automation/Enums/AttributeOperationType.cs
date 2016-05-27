using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [Flags]
    public enum AttributeOperationType
    {
        [XmlEnum("none")]
        None = 0,

        [XmlEnum("add")]
        Add = 1,

        [XmlEnum("replace")]
        Replace = 2,

        [XmlEnum("delete")]
        Delete = 4
    }
}
