using System;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;
using Samples.Utilities;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources.Models;
using Azure;

namespace CreateAppConfigurationSample
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
                Utilities.Log(ex);
            }
        }

        public static async Task RunSample(ArmClient client)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string appConfigName = Utilities.RandomResourceName("appcnfg", 20);
            string rgName = Utilities.RandomResourceName("rgNEMV", 24);
            string region = "eastus";

            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(region))).Value;

            // Create an App Configuration
            Utilities.Log("Creating an App Configuration...");
            var collection = resourceGroup.GetAppConfigurationStores();
            AppConfigurationStoreData configurationStoreData = new AppConfigurationStoreData(region, new AppConfigurationSku("Standard"))
            {
                PublicNetworkAccess = AppConfigurationPublicNetworkAccess.Disabled
            };
            AppConfigurationStoreResource configurationStore = (await collection.CreateOrUpdateAsync(WaitUntil.Completed, appConfigName, configurationStoreData)).Value;

            Utilities.Log("Created App Configuration");
            Utilities.PrintAppConfiguration(configurationStore);
        }
    }
}
