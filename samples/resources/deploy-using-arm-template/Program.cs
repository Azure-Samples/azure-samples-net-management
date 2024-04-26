// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Samples.Utilities;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeployUsingARMTemplate
{
    public class Program
    {
        public static async Task DeployUsingARMTemplate(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var location = AzureLocation.EastUS;

            // create resource identifier for subscription.
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");

            // Create resource group.

            Utilities.Log("Creating a resource group with name: " + rgName);
            ResourceGroupResource resourceGroup = (await client
                .GetSubscriptionResource(subscriptionResourceIdentifier)
                .GetResourceGroups()
                .CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;
            Utilities.Log("Created a resource group with name: " + rgName);

            try
            {
                // Create a deployment for an Azure App Service via an ARM
                // template.

                Utilities.Log("Starting a deployment for an Azure Storage Account: " + deploymentName);

                var deployMentCollection = resourceGroup.GetArmDeployments();

                //string templateJson = System.IO.File.ReadAllText("ArmTemplate.json");

                var templateJson = Utilities.GetArmTemplate("ArmTemplate.json");

                var deployMentData = new ArmDeploymentContent
                (
                    new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                    {
                        //Template = BinaryData.FromObjectAsJson(JsonSerializer.Deserialize<object>(templateJson)),
                        Template = new BinaryData(Encoding.UTF8.GetBytes(templateJson))
                    }
                 );

                var rawResult = await deployMentCollection
                    .CreateOrUpdateAsync(
                        Azure.WaitUntil.Completed,
                        deploymentName, deployMentData);
                var deployMent = rawResult.Value;

                Utilities.Log("Completed the Storage Account deployment: " + deployMent);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }

        private static async Task GetAllDeployments(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            var listdeploymentsCollection = client.GetSubscriptionResource(subscriptionResourceIdentifier).GetArmDeployments();

            // write code to iterate through the deployments.
            var listdeployments = listdeploymentsCollection.GetAllAsync();
            await foreach (var deployment in listdeployments)
            {
                Utilities.Log("Deployment: " + deployment.Data.Name);
            }
        }

        private static ResourceIdentifier GetResourceIdentifier(
            string subscriptionId,
            string resourceGroupName,
            string deploymentName)
        {
            StringBuilder resourceId = new StringBuilder();

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId), "SubscriptionId cannot be null.");
            }
            resourceId.Append($"/subscriptions/{subscriptionId}");

            if (!string.IsNullOrEmpty(resourceGroupName))
            {
                resourceId.Append($"/resourceGroups/{resourceGroupName}");
            }

            if (!string.IsNullOrEmpty(deploymentName))
            {
                resourceId.Append($"/providers/Microsoft.Resources/deployments/{deploymentName}");
            }

            return new ResourceIdentifier(resourceId.ToString());
        }

        private static async Task CheckIfResourceGroupExists(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            // Get the DeploymentsOperations
            SubscriptionResource subscription = client.GetSubscriptionResource(subscriptionResourceIdentifier);
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            bool isResourceExists = await resourceGroups.ExistsAsync(rgName);

            await Console.Out.WriteLineAsync($"Is Resource Group: {rgName} exists: {isResourceExists}");
        }

        private static async Task GetDeploymentForResourceGroup(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            // Get the DeploymentsOperations
            var deploymentsOperations = client.GetResourceGroupResource(resourceGroupResourceIdentifier).GetArmDeployments();

            // Get all deployments in the resource group
            var deployments = await deploymentsOperations.GetAsync(deploymentName);

            // Check if a given deloyment exists in a resource group.
            var isExists = deploymentsOperations.Exists(deploymentName);

            await Console.Out.WriteLineAsync($"The given deployment: {deployments.Value.Data.Name} of " +
                $"resource type {deployments.Value.Data.ResourceType} exists: {isExists}");
        }

        private static async Task CheckDeploymentExistsForResourceGroup(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            // Get the DeploymentsOperations
            var deploymentsOperations = client.GetResourceGroupResource(resourceGroupResourceIdentifier).GetArmDeployments();

            // Check if a given deloyment exists in a resource group.
            var isExists = deploymentsOperations.Exists(deploymentName);

            await Console.Out.WriteLineAsync($"The given deployment: {deploymentName} exists: {isExists}");
        }

        private static async Task CheckDeploymentExistsForRgInTerminalState(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            // Get the DeploymentsOperations
            var deploymentsOperations = client.GetResourceGroupResource(resourceGroupResourceIdentifier).GetArmDeployments();

            // Check if a given deloyment exists in a resource group.
            var isExists = deploymentsOperations.Exists(deploymentName);

            if(isExists)
            {
                // get provisioning state of deployment.
                var deployment = await deploymentsOperations.GetAsync(deploymentName);
                var provisioningState = deployment.Value.Data.Properties.ProvisioningState;
                await Console.Out.WriteLineAsync($"The given deployment: {deploymentName} is in state: {provisioningState}");
            }

        }

        private static async Task DeleteDeploymentHistoryForResourceGroup(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            // Get the DeploymentsOperations
            var deploymentsOperations = client.GetResourceGroupResource(resourceGroupResourceIdentifier).GetArmDeployments();

            // Get all deployments in the resource group
            var deployments = deploymentsOperations.GetAllAsync();

            // Iterate through the deployments and delete them.
            await foreach (var deployment in deployments)
            {
                Utilities.Log("Deleting deployment: " + deployment.Data.Name);
                await deployment.DeleteAsync(Azure.WaitUntil.Completed);
                Utilities.Log("Deleted deployment: " + deployment.Data.Name);
            }

        }
        private static async Task DeleteResourceGroup(ArmClient client, string subscriptionId, string rgName)
        {
            var resourceGroupResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
            // Get the DeploymentsOperations
            var resourceGroup = client.GetResourceGroupResource(resourceGroupResourceIdentifier);

            // Delete the resource group if exists.
            try
            {
                Utilities.Log("Deleting Resource Group: " + rgName);

                await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Deleted Resource Group: " + rgName);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }

        private static async Task GetAllPoliciesInSubscription(ArmClient client, string subscriptionId, string rgName)
        {
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            var subscription = client.GetSubscriptionResource(subscriptionResourceIdentifier);
            var subscriptionPolicyDefinitions = subscription.GetSubscriptionPolicyDefinitions();
            var policyDefinitions = subscriptionPolicyDefinitions.GetAllAsync();

            // Iterate through the policies in the subscription.
            await foreach (var policyDefinition in policyDefinitions)
            {
                Utilities.Log("Policy Definition: " + policyDefinition.Data.DisplayName);
            }
        }

        private static async Task UpdateTagsOnResourceGroup(ArmClient client, string subscriptionId, string rgName, string deploymentName)
        {
            var subscriptionResourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");

            // Create resource group.
            Utilities.Log("Creating a resource group with name: " + rgName);
            ResourceGroupResource resourceGroup = (await client
                .GetSubscriptionResource(subscriptionResourceIdentifier)
                .GetResourceGroups()
                .CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS))).Value;

            // Update tags on the resource group.
            var tags = new System.Collections.Generic.Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // set the new tags on resource group.
            resourceGroup = await resourceGroup.SetTagsAsync(tags);

            Utilities.Log($"Tags {string.Join(", ", resourceGroup.Data.Tags)} set on resource group: " + resourceGroup.Data.Id);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");

                var rgName = Utilities.RandomResourceName("rgRSAT", 24);
                var deploymentName = Utilities.RandomResourceName("dpRSAT", 24);

                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscriptionId);

                await DeployUsingARMTemplate(client, subscriptionId, rgName, deploymentName);

                await GetAllDeployments(client, subscriptionId, rgName, deploymentName);

                await UpdateTagsOnResourceGroup(client, subscriptionId, rgName, deploymentName);

                await GetDeploymentForResourceGroup(client, subscriptionId, rgName, deploymentName);

                await CheckIfResourceGroupExists(client, subscriptionId, rgName, deploymentName);

                await CheckDeploymentExistsForResourceGroup(client, subscriptionId, rgName, deploymentName);

                await CheckDeploymentExistsForRgInTerminalState(client, subscriptionId, rgName, deploymentName);

                await DeleteDeploymentHistoryForResourceGroup(client, subscriptionId, rgName, deploymentName);

                await DeleteResourceGroup(client, subscriptionId, rgName);

                await GetAllPoliciesInSubscription(client, subscriptionId, rgName);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }

}
