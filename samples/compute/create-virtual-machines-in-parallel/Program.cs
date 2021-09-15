// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
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
        private const string resourceGroupName = "QuickStartRG";
        private static readonly string SubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

        public static async Task RunSample(TokenCredential credential)
        {
            IDictionary<Location, int> virtualMachinesByLocation = new Dictionary<Location, int>();

            virtualMachinesByLocation.Add(Location.EastUS, 5);
            virtualMachinesByLocation.Add(Location.SouthCentralUS, 5);

            ArmClient client = new ArmClient(SubscriptionId, credential);

            ResourceGroupContainer resourceGroupContainer = client.DefaultSubscription.GetResourceGroups();
            ResourceGroupData resourceGroupData = new ResourceGroupData(Location.EastUS);
            ResourceGroup resourceGroup = await resourceGroupContainer.CreateOrUpdate(resourceGroupName, resourceGroupData).WaitForCompletionAsync();

            // Store the created public IP Addresses
            var publicIpCreatableKeys = new List<string>();
            var creatableVirtualMachines = new Dictionary<string, VirtualMachineData>();

            try
            {
                foreach ((Location location, int vmCount) in virtualMachinesByLocation)
                {
                    // Create 1 network creatable per region
                    // Prepare Creatable Network definition (Where all the virtual machines get added to)
                    string networkName = $"vnet-{location}";
                    VirtualNetworkData virtualNetworkParameters = new VirtualNetworkData
                    {
                        Location = location,
                        AddressSpace = new AddressSpace { AddressPrefixes = { "172.16.0.0/16" } },
                        Subnets =
                    {
                        new SubnetData
                        {
                            Name = "mySubnet",
                            AddressPrefix = "172.16.0.0/24",
                        }
                    }
                    };
                    VirtualNetwork virtualNetwork = await resourceGroup.GetVirtualNetworks().CreateOrUpdate(networkName, virtualNetworkParameters).WaitForCompletionAsync();

                    // Create 1 storage creatable per region (For storing VMs disk)
                    var storageAccountName = "stgcopd";
                    var storageAccountParameters = new StorageAccountCreateParameters(new Azure.ResourceManager.Storage.Models.Sku(SkuName.StandardGRS), Kind.Storage, location);
                    await resourceGroup.GetStorageAccounts().CreateOrUpdate(storageAccountName, storageAccountParameters).WaitForCompletionAsync();
                    string containerName = "cndisk";

                    // Assemble the virtual machine data to create
                    string linuxVMNamePrefix = "vm-";
                    for (int i = 1; i <= vmCount; i++)
                    {
                        // Create 1 public IP address creatable
                        PublicIPAddressData ipAddressData = new PublicIPAddressData
                        {
                            Location = location,
                            PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                            PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                            DnsSettings = new PublicIPAddressDnsSettings
                            {
                                DomainNameLabel = $"{linuxVMNamePrefix}-{i}"
                            }
                        };
                        PublicIPAddress ipAddress = await resourceGroup.GetPublicIPAddresses().CreateOrUpdate($"{linuxVMNamePrefix}-{i}", ipAddressData).WaitForCompletionAsync();
                        publicIpCreatableKeys.Add(ipAddress.Data.IpAddress);

                        Console.WriteLine("Created IP Address: " + ipAddress.Data.Name);

                        // Create Network Interface
                        NetworkInterfaceData nicData = new NetworkInterfaceData
                        {
                            Location = location,
                            IpConfigurations =
                        {
                            new NetworkInterfaceIPConfiguration
                            {
                                Name = "Primary",
                                Primary = true,
                                Subnet = virtualNetwork.Data.Subnets.First(),
                                PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                                PublicIPAddress = ipAddress.Data
                            }
                        }
                        };
                        NetworkInterface nic = await resourceGroup.GetNetworkInterfaces().CreateOrUpdate($"{linuxVMNamePrefix}-{i}", nicData).WaitForCompletionAsync();
                        Console.WriteLine("Created Network Interface: " + nic.Data.Name);

                        // Create 1 virtual machine creatable
                        var vhdContainer = $"https://{storageAccountName}.blob.core.windows.net/{containerName}";
                        var osVhduri = $"{vhdContainer}/os{linuxVMNamePrefix}-{i}.vhd";

                        VirtualMachineData virtualMachineCreatable = new VirtualMachineData(location)
                        {
                            NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                            {
                                NetworkInterfaces =
                            {
                                new NetworkInterfaceReference { Id = nic.Id }
                            }
                            },
                            OsProfile = new OSProfile
                            {
                                ComputerName = $"{linuxVMNamePrefix}-{i}",
                                AdminUsername = Username,
                                AdminPassword = Password,
                                LinuxConfiguration = new LinuxConfiguration
                                {
                                    DisablePasswordAuthentication = false,
                                    ProvisionVMAgent = true
                                }
                            },
                            StorageProfile = new StorageProfile
                            {
                                ImageReference = new ImageReference
                                {
                                    Offer = "UbuntuServer",
                                    Publisher = "Canonical",
                                    Sku = "16.04-LTS",
                                    Version = "latest"
                                },
                                OsDisk = new OSDisk(DiskCreateOptionTypes.FromImage)
                                {
                                    Caching = CachingTypes.None,
                                    Name = "test",
                                    Vhd = new VirtualHardDisk { Uri = osVhduri }
                                }
                            },
                            HardwareProfile = new HardwareProfile
                            {
                                VmSize = VirtualMachineSizeTypes.StandardD3V2
                            }
                        };
                        creatableVirtualMachines.Add($"{linuxVMNamePrefix}-{i}", virtualMachineCreatable);
                    }

                    // Create Virtual Machines
                    var t1 = DateTimeOffset.Now.UtcDateTime;
                    Console.WriteLine("Creating the virtual machines");

                    List<Task> TaskList = new List<Task>();
                    foreach ((string vmName, VirtualMachineData vmData) in creatableVirtualMachines)
                    {
                        Task task = CreateVM(resourceGroup, vmName, vmData);
                        TaskList.Add(task);
                    }

                    await Task.WhenAll(TaskList.ToArray());

                    var t2 = DateTimeOffset.Now.UtcDateTime;
                    Console.WriteLine("Created virtual machines");

                    Console.WriteLine($"Virtual machines create: took {(t2 - t1).TotalSeconds } seconds to create == " + creatableVirtualMachines.Count + " == virtual machines");

                    // TODO: Create a traffic manager to route traffic across the virtual machines(Wait for Track2 Traffic Manager ready)
                }
            }
            finally
            {
                try
                {
                    await resourceGroup.DeleteAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static async Task<VirtualMachine> CreateVM(ResourceGroup resourceGroup, string vmName, VirtualMachineData vmData)
        {
            VirtualMachine result = await resourceGroup.GetVirtualMachines().CreateOrUpdate(vmName, vmData).WaitForCompletionAsync();
            Console.WriteLine("VM created for: " + result.Id);
            return result;
        }

        public static async Task Main(string[] args)
        {
            try
            {
                // Authenticate
                TokenCredential credential = new DefaultAzureCredential();

                await RunSample(credential);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}