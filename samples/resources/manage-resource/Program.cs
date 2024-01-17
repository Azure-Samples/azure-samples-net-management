// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Resources.Models;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ManageResource
{
    public class Program
    {
        //Azure Resource sample for managing resources -
        // - Create a resource
        // - Update a resource
        // - Create another resource
        // - List resources
        // - Delete a resource.

        public static async Task RunSample(ArmClient client)
        {
            var resourceGroupName = Utilities.RandomResourceName("rgRSMR", 24);
            var resourceName1 = Utilities.RandomResourceName("rn1", 24);
            var resourceName2 = Utilities.RandomResourceName("rn2", 24);
            var location = AzureLocation.EastUS;
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            // Create resource group.

            Utilities.Log("Creating a resource group with name: " + resourceGroupName);
            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroupName, new ResourceGroupData(location))).Value;
            Utilities.Log("Created a resource group with name: " + resourceGroupName);
            try
            {

                // Create storage account.

                Utilities.Log("Creating a storage account with name: " + resourceName1);

                var storageCollection = resourceGroup.GetStorageAccounts();
                var rawResult = await storageCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceName1, new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2, location)
                {
                    AccessTier = StorageAccountAccessTier.Hot
                });
                var storageResource = rawResult.Value;

                Utilities.Log("Storage account created: " + storageResource.Id);

                // Update - set the sku name

                Utilities.Log("Updating the storage account with name: " + resourceName1);

                var updateResult = await storageResource.UpdateAsync(new StorageAccountPatch()
                {
                    AccessTier = StorageAccountAccessTier.Premium
                });

                Utilities.Log("Updated the storage account with name: " + resourceName1);

                // Create another storage account.

                Utilities.Log("Creating another storage account with name: " + resourceName2);

                var rawResult2 = await storageCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceName1, new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2, location)
                {
                    AccessTier = StorageAccountAccessTier.Hot
                });
                var storageResource2 = rawResult2.Value;

                Utilities.Log("Storage account created: " + storageResource2.Id);

                // List storage accounts.

                // Add Sleep to handle the lag for list operation
                System.Threading.Thread.Sleep(10 * 1000);

                Utilities.Log("Listing all storage accounts for resource group: " + resourceGroupName);

                await foreach (var sAccount in storageCollection.GetAllAsync())
                {
                    Utilities.Log("Storage account: " + sAccount.Data.Name);
                }

                // Delete a storage accounts.

                Utilities.Log("Deleting storage account: " + resourceName2);

                await storageResource2.DeleteAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Deleted storage account: " + resourceName2);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + resourceGroupName);

                    await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);

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
