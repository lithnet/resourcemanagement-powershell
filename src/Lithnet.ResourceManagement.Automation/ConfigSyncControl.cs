using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lithnet.ResourceManagement.Automation
{
    internal static class ConfigSyncControl
    {
        public static string CurrentPath { get; set; }

        public static ConfigFile CurrentConfig { get; set; }

        public static bool Preview { get; set; }
    }
}
