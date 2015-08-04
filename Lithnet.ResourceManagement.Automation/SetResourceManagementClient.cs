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
        [Parameter(Mandatory = true)]
        public string BaseAddress { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public PSCredential Credentials { get; set; }

        [Parameter]
        public string ServicePrincipalName { get; set; }

        [Parameter]
        public bool ForceKerberos { get; set; }

        protected override void EndProcessing()
        {
            NetworkCredential creds = null;

            if (this.Credentials != null)
            {
                creds = new NetworkCredential(this.Credentials.UserName, this.ConvertToUnsecureString(this.Credentials.Password));
            }
            
            Uri baseAddress = null;

            try
            {
                baseAddress = new Uri(this.BaseAddress);
            }
            catch(Exception ex)
            {
                this.WriteError(new ErrorRecord(ex, "InvalidUri", ErrorCategory.InvalidArgument, this.BaseAddress));
            }

            if (!string.IsNullOrWhiteSpace(this.ServicePrincipalName))
            {

            }

            RmcWrapper.Client = new Client.ResourceManagementClient(new Uri(this.BaseAddress), creds, this.ServicePrincipalName, !this.ForceKerberos);

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
