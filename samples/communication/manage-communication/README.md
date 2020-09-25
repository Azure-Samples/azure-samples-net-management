---
page_type: sample
languages:
- csharp
products:
- azure
description: "This code samples will show you how to managing resources using Azure SDK for .NET."
urlFragment: communication-manage-communication
---
# Getting started - Managing resources using Azure .NET SDK

This code sample will show you how to managing Communication Service resources using Azure SDK for .NET.

## Features

This project framework provides examples for the following services:

### Communication
* You can find the details for the library [here](https://azure.github.io/azure-sdk/releases/latest/#dotnet).

## Getting Started

### Prerequisites

You will need the following values run the sample:

-   **Subscription ID**
-   **Client ID**
-   **Client Secret**
-   **Tenant ID**
-   **Notification Hub Resource ID**
-   **Notification Hub Connection String**

These values can be obtained from the portal.

### Get Subscription ID

1.  Login into your Azure account.
2.  Select Subscriptions in the left sidebar.
3.  Select whichever subscription is needed.
4.  Click on Overview.
5.  Copy the Subscription ID.

### Get Client ID / Client Secret / Tenant ID

For information on how to get Client ID, Client Secret, and Tenant ID,
please refer to [this
document](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal)

### Get Notification Hubs Resource ID and Connection String

1.  Login into your Azure account.
2.  Search for Notification Hubs.
3.  Create a Notificaiton Hub.
4.  On the `Properties` blade, you will find the Notification Hub Resource ID. 
5.  On the `Access Policies` blade, you will find the Notification Hub Connection String. The Connection String only needs the Listen permission for this sample.

### Setting Environment Variables

After you obtained the values, you need to set the following values as
your environment variables

-   `AZURE_CLIENT_ID`
-   `AZURE_CLIENT_SECRET`
-   `AZURE_TENANT_ID`
-   `AZURE_SUBSCRIPTION_ID`
-   `AZURE_NOTIFICATION_HUB_ID`
-   `AZURE_NOTIFICATION_HUB_CONNECTION_STRING`

To set the following environment variables on your development system:

Windows (Note: Administrator access is required)

1.  Open the Control Panel
2.  Click System Security, then System
3.  Click Advanced system settings on the left
4.  Inside the System Properties window, click the Environment
    Variables… button.
5.  Click on the property you would like to change, then click the Edit…
    button. If the property name is not listed, then click the New…
    button.

Linux-based OS :

    export AZURE_CLIENT_ID="__CLIENT_ID__"
    export AZURE_CLIENT_SECRET="__CLIENT_SECRET__"
    export AZURE_TENANT_ID="__TENANT_ID__"
    export AZURE_SUBSCRIPTION_ID="__SUBSCRIPTION_ID__"
    export AZURE_NOTIFICATION_HUBS_ID="__AZURE_NOTIFICATION_HUBS_ID__"
    export AZURE_NOTIFICATION_HUBS_CONNECTION_STRING="__AZURE_NOTIFICATION_HUBS_CONNECTION_STRING__"

### Installation

To complete this tutorial:

* Install .NET Core latest version for [Linux] or [Windows]

If you don't have an Azure subscription, create a [free account] before you begin.

### Quickstart

1. Clone the repository on your machine:

```bash
git clone https://github.com/Azure-Samples/azure-samples-net-management.git
```

2. Switch to the project folder:
```bash
cd samples/resources/manage-resource
```

3. Run the application with the `dotnet run` command.

```console
dotnet run
```

## This sample shows how to do following operations to manage resources
 - Create a resource.
 - Update a resource.
 - Get a resource
 - Delete a resource.
 - List resources by subscription.
 - List resources by resource group.
 - List keys.
 - Regenerate keys.
 - Link notification hub.

## More information

The [Azure Communication documentation] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

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
