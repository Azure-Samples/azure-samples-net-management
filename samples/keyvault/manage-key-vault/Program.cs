// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Samples.Helpers;
using System;
using System.Threading.Tasks;

namespace ManageKeyVault
{
    public class Program
    {
        /**
         * Azure Key Vault sample for managing key vaults -
         *  - Create a key vault
         *  - Authorize an application
         *  - Update a key vault
         *    - alter configurations
         *    - change permissions
         *  - Create another key vault
         *  - List key vaults
         *  - Delete a key vault.
         */
        public static async Task RunSample(TokenCredential credential)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            Guid tenantId = new Guid(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));
            // Please pre-define the Client's Object in Environment Variable settings
            string objectId = Environment.GetEnvironmentVariable("AZURE_OBJECT_ID");
            string vaultName1 = Utilities.RandomResourceName("vault1", 20);
            string vaultName2 = Utilities.RandomResourceName("vault2", 20);
            string rgName = Utilities.RandomResourceName("rgNEMV", 24);
            string region = "eastus";

            var keyVaultManagementClient = new KeyVaultManagementClient(subscriptionId, credential);
            var vaults = keyVaultManagementClient.Vaults;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

                //============================================================
                // Create a key vault with empty access policy

                Utilities.Log("Creating a key vault...");

                var vaultProperties = new VaultProperties(tenantId, new Sku(SkuName.Standard))
                {
                    AccessPolicies = new[] { new AccessPolicyEntry(tenantId, objectId, new Permissions()) }
                };
                var vaultParameters = new VaultCreateOrUpdateParameters(region, vaultProperties);

                var rawResult = await vaults.StartCreateOrUpdateAsync(rgName, vaultName1, vaultParameters);
                var vault1 = (await rawResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created key vault");
                Utilities.PrintVault(vault1);

                //============================================================
                // Authorize an application

                Utilities.Log("Authorizing the application associated with the current service principal...");

                var permissions = new Permissions
                {
                    Keys = new KeyPermissions[] { new KeyPermissions("all") },
                    Secrets = new SecretPermissions[] { new SecretPermissions("get"), new SecretPermissions("list") },
                };
                var accessPolicyEntry = new AccessPolicyEntry(tenantId, objectId, permissions);
                var accessPolicyProperties = new VaultAccessPolicyProperties(new[] { accessPolicyEntry });
                var accessPolicyParameters = new VaultAccessPolicyParameters(accessPolicyProperties);

                await vaults.UpdateAccessPolicyAsync(rgName, vaultName1, AccessPolicyUpdateKind.Add, accessPolicyParameters);
                vault1 = (await vaults.GetAsync(rgName, vaultName1)).Value;

                Utilities.Log("Updated key vault");
                Utilities.PrintVault(vault1);

                //============================================================
                // Update a key vault

                Utilities.Log("Update a key vault to enable deployments and add permissions to the application...");

                permissions = new Permissions
                {
                    Secrets = new SecretPermissions[] { new SecretPermissions("all") }
                };
                accessPolicyEntry = new AccessPolicyEntry(tenantId, objectId, permissions);
                var vaultPatchProperties = new VaultPatchProperties
                {
                    EnabledForDeployment = true,
                    EnabledForTemplateDeployment = true,
                    AccessPolicies = new[] { accessPolicyEntry }
                };
                var vaultPatchParameters = new VaultPatchParameters
                {
                    Properties = vaultPatchProperties
                };
                await vaults.UpdateAsync(rgName, vaultName1, vaultPatchParameters);
                vault1 = (await vaults.GetAsync(rgName, vaultName1)).Value;

                Utilities.Log("Updated key vault");
                // Print the network security group
                Utilities.PrintVault(vault1);

                //============================================================
                // Create another key vault

                Utilities.Log("Create another key vault");

                permissions = new Permissions
                {
                    Keys = new KeyPermissions[] { new KeyPermissions("list"), new KeyPermissions("get"), new KeyPermissions("decrypt") },
                    Secrets = new SecretPermissions[] { new SecretPermissions("get") },
                };
                accessPolicyEntry = new AccessPolicyEntry(tenantId, objectId, permissions);
                vaultProperties = new VaultProperties(tenantId, new Sku(SkuName.Standard))
                {
                    AccessPolicies = new[] { accessPolicyEntry }
                };
                vaultParameters = new VaultCreateOrUpdateParameters(region, vaultProperties);

                rawResult = await vaults.StartCreateOrUpdateAsync(rgName, vaultName2, vaultParameters);
                var vault2 = (await rawResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created key vault");
                // Print the network security group
                Utilities.PrintVault(vault2);

                //============================================================
                // List key vaults

                Utilities.Log("Listing key vaults...");

                foreach (var vault in (await vaults.ListByResourceGroupAsync(rgName).ToEnumerableAsync()))
                {
                    Utilities.PrintVault(vault);
                }

                //============================================================
                // Delete key vaults
                Utilities.Log("Deleting the key vaults");
                await vaults.DeleteAsync(rgName, vaultName1);
                await vaults.DeleteAsync(rgName, vaultName2);
                Utilities.Log("Deleted the key vaults");
            }
            finally
            {
                try
                {
                    await ResourceGroupHelper.DeleteResourceGroup(rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
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
