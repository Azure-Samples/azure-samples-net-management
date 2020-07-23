// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System;
using System.Threading.Tasks;

namespace Samples.Utilities
{
    public class ResourceGroupHelper
    {
        public static DefaultAzureCredential Credentials = new DefaultAzureCredential();

        public static string SubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

        public static ResourcesManagementClient ResourceClient;

        public static ResourceGroupsOperations ResourceGroups;

        static ResourceGroupHelper()
        {
            ResourceClient = new ResourcesManagementClient(SubscriptionId, Credentials);
            ResourceGroups = ResourceClient.ResourceGroups;
        }

        public static async Task CreateOrUpdateResourceGroup(string rgName, string location)
        {
            Utilities.Log("Creating a resource group with name: " + rgName);

            var resourceGroup = new ResourceGroup(location);
            await ResourceGroups.CreateOrUpdateAsync(rgName, resourceGroup);

            Utilities.Log("Created a resource group with name: " + rgName);
        }

        public static async Task DeleteResourceGroup(string rgName)
        {
            Utilities.Log("Deleting resource group: " + rgName);

            await (await ResourceGroups.StartDeleteAsync(rgName)).WaitForCompletionAsync();

            Utilities.Log("Deleted resource group: " + rgName);
        }
    }
}
