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

namespace ManageVirtualMachine
{
    public class Program
    {
        /**
        * Azure Compute sample for managing virtual machines -
        *  - Create a virtual machine with managed OS Disk
        *  - Start a virtual machine
        *  - Stop a virtual machine
        *  - Restart a virtual machine
        *  - Update a virtual machine
        *    - Tag a virtual machine (there are many possible variations here)
        *    - Attach data disks
        *    - Detach data disks
        *  - List virtual machines
        *  - Delete a virtual machine.
        */
        public static async Task RunSample(TokenCredential credential)
        {
            var region = "westcentralus";
            var windowsVmName = Utilities.CreateRandomName("wVM");
            var linuxVmName = Utilities.CreateRandomName("lVM");
            var rgName = Utilities.CreateRandomName("rgCOMV");
            var userName = "tirekicker";
            var password = "12NewPA$$w0rd!";
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            var virtualNetworks = networkManagementClient.VirtualNetworks;
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            var computeManagementClient = new ComputeManagementClient(subscriptionId, credential);
            var virtualMachines = computeManagementClient.VirtualMachines;
            var disks = computeManagementClient.Disks;

            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, region);

                //=============================================================
                // Create a Windows virtual machine

                // Create a data disk to attach to VM
                //
                Utilities.Log("Creating a data disk");

                var dataDisk = new Disk(region)
                {
                    DiskSizeGB = 50,
                    CreationData = new CreationData(DiskCreateOption.Empty)
                };
                dataDisk = await (await disks
                    .StartCreateOrUpdateAsync(rgName, Utilities.CreateRandomName("dsk-"), dataDisk)).WaitForCompletionAsync();

                Utilities.Log("Created a data disk");

                // Create VNet
                //
                Utilities.Log("Creating a VNet");

                var vnet = new VirtualNetwork
                {
                    Location = region,
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
                vnet = await virtualNetworks
                    .StartCreateOrUpdate(rgName, windowsVmName + "_vent", vnet).WaitForCompletionAsync();

                Utilities.Log("Created a VNet");

                // Create Network Interface
                //
                Utilities.Log("Creating a Network Interface");

                var nic = new NetworkInterface
                {
                    Location = region,
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                nic = await networkInterfaces
                    .StartCreateOrUpdate(rgName, windowsVmName + "_nic", nic).WaitForCompletionAsync();

                Utilities.Log("Created a Network Interface");

                var t1 = new DateTime();

                // Create VM
                //
                Utilities.Log("Creating a Windows VM");

                var windowsVM = new VirtualMachine(region)
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
                        DataDisks = new List<DataDisk>
                        {
                            new DataDisk(0, DiskCreateOptionTypes.Empty)
                            {
                                DiskSizeGB = 10
                            },
                            new DataDisk(1, DiskCreateOptionTypes.Empty)
                            {
                                Name = Utilities.CreateRandomName("dsk-"),
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

                windowsVM = (await (await virtualMachines
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsVM)).WaitForCompletionAsync()).Value;

                var t2 = new DateTime();
                Utilities.Log($"Created VM: (took {(t2 - t1).TotalSeconds} seconds) " + windowsVM.Id);
                // Print virtual machine details
                Utilities.PrintVirtualMachine(windowsVM);

                //=============================================================
                // Update - Tag the virtual machine

                var update = new VirtualMachineUpdate
                {
                    Tags = new Dictionary<string, string>
                    {
                        { "who-rocks", "java" },
                        { "where", "on azure" }
                    }
                };

                windowsVM = await (await virtualMachines.StartUpdateAsync(rgName, windowsVmName, update)).WaitForCompletionAsync();

                Utilities.Log("Tagged VM: " + windowsVM.Id);

                //=============================================================
                // Update - Add data disk

                windowsVM.StorageProfile.DataDisks.Add(new DataDisk(3, DiskCreateOptionTypes.Empty) { DiskSizeGB = 10 });
                windowsVM = (await (await virtualMachines
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsVM)).WaitForCompletionAsync()).Value;

                Utilities.Log("Added a data disk to VM" + windowsVM.Id);
                Utilities.PrintVirtualMachine(windowsVM);

                //=============================================================
                // Update - detach data disk
                var removeDisk = windowsVM.StorageProfile.DataDisks.First(x => x.Lun == 0);
                windowsVM.StorageProfile.DataDisks.Remove(removeDisk);
                windowsVM = (await (await virtualMachines
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsVM)).WaitForCompletionAsync()).Value;

                Utilities.Log("Detached data disk at lun 0 from VM " + windowsVM.Id);

                //=============================================================
                // Restart the virtual machine

                Utilities.Log("Restarting VM: " + windowsVM.Id);
                await (await virtualMachines.StartRestartAsync(rgName, windowsVmName)).WaitForCompletionAsync();

                Utilities.Log("Restarted VM: " + windowsVM.Id);

                //=============================================================
                // Stop (powerOff) the virtual machine

                Utilities.Log("Powering OFF VM: " + windowsVM.Id);

                await (await virtualMachines.StartPowerOffAsync(rgName, windowsVmName)).WaitForCompletionAsync();

                Utilities.Log("Powered OFF VM: " + windowsVM.Id);

                //=============================================================
                // Create a Linux VM in the same virtual network

                Utilities.Log("Creating a Network Interface #2");

                var nic2 = new NetworkInterface
                {
                    Location = region,
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new Subnet { Id = vnet.Subnets.First().Id },
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        }
                    }
                };
                nic2 = await networkInterfaces
                    .StartCreateOrUpdate(rgName, linuxVmName + "_nic", nic2).WaitForCompletionAsync();

                Utilities.Log("Created a Network Interface #2");

                Utilities.Log("Creating a Linux VM in the network");

                var linuxVM = new VirtualMachine(region)
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile
                    {
                        NetworkInterfaces = new[]
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
                        },
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                linuxVM = await (await virtualMachines.StartCreateOrUpdateAsync(rgName, linuxVmName, linuxVM)).WaitForCompletionAsync();

                Utilities.Log("Created a Linux VM (in the same virtual network): " + linuxVM.Id);
                Utilities.PrintVirtualMachine(linuxVM);

                //=============================================================
                // List virtual machines in the resource group

                Utilities.Log("Printing list of VMs =======");

                foreach (var virtualMachine in await virtualMachines.ListAsync(rgName).ToEnumerableAsync())
                {
                    Utilities.PrintVirtualMachine(virtualMachine);
                }

                //=============================================================
                // Delete the virtual machine
                Utilities.Log("Deleting VM: " + windowsVM.Id);

                await (await virtualMachines.StartDeleteAsync(rgName, windowsVmName)).WaitForCompletionAsync();

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
