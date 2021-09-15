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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static async Task RunSample(TokenCredential credential)
        {
            var location = Location.WestCentralUS;
            var windowsVmName = "myWinVM";
            var linuxVmName = "myLinuxVM";
            var rgName = "rgCOMV";
            var userName = "tirekicker";
            var password = "<password>";

            var armClient = new ArmClient(credential);
            ResourceGroup resourceGroup = await armClient.DefaultSubscription.GetResourceGroups().CreateOrUpdate(rgName, new ResourceGroupData(location)).WaitForCompletionAsync();

            try
            {
                // Create a Windows virtual machine

                // Create a data disk to attach to VM
                Console.WriteLine("--------Creating a data disk--------");

                var dataDiskData = new DiskData(location)
                {
                    DiskSizeGB = 50,
                    CreationData = new CreationData(DiskCreateOption.Empty)
                };
                Disk dataDisk = await resourceGroup.GetDisks().CreateOrUpdate("myDataDisk1", dataDiskData).WaitForCompletionAsync();
                Console.WriteLine("--------Created data disk--------");

                // Create VNet
                Console.WriteLine("--------Creating a VNet--------");

                var vnetData = new VirtualNetworkData
                {
                    Location = location,
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
                VirtualNetwork vnet = await resourceGroup.GetVirtualNetworks().CreateOrUpdate(windowsVmName + "_vent", vnetData).WaitForCompletionAsync();
                Console.WriteLine("--------Created VNet--------");

                // Create Network Interface
                Console.WriteLine("--------Creating a Network Interface--------");

                var nicData = new NetworkInterfaceData
                {
                    Location = location,
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData { Id = vnet.Data.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                NetworkInterface nic = await resourceGroup.GetNetworkInterfaces().CreateOrUpdate(windowsVmName + "_nic", nicData).WaitForCompletionAsync();
                Console.WriteLine("--------Created Network Interface--------");

                var t1 = new DateTime();

                // Create VM
                Console.WriteLine("--------Creating a Windows VM--------");

                var windowsVMData = new VirtualMachineData(location)
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
                        ComputerName = windowsVmName,
                        AdminUsername = userName,
                        AdminPassword = password,
                    },
                    StorageProfile = new StorageProfile
                    {
                        ImageReference = new ImageReference
                        {
                            Offer = "WindowsServer",
                            Publisher = "MicrosoftWindowsServer",
                            Sku = "2016-Datacenter",
                            Version = "latest"
                        },
                        DataDisks =
                        {
                            new DataDisk(0, DiskCreateOptionTypes.Empty)
                            {
                                DiskSizeGB = 10
                            },
                            new DataDisk(1, DiskCreateOptionTypes.Empty)
                            {
                                Name = "myDataDisk2",
                                DiskSizeGB = 100,
                            },
                            new DataDisk(2, DiskCreateOptionTypes.Attach)
                            {
                                ManagedDisk = new ManagedDiskParameters()
                                {
                                    Id = dataDisk.Id
                                }
                            }
                        }
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                VirtualMachine windowsVM = await resourceGroup.GetVirtualMachines().CreateOrUpdate(windowsVmName, windowsVMData).WaitForCompletionAsync();

                var t2 = new DateTime();
                Console.WriteLine($"--------Created Windows VM ({(t2 - t1).TotalSeconds} seconds)--------");
                Console.WriteLine($"Windows Virtual Machine ID: {windowsVM.Id}");

                // Update - Tag the virtual machine
                Console.WriteLine("--------Tagging the Windows VM--------");
                var tags = new Dictionary<string, string>
                {
                    { "who-rocks", "java" },
                    { "where", "on azure" }
                };

                windowsVM = await windowsVM.SetTagsAsync(tags);
                Console.WriteLine("--------Tagged Windows VM--------");

                // Update - Add data disk
                Console.WriteLine("--------Adding a Data Disk to the Windows VM--------");
                var winVMUpdate = new VirtualMachineUpdate
                {
                    StorageProfile = new StorageProfile()
                };
                foreach (var d in windowsVM.Data.StorageProfile.DataDisks)
                {
                    winVMUpdate.StorageProfile.DataDisks.Add(d);
                }
                winVMUpdate.StorageProfile.DataDisks.Add(new DataDisk(3, DiskCreateOptionTypes.Empty) { DiskSizeGB = 10 });
                windowsVM = await windowsVM.Update(winVMUpdate).WaitForCompletionAsync();
                Console.WriteLine("--------Added the Data Disk to the Windows VM--------");

                Console.WriteLine("--------Detaching the first Data Disk from the Windows VM--------");
                // Update - detach data disk
                var removeDisk = windowsVM.Data.StorageProfile.DataDisks.First(x => x.Lun == 0);
                windowsVM.Data.StorageProfile.DataDisks.Remove(removeDisk);

                windowsVM = await resourceGroup.GetVirtualMachines().CreateOrUpdate(windowsVM.Data.Name, windowsVM.Data).WaitForCompletionAsync();

                Console.WriteLine($"--------Detached data disk at lun 0 from VM {windowsVM.Id}--------");

                // Restart the virtual machine

                Console.WriteLine("--------Restarting the Windows VM--------");
                await windowsVM.RestartAsync();
                Console.WriteLine("--------Restarted the Windows VM--------");

                // Stop (powerOff) the virtual machine
                Console.WriteLine("--------Powering off the Windows VM--------");
                await windowsVM.PowerOffAsync();
                Console.WriteLine("--------Powered off the Windows VM--------");

                // Create a Linux VM in the same virtual network
                Console.WriteLine("--------Creating a Network Interface #2--------");
                var nicData2 = new NetworkInterfaceData
                {
                    Location = location,
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData { Id = vnet.Data.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                NetworkInterface nic2 = await resourceGroup.GetNetworkInterfaces().CreateOrUpdate(linuxVmName + "_nic", nicData2).WaitForCompletionAsync();
                Console.WriteLine("--------Created the Network Interface #2--------");

                Console.WriteLine("--------Creating a Linux VM--------");
                var linuxVMData = new VirtualMachineData(location)
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces =
                        {
                            new NetworkInterfaceReference { Id = nic2.Id }
                        }
                    },
                    OsProfile = new OSProfile
                    {
                        ComputerName = linuxVmName,
                        AdminUsername = userName,
                        AdminPassword = password,
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
                            Sku = "18.04-LTS",
                            Version = "latest"
                        }
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                VirtualMachine linuxVM = await resourceGroup.GetVirtualMachines().CreateOrUpdate(linuxVmName, linuxVMData).WaitForCompletionAsync();
                Console.WriteLine("--------Created the Linux VM--------");
                Console.WriteLine($"Linux Virtual Machine ID: {linuxVM.Id}");

                // List virtual machines in the resource group
                Console.WriteLine($"--------Listing the VMs in Resource Group--------");

                await foreach (var virtualMachine in resourceGroup.GetVirtualMachines().GetAllAsync())
                {
                    Console.WriteLine(virtualMachine.Id);
                }
                Console.WriteLine("--------Listed the VMs in Resource Group--------");

                // Delete the virtual machine
                Console.WriteLine($"--------Deleting Windows VM ({windowsVM.Id})--------");
                await windowsVM.DeleteAsync();
                Console.WriteLine($"--------Deleted Windows VM ({windowsVM.Id})--------");
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

        public static async Task Main(string[] args)
        {
            try
            {
                // Authenticate
                var credential = new DefaultAzureCredential();

                await RunSample(credential);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
