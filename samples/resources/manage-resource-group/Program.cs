// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManageResourceGroup
{
    public class Program
    {
        //Azure Resource sample for managing resource groups -
        //  - Create a resource group
        //  - Update a resource group
        //  - Create another resource group
        //  - List resource groups
        //  - Delete a resource group.

        public static async Task RunSample(ArmClient client)
        {
            var rgName = Utilities.RandomResourceName("rgRSMA", 24);
            var rgName2 = Utilities.RandomResourceName("rgRSMA", 24);
            var resourceTagName = Utilities.RandomResourceName("rgRSTN", 24);
            var resourceTagValue = Utilities.RandomResourceName("rgRSTV", 24);
            var location = AzureLocation.WestUS;
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            // Create resource group.

            Utilities.Log("Creating a resource group with name: " + rgName);

            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

            Utilities.Log("Created a resource group with name: " + rgName);

            try
            {

                // Update the resource group.

                Utilities.Log("Updating the resource group with name: " + rgName);

                await resourceGroup.UpdateAsync(new ResourceGroupPatch()
                {
                    Tags =
                    {
                        [resourceTagName] = resourceTagValue
                    }
                });

                Utilities.Log("Updated the resource group with name: " + rgName);

                // Create another resource group.

                Utilities.Log("Creating another resource group with name: " + rgName2);

                ResourceGroupResource resourceGroup2 = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName2, new ResourceGroupData(location))).Value;

                Utilities.Log("Created another resource group with name: " + rgName2);

                // List resource groups.

                Utilities.Log("Listing all resource groups");


                await foreach (var rGroup in client.GetDefaultSubscription().GetResourceGroups().GetAllAsync())
                {
                    Utilities.Log("Resource group: " + rGroup.Data.Name);
                }

                // Delete a resource group.

                Utilities.Log("Deleting resource group: " + rgName2);

                await resourceGroup2.DeleteAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Deleted resource group: " + rgName2);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);

                    await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);

                    Utilities.Log("Deleted Resource Group: " + rgName);
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
