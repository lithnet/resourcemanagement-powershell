using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lithnet.ResourceManagement.Automation
{
    [Flags]
    public enum ResourceOperationType
    {
        None = 0,
        Add = 1,
        Update = 2,
        Delete = 4
    }
}