// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Samples.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManageEventHub
{
    /**
     * Azure Event Hub sample for managing event hub -
     *   - Create an event hub namespace
     *   - Create an event hub in the namespace with data capture enabled along with a consumer group and rule
     *   - List consumer groups in the event hub
     *   - Create a second event hub in the namespace
     *   - Create a consumer group in the second event hub
     *   - List consumer groups in the second event hub
     *   - Create an event hub namespace along with event hub.
     */
    public class Program
    {
        public static async Task RunSample(TokenCredential credential)
        {
            string region = "eastus";
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgeh", 15);
            string namespaceName1 = Utilities.RandomResourceName("ns", 15);
            string namespaceName2 = Utilities.RandomResourceName("ns", 15);
            string storageAccountName = Utilities.RandomResourceName("stg", 14);
            string eventHubName1 = Utilities.RandomResourceName("eh", 14);
            string eventHubName2 = Utilities.RandomResourceName("eh", 14);

            var eventHubsManagementClient = new EventHubsManagementClient(subscriptionId, credential);
            var namespaces = eventHubsManagementClient.Namespaces;
            var eventHubs = eventHubsManagementClient.EventHubs;
            var consumerGroups = eventHubsManagementClient.ConsumerGroups;
            var storageManagementClient = new StorageManagementClient(subscriptionId, credential);
            var storageAccounts = storageManagementClient.StorageAccounts;
            var blobContainers = storageManagementClient.BlobContainers;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

                //============================================================
                // Create an event hub namespace
                //

                Utilities.Log("Creating a namespace");

                var rawResult = await namespaces.StartCreateOrUpdateAsync(
                    rgName,
                    namespaceName1,
                    new EHNamespace
                    {
                        Location = "eastus2"
                    });
                var namespace1 = (await rawResult.WaitForCompletionAsync()).Value;

                Utilities.Print(namespace1);
                Utilities.Log("Created a namespace");

                //============================================================
                // Create an event hub in the namespace with data capture enabled, with consumer group and auth rule
                //

                var storageAccountCreatable = (await (await storageAccounts.StartCreateAsync(
                    rgName,
                    storageAccountName,
                    new StorageAccountCreateParameters(
                        new Azure.ResourceManager.Storage.Models.Sku("Standard_LRS"),
                        new Kind("StorageV2"),
                        "eastus2")
                    )).WaitForCompletionAsync()).Value;
                var container = await blobContainers.CreateAsync(
                    rgName,
                    storageAccountName,
                    "datacpt",
                    new BlobContainer()
                    );

                Utilities.Log("Creating an event hub with data capture enabled with a consumer group and rule in it");

                var eventHub1 = (await eventHubs.CreateOrUpdateAsync(
                    rgName,
                    namespace1.Name,
                    eventHubName1,
                    new Eventhub
                    {
                        MessageRetentionInDays = 4,
                        PartitionCount = 4,
                        Status = EntityStatus.Active,
                        // Optional - configure data capture
                        CaptureDescription = new CaptureDescription
                        {
                            Enabled = true,
                            Encoding = EncodingCaptureDescription.Avro,
                            IntervalInSeconds = 120,
                            SizeLimitInBytes = 10485763,
                            Destination = new Destination
                            {
                                Name = "EventHubArchive.AzureBlockBlob",
                                BlobContainer = "datacpt",
                                ArchiveNameFormat = "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}",
                                StorageAccountResourceId = "/subscriptions/" + subscriptionId + "/resourcegroups/" + rgName + "/providers/Microsoft.Storage/storageAccounts/" + storageAccountName
                            },
                            SkipEmptyArchives = true
                        }
                    }
                    )).Value;
                // Optional - create one consumer group in event hub
                var consumerGroup1 = await consumerGroups.CreateOrUpdateAsync(
                    rgName,
                    namespaceName1,
                    eventHubName1,
                    "cg1",
                    new ConsumerGroup
                    {
                        UserMetadata = "sometadata"
                    }
                    );
                // Optional - create an authorization rule for event hub
                var listenRule1 = await eventHubs.CreateOrUpdateAuthorizationRuleAsync(
                    rgName,
                    namespaceName1,
                    eventHubName1,
                    "listenrule1",
                    new AuthorizationRule
                    {
                        Rights = new List<AccessRights>() { AccessRights.Listen, AccessRights.Send }
                    }
                    );

                Utilities.Log("Created an event hub with data capture enabled with a consumer group and rule in it");

                var result = (await eventHubs.GetAsync(rgName, namespaceName1, eventHubName1)).Value;

                Utilities.Print(result);

                //============================================================
                // Retrieve consumer groups in the event hub
                //
                Utilities.Log("Retrieving consumer groups");

                var listResult = await consumerGroups.ListByEventHubAsync(rgName, namespaceName1, eventHubName1).ToEnumerableAsync();

                Utilities.Log("Retrieved consumer groups");

                foreach (var group in listResult)
                {
                    Utilities.Print(group);
                }

                //============================================================
                // Create another event hub in the namespace
                //

                Utilities.Log("Creating another event hub in the namespace");

                var eventHub2 = (await eventHubs.CreateOrUpdateAsync(
                    rgName,
                    namespaceName1,
                    eventHubName2,
                    new Eventhub()
                    )).Value;

                Utilities.Log("Created second event hub");
                Utilities.Print(eventHub2);

                //============================================================
                // Create a consumer group in the event hub
                //

                Utilities.Log("Creating a consumer group in the second event hub");

                var consumerGroup2 = (await consumerGroups.CreateOrUpdateAsync(
                    rgName,
                    namespaceName1,
                    eventHubName2,
                    "cg2",
                    // Optional
                    new ConsumerGroup
                    {
                        UserMetadata = "sometadata"
                    }
                    )).Value;

                Utilities.Log("Created a consumer group in the second event hub");
                Utilities.Print(consumerGroup2);

                //============================================================
                // Retrieve consumer groups in the event hub
                //
                Utilities.Log("Retrieving consumer groups in the second event hub");

                listResult = await consumerGroups.ListByEventHubAsync(rgName, namespaceName1, eventHubName2).ToEnumerableAsync();

                Utilities.Log("Retrieved consumer groups in the seoond event hub");

                foreach (var group in listResult)
                {
                    Utilities.Print(group);
                }

                //============================================================
                // Create an event hub namespace with event hub
                //

                Utilities.Log("Creating an event hub namespace along with event hub");

                var rawResult2 = await namespaces.StartCreateOrUpdateAsync(
                    rgName,
                    namespaceName2,
                    new EHNamespace
                    {
                        Location = "eastus2"
                    });
                var namespace2 = (await rawResult2.WaitForCompletionAsync()).Value;

                var newEventHub2 = (await eventHubs.CreateOrUpdateAsync(
                    rgName,
                    namespaceName2,
                    eventHubName2,
                    new Eventhub()
                    )).Value;


                Utilities.Log("Created an event hub namespace along with event hub");
                Utilities.Print(namespace2);

                foreach (var eh in await eventHubs.ListByNamespaceAsync(rgName, namespaceName2).ToEnumerableAsync())
                {
                    Utilities.Print(eh);
                }

            }
            finally
            {
                try
                {
                    await ResourceGroupHelper.DeleteResourceGroup(rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var credentials = new DefaultAzureCredential();

                await RunSample(credentials);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}
