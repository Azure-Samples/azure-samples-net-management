using System;
using System.Threading.Tasks;

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
                var credential = new DefaultAzureCredential();

                await RunSample(credential);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static async Task RunSample(DefaultAzureCredential credential)
        {
            const string StorageAccountName = "strg1";
            const string ResourceGroupName = "rgNEMV";
            const string Location = "eastus";

            var armClient = new ArmClient(credential);
            ResourceGroup resourceGroup = await armClient.GetDefaultSubscription().GetResourceGroups().CreateOrUpdate(ResourceGroupName, new ResourceGroupData(Location)).WaitForCompletionAsync();

            // Create a storage account
            Console.WriteLine("Creating a Storage Account...");

            var StorageAccountCreateParameters = new StorageAccountCreateParameters(new Sku(SkuName.StandardLRS), Kind.StorageV2, Location);

            var rawResult = await resourceGroup.GetStorageAccounts().CreateOrUpdateAsync(StorageAccountName, StorageAccountCreateParameters);
            var storageAccount = (await rawResult.WaitForCompletionAsync()).Value;

            Console.WriteLine("Created Storage Account");
            PrintStorageAccount(storageAccount);
        }

        private static void PrintStorageAccount(StorageAccount storageAccount)
        {
            Console.WriteLine($@"Storage Account: {storageAccount.Id}
Name: {storageAccount.Data.Name}
Location: {storageAccount.Data.Location}
Sku: {storageAccount.Data.Sku.Name} - {storageAccount.Data.Sku.Tier}");
        }
    }
}
