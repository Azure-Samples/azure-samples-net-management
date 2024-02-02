// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.Resources;
using Samples.Utilities;
using System;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ManageEventHubGeoDisasterRecovery
{
    public class Program
    {
        //Azure Event Hub sample for managing geo disaster recovery pairing -
        //    - Create two event hub namespaces
        //    - Create a pairing between two namespaces
        //    - Create an event hub in the primary namespace and retrieve it from the secondary namespace
        //    - Retrieve the pairing connection string
        //    - Fail over so that secondary namespace become primary.
        public static async Task RunSample(ArmClient client)
        {
            string location = AzureLocation.EastUS;
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgeh", 15);
            string primaryNamespaceName = Utilities.RandomResourceName("ns", 15);
            string secondaryNamespaceName = Utilities.RandomResourceName("ns", 15);
            string geoDRName = Utilities.RandomResourceName("geodr", 14);
            string eventHubName = Utilities.RandomResourceName("eh", 14);
            bool isFailOverSucceeded = false;
            EventHubsDisasterRecoveryResource pairing = null;
            ResourceGroupResource resourceGroup = null;
            try
            {
                resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

                // Create resource group for the namespaces and recovery pairings

                Utilities.Log($"Creating primary event hub namespace {primaryNamespaceName}");

                var nameSpaceCollection = resourceGroup.GetEventHubsNamespaces();
                var primaryNamespace = (await nameSpaceCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, primaryNamespaceName, new EventHubsNamespaceData(AzureLocation.SouthCentralUS))).Value;

                Utilities.Log("Primary event hub namespace created");
                Utilities.PrintNameSpace(primaryNamespace);

                Utilities.Log($"Creating secondary event hub namespace {primaryNamespaceName}");

                var secondaryNamespace = (await nameSpaceCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, secondaryNamespaceName, new EventHubsNamespaceData(AzureLocation.NorthCentralUS))).Value;

                Utilities.Log("Secondary event hub namespace created");
                Utilities.PrintNameSpace(secondaryNamespace);

                // Create primary and secondary namespaces and recovery pairing

                Utilities.Log($"Creating geo-disaster recovery pairing {geoDRName}");

                var disasterRecoveryCollection = primaryNamespace.GetEventHubsDisasterRecoveries();
                pairing = (await disasterRecoveryCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, geoDRName, new EventHubsDisasterRecoveryData()
                {
                    PartnerNamespace = secondaryNamespace.Id
                })).Value;
                while (pairing.Data.ProvisioningState != EventHubsDisasterRecoveryProvisioningState.Succeeded)
                {
                    Utilities.Log("Wait for create disaster recovery");
                    Thread.Sleep(15 * 1000);
                    if (pairing.Data.ProvisioningState == EventHubsDisasterRecoveryProvisioningState.Failed)
                    {
                        throw new Exception("Provisioning state of the pairing is FAILED");
                    }
                }

                Utilities.Log($"Created geo-disaster recovery pairing {geoDRName}");
                Utilities.PrintDisasterRecovery(pairing);

                // Create an event hub and consumer group in primary namespace

                Utilities.Log("Creating an event hub and consumer group in primary namespace");

                var eventCollection = primaryNamespace.GetEventHubs();
                EventHubResource primaryEventHub = (await eventCollection.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, new EventHubData())).Value;
                var consumerGroupCollection = primaryEventHub.GetEventHubsConsumerGroups();
                EventHubsConsumerGroupResource consumerGroup = (await consumerGroupCollection.CreateOrUpdateAsync(WaitUntil.Completed, "consumerGrp1", new EventHubsConsumerGroupData())).Value;

                var eventHubInPrimaryNamespace = (await primaryEventHub.GetAsync()).Value;

                Utilities.Log("Created event hub and consumer group in primary namespace");
                Utilities.PrintNameSpace(primaryNamespace);

                Utilities.Log("Waiting for 60 seconds to allow metadata to sync across primary and secondary");
                Thread.Sleep(60 * 1000); // Wait for syncing to finish

                Utilities.Log("Retrieving the event hubs in secondary namespace");

                var eventHubInSecondaryNamespace = (await secondaryNamespace.GetAsync()).Value;

                Utilities.Log("Retrieved the event hubs in secondary namespace");
                Utilities.PrintNameSpace(eventHubInSecondaryNamespace);

                // Retrieving the connection string

                var ruleCollection = primaryNamespace.GetEventHubsNamespaceAuthorizationRules();
                var rules = ruleCollection.GetAll();
                foreach (var rule in rules)
                {
                    EventHubsAccessKeys keys = await rule.GetKeysAsync();
                    Utilities.PrintAccessKey(keys);
                }

                Utilities.Log("Initiating fail over");

                var failOverResult =await pairing.FailOverAsync();
                Thread.Sleep(10 * 1000);
                while ((await pairing.GetAsync()).Value.Data.ProvisioningState == EventHubsDisasterRecoveryProvisioningState.Accepted)
                {
                    Utilities.Log("Wait for fail over");
                    Thread.Sleep(10 * 1000);
                }
                if ((await pairing.GetAsync()).Value.Data.ProvisioningState == EventHubsDisasterRecoveryProvisioningState.Succeeded)
                {
                    isFailOverSucceeded = true;
                    Utilities.Log("Fail over initiated");
                }
                else
                {
                    Utilities.Log("Fail over is FAILED");
                }

            }
            finally
            {
                try
                {
                    try
                    {
                        // It is necessary to break pairing before deleting resource group
                        if (pairing != null && !isFailOverSucceeded)
                        {
                            await pairing.BreakPairingAsync();
                            Thread.Sleep(10 * 1000);
                            while ((await pairing.GetAsync()).Value.Data.ProvisioningState == EventHubsDisasterRecoveryProvisioningState.Accepted)
                            {
                                Thread.Sleep(10 * 1000);
                            }
                            if ((await pairing.GetAsync()).Value.Data.ProvisioningState == EventHubsDisasterRecoveryProvisioningState.Failed)
                            {
                                throw new Exception("Provisioning state of the break pairing is FAILED");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("Pairing breaking failed:" + ex.Message);
                    }
                    await resourceGroup.DeleteAsync(WaitUntil.Completed);
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
