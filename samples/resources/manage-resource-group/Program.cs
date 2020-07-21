// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Samples.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManageResourceGroup
{
    public class Program
    {
        /**
         * Azure Resource sample for managing resource groups -
         * - Create a resource group
         * - Update a resource group
         * - Create another resource group
         * - List resource groups
         * - Delete a resource group.
         */
        public static async Task RunSample(TokenCredential credential)
        {
            var rgName = Utilities.RandomResourceName("rgRSMA", 24);
            var rgName2 = Utilities.RandomResourceName("rgRSMA", 24);
            var resourceTagName = Utilities.RandomResourceName("rgRSTN", 24);
            var resourceTagValue = Utilities.RandomResourceName("rgRSTV", 24);
            var location = "westus";
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var resourceClient = new ResourcesManagementClient(subscriptionId, credential);
            var resourceGroups = resourceClient.ResourceGroups;

            try
            {
                //=============================================================
                // Create resource group.

                Utilities.Log("Creating a resource group with name: " + rgName);

                var resourceGroup = new ResourceGroup(location);
                await resourceGroups.CreateOrUpdateAsync(rgName, resourceGroup);

                Utilities.Log("Created a resource group with name: " + rgName);

                //=============================================================
                // Update the resource group.

                Utilities.Log("Updating the resource group with name: " + rgName);

                resourceGroup = new ResourceGroup(location)
                {
                    Tags = new Dictionary<string, string>
                        {
                            { resourceTagName, resourceTagValue }
                        }
                };
                await resourceGroups.CreateOrUpdateAsync(rgName, resourceGroup);

                Utilities.Log("Updated the resource group with name: " + rgName);

                //=============================================================
                // Create another resource group.

                Utilities.Log("Creating another resource group with name: " + rgName2);

                var resourceGroup2 = new ResourceGroup(location);
                await resourceGroups.CreateOrUpdateAsync(rgName2, resourceGroup2);

                Utilities.Log("Created another resource group with name: " + rgName2);

                //=============================================================
                // List resource groups.

                Utilities.Log("Listing all resource groups");

                var listResult = await resourceGroups.ListAsync().ToEnumerableAsync();

                foreach (var rGroup in listResult)
                {
                    Utilities.Log("Resource group: " + rGroup.Name);
                }

                //=============================================================
                // Delete a resource group.

                Utilities.Log("Deleting resource group: " + rgName2);

                await (await resourceGroups.StartDeleteAsync(rgName2)).WaitForCompletionAsync();

                Utilities.Log("Deleted resource group: " + rgName2);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);

                    await (await resourceGroups.StartDeleteAsync(rgName)).WaitForCompletionAsync();

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
