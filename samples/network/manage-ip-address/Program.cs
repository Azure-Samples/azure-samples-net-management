// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManageIPAddress
{
    public class Program
    {
        //Azure Network sample for managing IP address -
        //   - Assign a public IP address for a virtual machine during its creation
        //   - Assign a public IP address for a virtual machine through an virtual machine update action
        //   - Get the associated public IP address for a virtual machine
        //   - Get the assigned public IP address for a virtual machine
        //   - Remove a public IP address from a virtual machine.


        public static async Task RunSample()
        {
            const string UserName = "tirekicker";
            const string Password = "<password>"; // replace with a password following the policy
            const string PublicIPAddressName1 = "pip1";
            const string PublicIPAddressName2 = "pip2";
            const string PublicIPAddressLeafDNS1 = "pipdns1";
            const string PublicIPAddressLeafDNS2 = "pipdns2";
            const string NetworkInterfaceName = "nic";
            const string VmName = "vm";
            const string ResourceGroupName = "rgNEMP";
            const string Location = "eastus";

            // create an ArmClient as the entry of all resource management API
            var armClient = new ArmClient(new DefaultAzureCredential());

            // implicit conversion from Resource<ResourceGroup> to ResourceGroup, similar cases can be found below
            ResourceGroup resourceGroup = await armClient.DefaultSubscription.GetResourceGroups().CreateOrUpdate(ResourceGroupName, new ResourceGroupData(Location)).WaitForCompletionAsync();

            // Assign a public IP address for a VM during its creation

            // Define a public IP address to be used during VM creation time

            Console.WriteLine("Creating a public IP address...");
            var publicIPAddressData = new PublicIPAddressData
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                Location = Location,
                DnsSettings = new PublicIPAddressDnsSettings
                {
                    DomainNameLabel = PublicIPAddressLeafDNS1
                }
            };

            var publicIPAddressContainer = resourceGroup.GetPublicIPAddresses();
            PublicIPAddress publicIPAddress = await publicIPAddressContainer.CreateOrUpdate(PublicIPAddressName1, publicIPAddressData).WaitForCompletionAsync();
            Console.WriteLine($"Created a public IP address {publicIPAddress.Id}");

            // Use the pre-created public IP for the new VM
            Console.WriteLine("Creating a Virtual Network");
            var vnetData = new VirtualNetworkData
            {
                Location = Location,
                AddressSpace = new AddressSpace { AddressPrefixes = { "10.0.0.0/16" } },
                Subnets =
                {
                    new SubnetData
                    {
                        Name = "mySubnet",
                        AddressPrefix = "10.0.0.0/28",
                    }
                },
            };

            var virtualNetworkContainer = resourceGroup.GetVirtualNetworks();
            VirtualNetwork vnet = await virtualNetworkContainer.CreateOrUpdate($"{VmName}_vent", vnetData).WaitForCompletionAsync();
            Console.WriteLine($"Created a Virtual Network {vnet.Id}");

            Console.WriteLine("Creating a Network Interface");
            var networkInterfaceData = new NetworkInterfaceData()
            {
                Location = Location,
                IpConfigurations =
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = publicIPAddress.Id }
                    }
                }
            };

            var networkInterfaceContainer = resourceGroup.GetNetworkInterfaces();
            NetworkInterface networkInterface = await (await networkInterfaceContainer.CreateOrUpdateAsync(NetworkInterfaceName, networkInterfaceData)).WaitForCompletionAsync();
            Console.WriteLine($"Created a Network Interface {networkInterface.Id}");

            Console.WriteLine("Creating a Windows VM");
            var t1 = DateTime.UtcNow;

            var vmData = new VirtualMachineData(Location)
            {
                NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                {
                    NetworkInterfaces =
                    {
                        new NetworkInterfaceReference() { Id = networkInterface.Id }
                    }
                },
                OsProfile = new OSProfile
                {
                    ComputerName = VmName,
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
                },
                HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
            };

            var virtualMachineContainer = resourceGroup.GetVirtualMachines();
            VirtualMachine vm = await virtualMachineContainer.CreateOrUpdate(VmName, vmData).WaitForCompletionAsync();

            var t2 = DateTime.UtcNow;
            Console.WriteLine($"Created VM: (took {(t2 - t1).TotalSeconds} seconds) {vmData.Id}");

            // Assign a new public IP address for the VM
            // Define a new public IP address
            Console.WriteLine("Creating a public IP address...");
            var publicIPAddressData2 = new PublicIPAddressData
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                Location = Location,
                DnsSettings = new PublicIPAddressDnsSettings
                {
                    DomainNameLabel = PublicIPAddressLeafDNS2
                }
            };

            PublicIPAddress publicIPAddress2 = await publicIPAddressContainer.CreateOrUpdate(PublicIPAddressName2, publicIPAddressData2)
                    .WaitForCompletionAsync();
            Console.WriteLine($"Created a public IP address {publicIPAddress.Id}");

            // Update VM's primary NIC to use the new public IP address
            Console.WriteLine("Updating the VM's primary NIC with new public IP address");
            networkInterfaceData = new NetworkInterfaceData
            {
                Location = Location,
                IpConfigurations =
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = publicIPAddress2.Id }
                    }
                }
            };

            await networkInterfaceContainer.CreateOrUpdate(NetworkInterfaceName, networkInterfaceData).WaitForCompletionAsync();
            Console.WriteLine("New public IP address associated with the VM's primary NIC");

            // Remove public IP associated with the VM
            Console.WriteLine("Removing public IP address associated with the VM");
            networkInterfaceData = new NetworkInterfaceData
            {
                Location = Location,
                IpConfigurations =
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                    }
                }
            };

            await networkInterfaceContainer.CreateOrUpdate(NetworkInterfaceName, networkInterfaceData).WaitForCompletionAsync();
            Console.WriteLine("Removed public IP address associated with the VM");

            // Delete the public ip
            Console.WriteLine("Deleting the public IP addresses");
            await publicIPAddress.Delete().WaitForCompletionResponseAsync();
            Console.WriteLine("Deleted the public IP addresses");

            // Delete resource group if necessary
            // await resourceGroup.Delete().WaitForCompletionResponseAsync();
        }

        public static async Task Main(string[] args)
        {
            try
            {
                await RunSample();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
