// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Samples.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManageIPAddress
{
    public class Program
    {
        /**
         * Azure Network sample for managing IP address -
         *  - Assign a public IP address for a virtual machine during its creation
         *  - Assign a public IP address for a virtual machine through an virtual machine update action
         *  - Get the associated public IP address for a virtual machine
         *  - Get the assigned public IP address for a virtual machine
         *  - Remove a public IP address from a virtual machine.
         */
        private static readonly string UserName = "tirekicker";
        private static readonly string Password = "12NewPA$$w0rd!";
        private static readonly string SubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

        public static async Task RunSample(TokenCredential credential)
        {
            string publicIPAddressName1 = Utilities.RandomResourceName("pip1", 20);
            string publicIPAddressName2 = Utilities.RandomResourceName("pip2", 20);
            string publicIPAddressLeafDNS1 = Utilities.RandomResourceName("pip1", 20);
            string publicIPAddressLeafDNS2 = Utilities.RandomResourceName("pip2", 20);
            string networkInterfaceName = Utilities.RandomResourceName("nic", 20);
            string vmName = Utilities.RandomResourceName("vm", 8);
            string rgName = Utilities.RandomResourceName("rgNEMP", 24);

            var networkManagementClient = new NetworkManagementClient(SubscriptionId, credential);
            var virtualNetworks = networkManagementClient.VirtualNetworks;
            var publicIPAddresses = networkManagementClient.PublicIPAddresses;
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            var computeManagementClient = new ComputeManagementClient(SubscriptionId, credential);
            var virtualMachines = computeManagementClient.VirtualMachines;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, "eastus");

                //============================================================
                // Assign a public IP address for a VM during its creation

                // Define a public IP address to be used during VM creation time

                Utilities.Log("Creating a public IP address...");

                var publicIPAddress = new PublicIPAddress
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = "eastus",
                    DnsSettings = new PublicIPAddressDnsSettings
                    {
                        DomainNameLabel = publicIPAddressLeafDNS1
                    }
                };

                publicIPAddress = (await publicIPAddresses.StartCreateOrUpdate(rgName, publicIPAddressName1, publicIPAddress)
                     .WaitForCompletionAsync()).Value;

                Utilities.Log("Created a public IP address");
                // Print public IP address details
                Utilities.PrintIPAddress(publicIPAddress);

                // Use the pre-created public IP for the new VM

                Utilities.Log("Creating a Virtual Network");

                var vnet = new VirtualNetwork
                {
                    Location = "eastus",
                    AddressSpace = new AddressSpace { AddressPrefixes = new List<string>() { "10.0.0.0/16" } },
                    Subnets = new List<Subnet>
                    {
                        new Subnet
                        {
                            Name = "mySubnet",
                            AddressPrefix = "10.0.0.0/28",
                        }
                    },
                };

                vnet = await virtualNetworks.StartCreateOrUpdate(rgName, vmName + "_vent", vnet).WaitForCompletionAsync();

                Utilities.Log("Created a Virtual Network");

                Utilities.Log("Creating a Network Interface");

                var networkInterface = new NetworkInterface()
                {
                    Location = "eastus",
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                    {
                        new NetworkInterfaceIPConfiguration()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddress() { Id = publicIPAddress.Id }
                        }
                    }
                };

                networkInterface = await (await networkInterfaces.StartCreateOrUpdateAsync(rgName, networkInterfaceName, networkInterface)).WaitForCompletionAsync();

                Utilities.Log("Created a Network Interface");

                Utilities.Log("Creating a Windows VM");
                var t1 = DateTime.UtcNow;

                var vm = new VirtualMachine("eastus")
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterfaceReference() { Id = networkInterface.Id }
                        }
                    },
                    OsProfile = new OSProfile
                    {
                        ComputerName = vmName,
                        AdminUsername = UserName,
                        AdminPassword = Password,
                    },
                    StorageProfile = new StorageProfile()
                    {
                        ImageReference = new ImageReference()
                        {
                            Offer = "WindowsServer",
                            Publisher = "MicrosoftWindowsServer",
                            Sku = "2016-Datacenter",
                            Version = "latest"
                        },
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                vm = (await (await virtualMachines.StartCreateOrUpdateAsync(rgName, vmName, vm)).WaitForCompletionAsync()).Value;

                var t2 = DateTime.UtcNow;
                Utilities.Log("Created VM: (took " + (t2 - t1).TotalSeconds + " seconds) " + vm.Id);
                // Print virtual machine details
                Utilities.PrintVirtualMachine(vm);

                //============================================================
                // Gets the public IP address associated with the VM's primary NIC

                Utilities.Log("Public IP address associated with the VM's primary NIC [After create]");
                // Print the public IP address details

                var publicIPDetail = (await publicIPAddresses.GetAsync(rgName, publicIPAddressName1)).Value;

                Utilities.PrintIPAddress(publicIPDetail);

                //============================================================
                // Assign a new public IP address for the VM

                // Define a new public IP address

                var publicIPAddress2 = new PublicIPAddress
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = "eastus",
                    DnsSettings = new PublicIPAddressDnsSettings
                    {
                        DomainNameLabel = publicIPAddressLeafDNS2
                    }
                };

                publicIPAddress2 = (await publicIPAddresses.StartCreateOrUpdate(rgName, publicIPAddressName2, publicIPAddress2)
                     .WaitForCompletionAsync()).Value;

                // Update VM's primary NIC to use the new public IP address

                Utilities.Log("Updating the VM's primary NIC with new public IP address");

                networkInterface = new NetworkInterface()
                {
                    Location = "eastus",
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                    {
                        new NetworkInterfaceIPConfiguration()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddress() { Id = publicIPAddress2.Id }
                        }
                    }
                };

                await (await networkInterfaces.StartCreateOrUpdateAsync(rgName, networkInterfaceName, networkInterface)).WaitForCompletionAsync();

                //============================================================
                // Gets the updated public IP address associated with the VM

                // Get the associated public IP address for a virtual machine
                Utilities.Log("Public IP address associated with the VM's primary NIC [After Update]");

                publicIPDetail = (await publicIPAddresses.GetAsync(rgName, publicIPAddressName2)).Value;

                Utilities.PrintIPAddress(publicIPDetail);

                //============================================================
                // Remove public IP associated with the VM

                Utilities.Log("Removing public IP address associated with the VM");

                networkInterface = new NetworkInterface()
                {
                    Location = "eastus",
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                    {
                        new NetworkInterfaceIPConfiguration()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };

                await (await networkInterfaces.StartCreateOrUpdateAsync(rgName, networkInterfaceName, networkInterface)).WaitForCompletionAsync();

                Utilities.Log("Removed public IP address associated with the VM");

                //============================================================
                // Delete the public ip
                Utilities.Log("Deleting the public IP address");
                await (await publicIPAddresses.StartDeleteAsync(rgName, publicIPAddressName1)).WaitForCompletionAsync();
                Utilities.Log("Deleted the public IP address");
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
