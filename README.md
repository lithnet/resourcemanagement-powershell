![](https://github.com/lithnet/resourcemanagement-powershell/wiki/images/logo-ex-small.png)

# Lithnet Resource Management PowerShell Module

The Lithnet Resource Management PowerShell module (LithnetRMA) makes working with the FIM/MIM service faster and easier. It abstracts away the complexity of the FIM service and the FIMAutomation PowerShell module, and provides a set of cmdlets for creating, updating, deleting, and searching for resources.

Version 2 of the module runs on both Windows PowerShell 5.1 and PowerShell 7, and is built on version 2 of the [Lithnet Resource Management Client](https://github.com/lithnet/resourcemanagement-client) library.

## Installation

Install the module from the [PowerShell Gallery](https://www.powershellgallery.com/packages/LithnetRMA)

```powershell
Install-Module LithnetRMA
```

Or download the MSI installer from the [releases page](https://github.com/lithnet/resourcemanagement-powershell/releases/). The installer places the module in the machine-wide module path, where both Windows PowerShell 5.1 and PowerShell 7 can load it.

Windows PowerShell 5.1 requires .NET Framework 4.8. PowerShell 7.4 or later is required for PowerShell 7 support.

## Connecting to the MIM service

Use `Set-ResourceManagementClient` to configure the connection before calling other cmdlets.

```powershell
# Windows PowerShell 5.1, connecting directly to the MIM service
Set-ResourceManagementClient -BaseAddress http://mim-service:5725

# PowerShell 7 on Windows, using the built-in local proxy
Set-ResourceManagementClient -BaseAddress pipe://mim-service

# PowerShell 7 on any platform, using the remote proxy service installed on the MIM server
Set-ResourceManagementClient -BaseAddress rmc://mim-service
```

The client library supports several connection modes, selected automatically from the URI scheme or with the `-ConnectionMode` parameter.

| Mode | URI scheme | Works in | Approval operations |
|------|------------|----------|---------------------|
| `DirectWsHttp` | `http://` (port 5725) | Windows PowerShell 5.1 | Supported |
| `DirectNetTcp` | `net.tcp://` (port 5736) | Both editions, all platforms | Not supported |
| `LocalProxy` | `pipe://` | PowerShell 7 on Windows | Supported |
| `RemoteProxy` | `rmc://` (port 5735) | Both editions, all platforms | Supported |

`DirectNetTcp` requires the net.tcp endpoints to be enabled on the MIM service, and `RemoteProxy` requires the Lithnet Resource Management Proxy service to be installed on the MIM server. See the [connection guide](https://github.com/lithnet/resourcemanagement-client/wiki/Connection-guide) for details of each mode, the server-side setup steps, and the full set of connection options.

## Guides

*   [Installing the module](https://docs.lithnet.io/resource-management-powershell/installation/installing-the-module)
*   [Quick reference guide](https://docs.lithnet.io/resource-management-powershell/help-and-support/quick-reference-guide)
*   [Cmdlet reference](https://docs.lithnet.io/resource-management-powershell/usage/cmdlet-reference)
*   [Connection guide](https://github.com/lithnet/resourcemanagement-client/wiki/Connection-guide)

## How can I contribute to the project?

*   Found an issue and want us to fix it? [Log it](https://github.com/lithnet/resourcemanagement-powershell/issues)
*   Want to fix an issue yourself or add functionality? Clone the project and submit a pull request

## Enterprise support

Lithnet offer enterprise support plans for our open-source products. Deploy our tools with confidence that you have the backing of the dedicated Lithnet support team if you run into any issues, have questions, or need advice. Simply fill out the [request form](https://lithnet.io/products/mim?utm_source=github&utm_medium=readme&utm_campaign=mim-ups), let us know the number of users you are managing with your MIM implementation, and we'll put together a quote.

## Keep up to date

*   [Visit our blog](http://blog.lithnet.io)
*   [Follow us on twitter](https://twitter.com/lithnet_io)![](http://twitter.com/favicon.ico)
