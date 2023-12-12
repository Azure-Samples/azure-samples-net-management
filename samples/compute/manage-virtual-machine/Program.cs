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
using Samples.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ManageVirtualMachine
{
    public class Program
    {

        //Azure Compute sample for managing virtual machines -
        //  - Create a virtual machine with managed OS Disk
        //  - Start a virtual machine
        //  - Stop a virtual machine
        //  - Restart a virtual machine
        //  - Update a virtual machine
        //    - Tag a virtual machine(there are many possible variations here)
        //    - Attach data disks
        //    - Detach data disks
        //  - List virtual machines
        //  - Delete a virtual machine.

        public static async Task RunSample(ArmClient client)
        {
            var region = "westcentralus";
            var nicName = Utilities.CreateRandomName("nic");
            var nicName2 = Utilities.CreateRandomName("nic2");
            var windowsVmName = Utilities.CreateRandomName("wVM");
            var linuxVmName = Utilities.CreateRandomName("lVM");
            var rgName = Utilities.CreateRandomName("rgCOMV");
            var userName = "tirekicker";
            var password = "<password>";
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            try
            {
                String location = AzureLocation.WestUS2;

                ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

                // Create a Windows virtual machine
                var collection = resourceGroup.GetVirtualMachines();
                // Create VNet
                Console.WriteLine("--------Start create VNet--------");
                var vnet = new VirtualNetworkData()
                {
                    Location = location,
                    AddressPrefixes = { "10.0.0.0/16" },
                    Subnets = { new SubnetData() { Name = "SubnetSampleName", AddressPrefix = "10.0.0.0/28" } }
                };

                // Create Network Interface
                Console.WriteLine("--------Start create Network Interface--------");
                var nicData = new NetworkInterfaceData()
                {
                    Location = location,
                    IPConfigurations = {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "SampleIpConfigName",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Primary = false,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Subnets.ElementAt(0).Id
                            }
                        }
                    }
                };
                var nicCollection = resourceGroup.GetNetworkInterfaces();
                var nic = (await nicCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, nicName, nicData)).Value;
                var t1 = new DateTime();
                // Create VM
                Console.WriteLine("--------Start create VM--------");
                var vmData = new VirtualMachineData(location)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardF2
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        AdminUsername = userName,
                        AdminPassword = password,
                        ComputerName = "linux Compute",
                        LinuxConfiguration = new LinuxConfiguration()
                        {
                            DisablePasswordAuthentication = true,
                        }
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
                            Name = "Sample",
                            OSType = SupportedOperatingSystemType.Windows,
                            Caching = CachingType.ReadWrite,
                            ManagedDisk = new VirtualMachineManagedDisk()
                            {
                                StorageAccountType = StorageAccountType.StandardLrs
                            }
                        },
                        DataDisks =
                        {
                            new VirtualMachineDataDisk(1, DiskCreateOptionType.Empty)
                            {
                                DiskSizeGB = 100,
                                ManagedDisk = new VirtualMachineManagedDisk()
                                {
                                    StorageAccountType = StorageAccountType.StandardLrs
                                }
                            },
                            new VirtualMachineDataDisk(2, DiskCreateOptionType.Empty)
                            {
                                DiskSizeGB = 10,
                                Caching = CachingType.ReadWrite,
                                ManagedDisk = new VirtualMachineManagedDisk()
                                {
                                    StorageAccountType = StorageAccountType.StandardLrs
                                }
                            },
                        },
                        ImageReference = new ImageReference()
                        {
                            Publisher = "MicrosoftWindowsServer",
                            Offer = "WindowsServer",
                            Sku = "2016-Datacenter",
                            Version = "latest",
                        }
                    }
                };

                var windowsVM = await collection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, windowsVmName, vmData);
                Console.WriteLine("VM ID: " + windowsVM.Id);
                Console.WriteLine("--------Done create VM--------");

                var t2 = new DateTime();
                Utilities.Log($"Created VM: (took {(t2 - t1).TotalSeconds} seconds) " + windowsVM.Id);
                // Print virtual machine details
                Utilities.PrintVirtualMachine(windowsVM.Value);

                // Update - Tag the virtual machine

                var update = new VirtualMachinePatch
                {
                    Tags =
                    {
                        { "who-rocks", "java" },
                        { "where", "on azure" }
                    }
                };

                windowsVM = await windowsVM.Value.UpdateAsync(Azure.WaitUntil.Completed, update);

                Utilities.Log("Tagged VM: " + windowsVM.Id);

                // Update - Add data disk

                windowsVM.Value.Data.StorageProfile.DataDisks.Add(new VirtualMachineDataDisk(3, DiskCreateOptionType.Empty) { DiskSizeGB = 10 });

                Utilities.Log("Added a data disk to VM" + windowsVM.Id);
                Utilities.PrintVirtualMachine(windowsVM.Value);

                // Update - detach data disk
                var removeDisk = windowsVM.Value.Data.StorageProfile.DataDisks.First(x => x.Lun == 0);
                windowsVM.Value.Data.StorageProfile.DataDisks.Remove(removeDisk);

                Utilities.Log("Detached data disk at lun 0 from VM " + windowsVM.Id);

                // Restart the virtual machine

                Utilities.Log("Restarting VM: " + windowsVM.Id);
                await windowsVM.Value.RestartAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Restarted VM: " + windowsVM.Id);

                // Stop (powerOff) the virtual machine

                Utilities.Log("Powering OFF VM: " + windowsVM.Id);

                await windowsVM.Value.PowerOffAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Powered OFF VM: " + windowsVM.Id);

                // Create a Linux VM in the same virtual network

                Utilities.Log("Creating a Network Interface #2");

                var nicData2 = new NetworkInterfaceData()
                {
                    Location = region,
                    /*IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }*/
                    IPConfigurations = {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Subnets.ElementAt(0).Id,
                            }
                        }
                    }
                };
                var nic2 = (await nicCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, nicName2, nicData2)).Value;

                Utilities.Log("Created a Network Interface #2");

                Utilities.Log("Creating a Linux VM in the network");

                var linuxVMData = new VirtualMachineData(region)
                {
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic2.Id,
                                Primary = true,
                            }
                        }
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = linuxVmName,
                        AdminUsername = userName,
                        AdminPassword = password,
                        LinuxConfiguration = new LinuxConfiguration
                        {
                            DisablePasswordAuthentication = false,
                            ProvisionVmAgent = true
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        ImageReference = new ImageReference
                        {
                            Offer = "UbuntuServer",
                            Publisher = "Canonical",
                            Sku = "18.04-LTS",
                            Version = "latest"
                        },
                        DataDisks =
                        {
                        }
                    },
                    HardwareProfile = new VirtualMachineHardwareProfile() { VmSize = VirtualMachineSizeType.StandardD3V2 },
                };

                var linuxVM = (await collection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, linuxVmName, linuxVMData)).Value;

                Utilities.Log("Created a Linux VM (in the same virtual network): " + linuxVM.Id);
                Utilities.PrintVirtualMachine(linuxVM);

                // List virtual machines in the resource group

                Utilities.Log("Printing list of VMs =======");

                await foreach(var virtualMachine in collection.GetAllAsync())
                {
                    Utilities.PrintVirtualMachine(virtualMachine);
                }

                // Delete the virtual machine
                Utilities.Log("Deleting VM: " + windowsVM.Id);

                await windowsVM.Value.DeleteAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Deleted VM: " + windowsVM.Id);
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
