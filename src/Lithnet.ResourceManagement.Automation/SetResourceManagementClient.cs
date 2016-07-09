using System;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.Set, "ResourceManagementClient")]
    public class SetResourceManagementClient : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string BaseAddress { get; set; }

        [Parameter(ValueFromPipeline = true, Position = 1)]
        public PSCredential Credentials { get; set; }

        [Parameter(Position = 2)]
        public string ServicePrincipalName { get; set; }

        [Parameter(Position = 3)]
        public bool ForceKerberos { get; set; }

        protected override void EndProcessing()
        {
            NetworkCredential creds = null;

            if (this.Credentials != null)
            {
                creds = this.Credentials.GetNetworkCredential();
            }

            Uri baseUri;

            try
            {
                baseUri = new Uri(this.BaseAddress);
            }
            catch (Exception ex)
            {
                try
                {
                    baseUri = new Uri(string.Format("http://{0}:5725", this.BaseAddress));
                }
                catch 
                {
                    this.WriteError(new ErrorRecord(ex, "InvalidUri", ErrorCategory.InvalidArgument, this.BaseAddress));
                    return;
                }
            }

            RmcWrapper.Client = new Client.ResourceManagementClient(baseUri, creds, this.ServicePrincipalName, !this.ForceKerberos);
            RmcWrapper.Client.RefreshSchema();

            base.EndProcessing();
        }

        private string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
