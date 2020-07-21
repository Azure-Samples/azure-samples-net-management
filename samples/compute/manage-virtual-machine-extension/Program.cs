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

namespace ManageVirtualMachineExtension
{
    public class Program
    {
        // Linux configurations
        //
        readonly static string FirstLinuxUserName = "tirekicker";
        readonly static string FirstLinuxUserPassword = "12NewPA$$w0rd!";
        readonly static string FirstLinuxUserNewPassword = "muy!234OR";

        readonly static string SecondLinuxUserName = "seconduser";
        readonly static string SecondLinuxUserPassword = "B12a6@12xyz!";
        readonly static string SecondLinuxUserExpiration = "2020-12-31";

        readonly static string ThirdLinuxUserName = "thirduser";
        readonly static string ThirdLinuxUserPassword = "12xyz!B12a6@";
        readonly static string ThirdLinuxUserExpiration = "2020-12-31";

        readonly static string LinuxCustomScriptExtensionName = "CustomScriptForLinux";
        readonly static string LinuxCustomScriptExtensionPublisherName = "Microsoft.OSTCExtensions";
        readonly static string LinuxCustomScriptExtensionTypeName = "CustomScriptForLinux";
        readonly static string LinuxCustomScriptExtensionVersionName = "1.4";

        readonly static string MySqlScriptLinuxInstallCommand = "bash install_mysql_server_5.6.sh Abc.123x(";
        readonly static List<string> MySQLLinuxInstallScriptFileUris = new List<string>()
        {
            "https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/mysql-standalone-server-ubuntu/scripts/install_mysql_server_5.6.sh"
        };

        readonly static string windowsCustomScriptExtensionName = "CustomScriptExtension";
        readonly static string windowsCustomScriptExtensionPublisherName = "Microsoft.Compute";
        readonly static string windowsCustomScriptExtensionTypeName = "CustomScriptExtension";
        readonly static string windowsCustomScriptExtensionVersionName = "1.7";

        readonly static string mySqlScriptWindowsInstallCommand = "powershell.exe -ExecutionPolicy Unrestricted -File installMySQL.ps1";
        readonly static List<string> mySQLWindowsInstallScriptFileUris = new List<string>()
        {
            "https://raw.githubusercontent.com/Azure/azure-libraries-for-net/master/Samples/Asset/installMySQL.ps1"
        };

        readonly static string linuxVmAccessExtensionName = "VMAccessForLinux";
        readonly static string linuxVmAccessExtensionPublisherName = "Microsoft.OSTCExtensions";
        readonly static string linuxVmAccessExtensionTypeName = "VMAccessForLinux";
        readonly static string linuxVmAccessExtensionVersionName = "1.4";

        // Windows configurations
        //
        readonly static string firstWindowsUserName = "tirekicker";
        readonly static string firstWindowsUserPassword = "12NewPA$$w0rd!";
        readonly static string firstWindowsUserNewPassword = "muy!234OR";

        readonly static string secondWindowsUserName = "seconduser";
        readonly static string secondWindowsUserPassword = "B12a6@12xyz!";

        readonly static string thirdWindowsUserName = "thirduser";
        readonly static string thirdWindowsUserPassword = "12xyz!B12a6@";

        readonly static string windowsVmAccessExtensionName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionPublisherName = "Microsoft.Compute";
        readonly static string windowsVmAccessExtensionTypeName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionVersionName = "2.3";

        /**
         * Azure Compute sample for managing virtual machine extensions. -
         *  - Create a Linux and Windows virtual machine
         *  - Add three users (user names and passwords for windows, SSH keys for Linux)
         *  - Resets user credentials
         *  - Remove a user
         *  - Install MySQL on Linux | something significant on Windows
         *  - Remove extensions
         */
        public static async Task RunSample(TokenCredential credential)
        {
            string rgName = Utilities.RandomResourceName("rgCOVE", 15);
            string linuxVmName = Utilities.RandomResourceName("lVM", 10);
            string windowsVmName = Utilities.RandomResourceName("wVM", 10);
            string pipDnsLabelLinuxVM = Utilities.RandomResourceName("rgPip1", 25);
            string pipDnsLabelWindowsVM = Utilities.RandomResourceName("rgPip2", 25);
            string location = "eastus";
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var networkManagementClient = new NetworkManagementClient(subscriptionId, credential);
            var virtualNetworks = networkManagementClient.VirtualNetworks;
            var publicIPAddresses = networkManagementClient.PublicIPAddresses;
            var networkInterfaces = networkManagementClient.NetworkInterfaces;
            var computeManagementClient = new ComputeManagementClient(subscriptionId, credential);
            var virtualMachines = computeManagementClient.VirtualMachines;
            var virtualMachineExtensions = computeManagementClient.VirtualMachineExtensions;
            try
            {
                await ResourceGroupHelper.CreateOrUpdateResourceGroup(rgName, location);

                //=============================================================
                // Create a Linux VM with root (sudo) user

                // Create IP Address
                Utilities.Log("Creating a IP Address");
                var ipAddress = new PublicIPAddress()
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = location,
                };

                ipAddress = await publicIPAddresses
                    .StartCreateOrUpdate(rgName, pipDnsLabelLinuxVM, ipAddress).WaitForCompletionAsync();

                Utilities.Log("Created a IP Address");

                // Create VNet
                Utilities.Log("Creating a VNet");
                var vnet = new VirtualNetwork()
                {
                    Location = location,
                    AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "10.0.0.0/16" } },
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
                    .StartCreateOrUpdate(rgName, linuxVmName + "_vent", vnet).WaitForCompletionAsync();

                Utilities.Log("Created a VNet");

                // Create Network Interface
                Utilities.Log("Creating a Network Interface");
                var nic = new NetworkInterface
                {
                    Location = location,
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                {
                    new NetworkInterfaceIPConfiguration
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new Subnet { Id = vnet.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddress { Id = ipAddress.Id }
                    }
                }
                };
                nic = await networkInterfaces
                    .StartCreateOrUpdate(rgName, linuxVmName + "_nic", nic).WaitForCompletionAsync();

                Utilities.Log("Created a Network Interface");

                // Create VM

                Utilities.Log("Creating a Linux VM");

                var linuxVM = new VirtualMachine(location)
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile { NetworkInterfaces = new[] { new NetworkInterfaceReference { Id = nic.Id } } },
                    OsProfile = new OSProfile
                    {
                        ComputerName = linuxVmName,
                        AdminUsername = FirstLinuxUserName,
                        AdminPassword = FirstLinuxUserPassword,
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
                            Sku = "14.04.5-LTS",
                            Version = "latest"
                        },
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                linuxVM = await (await virtualMachines.StartCreateOrUpdateAsync(rgName, linuxVmName, linuxVM)).WaitForCompletionAsync();

                Utilities.Log("Created a Linux VM:" + linuxVM.Id);
                Utilities.PrintVirtualMachine(linuxVM);

                //=============================================================
                // Add a second sudo user to Linux VM using VMAccess extension

                var vmExtension = new VirtualMachineExtension(location)
                {
                    Publisher = linuxVmAccessExtensionPublisherName,
                    TypePropertiesType = linuxVmAccessExtensionTypeName,
                    TypeHandlerVersion = linuxVmAccessExtensionVersionName,
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", SecondLinuxUserName },
                        { "password", SecondLinuxUserPassword },
                        { "expiration", SecondLinuxUserExpiration }
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartCreateOrUpdateAsync(rgName, linuxVmName, linuxVmAccessExtensionName, vmExtension)).WaitForCompletionAsync();

                Utilities.Log("Added a second sudo user to the Linux VM");

                //=============================================================
                // Add a third sudo user to Linux VM by updating VMAccess extension

                var vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", ThirdLinuxUserName },
                        { "password", ThirdLinuxUserPassword },
                        { "expiration", ThirdLinuxUserExpiration }
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartUpdateAsync(rgName, linuxVmName, linuxVmAccessExtensionName, vmExtensionUpdate)).WaitForCompletionAsync();

                Utilities.Log("Added a third sudo user to the Linux VM");

                //=============================================================
                // Reset ssh password of first user of Linux VM by updating VMAccess extension

                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", FirstLinuxUserName },
                        { "password", FirstLinuxUserNewPassword },
                        { "reset_ssh", "true" }
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartUpdateAsync(rgName, linuxVmName, linuxVmAccessExtensionName, vmExtensionUpdate)).WaitForCompletionAsync();

                Utilities.Log("Password of first user of Linux VM has been updated");

                //=============================================================
                // Removes the second sudo user from Linux VM using VMAccess extension

                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "remove_user", SecondLinuxUserName },
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartUpdateAsync(rgName, linuxVmName, linuxVmAccessExtensionName, vmExtensionUpdate)).WaitForCompletionAsync();

                Utilities.Log("Removed the second user from Linux VM using VMAccess extension");

                //=============================================================
                // Install MySQL in Linux VM using CustomScript extension

                vmExtension = new VirtualMachineExtension(location)
                {
                    Publisher = LinuxCustomScriptExtensionPublisherName,
                    TypePropertiesType = LinuxCustomScriptExtensionTypeName,
                    TypeHandlerVersion = LinuxCustomScriptExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    Settings = new Dictionary<string, object>
                    {
                        { "fileUris", MySQLLinuxInstallScriptFileUris },
                        { "commandToExecute", MySqlScriptLinuxInstallCommand },
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartCreateOrUpdateAsync(rgName, linuxVmName, LinuxCustomScriptExtensionName, vmExtension)).WaitForCompletionAsync();

                Utilities.Log("Installed MySql using custom script extension");
                Utilities.PrintVirtualMachine(linuxVM);

                //=============================================================
                // Removes the extensions from Linux VM

                await (await virtualMachineExtensions
                    .StartDeleteAsync(rgName, linuxVmName, LinuxCustomScriptExtensionName)).WaitForCompletionAsync();
                await (await virtualMachineExtensions
                    .StartDeleteAsync(rgName, linuxVmName, linuxVmAccessExtensionName)).WaitForCompletionAsync();

                Utilities.Log("Removed the custom script and VM Access extensions from Linux VM");
                Utilities.PrintVirtualMachine(linuxVM);

                //=============================================================
                // Create a Windows VM with admin user

                // Create IP Address

                Utilities.Log("Creating a IP Address");
                ipAddress = new PublicIPAddress
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = location,
                };

                ipAddress = await publicIPAddresses
                    .StartCreateOrUpdate(rgName, pipDnsLabelWindowsVM, ipAddress).WaitForCompletionAsync();

                Utilities.Log("Created a IP Address");

                // Create Network Interface #2

                Utilities.Log("Creating a Network Interface #2");
                nic = new NetworkInterface
                {
                    Location = location,
                    IpConfigurations = new List<NetworkInterfaceIPConfiguration>
                {
                    new NetworkInterfaceIPConfiguration
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new Subnet { Id = vnet.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddress { Id = ipAddress.Id }
                    }
                }
                };
                nic = await networkInterfaces
                    .StartCreateOrUpdate(rgName, windowsVmName + "_nic", nic).WaitForCompletionAsync();

                Utilities.Log("Created a Network Interface");

                // Create Windows VM

                Utilities.Log("Creating a Windows VM");

                var windowsVM = new VirtualMachine(location)
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
                        AdminUsername = firstWindowsUserName,
                        AdminPassword = firstWindowsUserPassword,
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
                        DataDisks = new List<DataDisk>()
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                windowsVM = await (await virtualMachines
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsVM)).WaitForCompletionAsync();

                vmExtension = new VirtualMachineExtension(location)
                {
                    Publisher = windowsCustomScriptExtensionPublisherName,
                    TypePropertiesType = windowsCustomScriptExtensionTypeName,
                    TypeHandlerVersion = windowsCustomScriptExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    Settings = new Dictionary<string, object>
                    {
                        { "fileUris", mySQLWindowsInstallScriptFileUris },
                        { "commandToExecute", mySqlScriptWindowsInstallCommand },
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsCustomScriptExtensionName, vmExtension)).WaitForCompletionAsync();

                Utilities.Log("Created a Windows VM:" + windowsVM.Id);
                Utilities.PrintVirtualMachine(windowsVM);

                //=============================================================
                // Add a second admin user to Windows VM using VMAccess extension

                vmExtension = new VirtualMachineExtension(location)
                {
                    Publisher = windowsVmAccessExtensionPublisherName,
                    TypePropertiesType = windowsVmAccessExtensionTypeName,
                    TypeHandlerVersion = windowsVmAccessExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", secondWindowsUserName },
                        { "password", secondWindowsUserPassword },
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartCreateOrUpdateAsync(rgName, windowsVmName, windowsVmAccessExtensionName, vmExtension)).WaitForCompletionAsync();

                Utilities.Log("Added a second admin user to the Windows VM");

                //=============================================================
                // Add a third admin user to Windows VM by updating VMAccess extension

                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", thirdWindowsUserName },
                        { "password", thirdWindowsUserPassword }
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartUpdateAsync(rgName, windowsVmName, windowsVmAccessExtensionName, vmExtensionUpdate)).WaitForCompletionAsync();

                Utilities.Log("Added a third admin user to the Windows VM");

                //=============================================================
                // Reset admin password of first user of Windows VM by updating VMAccess extension

                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", firstWindowsUserName },
                        { "password", firstWindowsUserNewPassword }
                    }
                };

                vmExtension = await (await virtualMachineExtensions
                    .StartUpdateAsync(rgName, windowsVmName, windowsVmAccessExtensionName, vmExtensionUpdate)).WaitForCompletionAsync();


                Utilities.Log("Password of first user of Windows VM has been updated");

                //=============================================================
                // Removes the extensions from Linux VM

                await (await virtualMachineExtensions
                    .StartDeleteAsync(rgName, windowsVmName, windowsVmAccessExtensionName)).WaitForCompletionAsync();

                Utilities.Log("Removed the VM Access extensions from Windows VM");
                Utilities.PrintVirtualMachine(windowsVM);
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
