// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Authenticate
                var credential = new DefaultAzureCredential();

                await RunSample(credential);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }

        public static async Task RunSample(DefaultAzureCredential credential)
        {
            String resourceGroupName = Utilities.RandomResourceName("rg-manage-comm-", 24);
            String resourceName = Utilities.RandomResourceName("manage-comm-", 24);
            String region = "westus";

            CommunicationManagementClient acsClient = CreateCommunicationManagementClient(credential);
            await ResourceGroupHelper.CreateOrUpdateResourceGroup(resourceGroupName, region);

            await CreateCommunicationServiceAsync(acsClient, resourceGroupName, resourceName);
            await GetCommunicationServiceAsync(acsClient, resourceGroupName, resourceName);
            await UpdateCommunicationServiceAsync(acsClient, resourceGroupName, resourceName);

            ListCommunicationServiceBySubscription(acsClient);
            ListCommunicationServiceByResourceGroup(acsClient, resourceGroupName);

            await ListKeysAsync(acsClient, resourceGroupName, resourceName);
            await RegenerateKeyAsync(acsClient, resourceGroupName, resourceName, KeyType.Primary);
            await RegenerateKeyAsync(acsClient, resourceGroupName, resourceName, KeyType.Secondary);

            await LinkNotificationHubAsync(acsClient, resourceGroupName, resourceName);

            await DeleteCommunicationServiceAsync(acsClient, resourceGroupName, resourceName);
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
                Utilities.Log("\nCommunicationService Create...");

                // Set up a CommunicationServiceResource with attributes of the resource we intend to create
                var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };

                // Create a resource in the specificed resource group and waits for a response
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                var operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Utilities.Log("Gained the CommunicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Utilities.Log("\tresponse: " + response.ToString());
                Utilities.Print(response.Value);
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
                Utilities.Log("\nCommunicationService Fetch...");

                // Fetch a previously created CommunicationServiceResource
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                Response<CommunicationServiceResource> response = await acsClient.CommunicationService.GetAsync(resourceGroupName, resourceName);
                Utilities.Log("\tresponse: " + response.ToString());
                Utilities.Print(response.Value);
            }
            catch (Exception e)
            {
                Utilities.Log("GetCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task UpdateCommunicationServiceAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Update...");

                // Create a CommunicationServiceResource with the updated resource attributes
                var resource = new CommunicationServiceResource { Location = "global", DataLocation = "UnitedStates" };

                var tags = new Dictionary<string, string>();
                tags.Add("ExampleTagName1", "ExampleTagValue1");
                tags.Add("ExampleTagName2", "ExampleTagValue2");

                // Update an existing resource in Azure with the attributes in `resource` and wait for a response
                Utilities.Log("Waiting for acsClient.CommunicationService.StartCreateOrUpdateAsync");
                CommunicationServiceCreateOrUpdateOperation operation = await acsClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);

                Utilities.Log("Gained the communicationServiceCreateOrUpdateOperation. Waiting for it to complete...");
                Response<CommunicationServiceResource> response = await operation.WaitForCompletionAsync();
                Utilities.Log("\tresponse: " + response.ToString());
                Utilities.Print(response.Value);
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
                Utilities.Log("\nCommunicationService Delete...");

                CommunicationServiceDeleteOperation operation = await acsClient.CommunicationService.StartDeleteAsync(resourceGroupName, resourceName);

                Utilities.Log("Gained the CommunicationServiceDeleteOperation. Waiting for it to complete...");
                Response<Response> response = await operation.WaitForCompletionAsync();
                Utilities.Log("\tresponse: " + response.ToString());
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
                Utilities.Log("\nCommunicationService List by Subscription...");

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
                Utilities.Log("\nCommunicationService List by Resource Group...");

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
                Utilities.Log("\nCommunicationService List Keys...");

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

        private static async Task RegenerateKeyAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName, KeyType type)
        {
            try
            {
                Utilities.Log("\nCommunicationService Regenerate Keys...");

                var keyTypeParameters = new RegenerateKeyParameters();
                keyTypeParameters.KeyType = type;

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

        private static async Task LinkNotificationHubAsync(CommunicationManagementClient acsClient, string resourceGroupName, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Link Notification Hub...");

                var notificationHubId = Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_ID");
                var notificationHubConnectionString = Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_CONNECTION_STRING");

                Response<LinkedNotificationHub> response = await acsClient.CommunicationService.LinkNotificationHubAsync(resourceGroupName, resourceName, new LinkNotificationHubParameters(notificationHubId, notificationHubConnectionString));
                Utilities.Log("\tresponse: " + response.ToString());
            }
            catch (Exception e)
            {
                Utilities.Log("LinkNotificationHubAsync encountered: " + e.Message);
            }
        }
    }
}
