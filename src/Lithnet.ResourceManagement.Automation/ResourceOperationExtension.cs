using Lithnet.ResourceManagement.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{
    public static class ResourceOperationExtension
    {
        public static bool ProcessObjectExclusion(this ResourceObject ResourceObject, ConfigSyncConfiguration configuration )
        {
            bool objectExcluded = false;

            // Processing ObjectSpecificExlusions
            if (configuration.ObjectSpecificExlusions != null)
            {
                foreach (var objexclusion in configuration.ObjectSpecificExlusions)
                {
                    foreach (var k in objexclusion.AnchorKeyValueList)
                    {
                        if (k.Value == ResourceObject.Attributes[k.Key].Value)
                        {
                            if (objexclusion.AttributExclusions.Contains("*"))
                            {
                                //EXCLUDE OBJECT
                                objectExcluded = true;                                
                                break;
                            }
                            else
                            {
                                foreach (string ea in objexclusion.AttributExclusions)
                                {
                                    if (!configuration.AnchorAttributes.Contains(ea))
                                        ResourceObject.Attributes[k.Key].Value = null;
                                }
                            }
                        }
                    }
                    if (objectExcluded)
                        break;
                }
            }

            return objectExcluded;
        }
    }
}
