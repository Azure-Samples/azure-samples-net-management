// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CreateVirtualMachinesInParallel
{
    public class Program
    {
        //Azure compute sample for creating multiple virtual machines in parallel.
        // - Define 1 virtual network per region
        // - Define 1 storage account per region
        // - Create 5 virtual machines in 2 regions using defined virtual network and storage account
        // - Create a traffic manager to route traffic across the virtual machines(Wait for Track2 Traffic Manager ready)

        private const string Username = "tirekicker";
        private const string Password = "<password>";
        private static readonly string rgName = Utilities.RandomResourceName("rgCOMV", 10);
        private static readonly string SubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

        public static async Task RunSample(ArmClient client)
        {

            IDictionary<string, int> virtualMachinesByLocation = new Dictionary<string, int>();

            virtualMachinesByLocation.Add("eastus", 5);
            virtualMachinesByLocation.Add("southcentralus", 5);

            var resourceGroupName = "QuickStartRG";
            String location = AzureLocation.WestUS2;
            // Create a resource group (Where all resources gets created)
            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroupName, new ResourceGroupData(location))).Value;

            try
            {
                var publicIpCreatableKeys = new List<string>();
                // Prepare a batch of Creatable definitions
                //var creatableVirtualMachines = new Dictionary<string, VirtualMachineResource>();
                var creatableVirtualMachines = new List<VirtualMachineResource>();
                var startTime = DateTimeOffset.Now.UtcDateTime;

                foreach (var entry in virtualMachinesByLocation)
                {
                    var region = entry.Key;
                    var vmCount = entry.Value;

                    // Create 1 network creatable per region
                    // Prepare Creatable Network definition (Where all the virtual machines get added to)
                    var networkName = Utilities.RandomResourceName("vnetCOPD-", 20);
                    var networkCollection = resourceGroup.GetVirtualNetworks();
                    var networkData = new VirtualNetworkData()
                    {
                        Location = region,
                        AddressPrefixes =
                    {
                        "172.16.0.0/16"
                    }
                    };
                    var networkCreatable = networkCollection.CreateOrUpdate(Azure.WaitUntil.Completed, networkName, networkData).Value;

                    // Create 1 storage creatable per region (For storing VMs disk)
                    var storageAccountSkuName = Utilities.RandomResourceName("stgsku", 20);
                    var storageAccountName = Utilities.RandomResourceName("stgcopd", 20);
                    var storageAccountCollection = resourceGroup.GetStorageAccounts();
                    var storageAccountData = new StorageAccountCreateOrUpdateContent(new StorageSku(storageAccountSkuName), StorageKind.Storage, region);
                    {
                    };
                    var storageAccountCreatable = storageAccountCollection.CreateOrUpdate(WaitUntil.Completed, storageAccountName, storageAccountData).Value;
                    string containerName = Utilities.RandomResourceName("cndisk", 20);

                    var linuxVMNamePrefix = Utilities.RandomResourceName("vm-", 15);
                    var pipDnsLabelLinuxVM = Utilities.CreateRandomName("rgpip1");
                    string pipName = Utilities.CreateRandomName("pip1");
                    var publicIpAddressCollection = resourceGroup.GetPublicIPAddresses();
                    var publicIPAddressData = new PublicIPAddressData()
                    {
                        Location = region,
                        DnsSettings =
                            {
                                DomainNameLabel = pipDnsLabelLinuxVM
                            }
                    };
                    var publicIpAddressCreatable = (publicIpAddressCollection.CreateOrUpdate(Azure.WaitUntil.Completed, pipName, publicIPAddressData)).Value;
                    //Create a subnet
                    Utilities.Log("Creating a Linux subnet...");
                    var subnetName = Utilities.CreateRandomName("subnet_");
                    var subnetData = new SubnetData()
                    {
                        ServiceEndpoints =
                    {
                        new ServiceEndpointProperties()
                        {
                            Service = "Microsoft.Storage"
                        }
                    },
                        Name = subnetName,
                        AddressPrefix = "10.0.0.0/28",
                    };
                    var subnetLRro = networkCreatable.GetSubnets().CreateOrUpdate(WaitUntil.Completed, subnetName, subnetData);
                    var subnet = subnetLRro.Value;
                    Utilities.Log("Created a Linux subnet with name : " + subnet.Data.Name);

                    //Create a networkInterface
                    Utilities.Log("Created a linux networkInterface");
                    var networkInterfaceData = new NetworkInterfaceData()
                    {
                        Location = region,
                        IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "internal",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Name = subnetName,
                                Id = new ResourceIdentifier($"{networkCreatable.Data.Id}/subnets/{subnetName}")
                            },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = publicIpAddressCreatable.Data,
                        }
                    }
                    };
                    var networkInterfaceName = Utilities.CreateRandomName("networkInterface");
                    var nic = (resourceGroup.GetNetworkInterfaces().CreateOrUpdate(WaitUntil.Completed, networkInterfaceName, networkInterfaceData)).Value;
                    Utilities.Log("Created a Linux networkInterface with name : " + nic.Data.Name);
                    var virtualMachineCollection = resourceGroup.GetVirtualMachines();
                    var linuxComputerName = Utilities.CreateRandomName("linuxComputer");
                    // Create 1 virtual machine creatable
                    for (int i = 1; i <= vmCount; i++)
                    {
                        var vhdContainer = "https://" + storageAccountName + ".blob.core.windows.net/" + containerName;

                        var linuxVmdata = new VirtualMachineData(region)
                        {
                            HardwareProfile = new VirtualMachineHardwareProfile()
                            {
                                VmSize = "Standard_D2a_v4"
                            },
                            OSProfile = new VirtualMachineOSProfile()
                            {
                                AdminUsername = Username,
                                AdminPassword = Password,
                                ComputerName = linuxComputerName,
                            },
                            NetworkProfile = new VirtualMachineNetworkProfile()
                            {
                                NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic.Id,
                                Primary = true,
                            }
                        }
                            },
                            StorageProfile = new VirtualMachineStorageProfile()
                            {
                                OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                                {
                                    OSType = SupportedOperatingSystemType.Linux,
                                    Caching = CachingType.ReadWrite,
                                    ManagedDisk = new VirtualMachineManagedDisk()
                                    {
                                        StorageAccountType = StorageAccountType.StandardLrs
                                    }
                                },
                                ImageReference = new ImageReference()
                                {
                                    Publisher = "Canonical",
                                    Offer = "UbuntuServer",
                                    Sku = "16.04-LTS",
                                    Version = "latest",
                                },
                            },
                            Zones =
                    {
                        "1"
                    },
                            BootDiagnostics = new BootDiagnostics()
                            {
                                StorageUri = new Uri($"http://{storageAccountCreatable.Data.Name}.blob.core.windows.net")
                            }
                        };
                        var creatableVirtualMachine = virtualMachineCollection.CreateOrUpdate(WaitUntil.Completed, "vm-" + i, linuxVmdata).Value;
                        creatableVirtualMachines.Add(creatableVirtualMachine);
                    }
                    var virtualMachines = virtualMachineCollection.GetAll();
                    foreach (var virtualMachine in virtualMachines)
                    {
                        Utilities.Log(virtualMachine.Id);
                    }
                    var endTime = DateTimeOffset.Now.UtcDateTime;
                    Utilities.Log($"Created VM: took {(endTime - startTime).TotalSeconds} seconds");
                }

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
