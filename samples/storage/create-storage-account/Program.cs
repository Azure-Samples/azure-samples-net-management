using System;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

using Samples.Utilities;

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
                Utilities.Log(ex);
            }
        }

        public static async Task RunSample(DefaultAzureCredential credential)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string storageAccountName = Utilities.RandomResourceName("strg", 20);
            string rgName = Utilities.RandomResourceName("rgNEMV", 24);
            string region = "eastus";

            var storageManagementClient = new StorageManagementClient(subscriptionId, credential);
            var storageAccounts = storageManagementClient.StorageAccounts;

            await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

            // Create a storage account
            Utilities.Log("Creating a Storage Account...");

            var StorageAccountCreateParameters = new StorageAccountCreateParameters(new Sku(SkuName.StandardLRS), Kind.StorageV2, region);

            var rawResult = await storageAccounts.StartCreateAsync(rgName, storageAccountName, StorageAccountCreateParameters);
            var storageAccount = (await rawResult.WaitForCompletionAsync()).Value;

            Utilities.Log("Created Storage Account");
            Utilities.PrintStorageAccount(storageAccount);
        }
    }
}
