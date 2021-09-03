// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
            const string resourceGroupName = "rgNEMV";
            const string vnetName1 = "vnet1";
            const string vnetName2 = "vnet2";
            const string frontEndVmName = "fevm";
            const string backEndVmName = "bevm";
            const string VNet1FrontEndSubnetName = "frontend";
            const string VNet1BackEndSubnetName = "backend";
            const string VNet1FrontEndSubnetNsgName = "frontendnsg";
            const string VNet1BackEndSubnetNsgName = "backendnsg";
            const string UserName = "tirekicker";
            const string SshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+wWK73dCr+jgQOAxNsHAnNNNMEMWOHYEccp6wJm2gotpr9katuF/ZAdou5AaW1C61slRkHRkpRRX9FA9CYBiitZgvCCz+3nWNN7l/Up54Zps/pHWGZLHNJZRYyAB6j5yVLMVHIHriY49d/GZTZVNB8GoJv9Gakwc/fuEZYYl4YDFiGMBP///TzlI4jhiJzjKnEvqPFki5p2ZRJqcbCiF4pJrxUQR/RXqVFQdbRLZgYfJ8xGB878RENq3yQ39d8dVOkq4edbkzwcUmwwwkYVPIoDGsYLaRHnG+To7FvMeyO7xDVQkMKzopTQV8AuKpyvpqu0a9pWOMaiCyDytO7GGN you@me.com";


            // create an ArmClient as the entry of all resource management API
            ArmClient armClient = new ArmClient(new DefaultAzureCredential());

            ResourceGroup resourceGroup = null;
            try
            {
                resourceGroup = await armClient.DefaultSubscription.GetResourceGroups().CreateOrUpdate(resourceGroupName, new ResourceGroupData("eastus")).WaitForCompletionAsync();

                // Create a virtual network with specific address-space and two subnet

                // Creates a network security group for backend subnet
                Console.WriteLine("Creating a network security group for virtual network backend subnet...");
                var networkSecurityGroupData = new NetworkSecurityGroupData
                {
                    Location = "eastus",
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
                NetworkSecurityGroup backendSubnetNsg = await networkSecurityGroupContainer.CreateOrUpdate(VNet1BackEndSubnetNsgName, networkSecurityGroupData).WaitForCompletionAsync();
                Console.WriteLine($"Created backend network security group {backendSubnetNsg.Id}");

                Console.WriteLine("Creating a network security group for virtual network frontend subnet...");
                networkSecurityGroupData = new NetworkSecurityGroupData
                {
                    Location = "eastus",
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

                NetworkSecurityGroup frontendSubnetNsg = await networkSecurityGroupContainer.CreateOrUpdate(VNet1FrontEndSubnetNsgName, networkSecurityGroupData).WaitForCompletionAsync();
                Console.WriteLine($"Created frontend network security group {frontendSubnetNsg.Id}");

                Console.WriteLine("Creating virtual network #1...");
                var virtualNetworkData = new VirtualNetworkData
                {
                    Location = "eastus",
                    AddressSpace = new AddressSpace { AddressPrefixes = { "192.168.0.0/16" } },
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
                VirtualNetwork virtualNetwork1 = await virtualNetworkContainer.CreateOrUpdate(vnetName1, virtualNetworkData).WaitForCompletionAsync();
                Console.WriteLine($"Created a virtual network {virtualNetwork1.Id}");

                // Update a virtual network
                // Update the virtual network frontend subnet by associating it with network security group
                Console.WriteLine("Associating network security group rule to frontend subnet");

                virtualNetworkData = new VirtualNetworkData
                {
                    Location = "eastus",
                    AddressSpace = new AddressSpace { AddressPrefixes = { "192.168.0.0/16" } },
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
                virtualNetwork1 = await virtualNetworkContainer.CreateOrUpdate(vnetName1, virtualNetworkData).WaitForCompletionAsync();
                Console.WriteLine("Network security group rule associated with the frontend subnet");

                Console.WriteLine("Creating Public IP Address #1...");

                var ipAddressData = new PublicIPAddressData
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = "eastus",
                };

                PublicIPAddress ipAddress = await resourceGroup.GetPublicIPAddresses().CreateOrUpdate(frontEndVmName + "_ip", ipAddressData).WaitForCompletionAsync();

                Console.WriteLine("Creating Network Interface #1...");
                var networkInterfaceData = new NetworkInterfaceData
                {
                    Location = "eastus",
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Id = virtualNetwork1.Data.Subnets.First(s => s.Name == VNet1FrontEndSubnetName).Id
                            },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddressData { Id = ipAddress.Id }
                        }
                    }
                };
                var networkInterfaceContainer = resourceGroup.GetNetworkInterfaces();
                NetworkInterface networkInterface = await networkInterfaceContainer.CreateOrUpdate(frontEndVmName + "_nic", networkInterfaceData).WaitForCompletionAsync();
                Console.WriteLine("Created Network Interface #1...");

                // Create a virtual machine in each subnet
                // Creates the first virtual machine in frontend subnet
                Console.WriteLine("Creating a Linux virtual machine in the frontend subnet");
                var t1 = DateTime.UtcNow;
                var virtualMachineData = new VirtualMachineData("eastus")
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces =
                        {
                            new NetworkInterfaceReference { Id = networkInterface.Id }
                        }
                    },
                    OsProfile = new OSProfile
                    {
                        ComputerName = frontEndVmName,
                        AdminUsername = UserName,
                        LinuxConfiguration = new LinuxConfiguration
                        {
                            DisablePasswordAuthentication = true,
                            Ssh = new SshConfiguration
                            {
                                PublicKeys =
                                {
                                    new SshPublicKeyInfo
                                    {
                                       Path = $"/home/{UserName}/.ssh/authorized_keys",
                                       KeyData = SshKey
                                    }
                                }
                            }
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
                        }
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = VirtualMachineSizeTypes.StandardD3V2
                    }
                };

                var virtualMachineContainer = resourceGroup.GetVirtualMachines();
                VirtualMachine frontendVM = await virtualMachineContainer.CreateOrUpdate(frontEndVmName, virtualMachineData).WaitForCompletionAsync();
                var t2 = DateTime.UtcNow;
                Console.WriteLine($"Created Linux VM: (took {(t2 - t1).TotalSeconds} seconds) {frontendVM.Id}");

                // Create a virtual network with default address-space and one default subnet
                Console.WriteLine("Creating Network Interface #2...");
                networkInterfaceData = new NetworkInterfaceData
                {
                    Location = "eastus",
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Id = virtualNetwork1.Data.Subnets.First(s => s.Name == VNet1BackEndSubnetName).Id
                            },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                networkInterface = await networkInterfaceContainer.CreateOrUpdate(backEndVmName + "_nic", networkInterfaceData).WaitForCompletionAsync();
                Console.WriteLine("Created Network Interface #2...");

                // Creates the second virtual machine in the backend subnet
                Console.WriteLine("Creating a Linux virtual machine in the backend subnet");
                t1 = DateTime.UtcNow;
                virtualMachineData = new VirtualMachineData("eastus")
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces =
                        {
                            new NetworkInterfaceReference { Id = networkInterface.Id }
                        }
                    },
                    OsProfile = new OSProfile
                    {
                        ComputerName = frontEndVmName,
                        AdminUsername = UserName,
                        LinuxConfiguration = new LinuxConfiguration
                        {
                            DisablePasswordAuthentication = true,
                            Ssh = new SshConfiguration
                            {
                                PublicKeys =
                                {
                                    new SshPublicKeyInfo
                                    {
                                       Path = $"/home/{UserName}/.ssh/authorized_keys",
                                        KeyData = SshKey
                                    }
                                }
                            }
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
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = VirtualMachineSizeTypes.StandardD3V2
                    }
                };

                VirtualMachine backendVM = await virtualMachineContainer.CreateOrUpdate(backEndVmName, virtualMachineData).WaitForCompletionAsync();

                var t3 = DateTime.UtcNow;
                Console.WriteLine($"Created Linux VM: (took {(t3 - t1).TotalSeconds} seconds) {backendVM.Id}");

                Console.WriteLine("Creating a virtual network #2");
                VirtualNetwork virtualNetwork2 = await virtualNetworkContainer.CreateOrUpdate(
                    vnetName2,
                    new VirtualNetworkData
                    {
                        Location = "eastus",
                        AddressSpace = new AddressSpace
                        {
                            AddressPrefixes = { "10.0.0.0/16" }
                        }
                    }).WaitForCompletionAsync();

                Console.WriteLine("Created a virtual network #2");

                // Delete a virtual network
                Console.WriteLine("Deleting the virtual network");
                await virtualNetwork2.Delete().WaitForCompletionResponseAsync();
                Console.WriteLine("Deleted the virtual network");
            }
            finally
            {
                if (resourceGroup != null)
                {
                    try
                    {
                        await resourceGroup.Delete().WaitForCompletionResponseAsync();
                    }
                    catch (NullReferenceException)
                    {
                        Console.WriteLine("Did not create any resources in Azure. No clean up is necessary");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
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
