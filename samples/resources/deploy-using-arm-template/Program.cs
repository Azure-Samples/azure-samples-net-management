// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Samples.Utilities;
using System;
using System.Threading.Tasks;

namespace DeployUsingARMTemplate
{
    public class Program
    {
        public static async Task RunSample(ArmClient client)
        {
            var rgName = Utilities.RandomResourceName("rgRSAT", 24);
            var deploymentName = Utilities.RandomResourceName("dpRSAT", 24);
            var location = AzureLocation.WestUS;
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            var templateJson = Utilities.GetArmTemplate("ArmTemplate.json");

            // Create resource group.

            Utilities.Log("Creating a resource group with name: " + rgName);
            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;
            Utilities.Log("Created a resource group with name: " + rgName);

            try
            {
                // Create a deployment for an Azure App Service via an ARM
                // template.

                Utilities.Log("Starting a deployment for an Azure App Service: " + deploymentName);

                var deployMentCollection = resourceGroup.GetArmDeployments();

                var deployMentData = new ArmDeploymentContent
                (
                    new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                    {
                        Template = BinaryData.FromObjectAsJson(templateJson),
                        Parameters = BinaryData.FromString("\"{}\"")
                    }
                 );
                var rawResult = await deployMentCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, deploymentName, deployMentData);
                var deployMent = rawResult.Value;

                Utilities.Log("Completed the deployment: " + deploymentName);
            }
            finally
            {
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
