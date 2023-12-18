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
            var location = AzureLocation.EastUS;

            // create an ArmClient as the entry of all resource management API
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
            ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            ArmClient client = new ArmClient(credential, subscription);

            // implicit conversion from Resource<ResourceGroup> to ResourceGroup, similar cases can be found below
            ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, ResourceGroupName, new ResourceGroupData(location))).Value;

            // Assign a public IP address for a VM during its creation

            // Define a public IP address to be used during VM creation time

            Console.WriteLine("Creating a public IP address...");
            var publicIPAddressData = new PublicIPAddressData()
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = location,
                DnsSettings = new PublicIPAddressDnsSettings
                {
                    DomainNameLabel = PublicIPAddressLeafDNS1
                }
            };

            var publicIPAddressContainer = resourceGroup.GetPublicIPAddresses();
            var publicIPAddress = (await publicIPAddressContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, PublicIPAddressName1, publicIPAddressData)).Value;
            Console.WriteLine($"Created a public IP address {publicIPAddress.Id}");

            // Use the pre-created public IP for the new VM
            Console.WriteLine("Creating a Virtual Network");
            var vnetData = new VirtualNetworkData()
            {
                Location = location,
                AddressPrefixes =
                {
                    "10.0.0.0/16"
                },
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
            var vnet = (await virtualNetworkContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"{VmName}_vent", vnetData)).Value;
            Console.WriteLine($"Created a Virtual Network {vnet.Id}");

            Console.WriteLine("Creating a Network Interface");
            var networkInterfaceData = new NetworkInterfaceData()
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = publicIPAddress.Id }
                    }
                }
            };

            var networkInterfaceContainer = resourceGroup.GetNetworkInterfaces();
            var networkInterface = (await networkInterfaceContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, NetworkInterfaceName, networkInterfaceData)).Value;
            Console.WriteLine($"Created a Network Interface {networkInterface.Id}");

            Console.WriteLine("Creating a Windows VM");
            var t1 = DateTime.UtcNow;

            var vmData = new VirtualMachineData(location)
            {
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference() { Id = networkInterface.Id }
                    }
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = VmName,
                    AdminUsername = UserName,
                    AdminPassword = Password,
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    ImageReference = new ImageReference()
                    {
                        Offer = "WindowsServer",
                        Publisher = "MicrosoftWindowsServer",
                        Sku = "2016-Datacenter",
                        Version = "latest"
                    },
                },
                HardwareProfile = new VirtualMachineHardwareProfile() { VmSize = VirtualMachineSizeType.StandardD3V2 },
            };

            var virtualMachineContainer = resourceGroup.GetVirtualMachines();
            var vm = (await virtualMachineContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, VmName, vmData)).Value;

            var t2 = DateTime.UtcNow;
            Console.WriteLine($"Created VM: (took {(t2 - t1).TotalSeconds} seconds) {vmData.Id}");

            // Assign a new public IP address for the VM
            // Define a new public IP address
            Console.WriteLine("Creating a public IP address...");
            var publicIPAddressData2 = new PublicIPAddressData()
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = location,
                DnsSettings = new PublicIPAddressDnsSettings
                {
                    DomainNameLabel = PublicIPAddressLeafDNS2
                }
            };

            var publicIPAddress2 = (await publicIPAddressContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, PublicIPAddressName2, publicIPAddressData2)).Value;
            Console.WriteLine($"Created a public IP address {publicIPAddress.Id}");

            // Update VM's primary NIC to use the new public IP address
            Console.WriteLine("Updating the VM's primary NIC with new public IP address");
            networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = publicIPAddress2.Id }
                    }
                }
            };
            var networkInterfaceUpdate = (await networkInterfaceContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, NetworkInterfaceName, networkInterfaceData)).Value;
            Console.WriteLine("New public IP address associated with the VM's primary NIC");

            // Remove public IP associated with the VM
            Console.WriteLine("Removing public IP address associated with the VM");
            networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    }
                }
            };

            var networkInterFaceRemove = (await networkInterfaceContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, NetworkInterfaceName, networkInterfaceData)).Value;
            Console.WriteLine("Removed public IP address associated with the VM");

            // Delete the public ip
            Console.WriteLine("Deleting the public IP addresses");
            await publicIPAddress.DeleteAsync(Azure.WaitUntil.Completed);
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
