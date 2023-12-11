// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Communication;
using Azure.ResourceManager.Communication.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
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
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }

        public static async Task RunSample(ArmClient client)
        {
            String resourceGroupName = Utilities.RandomResourceName("rg-manage-comm-", 24);
            String resourceName = Utilities.RandomResourceName("manage-comm-", 24);
            String region = AzureLocation.WestUS;

            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroupName, new ResourceGroupData(region))).Value;
            //collection
            var collection = resourceGroup.GetCommunicationServiceResources();
            await CreateCommunicationServiceAsync(collection, resourceName);
            await GetCommunicationServiceAsync(collection, resourceName);
            await UpdateCommunicationServiceAsync(collection, resourceName);

            ListCommunicationServiceByCollection(collection);

            await ListKeysAsync(collection, resourceName);
            await RegenerateKeyAsync(collection, resourceName);
            await RegenerateKeyAsync(collection, resourceName);

            await LinkNotificationHubAsync(collection, resourceName);

            await DeleteCommunicationServiceAsync(collection, resourceName);
        }

        private static async Task CreateCommunicationServiceAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Create...");
                // Data
                var data = new CommunicationServiceResourceData(new AzureLocation("global"))
                {
                    DataLocation = new AzureLocation("UnitedStates")
                };

                // Create a resource in the specificed resource group and waits for a response
                Utilities.Log("Waiting for acsClient.CommunicationServiceCollection.CreateOrUpdateAsync");
                var resource = (await collection.CreateOrUpdateAsync(WaitUntil.Completed, resourceName, data)).Value;

                Utilities.Log("CommunicationServiceResource");
                Utilities.Print(resource);
            }
            catch (Exception e)
            {
                Utilities.Log("CreateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task GetCommunicationServiceAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Fetch...");

                // Fetch a previously created CommunicationServiceResource
                Utilities.Log("Waiting for CommunicationServiceCollection.Get()");
                var resource = (await collection.GetAsync(resourceName)).Value;
                Utilities.Print(resource);
            }
            catch (Exception e)
            {
                Utilities.Log("GetCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task UpdateCommunicationServiceAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Update...");

                // Get Resource
                var resource = (await collection.GetAsync(resourceName)).Value;
                //Update
                var updateResource = (await resource.UpdateAsync(new CommunicationServiceResourcePatch()
                {
                    Tags =
                    {
                        ["ExampleTagName1"] = "ExampleTagValue1",
                        ["ExampleTagName2"] = "ExampleTagValue2",
                    }
                })).Value;
                Utilities.Print(updateResource);
            }
            catch (Exception e)
            {
                Utilities.Log("UpdateCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static async Task DeleteCommunicationServiceAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Delete...");

                var resource = (await collection.GetAsync(resourceName)).Value;
                await resource.DeleteAsync(WaitUntil.Completed);
            }
            catch (Exception e)
            {
                Utilities.Log("DeleteCommunicationServiceAsync encountered: " + e.Message);
            }
        }

        private static void ListCommunicationServiceByCollection(CommunicationServiceResourceCollection collection)
        {
            try
            {
                Utilities.Log("\nCommunicationService List by Collection...");

                // Fetch all Azure Communication Services resources in the subscription
                var resources = collection.GetAll();
                Utilities.Log("Found number of resources: " + resources.ToArray().Length);

                foreach (var resource in resources)
                {
                    Utilities.Print(resource);
                }
            }
            catch (Exception e)
            {
                Utilities.Log("ListCommunicationServiceByCollection encountered: " + e.Message);
            }
        }

        private static async Task ListKeysAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService List Keys...");

                var resource = (await collection.GetAsync(resourceName)).Value;
                var keys =await resource.GetKeysAsync();
                Utilities.Log("\tPrimaryKey: " + keys.Value.PrimaryKey);
                Utilities.Log("\tSecondaryKey: " + keys.Value.SecondaryKey);
                Utilities.Log("\tPrimaryConnectionString: " + keys.Value.PrimaryConnectionString);
                Utilities.Log("\tSecondaryConnectionString: " + keys.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Utilities.Log("ListKeysAsync encountered: " + e.Message);
            }
        }

        private static async Task RegenerateKeyAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Regenerate Keys...");

                var resource = (await collection.GetAsync(resourceName)).Value;
                var keys = await resource.GetKeysAsync();
                string primaryKey = keys.Value.PrimaryKey;
                string secondaryKey = keys.Value.SecondaryKey;
                string primaryConnectionString = keys.Value.PrimaryConnectionString;
                string secondaryConnectionString = keys.Value.SecondaryConnectionString;
                var content = new RegenerateCommunicationServiceKeyContent()
                {
                    KeyType = CommunicationServiceKeyType.Primary
                };
                var newkeys = await resource.RegenerateKeyAsync(content);

                Utilities.Log("\tPrimaryKey: " + newkeys.Value.PrimaryKey);
                Utilities.Log("\tSecondaryKey: " + newkeys.Value.SecondaryKey);
                Utilities.Log("\tPrimaryConnectionString: " + newkeys.Value.PrimaryConnectionString);
                Utilities.Log("\tSecondaryConnectionString: " + newkeys.Value.SecondaryConnectionString);
            }
            catch (Exception e)
            {
                Utilities.Log("RegenerateKeyAsync encountered: " + e.Message);
            }
        }

        private static async Task LinkNotificationHubAsync(CommunicationServiceResourceCollection collection, string resourceName)
        {
            try
            {
                Utilities.Log("\nCommunicationService Link Notification Hub...");
                var resource = (await collection.GetAsync(resourceName)).Value;
                var notificationHubId = Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_ID");
                var notificationHubConnectionString = Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_CONNECTION_STRING");

                var parameter = new LinkNotificationHubContent(new ResourceIdentifier(notificationHubId), notificationHubConnectionString);
                var hub = await resource.LinkNotificationHubAsync(parameter);
            }
            catch (Exception e)
            {
                Utilities.Log("LinkNotificationHubAsync encountered: " + e.Message);
            }
        }
    }
}
