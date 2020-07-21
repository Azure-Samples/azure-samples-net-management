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

namespace ManageResource
{
    public class Program
    {
        /**
        * Azure Resource sample for managing resources -
        * - Create a resource
        * - Update a resource
        * - Create another resource
        * - List resources
        * - Delete a resource.
        */
        public static async Task RunSample(TokenCredential credential)
        {
            var resourceGroupName = Utilities.RandomResourceName("rgRSMR", 24);
            var resourceName1 = Utilities.RandomResourceName("rn1", 24);
            var resourceName2 = Utilities.RandomResourceName("rn2", 24);
            var location = "westus";
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var resourceClient = new ResourcesManagementClient(subscriptionId, credential);
            var resourceGroups = resourceClient.ResourceGroups;
            var resources = resourceClient.Resources;

            try
            {
                //=============================================================
                // Create resource group.

                Utilities.Log("Creating a resource group with name: " + resourceGroupName);

                var resourceGroup = new ResourceGroup(location);
                resourceGroup = await resourceGroups.CreateOrUpdateAsync(resourceGroupName, resourceGroup);

                //=============================================================
                // Create storage account.

                Utilities.Log("Creating a storage account with name: " + resourceName1);

                var rawResult = await resources.StartCreateOrUpdateAsync(
                    resourceGroupName,
                    "Microsoft.Storage",
                    "",
                    "storageAccounts",
                    resourceName1,
                    "2019-06-01",
                    new GenericResource
                    {
                        Location = "westus",
                        Sku = new Sku()
                        {
                            Name = "Standard_LRS",
                            Tier = "Standard"
                        },
                        Kind = "StorageV2",
                        Properties = new Dictionary<string, object>
                        {
                            { "accessTier", "hot" }
                        }
                    }
                    );
                var result = (await rawResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Storage account created: " + result.Id);

                //=============================================================
                // Update - set the sku name

                Utilities.Log("Updating the storage account with name: " + resourceName1);

                var rawUpdateResult = await resources.StartCreateOrUpdateByIdAsync(
                    result.Id,
                    "2019-06-01",
                    new GenericResource
                    {
                        Location = "westus",
                        Sku = new Sku()
                        {
                            Name = "Standard_RAGRS",
                            Tier = "Standard"
                        },
                        Kind = "StorageV2",
                        Properties = new Dictionary<string, object>
                        {
                            { "accessTier", "hot" }
                        }
                    }
                    );
                var updateResult = (await rawUpdateResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Updated the storage account with name: " + resourceName1);

                //=============================================================
                // Create another storage account.

                Utilities.Log("Creating another storage account with name: " + resourceName2);

                var rawResult2 = await resources.StartCreateOrUpdateAsync(
                    resourceGroupName,
                    "Microsoft.Storage",
                    "",
                    "storageAccounts",
                    resourceName2,
                    "2019-06-01",
                    new GenericResource
                    {
                        Location = "westus",
                        Sku = new Sku
                        {
                            Name = "Standard_LRS",
                            Tier = "Standard"
                        },
                        Kind = "StorageV2",
                        Properties = new Dictionary<string, object>
                        {
                            { "accessTier", "hot" }
                        }
                    }
                    );
                var result2 = (await rawResult2.WaitForCompletionAsync()).Value;

                Utilities.Log("Storage account created: " + result2.Id);

                //=============================================================
                // List storage accounts.

                // Add Sleep to handle the lag for list operation
                System.Threading.Thread.Sleep(10 * 1000);

                Utilities.Log("Listing all storage accounts for resource group: " + resourceGroupName);

                var listResult = await resources.ListByResourceGroupAsync(resourceGroupName).ToEnumerableAsync();

                foreach (var sAccount in listResult)
                {
                    Utilities.Log("Storage account: " + sAccount.Name);
                }

                //=============================================================
                // Delete a storage accounts.

                Utilities.Log("Deleting storage account: " + resourceName2);

                await (await resources.StartDeleteAsync(
                    resourceGroupName,
                    "Microsoft.Storage",
                    "",
                    "storageAccounts",
                    resourceName2,
                    "2019-06-01"
                    )).WaitForCompletionAsync();

                Utilities.Log("Deleted storage account: " + resourceName2);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + resourceGroupName);

                    await (await resourceGroups.StartDeleteAsync(resourceGroupName)).WaitForCompletionAsync();

                    Utilities.Log("Deleted Resource Group: " + resourceGroupName);
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
