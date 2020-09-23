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

namespace mgmt_sdk_quickstart
{
    class Program
    {
        static int Main(string[] args)
        {

            // Create a root command with some options
            var rootCommand = new RootCommand { };

            rootCommand.Description = "Demo app for C# SDK for CommunicationManagementClient";

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
                Console.WriteLine(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Console.WriteLine("----------------------");

                CreateCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
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
                Console.WriteLine(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Console.WriteLine("----------------------");

                GetCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
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
                Console.WriteLine(String.Format("START: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Console.WriteLine("----------------------");

                // Provide example tag values to update the resource with
                var tags = new Dictionary<string,string>();
                tags.Add("ExampleTagName1", "ExampleTagValue1");
                tags.Add("ExampleTagName2", "ExampleTagValue2");

                UpdateCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, tags).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine(String.Format("FINISHED: Create action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
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
                Console.WriteLine(String.Format("START: Delete action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
                Console.WriteLine("----------------------");

                DeleteCommunicationServiceAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine(String.Format("FINISHED: Delete action resourceGroupName: {0} resourceName: {1}", resourceGroupName, resourceName));
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
                Console.WriteLine("START: List by subscription action");
                Console.WriteLine("----------------------");

                ListCommunicationServiceBySubscription(CreateCommunicationManagementClient(CreateEnvironmentCredential()));

                Console.WriteLine("----------------------");
                Console.WriteLine("FINISHED: List by subscription action");
            });

            return command;
        }

        private static Command GenerateCommandListCommunicationServiceByResourceGroup()
        {
            var command = new Command("list-by-rg");
            command.Handler = CommandHandler.Create((string resourceGroupName) =>
            {
                Console.WriteLine("START: List By Resource Group action");
                Console.WriteLine("----------------------");

                ListCommunicationServiceByResourceGroup(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName);

                Console.WriteLine("----------------------");
                Console.WriteLine("FINISHED: List By Resource Group action");
            });
            command.AddArgument(new Argument<string>("resource-group-name"));

            return command;
        }

        private static Command GenerateCommandListKeys()
        {
            var command = new Command("list-keys");
            command.Handler = CommandHandler.Create((string resourceGroupName, string resourceName) =>
            {
                Console.WriteLine("START: List Keys action");
                Console.WriteLine("----------------------");

                ListKeysAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine("FINISHED: List Keys action");
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
                Console.WriteLine("START: Regenerate Key action");
                Console.WriteLine("----------------------");

                RegenerateKeyAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, type).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine("FINISHED: Regenerate Key action");
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
                Console.WriteLine("START: LinkNotificationHub action");
                Console.WriteLine("----------------------");

                LinkNotificationHubAsync(CreateCommunicationManagementClient(CreateEnvironmentCredential()), resourceGroupName, resourceName, notificationHubId, notificationHubConnectionString).GetAwaiter().GetResult();

                Console.WriteLine("----------------------");
                Console.WriteLine("FINISHED: LinkNotificationHub action");
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

        // Explicit version of EnvironmentCredential; helpful for debugging if EnvironmentCredential fails to auth
        private static TokenCredential CreateClientSecretCredential()
        {
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

            Console.WriteLine("clientId: " + clientId);
            Console.WriteLine("clientSecret: " + clientSecret);
            Console.WriteLine("tenantId: " + tenantId);

            return new ClientSecretCredential(tenantId, clientId, clientSecret);
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
                var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };

                Console.WriteLine("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                var operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Console.WriteLine("Gained the communicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Console.WriteLine("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task GetCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Console.WriteLine("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                Response<CommunicationServiceResource> response = await acsClient.CommunicationService.GetAsync(resourceGroupName, resourceName);
                Console.WriteLine("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("GetCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task UpdateCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, Dictionary<string,string> tags)
        {
            // Use existing resource name and new resource object
            var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };
            foreach (KeyValuePair<string, string> tag in tags)
            {
                resource.Tags.Add(tag.Value, tag.Key);
            }

            try
            {
                Console.WriteLine("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                CommunicationServiceCreateOrUpdateOperation operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Console.WriteLine("Gained the communicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Console.WriteLine("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("UpdateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task DeleteCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Console.WriteLine("Waiting for acsClient.CommunicationService.StartDeleteAsync");
                CommunicationServiceDeleteOperation operation = await acsClient.CommunicationService.StartDeleteAsync(resourceGroupName, resourceName);

                Console.WriteLine("Gained the CommunicationServiceDeleteOperation. Waiting for it to complete...");
                Response<Response> response = await operation.WaitForCompletionAsync();
                Console.WriteLine("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static void ListCommunicationServiceBySubscription(CommunicationManagementClient acsClient)
        {
            try
            {
                var resources = acsClient.CommunicationService.ListBySubscription();
                Console.WriteLine("Found number of resources: " + resources.ToArray().Length);
                foreach (var resource in resources)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Name: " + resource.Name);
                    Console.WriteLine("ProvisioningState: " + resource.ProvisioningState);
                    Console.WriteLine("ImmutableResourceId: " + resource.ImmutableResourceId);

                    string tags = "None";
                    if (resource.Tags != null)
                    {
                        tags = string.Join(", ", resource.Tags.Select(kvp => kvp.Key + ": " + kvp.Value.ToString()));
                    }
                    Console.WriteLine("Tags: " + tags);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ListCommunicationServiceBySubscription encountered: " + e.Message);
            }
        }

        private static void ListCommunicationServiceByResourceGroup(CommunicationManagementClient acsClient, string resourceGroupName)
        {
            try
            {
                var resources = acsClient.CommunicationService.ListByResourceGroup(resourceGroupName);
                Console.WriteLine("Found number of resources: " + resources.ToArray().Length);
                foreach (var resource in resources)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Name: " + resource.Name);
                    Console.WriteLine("ProvisioningState: " + resource.ProvisioningState);
                    Console.WriteLine("ImmutableResourceId: " + resource.ImmutableResourceId);

                    string tags = "None";
                    if (resource.Tags != null)
                    {
                        tags = string.Join(", ", resource.Tags.Select(kvp => kvp.Key + ": " + kvp.Value.ToString()));
                    }
                    Console.WriteLine("Tags: " + tags);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ListCommunicationServiceByResourceGroup encountered: " + e.Message);
            }
        }

        private static async Task ListKeysAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Response<CommunicationServiceKeys> response = await acsClient.CommunicationService.ListKeysAsync(resourceGroupName, resourceName);
                Console.WriteLine("PrimaryKey: " + response.Value.PrimaryKey);
                Console.WriteLine("SecondaryKey: " + response.Value.SecondaryKey);
                Console.WriteLine("PrimaryConnectionString: " + response.Value.PrimaryConnectionString);
                Console.WriteLine("SecondaryConnectionString: " + response.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Console.WriteLine("ListKeysAsync encountered: " + e.Message);
            }
        }

        private static async Task RegenerateKeyAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, string type)
        {
            try
            {
                var keyTypeParameters = new RegenerateKeyParameters();
                keyTypeParameters.KeyType = ToKeyType(type);

                Response<CommunicationServiceKeys> response = await acsClient.CommunicationService.RegenerateKeyAsync(resourceGroupName, resourceName, keyTypeParameters);
                Console.WriteLine("PrimaryKey: " + response.Value.PrimaryKey);
                Console.WriteLine("SecondaryKey: " + response.Value.SecondaryKey);
                Console.WriteLine("PrimaryConnectionString: " + response.Value.PrimaryConnectionString);
                Console.WriteLine("SecondaryConnectionString: " + response.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Console.WriteLine("RegenerateKeyAsync encountered: " + e.Message);
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
                Console.WriteLine("response: " + response.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("LinkNotificationHubAsync encountered: " + e.Message);
            }
        }
    }
}
