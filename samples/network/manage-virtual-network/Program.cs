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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManageVirtualNetwork
{
    public class Program
    {
        // Azure Network sample for managing virtual networks.
        //  - Create a virtual network with Subnets
        //  - Update a virtual network
        //  - Create virtual machines in the virtual network subnets
        //  - Create another virtual network
        //  - List virtual networks
        public static async Task RunSample()
        {
            const string ResourceGroupName = "rgNEMV";
            const string VnetName1 = "vnet1";
            const string VnetName2 = "vnet2";
            const string FrontEndVmName = "fevm";
            const string BackEndVmName = "bevm";
            const string VNet1FrontEndSubnetName = "frontend";
            const string VNet1BackEndSubnetName = "backend";
            const string VNet1FrontEndSubnetNsgName = "frontendnsg";
            const string VNet1BackEndSubnetNsgName = "backendnsg";
            const string UserName = "tirekicker";
            const string SshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+wWK73dCr+jgQOAxNsHAnNNNMEMWOHYEccp6wJm2gotpr9katuF/ZAdou5AaW1C61slRkHRkpRRX9FA9CYBiitZgvCCz+3nWNN7l/Up54Zps/pHWGZLHNJZRYyAB6j5yVLMVHIHriY49d/GZTZVNB8GoJv9Gakwc/fuEZYYl4YDFiGMBP///TzlI4jhiJzjKnEvqPFki5p2ZRJqcbCiF4pJrxUQR/RXqVFQdbRLZgYfJ8xGB878RENq3yQ39d8dVOkq4edbkzwcUmwwwkYVPIoDGsYLaRHnG+To7FvMeyO7xDVQkMKzopTQV8AuKpyvpqu0a9pWOMaiCyDytO7GGN you@me.com";
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

            // Create a virtual network with specific address-space and two subnet

            // Creates a network security group for backend subnet
            Console.WriteLine("Creating a network security group for virtual network backend subnet...");
            var networkSecurityGroupData = new NetworkSecurityGroupData()
            {
                Location = location,
                SecurityRules = {
                    new SecurityRuleData
                    {
                        Name = "DenyInternetInComing",
                        Priority = 700,
                        Access = SecurityRuleAccess.Deny,
                        Direction = SecurityRuleDirection.Inbound,
                        SourceAddressPrefix = "Internet",
                        SourcePortRange = "*",
                        DestinationAddressPrefix = "*",
                        DestinationPortRange = "*",
                        Protocol = SecurityRuleProtocol.Asterisk
                    },
                    new SecurityRuleData
                    {
                        Name = "DenyInternetOutGoing",
                        Priority = 701,
                        Access = SecurityRuleAccess.Deny,
                        Direction = SecurityRuleDirection.Outbound,
                        SourceAddressPrefix = "*",
                        SourcePortRange = "*",
                        DestinationAddressPrefix = "*",
                        DestinationPortRange = "*",
                        Protocol = SecurityRuleProtocol.Asterisk
                    }
                }
            };

            var networkSecurityGroupContainer = resourceGroup.GetNetworkSecurityGroups();
            var backendSubnetNsg = (await networkSecurityGroupContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, VNet1BackEndSubnetNsgName, networkSecurityGroupData)).Value;
            Console.WriteLine($"Created backend network security group {backendSubnetNsg.Id}");

            Console.WriteLine("Creating a network security group for virtual network frontend subnet...");
            networkSecurityGroupData = new NetworkSecurityGroupData()
            {
                Location = location,
                SecurityRules = {
                    new SecurityRuleData
                    {
                        Name = "AllowHttpInComing",
                        Priority = 700,
                        Access = SecurityRuleAccess.Allow,
                        Direction = SecurityRuleDirection.Inbound,
                        SourceAddressPrefix = "Internet",
                        SourcePortRange = "*",
                        DestinationAddressPrefix = "*",
                        DestinationPortRange = "80",
                        Protocol = SecurityRuleProtocol.Tcp
                    },
                    new SecurityRuleData
                    {
                        Name = "DenyInternetOutGoing",
                        Priority = 701,
                        Access = SecurityRuleAccess.Deny,
                        Direction = SecurityRuleDirection.Outbound,
                        SourceAddressPrefix = "*",
                        SourcePortRange = "*",
                        DestinationAddressPrefix = "INTERNET",
                        DestinationPortRange = "*",
                        Protocol = SecurityRuleProtocol.Asterisk
                    }
                }
            };

            var frontendSubnetNsg = (await networkSecurityGroupContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, VNet1FrontEndSubnetNsgName, networkSecurityGroupData)).Value;
            Console.WriteLine($"Created frontend network security group {frontendSubnetNsg.Id}");

            Console.WriteLine("Creating virtual network #1...");
            var virtualNetworkData = new VirtualNetworkData
            {
                Location = location,
                AddressPrefixes =
                {
                    "192.168.0.0/16"
                },
                Subnets = {
                    new SubnetData
                    {
                        Name = VNet1FrontEndSubnetName,
                        AddressPrefix = "192.168.1.0/24",
                    },
                    new SubnetData
                    {
                        Name = VNet1BackEndSubnetName,
                        AddressPrefix = "192.168.2.0/24",
                        NetworkSecurityGroup = backendSubnetNsg.Data
                    }
                }
            };
            var virtualNetworkContainer = resourceGroup.GetVirtualNetworks();
            var virtualNetwork1 = (await virtualNetworkContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, VnetName1, virtualNetworkData)).Value;
            Console.WriteLine($"Created a virtual network {virtualNetwork1.Id}");

            // Update a virtual network
            // Update the virtual network frontend subnet by associating it with network security group
            Console.WriteLine("Associating network security group rule to frontend subnet");

            virtualNetworkData = new VirtualNetworkData
            {
                Location = location,
                AddressPrefixes = { "192.168.0.0/16" },
                Subnets = {
                    new SubnetData
                    {
                        Name = VNet1FrontEndSubnetName,
                        AddressPrefix = "192.168.1.0/24",
                        NetworkSecurityGroup = frontendSubnetNsg.Data
                    },
                    new SubnetData
                    {
                        Name = VNet1BackEndSubnetName,
                        AddressPrefix = "192.168.2.0/24",
                        NetworkSecurityGroup = backendSubnetNsg.Data
                    }
                }
            };
            var virtualNetwork2 = (await virtualNetworkContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, VnetName1, virtualNetworkData)).Value;
            Console.WriteLine("Network security group rule associated with the frontend subnet");

            Console.WriteLine("Creating Public IP Address #1...");

            var ipAddressData = new PublicIPAddressData
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = location,
            };

            var ipAddress = (await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(Azure.WaitUntil.Completed, FrontEndVmName + "_ip", ipAddressData)).Value;

            Console.WriteLine("Creating Network Interface #1...");
            var networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData
                        {
                            Id = virtualNetwork1.Data.Subnets.First(s => s.Name == VNet1FrontEndSubnetName).Id
                        },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData { Id = ipAddress.Id }
                    }
                }
            };
            var networkInterfaceContainer = resourceGroup.GetNetworkInterfaces();
            var networkInterface = (await networkInterfaceContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, FrontEndVmName + "_nic", networkInterfaceData)).Value;
            Console.WriteLine("Created Network Interface #1...");

            // Create a virtual machine in each subnet
            // Creates the first virtual machine in frontend subnet
            Console.WriteLine("Creating a Linux virtual machine in the frontend subnet");
            var t1 = DateTime.UtcNow;
            var virtualMachineData = new VirtualMachineData(location)
            {
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference { Id = networkInterface.Id }
                    }
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = FrontEndVmName,
                    AdminUsername = UserName,
                    LinuxConfiguration = new LinuxConfiguration
                    {
                        DisablePasswordAuthentication = true,
                        SshPublicKeys =
                        {
                            new SshPublicKeyConfiguration()
                        {
                            Path = $"/home/{UserName}/.ssh/authorized_keys",
                            KeyData = SshKey
                        }
                        }
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    ImageReference = new ImageReference
                    {
                        Offer = "UbuntuServer",
                        Publisher = "Canonical",
                        Sku = "16.04-LTS",
                        Version = "latest"
                    }
                },
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardD3V2
                }
            };

            var virtualMachineContainer = resourceGroup.GetVirtualMachines();
            var frontendVM = (await virtualMachineContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, FrontEndVmName, virtualMachineData)).Value;
            var t2 = DateTime.UtcNow;
            Console.WriteLine($"Created Linux VM: (took {(t2 - t1).TotalSeconds} seconds) {frontendVM.Id}");

            // Create a virtual network with default address-space and one default subnet
            Console.WriteLine("Creating Network Interface #2...");
            networkInterfaceData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData
                        {
                            Id = virtualNetwork1.Data.Subnets.First(s => s.Name == VNet1BackEndSubnetName).Id
                        },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    }
                }
            };
            var networkInterface2 = (await networkInterfaceContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, BackEndVmName + "_nic", networkInterfaceData)).Value;
            Console.WriteLine("Created Network Interface #2...");

            // Creates the second virtual machine in the backend subnet
            Console.WriteLine("Creating a Linux virtual machine in the backend subnet");
            t1 = DateTime.UtcNow;
            virtualMachineData = new VirtualMachineData(location)
            {
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference { Id = networkInterface2.Id }
                    }
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = FrontEndVmName,
                    AdminUsername = UserName,
                    LinuxConfiguration = new LinuxConfiguration
                    {
                        DisablePasswordAuthentication = true,
                        SshPublicKeys =
                        {
                            new SshPublicKeyConfiguration()
                        {
                            Path = $"/home/{UserName}/.ssh/authorized_keys",
                            KeyData = SshKey
                        }
                        }
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    ImageReference = new ImageReference
                    {
                        Offer = "UbuntuServer",
                        Publisher = "Canonical",
                        Sku = "16.04-LTS",
                        Version = "latest"
                    },
                },
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardD3V2
                }
            };

            var backendVM = (await virtualMachineContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, BackEndVmName, virtualMachineData)).Value;

            var t3 = DateTime.UtcNow;
            Console.WriteLine($"Created Linux VM: (took {(t3 - t1).TotalSeconds} seconds) {backendVM.Id}");

            Console.WriteLine("Creating a virtual network #2");
            var virtualNetwork3 = (await virtualNetworkContainer.CreateOrUpdateAsync(
                Azure.WaitUntil.Completed, VnetName2,
                new VirtualNetworkData
                {
                    Location = location,
                    AddressPrefixes =
                    {
                        "10.0.0.0/16"
                    }
                })).Value;

            Console.WriteLine("Created a virtual network #2");

            // Delete a virtual network
            Console.WriteLine("Deleting the virtual network");
            await virtualNetwork3.DeleteAsync(Azure.WaitUntil.Completed);
            Console.WriteLine("Deleted the virtual network");

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
