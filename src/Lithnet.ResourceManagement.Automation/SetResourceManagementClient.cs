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
        public SwitchParameter RefreshSchema { get; set; }

        [Parameter]
        public SwitchParameter UsernamePassthrough { get; set; }

        protected override void EndProcessing()
        {
            NetworkCredential creds = null;

            if (this.Credentials != null)
            {
                creds = this.Credentials.GetNetworkCredential();
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !this.UsernamePassthrough)
                {
                    var i = creds.UserName.IndexOf("@");

                    if (i > 0)
                    {
                        var parts = creds.UserName.Split('@');
                        creds.UserName = $"{parts[0]}@{parts[1].ToUpperInvariant()}";
                    }
                }
            }

            Uri baseUri;

            try
            {
                if (!Uri.TryCreate(this.BaseAddress, UriKind.Absolute, out baseUri))
                {
                    baseUri = new Uri($"http://{this.BaseAddress}:5725");
                }
            }
            catch (Exception ex)
            {
                this.WriteError(new ErrorRecord(ex, "InvalidUri", ErrorCategory.InvalidArgument, this.BaseAddress));
                return;
            }

            RmcWrapper.Client = new Client.ResourceManagementClient(baseUri.AbsoluteUri, creds, this.ServicePrincipalName);

            if (this.RefreshSchema.IsPresent)
            {
                RmcWrapper.Client.RefreshSchema();
            }

            base.EndProcessing();
        }

        private string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException(nameof(securePassword));

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
