// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Samples.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManageEventHubGeoDisasterRecovery
{
    public class Program
    {
        /**
         * Azure Event Hub sample for managing geo disaster recovery pairing -
         *   - Create two event hub namespaces
         *   - Create a pairing between two namespaces
         *   - Create an event hub in the primary namespace and retrieve it from the secondary namespace
         *   - Retrieve the pairing connection string
         *   - Fail over so that secondary namespace become primary.
         */
        public static async Task RunSample(TokenCredential credential)
        {
            string region = "eastus";
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string rgName = Utilities.RandomResourceName("rgeh", 15);
            string primaryNamespaceName = Utilities.RandomResourceName("ns", 15);
            string secondaryNamespaceName = Utilities.RandomResourceName("ns", 15);
            string geoDRName = Utilities.RandomResourceName("geodr", 14);
            string eventHubName = Utilities.RandomResourceName("eh", 14);
            bool isFailOverSucceeded = false;
            ArmDisasterRecovery pairing = null;

            var eventHubsManagementClient = new EventHubsManagementClient(subscriptionId, credential);
            var namespaces = eventHubsManagementClient.Namespaces;
            var eventHubs = eventHubsManagementClient.EventHubs;
            var consumerGroups = eventHubsManagementClient.ConsumerGroups;
            var disasterRecoveryConfigs = eventHubsManagementClient.DisasterRecoveryConfigs;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

                //============================================================
                // Create resource group for the namespaces and recovery pairings
                //

                Utilities.Log($"Creating primary event hub namespace {primaryNamespaceName}");

                var primaryResult = await namespaces.StartCreateOrUpdateAsync(
                    rgName,
                    primaryNamespaceName,
                    new EHNamespace
                    {
                        Location = "southcentralus"
                    });
                var primaryNamespace = (await primaryResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Primary event hub namespace created");
                Utilities.Print(primaryNamespace);

                Utilities.Log($"Creating secondary event hub namespace {primaryNamespaceName}");

                var secondaryResult = await namespaces.StartCreateOrUpdateAsync(
                    rgName,
                    secondaryNamespaceName,
                    new EHNamespace
                    {
                        Location = "northcentralus"
                    });
                var secondaryNamespace = (await secondaryResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Secondary event hub namespace created");
                Utilities.Print(secondaryNamespace);

                //============================================================
                // Create primary and secondary namespaces and recovery pairing
                //

                Utilities.Log($"Creating geo-disaster recovery pairing {geoDRName}");

                pairing = (await disasterRecoveryConfigs.CreateOrUpdateAsync(
                    rgName,
                    primaryNamespaceName,
                    geoDRName,
                    new ArmDisasterRecovery
                    {
                        PartnerNamespace = secondaryNamespace.Id
                    }
                    )).Value;
                while ((await disasterRecoveryConfigs.GetAsync(rgName, primaryNamespaceName, geoDRName)).Value.ProvisioningState != ProvisioningStateDR.Succeeded)
                {
                    Utilities.Log("Wait for create disaster recovery");
                    Thread.Sleep(15 * 1000);
                    if (pairing.ProvisioningState == ProvisioningStateDR.Failed)
                    {
                        throw new Exception("Provisioning state of the pairing is FAILED");
                    }
                }

                Utilities.Log($"Created geo-disaster recovery pairing {geoDRName}");
                Utilities.Print(pairing);

                //============================================================
                // Create an event hub and consumer group in primary namespace
                //

                Utilities.Log("Creating an event hub and consumer group in primary namespace");

                var primaryEventHub = (await eventHubs.CreateOrUpdateAsync(
                    rgName,
                    primaryNamespaceName,
                    eventHubName,
                    new Eventhub()
                    )).Value;
                var primaryConsumerGroup = (await consumerGroups.CreateOrUpdateAsync(
                    rgName,
                    primaryNamespaceName,
                    eventHubName,
                    "consumerGrp1",
                    new ConsumerGroup()
                    )).Value;

                var eventHubInPrimaryNamespace = (await namespaces.GetAsync(rgName, primaryNamespaceName)).Value;

                Utilities.Log("Created event hub and consumer group in primary namespace");
                Utilities.Print(eventHubInPrimaryNamespace);

                Utilities.Log("Waiting for 60 seconds to allow metadata to sync across primary and secondary");
                Thread.Sleep(60 * 1000); // Wait for syncing to finish

                Utilities.Log("Retrieving the event hubs in secondary namespace");

                var eventHubInSecondaryNamespace = (await namespaces.GetAsync(rgName, secondaryNamespaceName)).Value;

                Utilities.Log("Retrieved the event hubs in secondary namespace");
                Utilities.Print(eventHubInSecondaryNamespace);

                //============================================================
                // Retrieving the connection string
                //

                var rules = await disasterRecoveryConfigs.ListAuthorizationRulesAsync(rgName, primaryNamespaceName, geoDRName).ToEnumerableAsync();
                foreach (var rule in rules)
                {
                    var key = (await disasterRecoveryConfigs.ListKeysAsync(rgName, primaryNamespaceName, geoDRName, rule.Name)).Value;
                    Utilities.Print(key);
                }

                Utilities.Log("Initiating fail over");

                var failOverResult = await disasterRecoveryConfigs.FailOverAsync(rgName, secondaryNamespaceName, geoDRName);
                Thread.Sleep(10 * 1000);
                while ((await disasterRecoveryConfigs.GetAsync(rgName, secondaryNamespaceName, geoDRName)).Value.ProvisioningState == ProvisioningStateDR.Accepted)
                {
                    Utilities.Log("Wait for fail over");
                    Thread.Sleep(10 * 1000);
                }
                if ((await disasterRecoveryConfigs.GetAsync(rgName, secondaryNamespaceName, geoDRName)).Value.ProvisioningState == ProvisioningStateDR.Succeeded)
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
                        //
                        if (pairing != null && !isFailOverSucceeded)
                        {
                            await disasterRecoveryConfigs.BreakPairingAsync(rgName, primaryNamespaceName, geoDRName);
                            Thread.Sleep(10 * 1000);
                            while ((await disasterRecoveryConfigs.GetAsync(rgName, primaryNamespaceName, geoDRName)).Value.ProvisioningState == ProvisioningStateDR.Accepted)
                            {
                                Thread.Sleep(10 * 1000);
                            }
                            if ((await disasterRecoveryConfigs.GetAsync(rgName, primaryNamespaceName, geoDRName)).Value.ProvisioningState == ProvisioningStateDR.Failed)
                            {
                                throw new Exception("Provisioning state of the break pairing is FAILED");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("Pairing breaking failed:" + ex.Message);
                    }
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
