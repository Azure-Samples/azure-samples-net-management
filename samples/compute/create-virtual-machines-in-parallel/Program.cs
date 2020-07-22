// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
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

        public static async Task RunSample(TokenCredential credential)
        {

            IDictionary<string, int> virtualMachinesByLocation = new Dictionary<string, int>();

            virtualMachinesByLocation.Add("eastus", 5);
            virtualMachinesByLocation.Add("southcentralus", 5);

            var networkManagementClient = new NetworkManagementClient(SubscriptionId, credential);
            var virtualNetworks = networkManagementClient.VirtualNetworks;
            var publicIPAddresses = networkManagementClient.PublicIPAddresses;
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            var computeManagementClient = new ComputeManagementClient(SubscriptionId, credential);
            var virtualMachines = computeManagementClient.VirtualMachines;
            var storageManagementClient = new StorageManagementClient(SubscriptionId, credential);
            var storageAccounts = storageManagementClient.StorageAccounts;

            try
            {
                // Create a resource group (Where all resources gets created)
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, "westus");

                var publicIpCreatableKeys = new List<string>();
                // Prepare a batch of Creatable definitions
                var creatableVirtualMachines = new Dictionary<string, VirtualMachine>();
                //var creatableVirtualMachines = new List<VirtualMachine>();

                foreach (var entry in virtualMachinesByLocation)
                {
                    var region = entry.Key;
                    var vmCount = entry.Value;

                    // Create 1 network creatable per region
                    // Prepare Creatable Network definition (Where all the virtual machines get added to)
                    var networkName = Utilities.RandomResourceName("vnetCOPD-", 20);
                    var virtualNetworkParameters = new VirtualNetwork
                    {
                        Location = region,
                        AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "172.16.0.0/16" } },
                        Subnets = new List<Subnet>
                            {
                                new Subnet
                                {
                                    Name = "mySubnet",
                                    AddressPrefix = "172.16.0.0/24",
                                }
                            }
                    };
                    var networkCreatable = (await (await virtualNetworks
                        .StartCreateOrUpdateAsync(rgName, networkName, virtualNetworkParameters)).WaitForCompletionAsync()).Value;

                    // Create 1 storage creatable per region (For storing VMs disk)
                    var storageAccountName = Utilities.RandomResourceName("stgcopd", 20);
                    var storageAccountParameters = new StorageAccountCreateParameters(new Azure.ResourceManager.Storage.Models.Sku(SkuName.StandardGRS), Kind.Storage, region);
                    await (await storageAccounts.StartCreateAsync(rgName, storageAccountName, storageAccountParameters)).WaitForCompletionAsync();
                    string containerName = Utilities.RandomResourceName("cndisk", 20);

                    var linuxVMNamePrefix = Utilities.RandomResourceName("vm-", 15);
                    for (int i = 1; i <= vmCount; i++)
                    {
                        // Create 1 public IP address creatable
                        var ipAddress = new PublicIPAddress
                        {
                            PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                            PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                            Location = region,
                            DnsSettings = new PublicIPAddressDnsSettings
                            {
                                DomainNameLabel = $"{linuxVMNamePrefix}-{i}"
                            }
                        };

                        ipAddress = (await publicIPAddresses.StartCreateOrUpdate(rgName, $"{linuxVMNamePrefix}-{i}", ipAddress)
                            .WaitForCompletionAsync()).Value;

                        publicIpCreatableKeys.Add(ipAddress.IpAddress);

                        Utilities.Log("Created IP Address: " + ipAddress.Name);

                        // Create Network Interface
                        var nic = new NetworkInterface
                        {
                            Location = region,
                            IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                            {
                                new NetworkInterfaceIPConfiguration
                                {
                                    Name = "Primary",
                                    Primary = true,
                                    Subnet = new Subnet { Id = networkCreatable.Subnets.First().Id },
                                    PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                                    PublicIPAddress = new PublicIPAddress { Id = ipAddress.Id }
                                }
                            }
                        };
                        nic = await networkInterfaces.StartCreateOrUpdate(rgName, $"{linuxVMNamePrefix}-{i}", nic).WaitForCompletionAsync();
                        Utilities.Log("Created Network Interface: " + nic.Name);

                        // Create 1 virtual machine creatable
                        var vhdContainer = "https://" + storageAccountName + ".blob.core.windows.net/" + containerName;
                        var osVhduri = vhdContainer + string.Format("/os{0}.vhd", $"{linuxVMNamePrefix}-{i}");

                        var virtualMachineCreatable = new VirtualMachine(region)
                        {
                            NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                            {
                                NetworkInterfaces = new[]
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
                                },
                                DataDisks = new List<DataDisk>()
                            },
                            HardwareProfile = new HardwareProfile
                            {
                                VmSize = VirtualMachineSizeTypes.StandardD3V2
                            }
                        };
                        creatableVirtualMachines.Add($"{linuxVMNamePrefix}-{i}", virtualMachineCreatable);
                    }
                }

                // Create !!
                var t1 = DateTimeOffset.Now.UtcDateTime;
                Utilities.Log("Creating the virtual machines");

                List<Task> TaskList = new List<Task>();
                foreach (var item in creatableVirtualMachines)
                {
                    Task task = CreateVM(virtualMachines, item.Key, item.Value);
                    TaskList.Add(task);
                }

                await Task.WhenAll(TaskList.ToArray());

                var t2 = DateTimeOffset.Now.UtcDateTime;
                Utilities.Log("Created virtual machines");

                Utilities.Log($"Virtual machines create: took {(t2 - t1).TotalSeconds } seconds to create == " + creatableVirtualMachines.Count + " == virtual machines");

                // TODO: Create a traffic manager to route traffic across the virtual machines(Wait for Track2 Traffic Manager ready)
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

        public static async Task<VirtualMachine> CreateVM(VirtualMachinesOperations virtualMachines, string vmName, VirtualMachine vmParameter)
        {
            var result = await (await virtualMachines
                        .StartCreateOrUpdateAsync(rgName, vmName, vmParameter)).WaitForCompletionAsync();
            Utilities.Log("VM created for: " + result.Value.Id);
            return result.Value;
        }

        public static async Task Main(string[] args)
        {
            try
            {
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
