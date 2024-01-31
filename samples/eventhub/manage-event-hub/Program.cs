// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManageEventHub
{
    //  Azure Event Hub sample for managing event hub -
    //    - Create an event hub namespace
    //    - Create an event hub in the namespace with data capture enabled along with a consumer group and rule
    //    - List consumer groups in the event hub
    //    - Create a second event hub in the namespace
    //    - Create a consumer group in the second event hub
    //    - List consumer groups in the second event hub
    //    - Create an event hub namespace along with event hub.

    public class Program
    {
        public static async Task RunSample(ArmClient client)
        {
            string location = AzureLocation.EastUS;
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgeh", 15);
            string namespaceName1 = Utilities.RandomResourceName("ns", 15);
            string namespaceName2 = Utilities.RandomResourceName("ns", 15);
            string storageAccountName = Utilities.RandomResourceName("stg", 14);
            string eventHubName1 = Utilities.RandomResourceName("eh", 14);
            string eventHubName2 = Utilities.RandomResourceName("eh", 14);

            try
            {
                ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

                // Create an event hub namespace

                Utilities.Log("Creating a namespace");
                EventHubsNamespaceCollection namespaceCollection = resourceGroup.GetEventHubsNamespaces();
                EventHubsNamespaceResource eventHubNamespace = (await namespaceCollection.CreateOrUpdateAsync(WaitUntil.Completed, namespaceName1, new EventHubsNamespaceData(location))).Value;

                Utilities.PrintNameSpace(eventHubNamespace);
                Utilities.Log("Created a namespace");

                // Create an event hub in the namespace with data capture enabled, with consumer group and auth rule

                GenericResourceData input = new GenericResourceData(AzureLocation.EastUS2)
                {
                    Sku = new ResourcesSku
                    {
                        Name = "Standard_LRS"
                    },
                    Kind = "StorageV2",
                };
                ResourceIdentifier storageAccountId = resourceGroup.Id.AppendProviderResource("Microsoft.Storage", "storageAccounts", storageAccountName);
                GenericResource account = (await client.GetGenericResources().CreateOrUpdateAsync(WaitUntil.Completed, storageAccountId, input)).Value;

                //create eventhub with Cleanup policy Compaction.
                EventHubData parameter = new EventHubData()
                {
                    PartitionCount = 4,
                    Status = EventHubEntityStatus.Active,
                    CaptureDescription = new CaptureDescription()
                    {
                        Enabled = true,
                        Encoding = EncodingCaptureDescription.Avro,
                        IntervalInSeconds = 120,
                        SizeLimitInBytes = 10485763,
                        Destination = new EventHubDestination()
                        {
                            Name = "EventHubArchive.AzureBlockBlob",
                            BlobContainer = "container",
                            ArchiveNameFormat = "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}",
                            StorageAccountResourceId = new ResourceIdentifier(account.Id.ToString())
                        }
                    },
                };
                var eventCollection = eventHubNamespace.GetEventHubs();
                EventHubResource eventHub = (await eventCollection.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName1, parameter)).Value;
                // Optional - create one consumer group in event hub
                var consumerGroupCollection = eventHub.GetEventHubsConsumerGroups();
                EventHubsConsumerGroupResource consumerGroup = (await consumerGroupCollection.CreateOrUpdateAsync(WaitUntil.Completed, "cg1", new EventHubsConsumerGroupData())).Value;
                // Optional - create an authorization rule for event hub
                EventHubsNamespaceAuthorizationRuleCollection collection = eventHubNamespace.GetEventHubsNamespaceAuthorizationRules();
                var listenRuleData1 = new EventHubsAuthorizationRuleData()
                {
                    Rights =
                    {
                        EventHubsAccessRight.Listen,EventHubsAccessRight.Send
                    }
                };
                var listenRule1 = (await collection.CreateOrUpdateAsync(WaitUntil.Completed, "listenrule1", listenRuleData1)).Value;

                // Retrieve consumer groups in the event hub
                Utilities.Log("Retrieving consumer groups");

                var consumerGroups = consumerGroupCollection.GetAll();

                Utilities.Log("Retrieved consumer groups");

                foreach (var group in consumerGroups)
                {
                    Utilities.PrintConsumerGroup(group);
                }

                // Create another event hub in the namespace

                Utilities.Log("Creating another event hub in the namespace");

                var eventHub2 = (await eventCollection.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName2, new EventHubData())).Value;

                Utilities.Log("Created second event hub");
                Utilities.PrintEventHub(eventHub2);

                // Create a consumer group in the event hub

                Utilities.Log("Creating a consumer group in the second event hub");

                var consumerGroupCollection2 = eventHub2.GetEventHubsConsumerGroups();
                var consumerGroup2 = (await consumerGroupCollection2.CreateOrUpdateAsync(WaitUntil.Completed, "cg2", new EventHubsConsumerGroupData()
                {
                    UserMetadata = "sometadata"
                })).Value;

                Utilities.PrintConsumerGroup(consumerGroup2);

                // Retrieve consumer groups in the event hub
                Utilities.Log("Retrieving consumer groups in the second event hub");

                var listResult2 = consumerGroupCollection2.GetAll();

                Utilities.Log("Retrieved consumer groups in the seoond event hub");

                foreach (var group in listResult2)
                {
                    Utilities.PrintConsumerGroup(group);
                }

                // Create an event hub namespace with event hub

                Utilities.Log("Creating an event hub namespace along with event hub");

                var namespace2 = (await namespaceCollection.CreateOrUpdateAsync(WaitUntil.Completed, namespaceName2, new EventHubsNamespaceData(location))).Value;

                var eventHubCollection = namespace2.GetEventHubs();
                var newEventHub2 = (await eventHubCollection.CreateOrUpdateAsync(WaitUntil.Completed, namespaceName2, new EventHubData())).Value;

                Utilities.Log("Created an event hub namespace along with event hub");
                Utilities.PrintNameSpace(namespace2);

                await foreach (var eh in eventHubCollection.GetAllAsync())
                {
                    Utilities.PrintEventHub(eh);
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
    }
}
