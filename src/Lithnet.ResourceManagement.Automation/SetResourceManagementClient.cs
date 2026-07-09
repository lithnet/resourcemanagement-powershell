using System;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using Lithnet.ResourceManagement.Client;

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

        [Parameter]
        public SwitchParameter RefreshSchema { get; set; }

        [Parameter]
        public SwitchParameter UsernamePassthrough { get; set; }

        [Parameter]
        public ConnectionMode ConnectionMode { get; set; }

        [Parameter]
        public int? ConcurrentConnectionLimit { get; set; }

        [Parameter]
        public int? ConnectTimeoutSeconds { get; set; }

        [Parameter]
        public int? ReceiveTimeoutSeconds { get; set; }

        [Parameter]
        public int? SendTimeoutSeconds { get; set; }

        protected override void EndProcessing()
        {
            ResourceManagementClientOptions options = new ResourceManagementClientOptions();

            try
            {
                if (Uri.TryCreate(this.BaseAddress, UriKind.Absolute, out _))
                {
                    options.BaseUri = this.BaseAddress;
                }
                else
                {
                    options.BaseUri = $"http://{this.BaseAddress}:{ResourceManagementClientOptions.DefaultFimServicePort}";
                }
            }
            catch (Exception ex)
            {
                this.WriteError(new ErrorRecord(ex, "InvalidUri", ErrorCategory.InvalidArgument, this.BaseAddress));
                return;
            }

            if (this.Credentials != null)
            {
                NetworkCredential creds = this.Credentials.GetNetworkCredential();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !this.UsernamePassthrough)
                {
                    int i = creds.UserName.IndexOf("@");

                    if (i > 0)
                    {
                        string[] parts = creds.UserName.Split('@');
                        options.Username = $"{parts[0]}@{parts[1].ToUpperInvariant()}";
                    }
                    else
                    {
                        options.Username = string.IsNullOrWhiteSpace(creds.Domain) ? creds.UserName : $"{creds.Domain}\\{creds.UserName}";
                    }
                }
                else
                {
                    options.Username = string.IsNullOrWhiteSpace(creds.Domain) ? creds.UserName : $"{creds.Domain}\\{creds.UserName}";
                }

                options.Password = creds.Password;
            }

            options.Spn = this.ServicePrincipalName;
            options.ConnectionMode = this.ConnectionMode;

            if (this.ConcurrentConnectionLimit.HasValue)
            {
                options.ConcurrentConnectionLimit = this.ConcurrentConnectionLimit.Value;
            }

            if (this.ConnectTimeoutSeconds.HasValue)
            {
                options.ConnectTimeoutSeconds = this.ConnectTimeoutSeconds.Value;
            }

            if (this.ReceiveTimeoutSeconds.HasValue)
            {
                options.RecieveTimeoutSeconds = this.ReceiveTimeoutSeconds.Value;
            }

            if (this.SendTimeoutSeconds.HasValue)
            {
                options.SendTimeoutSeconds = this.SendTimeoutSeconds.Value;
            }

            RmcWrapper.Client = new ResourceManagementClient(options);

            if (this.RefreshSchema.IsPresent)
            {
                RmcWrapper.Client.RefreshSchema();
            }

            base.EndProcessing();
        }
    }
}
