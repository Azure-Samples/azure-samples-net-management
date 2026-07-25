using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using static Azure.Core.HttpHeader;


namespace CreateStorageSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");

                //// Authenticate also works
                //var credential = new DefaultAzureCredential();
                //var subscriptionId = "62c2cd29-ee54-4c2f-99a4-5a46187d67a5";

                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client, subscription);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static async Task RunSample(ArmClient client, string subscriptionId)
        {
            const string StorageAccountName = "strg1suffix3";
            const string ResourceGroupName = "rgNEMV";
            AzureLocation location = AzureLocation.EastUS;

            // get the subscription resource.
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            var subscription = client.GetSubscriptionResource(subscriptionResourceIdentifier);

            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, ResourceGroupName, new ResourceGroupData(location))).Value;
            resourceGroup = client
                .GetDefaultSubscription()
                .GetResourceGroups()
                .GetAsync(ResourceGroupName).Result;
            //print the resource group details.
            Console.WriteLine($@"Resource Group: {resourceGroup.Id}
                                 Name: {resourceGroup.Data.Name}
                                 Location: {resourceGroup.Data.Location}
                                 Provisioning State: {resourceGroup.Data.ResourceGroupProvisioningState}");

            // check if storage account name is available.
            StorageAccountNameAvailabilityContent parameters = new StorageAccountNameAvailabilityContent(StorageAccountName);

            // Check the storage account name availability
            Response<StorageAccountNameAvailabilityResult> result = await subscription.CheckStorageAccountNameAvailabilityAsync(parameters);

            // check if the storage account name is available.
            if (!result.Value.IsNameAvailable ?? false)
            {
                Console.WriteLine($"The storage account name '{StorageAccountName}' is not available.");
                Console.WriteLine($"Reason: {result.Value.Reason}");
                Console.WriteLine($"Message: {result.Value.Message}");
                //return;
            }

            // Check if storage account exists
            bool exists = await CheckIfStorageAccountExists(resourceGroup, StorageAccountName);

            if (exists)
            {
                Console.WriteLine($"Storage account '{StorageAccountName}' already exists.");
                // Code to get existing storage account
                var storageAccountExisting = await resourceGroup.GetStorageAccounts().GetAsync(StorageAccountName);
                // Get storage account properties using GetPropertiesAsync
                StorageAccountResource storageAccountResource = await resourceGroup.GetStorageAccounts().GetAsync(StorageAccountName);

                // Create management policies for a storage account.
                PolicyAssignmentCollection policyCollection = storageAccountResource
                    .GetStorageAccountManagementPolicy()
                    .GetPolicyAssignments();

                var newDateTime = storageAccountResource?.Data.CreatedOn.Value.DateTime;

                Console.WriteLine($"Storage account created time: {storageAccountResource.Data.CreatedOn} and " +
                    $"DateTime eq {newDateTime}");

                //print storage account details
                PrintStorageAccount(storageAccountExisting);
                return;
            }
            bool isVrs = true;
            StorageAccountResource storageAccount = null;

            if (isVrs)
            {
                storageAccount = await CreateStorageAccountWithVRS(resourceGroup, StorageAccountName, location);
            }
            else
            {
                // Create a storage account
                Console.WriteLine("Creating a Storage Account...");

                var StorageAccountCreateParameters = new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2, location);

                var rawResult = await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(Azure.WaitUntil.Completed, StorageAccountName, StorageAccountCreateParameters);
                storageAccount = rawResult.Value;

                Console.WriteLine("Created Storage Account");
            }

            PrintStorageAccount(storageAccount);

            // Delete the storage account.
            Console.WriteLine("Deleting the Storage Account...");
            await storageAccount.DeleteAsync(Azure.WaitUntil.Completed);
            Console.WriteLine("Deleted the Storage Account");
        }

        public static async Task<bool> CheckIfStorageAccountExists(ResourceGroupResource resourceGroup, string saName)
        {
            try
            {
                var storageAccount = await resourceGroup.GetStorageAccounts().GetAsync(saName);
                return storageAccount != null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Storage account does not exist
                return false;
            }
        }
        public static async Task<StorageAccountResource> CreateStorageAccountWithVRS(ResourceGroupResource resourceGroup, string storageAccountName, AzureLocation location)
        {
            var storageAccountCreateParameters = new StorageAccountCreateParametersWithVrs(
                new StorageSku(StorageSkuName.StandardLrs),
                StorageKind.StorageV2,
                location)
            {
                ResidencyBoundary = "boundary",
                ResiliencyMinimum = "minimum",
                ResiliencyMaximum = "maximum",
                ResilienciesProgressionId = "progressionId",
                AdditionalLocations = new AdditionalLocation[]
                {
                new AdditionalLocation { Location = "location1" },
                new AdditionalLocation { Location = "location2"}
                // Add more additional locations as needed
                }
            };

            StorageAccountCollection storageAccountCollection = resourceGroup.GetStorageAccounts();
            var storage = await storageAccountCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, storageAccountName, storageAccountCreateParameters);
            return storage.Value;
        }

        private static void PrintStorageAccount(StorageAccountResource storageAccount)
        {
            Console.WriteLine($@"Storage Account: {storageAccount.Id}
                                 Name: {storageAccount.Data.Name}
                                 Location: {storageAccount.Data.Location}
                                 Sku: {storageAccount.Data.Sku.Name} - {storageAccount.Data.Sku.Tier}
                                 Created On: {storageAccount.Data.CreatedOn}");
        }
    }
}
