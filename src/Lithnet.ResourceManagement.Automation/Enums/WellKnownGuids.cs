using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.Enums
{
    public static class WellKnownGuids
    {
        public static readonly string INSTALLERACCOUNT = "7fb2b853-24f0-4498-9534-4e10589723c4";
        public static readonly string BUILTINSYNCHRONIZATIONACCOUNT = "fb89aefa-5ea1-47f1-8890-abe7797d6497";
        public static readonly string FIMSERVICEACCOUNT = "e05d1f1b-3d5e-4014-baa6-94dee7d68c89";
        public static readonly string ANONYMOUS = "b0b36673-d43b-4cfa-a7a2-aff14fd90522";


        public static bool Contains(string Guid)
        {
            return
                INSTALLERACCOUNT == Guid ||
                BUILTINSYNCHRONIZATIONACCOUNT == Guid ||
                FIMSERVICEACCOUNT == Guid ||
                ANONYMOUS == Guid;
        }

        public static List<string> All
        {
            get
            {
                return new List<string>()
                                {
                                    INSTALLERACCOUNT,
                                    BUILTINSYNCHRONIZATIONACCOUNT,
                                    FIMSERVICEACCOUNT,
                                    ANONYMOUS
                                };
            }
            private set { }
        }
        
    }
}
