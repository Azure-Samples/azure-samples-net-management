// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManageVirtualNetwork
{
    public class Program
    {
        //Azure Network sample for managing virtual networks.
        //  - Create a virtual network with Subnets
        //  - Update a virtual network
        //  - Create virtual machines in the virtual network subnets
        //  - Create another virtual network
        //  - List virtual networks

        private static readonly string SubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        private static readonly string VNet1FrontEndSubnetName = "frontend";
        private static readonly string VNet1BackEndSubnetName = "backend";
        private static readonly string VNet1FrontEndSubnetNsgName = "frontendnsg";
        private static readonly string VNet1BackEndSubnetNsgName = "backendnsg";
        private static readonly string UserName = "tirekicker";
        private static readonly string SshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQCfSPC2K7LZcFKEO+/t3dzmQYtrJFZNxOsbVgOVKietqHyvmYGHEC0J2wPdAqQ/63g/hhAEFRoyehM+rbeDri4txB3YFfnOK58jqdkyXzupWqXzOrlKY4Wz9SKjjN765+dqUITjKRIaAip1Ri137szRg71WnrmdP3SphTRlCx1Bk2nXqWPsclbRDCiZeF8QOTi4JqbmJyK5+0UqhqYRduun8ylAwKKQJ1NJt85sYIHn9f1Rfr6Tq2zS0wZ7DHbZL+zB5rSlAr8QyUdg/GQD+cmSs6LvPJKL78d6hMGk84ARtFo4A79ovwX/Fj01znDQkU6nJildfkaolH2rWFG/qttD azjava@javalib.Com";
        private static readonly string ResourceGroupName = Utilities.RandomResourceName("rgNEMV", 24);

        public static async Task RunSample(TokenCredential credential)
        {
            string vnetName1 = Utilities.RandomResourceName("vnet1", 20);
            string vnetName2 = Utilities.RandomResourceName("vnet2", 20);
            string frontEndVmName = Utilities.RandomResourceName("fevm", 24);
            string backEndVmName = Utilities.RandomResourceName("bevm", 24);
            string publicIpAddressLeafDnsForFrontEndVm = Utilities.RandomResourceName("pip1", 24);

            var networkManagementClient = new NetworkManagementClient(SubscriptionId, credential);
            var networkSecurityGroups = networkManagementClient.NetworkSecurityGroups;
            var virtualNetworks = networkManagementClient.VirtualNetworks;
            var publicIPAddresses = networkManagementClient.PublicIPAddresses;
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            var computeManagementClient = new ComputeManagementClient(SubscriptionId, credential);
            var virtualMachines = computeManagementClient.VirtualMachines;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(ResourceGroupName, "eastus");

                // Create a virtual network with specific address-space and two subnet

                // Creates a network security group for backend subnet

                Utilities.Log("Creating a network security group for virtual network backend subnet...");
                Utilities.Log("Creating a network security group for virtual network frontend subnet...");

                var networkSecurityGroupParameters = new NetworkSecurityGroup
                {
                    Location = "eastus",
                    SecurityRules = new[]
                    {
                        new SecurityRule
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
                        new SecurityRule
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
                var rawNetworkSecurityGroupResult = await networkSecurityGroups.StartCreateOrUpdateAsync(ResourceGroupName, VNet1BackEndSubnetNsgName, networkSecurityGroupParameters);
                var backEndSubnetNsg = (await rawNetworkSecurityGroupResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created backend network security group");
                // Print the network security group
                Utilities.PrintNetworkSecurityGroup(backEndSubnetNsg);

                networkSecurityGroupParameters = new NetworkSecurityGroup
                {
                    Location = "eastus",
                    SecurityRules = new[]
                    {
                        new SecurityRule
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
                        new SecurityRule
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

                rawNetworkSecurityGroupResult = await networkSecurityGroups.StartCreateOrUpdateAsync(ResourceGroupName, VNet1FrontEndSubnetNsgName, networkSecurityGroupParameters);
                var frontEndSubnetNsg = (await rawNetworkSecurityGroupResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created frontend network security group");
                // Print the network security group
                Utilities.PrintNetworkSecurityGroup(frontEndSubnetNsg);

                Utilities.Log("Creating virtual network #1...");

                var virtualNetworkParameters = new VirtualNetwork
                {
                    Location = "eastus",
                    AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "192.168.0.0/16" } },
                    Subnets = new List<Subnet>
                    {
                        new Subnet
                        {
                            Name = VNet1FrontEndSubnetName,
                            AddressPrefix = "192.168.1.0/24",
                        },
                        new Subnet
                        {
                            Name = VNet1BackEndSubnetName,
                            AddressPrefix = "192.168.2.0/24",
                            NetworkSecurityGroup = backEndSubnetNsg
                        }
                    }
                };
                var rawVirtualNetworkResult = await virtualNetworks.StartCreateOrUpdateAsync(ResourceGroupName, vnetName1, virtualNetworkParameters);
                var virtualNetwork1 = (await rawVirtualNetworkResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created a virtual network");
                // Print the virtual network details
                Utilities.PrintVirtualNetwork(virtualNetwork1);

                // Update a virtual network

                // Update the virtual network frontend subnet by associating it with network security group

                Utilities.Log("Associating network security group rule to frontend subnet");

                virtualNetworkParameters = new VirtualNetwork
                {
                    Location = "eastus",
                    AddressSpace = new AddressSpace
                    {
                        AddressPrefixes = new List<string> { "192.168.0.0/16" }
                    },
                    Subnets = new List<Subnet>
                    {
                        new Subnet
                        {
                            Name = VNet1FrontEndSubnetName,
                            AddressPrefix = "192.168.1.0/24",
                            NetworkSecurityGroup = frontEndSubnetNsg
                        },
                        new Subnet {
                            Name = VNet1BackEndSubnetName,
                            AddressPrefix = "192.168.2.0/24",
                            NetworkSecurityGroup = backEndSubnetNsg
                        }
                    }
                };
                rawVirtualNetworkResult = await virtualNetworks.StartCreateOrUpdateAsync(ResourceGroupName, vnetName1, virtualNetworkParameters);
                virtualNetwork1 = (await rawVirtualNetworkResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Network security group rule associated with the frontend subnet");
                // Print the virtual network details
                Utilities.PrintVirtualNetwork(virtualNetwork1);

                // Create a virtual machine in each subnet

                // Creates the first virtual machine in frontend subnet
                Utilities.Log("Creating a Linux virtual machine in the frontend subnet");
                // Creates the second virtual machine in the backend subnet
                Utilities.Log("Creating a Linux virtual machine in the backend subnet");
                // Create a virtual network with default address-space and one default subnet
                Utilities.Log("Creating virtual network #2...");

                Utilities.Log("Creating Public IP Address #1...");

                var ipAddress = new PublicIPAddress
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = "eastus",
                };

                ipAddress = (await publicIPAddresses.StartCreateOrUpdate(ResourceGroupName, frontEndVmName + "_ip", ipAddress)
                    .WaitForCompletionAsync()).Value;

                Utilities.Log("Created Public IP Address #1...");

                Utilities.Log("Creating Network Interface #1...");

                var networkInterface = new NetworkInterface
                {
                    Location = "eastus",
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet
                            {
                                Id = virtualNetwork1.Subnets.First(s => s.Name == VNet1FrontEndSubnetName).Id
                            },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddress { Id = ipAddress.Id }
                        }
                    }
                };
                networkInterface = (await networkInterfaces
                    .StartCreateOrUpdate(ResourceGroupName, frontEndVmName + "_nic", networkInterface).WaitForCompletionAsync()).Value;

                Utilities.Log("Created Network Interface #1...");

                var t1 = DateTime.UtcNow;
                var virtualMachineParameters = new VirtualMachine("eastus")
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces = new[]
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
                            Ssh = new SshConfiguration
                            {
                                PublicKeys = new List<SshPublicKey>
                               {
                                   new SshPublicKey
                                   {
                                       Path = "/home/" + UserName + "/.ssh/authorized_keys",
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
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = VirtualMachineSizeTypes.StandardD3V2
                    }
                };

                var rawVirtualMachineResult = await virtualMachines.StartCreateOrUpdateAsync(ResourceGroupName, frontEndVmName, virtualMachineParameters);
                var frontEndVM = (await rawVirtualMachineResult.WaitForCompletionAsync()).Value;

                var t2 = DateTime.UtcNow;
                Utilities.Log("Created Linux VM: (took " + (t2 - t1).TotalSeconds + " seconds) " + frontEndVM.Id);
                // Print virtual machine details
                Utilities.PrintVirtualMachine(frontEndVM);

                Utilities.Log("Creating Network Interface #2...");

                networkInterface = new NetworkInterface
                {
                    Location = "eastus",
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet
                            {
                                Id = virtualNetwork1.Subnets.First(s => s.Name == VNet1BackEndSubnetName).Id
                            },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                networkInterface = (await networkInterfaces
                    .StartCreateOrUpdate(ResourceGroupName, backEndVmName + "_nic", networkInterface)
                    .WaitForCompletionAsync()).Value;

                Utilities.Log("Created Network Interface #1...");

                t1 = DateTime.UtcNow;
                virtualMachineParameters = new VirtualMachine("eastus")
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces = new[]
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
                            Ssh = new SshConfiguration
                            {
                                PublicKeys = new List<SshPublicKey>
                                {
                                    new SshPublicKey
                                    {
                                        Path = "/home/"+ UserName +"/.ssh/authorized_keys",
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
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = VirtualMachineSizeTypes.StandardD3V2
                    }
                };

                rawVirtualMachineResult = await virtualMachines.StartCreateOrUpdateAsync(ResourceGroupName, backEndVmName, virtualMachineParameters);
                var backEndVM = (await rawVirtualMachineResult.WaitForCompletionAsync()).Value;

                var t3 = DateTime.UtcNow;
                Utilities.Log("Created Linux VM: (took " + (t3 - t1).TotalSeconds + " seconds) " + backEndVM.Id);
                // Print virtual machine details
                Utilities.PrintVirtualMachine(backEndVM);

                Utilities.Log("Creating a virtual network #2");

                rawVirtualNetworkResult = await virtualNetworks.StartCreateOrUpdateAsync(
                    ResourceGroupName,
                    vnetName2,
                    new VirtualNetwork
                    {
                        Location = "eastus",
                        AddressSpace = new AddressSpace
                        {
                            AddressPrefixes = new List<string> { "10.0.0.0/16" }
                        }
                    });
                var virtualNetwork2 = (await rawVirtualNetworkResult.WaitForCompletionAsync()).Value;

                Utilities.Log("Created a virtual network #2");
                // Print the virtual network details
                Utilities.PrintVirtualNetwork(virtualNetwork2);

                // List virtual networks

                Utilities.Log("List virtual networks");

                foreach (var virtualNetwork in await virtualNetworks.ListAsync(ResourceGroupName).ToEnumerableAsync())
                {
                    Utilities.PrintVirtualNetwork(virtualNetwork);
                }

                // Delete a virtual network
                Utilities.Log("Deleting the virtual network");
                await (await virtualNetworks.StartDeleteAsync(ResourceGroupName, vnetName2)).WaitForCompletionAsync();
                Utilities.Log("Deleted the virtual network");
            }
            finally
            {
                try
                {
                    await ResourceGroupHelper.DeleteResourceGroup(ResourceGroupName);
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
