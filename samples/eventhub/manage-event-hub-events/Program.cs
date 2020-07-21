// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Samples.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManageEventHubEvents
{
    /**
     * Azure Event Hub sample for managing event hub models.
     *   - Create a Event Hub namespace and an Event Hub in it
     *   - Create Authorization Rule and get key
     *   - Send events to Event Hub and read them
     */
    public class Program
    {
        public static async Task RunSample(TokenCredential credential)
        {
            string region = "eastus";
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgEvHb", 15);
            string namespaceName = Utilities.RandomResourceName("ns", 15);
            string eventHubName = "FirstEventHub";

            var eventHubsManagementClient = new EventHubsManagementClient(subscriptionId, credential);
            var namespaces = eventHubsManagementClient.Namespaces;
            var eventHubs = eventHubsManagementClient.EventHubs;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

                //=============================================================
                // Creates a Event Hub namespace and an Event Hub in it.
                //
                Utilities.Log("Creating event hub namespace and event hub");

                Utilities.Log("Creating a namespace");

                var rawResult = await namespaces.StartCreateOrUpdateAsync(
                    rgName,
                    namespaceName,
                    new EHNamespace
                    {
                        Location = "eastus2"
                    });
                var ehNamespace = (await rawResult.WaitForCompletionAsync()).Value;

                Utilities.Print(ehNamespace);
                Utilities.Log("Created a namespace");

                var eventHub = (await eventHubs.CreateOrUpdateAsync(
                    rgName,
                    ehNamespace.Name,
                    eventHubName,
                    new Eventhub
                    {
                        MessageRetentionInDays = 4,
                        PartitionCount = 4,
                        Status = EntityStatus.Active,
                    }
                    )).Value;

                Utilities.Log($"Created event hub namespace {ehNamespace.Name} and event hub {eventHubName}");
                Utilities.Print(ehNamespace);

                //=============================================================
                // Create a Authorization Rule for Event Hub created.
                //
                Utilities.Log("Creating a Authorization Rule");

                var listenRule = (await eventHubs.CreateOrUpdateAuthorizationRuleAsync(
                    rgName,
                    namespaceName,
                    eventHubName,
                    "listenrule1",
                    new AuthorizationRule
                    {
                        Rights = new List<AccessRights>() { AccessRights.Listen, AccessRights.Send }
                    }
                    )).Value;

                Utilities.Log("Created a Authorization Rule");

                //=============================================================
                // Send events using dataplane eventhub sdk.
                //
                Utilities.Log("Sending events");

                var keys = (await eventHubs.ListKeysAsync(rgName, namespaceName, eventHubName, listenRule.Name)).Value;
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
