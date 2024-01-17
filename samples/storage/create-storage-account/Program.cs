using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

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
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static async Task RunSample(ArmClient client)
        {
            const string StorageAccountName = "strg1";
            const string ResourceGroupName = "rgNEMV";
            AzureLocation location = AzureLocation.EastUS;

            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, ResourceGroupName, new ResourceGroupData(location))).Value;

            // Create a storage account
            Console.WriteLine("Creating a Storage Account...");

            var StorageAccountCreateParameters = new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2, location);

            var rawResult = await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(Azure.WaitUntil.Completed, StorageAccountName, StorageAccountCreateParameters);
            var storageAccount = rawResult.Value;

            Console.WriteLine("Created Storage Account");
            PrintStorageAccount(storageAccount);
        }

        private static void PrintStorageAccount(StorageAccountResource storageAccount)
        {
            Console.WriteLine($@"Storage Account: {storageAccount.Id}
                                 Name: {storageAccount.Data.Name}
                                 Location: {storageAccount.Data.Location}
                                 Sku: {storageAccount.Data.Sku.Name} - {storageAccount.Data.Sku.Tier}");
        }
    }
}
