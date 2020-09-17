using System;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.ResourceManager.AppConfiguration;
using Azure.ResourceManager.AppConfiguration.Models;

using Samples.Utilities;

namespace CreateAppConfigurationSample
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
            string appConfigName = Utilities.RandomResourceName("appcnfg", 20);
            string rgName = Utilities.RandomResourceName("rgNEMV", 24);
            string region = "eastus";

            var appConfigManagementClient = new AppConfigurationManagementClient(subscriptionId, credential);
            var configurationStores = appConfigManagementClient.ConfigurationStores;

            await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

            // Create an App Configuration
            Utilities.Log("Creating an App Configuration...");

            var configurationStore = new ConfigurationStore(region, new Sku("free"));

            var rawResult = await configurationStores.StartCreateAsync(rgName, appConfigName, configurationStore);
            var appConfiguration = (await rawResult.WaitForCompletionAsync()).Value;

            Utilities.Log("Created App Configuration");
            Utilities.PrintAppConfiguration(appConfiguration);
        }
    }
}
