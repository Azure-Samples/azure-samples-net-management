---
page_type: sample
languages:
- csharp
products:
- azure
description: "This code samples will show you how to manage ip address using Azure SDK for .NET."
urlFragment: network-manage-ip-address
---
# Getting started - Managing IP Address using Azure .NET SDK

This code sample will show you how to manage IP Address using Azure SDK for .NET.

## Features

This project framework provides examples for the following services:

### Network

- You can find the details for the library [here](https://azure.github.io/azure-sdk/releases/latest/mgmt/dotnet.html).

## Getting Started

### Prerequisites

You will need the following values to authenticate to Azure

- **Subscription ID**
- **Client ID**
- **Client Secret**
- **Tenant ID**

These values can be obtained from the portal, here's the instructions:

#### Get Subscription ID

1. Login into your Azure account
2. Select `Subscriptions` under `Navigation` section in the portal
3. Select whichever subscription is needed
4. Click on `Overview`
5. Copy the `Subscription ID`

#### Get Client ID / Client Secret / Tenant ID

For information on how to get Client ID, Client Secret, and Tenant ID,
please refer to [this
document](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal)

### Setting Environment Variables

After you obtained the values, you need to set the following values as
your environment variables

- `AZURE_CLIENT_ID`
- `AZURE_CLIENT_SECRET`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

To set the following environment variables on your development system:

Windows: (Note: Administrator access is required)

1. Open the System Control Panel
2. Select `Advanced system settings`
3. Open the `Advanced` tab, then click `Environment Variables...`
   button.
4. Click on the property you would like to change, then click the `Edit…`
   button. If the property name is not listed, then click the `New…`
   button.

Linux-based OS :

```bash
export AZURE_CLIENT_ID="__CLIENT_ID__"
export AZURE_CLIENT_SECRET="__CLIENT_SECRET__"
export AZURE_TENANT_ID="__TENANT_ID__"
export AZURE_SUBSCRIPTION_ID="__SUBSCRIPTION_ID__"
```

### Installation

To complete this tutorial:

- Install .NET Core latest version for [Linux] or [Windows]

If you don't have an Azure subscription, create a [free account] before you begin.

### Quickstart

1. Clone the repository on your machine:

```bash
git clone https://github.com/Azure-Samples/azure-samples-net-management.git
```

2. Switch to the project folder:

```bash
cd samples/network/manage-ip-address
```

3. Replace all the ```<password>``` placeholder with a valid password in the Program.cs file.  
4. Run the application with the `dotnet run` command.

## This sample shows how to do following operations to manage IP Address

- Assign a public IP address for a virtual machine during its creation.
- Assign a public IP address for a virtual machine through an virtual machine update action.
- Get the associated public IP address for a virtual machine.
- Get the assigned public IP address for a virtual machine.
- Remove a public IP address from a virtual machine.

## More information

The [Azure Compute documentation] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

This project has adopted the [Microsoft Open Source Code of Conduct].
For more information see the [Code of Conduct FAQ] or contact [opencode@microsoft.com] with any additional questions or comments.

<!-- LINKS -->
[Linux]: https://dotnet.microsoft.com/download
[Windows]: https://dotnet.microsoft.com/download
[free account]: https://azure.microsoft.com/free/?WT.mc_id=A261C142F
[Azure Portal]: https://portal.azure.com
[Azure Compute documentation]: https://docs.microsoft.com/azure/?product=compute
[Microsoft Open Source Code of Conduct]: https://opensource.microsoft.com/codeofconduct/
[Code of Conduct FAQ]: https://opensource.microsoft.com/codeofconduct/faq/
[opencode@microsoft.com]: mailto:opencode@microsoft.com
