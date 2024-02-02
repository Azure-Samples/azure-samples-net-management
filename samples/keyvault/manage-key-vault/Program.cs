// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManageKeyVault
{
    public class Program
    {
        //Azure Key Vault sample for managing key vaults -
        //   - Create a key vault
        //   - Authorize an application
        //   - Update a key vault
        //     - alter configurations
        //     - change permissions
        //   - Create another key vault
        //   - List key vaults
        //   - Delete a key vault.
        public static async Task RunSample(ArmClient client)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            Guid tenantId = new Guid(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));
            // Please pre-define the Client's Object in Environment Variable settings
            string objectId = Environment.GetEnvironmentVariable("AZURE_OBJECT_ID");
            string vaultName1 = Utilities.RandomResourceName("vault1", 20);
            string vaultName2 = Utilities.RandomResourceName("vault2", 20);
            string rgName = Utilities.RandomResourceName("rgNEMV", 24);
            string location = AzureLocation.EastUS;
            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

            try
            {
                // Create a key vault with empty access policy

                Utilities.Log("Creating a key vault...");

                var vaultCollection = resourceGroup.GetKeyVaults();
                var vaultProperties = new KeyVaultProperties(tenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
                {
                    AccessPolicies =
                    {
                        new KeyVaultAccessPolicy(tenantId, objectId, new IdentityAccessPermissions())
                    }
                };
                KeyVaultCreateOrUpdateContent parameters = new KeyVaultCreateOrUpdateContent(location, vaultProperties);
                var keyVault = (await vaultCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, vaultName1, parameters)).Value;

                Utilities.Log("Created key vault");
                Utilities.PrintVault(keyVault);

                // Authorize an application

                Utilities.Log("Authorizing the application associated with the current service principal...");

                IEnumerable<KeyVaultAccessPolicy> policies = new List<KeyVaultAccessPolicy>();
                {
                    new KeyVaultAccessPolicy(tenantId, objectId, new IdentityAccessPermissions()
                    {
                        Keys =
                        {
                            IdentityAccessKeyPermission.All
                        },
                        Secrets =
                        {
                            IdentityAccessSecretPermission.Get,
                            IdentityAccessSecretPermission.List

                        }
                    });
                }
                var UpdateProperties = new KeyVaultAccessPolicyProperties(policies);
                var UpdateAccessPolicy = (await keyVault.UpdateAccessPolicyAsync(AccessPolicyUpdateKind.Add, new KeyVaultAccessPolicyParameters(UpdateProperties))).Value;
                var UpdateKeyVault = await keyVault.UpdateAsync(new KeyVaultPatch()
                {
                    Properties = new KeyVaultPatchProperties()
                    {
                        AccessPolicies =
                        {
                            UpdateAccessPolicy.AccessPolicies.ElementAt(0)
                        }
                    }
                });

                Utilities.Log("Updated key vault");
                Utilities.PrintVault(UpdateKeyVault.Value);

                // Update a key vault

                Utilities.Log("Update a key vault to enable deployments and add permissions to the application...");

                var permissions = new IdentityAccessPermissions()
                {
                    Secrets =
                    {
                        IdentityAccessSecretPermission.All
                    }
                };
                var patch = new KeyVaultPatch()
                {
                    Properties = new KeyVaultPatchProperties()
                    {
                        EnabledForDeployment = true,
                        EnabledForTemplateDeployment = true,
                        AccessPolicies =
                        {
                            new KeyVaultAccessPolicy(tenantId, objectId, permissions)
                        }
                    },
                };
                var UpdateKeyVault2 = await keyVault.UpdateAsync(patch);

                Utilities.Log("Updated key vault");

                Utilities.PrintVault(UpdateKeyVault2);

                // Create another key vault

                Utilities.Log("Create another key vault");

                permissions = new IdentityAccessPermissions()
                {
                    Keys =
                    {
                        IdentityAccessKeyPermission.Get
                    },
                    Secrets =
                    {
                        IdentityAccessSecretPermission.List,
                        IdentityAccessSecretPermission.Get,
                        IdentityAccessSecretPermission.Purge
                    }
                };
                //accessPolicyEntry = new AccessPolicyEntry(tenantId, objectId, permissions);
                var vaultProperties2 = new KeyVaultProperties(tenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
                {
                    AccessPolicies =
                    {
                        new KeyVaultAccessPolicy(tenantId, objectId, permissions)
                    }
                };
                var vaultParameters2 = new KeyVaultCreateOrUpdateContent(location, vaultProperties2);

                var rawResult = await vaultCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, vaultName2, vaultParameters2);
                var keyvault2 = rawResult.Value;

                Utilities.Log("Created key vault");
                // Print the network security group
                Utilities.PrintVault(keyvault2);

                // List key vaults

                Utilities.Log("Listing key vaults...");

                await foreach (var vault in vaultCollection.GetAllAsync())
                {
                    Utilities.PrintVault(vault);
                }

                // Delete key vaults
                Utilities.Log("Deleting the key vaults");
                await keyVault.DeleteAsync(Azure.WaitUntil.Completed);
                await keyvault2.DeleteAsync(Azure.WaitUntil.Completed);
                Utilities.Log("Deleted the key vaults");
            }
            finally
            {
                try
                {
                    await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);
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
