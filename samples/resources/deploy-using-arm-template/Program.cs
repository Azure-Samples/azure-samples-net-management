// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Samples.Helpers;
using System;
using System.Threading.Tasks;

namespace DeployUsingARMTemplate
{
    public class Program
    {
        public static async Task RunSample(TokenCredential credential)
        {
            var rgName = Utilities.RandomResourceName("rgRSAT", 24);
            var deploymentName = Utilities.RandomResourceName("dpRSAT", 24);
            var location = "westus";
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            var templateJson = Utilities.GetArmTemplate("ArmTemplate.json");

            var resourceClient = new ResourcesManagementClient(subscriptionId, credential);
            var resourceGroups = resourceClient.ResourceGroups;
            var deployments = resourceClient.Deployments;

            try
            {
                //=============================================================
                // Create resource group.

                Utilities.Log("Creating a resource group with name: " + rgName);

                var resourceGroup = new ResourceGroup(location);
                resourceGroup = await resourceGroups.CreateOrUpdateAsync(rgName, resourceGroup);

                Utilities.Log("Created a resource group with name: " + rgName);

                //=============================================================
                // Create a deployment for an Azure App Service via an ARM
                // template.

                Utilities.Log("Starting a deployment for an Azure App Service: " + deploymentName);

                var parameters = new Deployment
                (
                    new DeploymentProperties(DeploymentMode.Incremental)
                    {
                        Template = templateJson,
                        Parameters = "{}"
                    }
                 );
                var rawResult = await deployments.StartCreateOrUpdateAsync(rgName, deploymentName, parameters);
                await rawResult.WaitForCompletionAsync();

                Utilities.Log("Completed the deployment: " + deploymentName);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);

                    await (await resourceGroups.StartDeleteAsync(rgName)).WaitForCompletionAsync();

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
                //=================================================================
                // Authenticate
                var credentials = new DefaultAzureCredential();

                await RunSample(credentials);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }

}
