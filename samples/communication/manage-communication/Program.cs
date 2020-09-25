// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Communication;
using Azure.ResourceManager.Communication.Models;
using Samples.Utilities;

namespace ManageCommunication
{
    class Program
    {
        static int Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = new RootCommand { };

            rootCommand.Description = "Sample app for C# SDK for CommunicationManagementClient";

            rootCommand.Add(GenerateCommandCreateCommunicationService());
            rootCommand.Add(GenerateCommandGetCommunicationService());
            rootCommand.Add(GenerateCommandUpdateCommunicationService());
            rootCommand.Add(GenerateCommandDeleteCommunicationService());
            rootCommand.Add(GenerateCommandListCommunicationServiceBySubscription());
            rootCommand.Add(GenerateCommandListCommunicationServiceByResourceGroup());
            rootCommand.Add(GenerateCommandListKeys());
            rootCommand.Add(GenerateCommandRegenerateKey());
            rootCommand.Add(GenerateCommandLinkNotificationHub());

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        private static Command GenerateCommandCreateCommunicationService()
        {
            var command = new Command("create");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Utilities.Log(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Utilities.Log("----------------------");

                CreateCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
            });

            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));

            return command;
        }

        private static Command GenerateCommandGetCommunicationService()
        {
            var command = new Command("get");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Utilities.Log(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Utilities.Log("----------------------");

                GetCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
            });

            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));

            return command;
        }


        private static Command GenerateCommandUpdateCommunicationService()
        {
            var command = new Command("update");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Utilities.Log(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Utilities.Log("----------------------");

                // Provide example tag values to update the resource with
                var tags = new Dictionary<string,string>();
                tags.Add("ExampleTagName1", "ExampleTagValue1");
                tags.Add("ExampleTagName2", "ExampleTagValue2");

                UpdateCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, tags).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
            });
            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));

            return command;
        }

        private static Command GenerateCommandDeleteCommunicationService()
        {
            var command = new Command("delete");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Utilities.Log(String.Format("START: Delete action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Utilities.Log("----------------------");

                DeleteCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log(String.Format("FINISHED: Delete action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
            });
            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));

            return command;
        }

        private static Command GenerateCommandListCommunicationServiceBySubscription()
        {
            var command = new Command("list");
            command.Handler = CommandHandler.Create(() =>
            {
                Utilities.Log("START: List by subscription action");
                Utilities.Log("----------------------");

                ListCommunicationServiceBySubscription(CreateCommunicationManagementClient(CreateEnvironmentCredential()));

                Utilities.Log("----------------------");
                Utilities.Log("FINISHED: List by subscription action");
            });

            return command;
        }

        private static Command GenerateCommandListCommunicationServiceByResourceGroup()
        {
            var command = new Command("list-by-rg");
            command.Handler = CommandHandler.Create((string resourceGroupName) =>
            {
                Utilities.Log("START: List By Resource Group action");
                Utilities.Log("----------------------");

                ListCommunicationServiceByResourceGroup(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName);

                Utilities.Log("----------------------");
                Utilities.Log("FINISHED: List By Resource Group action");
            });
            command.AddArgument(new Argument<string>("resource-group-name"));

            return command;
        }

        private static Command GenerateCommandListKeys()
        {
            var command = new Command("list-keys");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Utilities.Log("START: List Keys action");
                Utilities.Log("----------------------");

                ListKeysAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log("FINISHED: List Keys action");
            });
            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));

            return command;
        }

        private static Command GenerateCommandRegenerateKey()
        {
            var command = new Command("regenerate-key");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName, string type) =>
            {
                Utilities.Log("START: Regenerate Key action");
                Utilities.Log("----------------------");

                RegenerateKeyAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, type).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log("FINISHED: Regenerate Key action");
            });
            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));
            command.AddArgument(new Argument<string>("type"));

            return command;
        }

        private static Command GenerateCommandLinkNotificationHub()
        {
            var command = new Command("link-notification-hub");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName, string notificationHubId, string notificationHubConnectionString) =>
            {
                Utilities.Log("START: LinkNotificationHub action");
                Utilities.Log("----------------------");

                LinkNotificationHubAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, notificationHubId, notificationHubConnectionString).GetAwaiter().GetResult();

                Utilities.Log("----------------------");
                Utilities.Log("FINISHED: LinkNotificationHub action");
            });
            command.AddArgument(new Argument<string>("resource-group-name"));
            command.AddArgument(new Argument<string>("resource-name"));
            command.AddArgument(new Argument<string>("notification-hub-id"));
            command.AddArgument(new Argument<string>("notification-hub-connection-string"));

            return command;
        }

        private static TokenCredential CreateEnvironmentCredential()
        {
            return new EnvironmentCredential();
        }

        private static CommunicationManagementClient CreateCommunicationManagementClient(TokenCredential tokenCredential)
        {
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            return new CommunicationManagementClient(subscriptionId, tokenCredential);
        }

        private static async Task CreateCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                // Set up a CommunicationServiceResource with attributes of the resource we intend to create
                var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };

                // Create a resource in the specificed resource group and waits for a response
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                var operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Utilities.Log("Gained the CommunicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Utilities.Log("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("CreateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task GetCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                // Fetch a previously created CommunicationServiceResource
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                Response<CommunicationServiceResource> response = await acsClient.CommunicationService.GetAsync(resourceGroupName, resourceName);
                Utilities.Log("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("GetCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task UpdateCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, Dictionary<string,string> tags)
        {
            // Create a CommunicationServiceResource with the updated resource attributes
            var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };
            foreach (KeyValuePair<string, string> tag in tags)
            {
                resource.Tags.Add(tag.Value, tag.Key);
            }

            try
            {
                // Update an existing resource in Azure with the attributes in `resource` and wait for a response
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                CommunicationServiceCreateOrUpdateOperation operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Utilities.Log("Gained the communicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Utilities.Log("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("UpdateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task DeleteCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                // Delete the resource
                Utilities.Log("Waiting for acsClient.CommunicationService.StartDeleteAsync");
                CommunicationServiceDeleteOperation operation = await acsClient.CommunicationService.StartDeleteAsync(resourceGroupName, resourceName);

                Utilities.Log("Gained the CommunicationServiceDeleteOperation. Waiting for it to complete...");
                Response<Response> response = await operation.WaitForCompletionAsync();
                Utilities.Log("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("DeleteCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static void ListCommunicationServiceBySubscription(CommunicationManagementClient acsClient)
        {
            try
            {
                // Fetch all Azure Communication Services resources in the subscription
                var resources = acsClient.CommunicationService.ListBySubscription();
                Utilities.Log("Found number of resources: " + resources.ToArray().Length);

                foreach (var resource in resources)
                {
                    Utilities.Print(resource);
                }
            }
            catch (Exception e)
            {
                Utilities.Log("ListCommunicationServiceBySubscription encountered: " + e.Message);
            }
        }

        private static void ListCommunicationServiceByResourceGroup(CommunicationManagementClient acsClient, string resourceGroupName)
        {
            try
            {
                var resources = acsClient.CommunicationService.ListByResourceGroup(resourceGroupName);
                Utilities.Log("Found number of resources: " + resources.ToArray().Length);
                foreach (var resource in resources)
                {
                    Utilities.Print(resource);
                }
            }
            catch (Exception e)
            {
                Utilities.Log("ListCommunicationServiceByResourceGroup encountered: " + e.Message);
            }
        }

        private static async Task ListKeysAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Response<CommunicationServiceKeys> response = await acsClient.CommunicationService.ListKeysAsync(resourceGroupName, resourceName);
                Utilities.Log("PrimaryKey: " + response.Value.PrimaryKey);
                Utilities.Log("SecondaryKey: " + response.Value.SecondaryKey);
                Utilities.Log("PrimaryConnectionString: " + response.Value.PrimaryConnectionString);
                Utilities.Log("SecondaryConnectionString: " + response.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Utilities.Log("ListKeysAsync encountered: " + e.Message);
            }
        }

        private static async Task RegenerateKeyAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, string type)
        {
            try
            {
                var keyTypeParameters = new RegenerateKeyParameters();
                keyTypeParameters.KeyType = ToKeyType(type);

                Response<CommunicationServiceKeys> response = await acsClient.CommunicationService.RegenerateKeyAsync(resourceGroupName, resourceName, keyTypeParameters);
                Utilities.Log("PrimaryKey: " + response.Value.PrimaryKey);
                Utilities.Log("SecondaryKey: " + response.Value.SecondaryKey);
                Utilities.Log("PrimaryConnectionString: " + response.Value.PrimaryConnectionString);
                Utilities.Log("SecondaryConnectionString: " + response.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Utilities.Log("RegenerateKeyAsync encountered: " + e.Message);
            }
        }

        private static KeyType ToKeyType(string value)
        {
            if (string.Equals(value, "Primary", StringComparison.InvariantCultureIgnoreCase)) return KeyType.Primary;
            if (string.Equals(value, "Secondary", StringComparison.InvariantCultureIgnoreCase)) return KeyType.Secondary;
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown KeyType value.");
        }

        private static async Task LinkNotificationHubAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, string notificationHubId, string notificationHubConnectionString)
        {
            try
            {
                Response<LinkedNotificationHub> response = await acsClient.CommunicationService.LinkNotificationHubAsync(resourceGroupName, resourceName, new LinkNotificationHubParameters(notificationHubId, notificationHubConnectionString));
                Utilities.Log("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("LinkNotificationHubAsync encountered: " + e.Message);
            }
        }
    }
}
