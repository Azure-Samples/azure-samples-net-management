// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.Resources;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManageEventHubEvents
{
    //Azure Event Hub sample for managing event hub models.
    //    - Create a Event Hub namespace and an Event Hub in it
    //    - Create Authorization Rule and get key
    //    - Send events to Event Hub and read them

    public class Program
    {
        public static async Task RunSample(ArmClient client)
        {
            string location = AzureLocation.EastUS;
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgEvHb", 15);
            string namespaceName = Utilities.RandomResourceName("ns", 15);
            string eventHubName = "FirstEventHub";


            try
            {
                ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

                // Creates a Event Hub namespace and an Event Hub in it.
                Utilities.Log("Creating event hub namespace and event hub");

                Utilities.Log("Creating a namespace");

                var ehNamespaceCollection = resourceGroup.GetEventHubsNamespaces();
                var ehNamespace = (await ehNamespaceCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, namespaceName, new EventHubsNamespaceData(AzureLocation.EastUS2))).Value;

                Utilities.PrintNameSpace(ehNamespace);
                Utilities.Log("Created a namespace");

                var eventHubCollection = ehNamespace.GetEventHubs();
                var eventHub = (await eventHubCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, eventHubName, new EventHubData()
                {
                    Status = EventHubEntityStatus.Active,
                    PartitionCount = 4,
                    RetentionDescription = new RetentionDescription()
                    {
                        RetentionTimeInHours = 48,
                    }
                })).Value;

                Utilities.Log($"Created event hub namespace {ehNamespace.Data.Name} and event hub {eventHubName}");
                Utilities.PrintEventHub(eventHub);

                // Create a Authorization Rule for Event Hub created.
                Utilities.Log("Creating a Authorization Rule");

                var listenRuleCollection = eventHub.GetEventHubAuthorizationRules();
                var listenRule = (await listenRuleCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, "listenrule1", new EventHubsAuthorizationRuleData()
                {
                    Rights =
                    {
                        EventHubsAccessRight.Listen,
                        EventHubsAccessRight.Send
                    }
                })).Value;

                Utilities.Log("Created a Authorization Rule");

                // Send events using dataplane eventhub sdk.
                Utilities.Log("Sending events");

                var keys = (await listenRule.GetKeysAsync()).Value;
                var producerClient = new EventHubProducerClient(keys.PrimaryConnectionString, eventHubName);

                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Hello, Event Hubs!")));
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("The middle event is this one")));
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Goodbye, Event Hubs!")));

                await producerClient.SendAsync(eventBatch);

                Utilities.Log("Sent events");

                Utilities.Log("Reading events");

                var consumerClient = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, keys.PrimaryConnectionString, eventHubName);
                using CancellationTokenSource cancellationSource = new CancellationTokenSource();
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(90));

                int eventsRead = 0;
                int maximumEvents = 3;

                await foreach (PartitionEvent partitionEvent in consumerClient.ReadEventsAsync(cancellationSource.Token))
                {
                    Utilities.Log($"Event Read: { Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray()) }");
                    eventsRead++;

                    if (eventsRead >= maximumEvents)
                    {
                        break;
                    }
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
