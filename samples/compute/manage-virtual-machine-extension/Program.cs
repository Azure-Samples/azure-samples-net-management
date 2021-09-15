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

namespace ManageVirtualMachineExtension
{
    public class Program
    {
        // Linux configurations
        readonly static string FirstLinuxUserName = "tirekicker";
        readonly static string FirstLinuxUserPassword = "<password>";
        readonly static string FirstLinuxUserNewPassword = "<password>";

        readonly static string SecondLinuxUserName = "seconduser";
        readonly static string SecondLinuxUserPassword = "<password>";
        readonly static string SecondLinuxUserExpiration = "2020-12-31";

        readonly static string ThirdLinuxUserName = "thirduser";
        readonly static string ThirdLinuxUserPassword = "<password>";
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
        readonly static string firstWindowsUserName = "tirekicker";
        readonly static string firstWindowsUserPassword = "<password>";
        readonly static string firstWindowsUserNewPassword = "<password>";

        readonly static string secondWindowsUserName = "seconduser";
        readonly static string secondWindowsUserPassword = "<password>";

        readonly static string thirdWindowsUserName = "thirduser";
        readonly static string thirdWindowsUserPassword = "<password>";

        readonly static string windowsVmAccessExtensionName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionPublisherName = "Microsoft.Compute";
        readonly static string windowsVmAccessExtensionTypeName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionVersionName = "2.3";

        //Azure Compute sample for managing virtual machine extensions. -
        //   - Create a Linux and Windows virtual machine
        //   - Add three users(user names and passwords for windows, SSH keys for Linux)
        //   - Resets user credentials
        //   - Remove a user
        //   - Install MySQL on Linux | something significant on Windows
        //   - Remove extensions

        public static async Task RunSample(TokenCredential credential)
        {
            string resourceGroupName = "rgCOVE";
            string linuxVmName = "lVM";
            string windowsVmName = "wVM";
            string pipDnsLabelLinuxVM = "rgPip1";
            string pipDnsLabelWindowsVM = "rgPip2";
            Location location = Location.EastUS;
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            ArmClient client = new ArmClient(subscriptionId, credential);

            ResourceGroupContainer resourceGroupContainer = client.DefaultSubscription.GetResourceGroups();
            ResourceGroupData resourceGroupData = new ResourceGroupData(Location.EastUS);
            ResourceGroup resourceGroup = await resourceGroupContainer.CreateOrUpdate(resourceGroupName, resourceGroupData).WaitForCompletionAsync();

            try
            {
                // Create a Linux VM with root (sudo) user

                // Create IP Address
                Console.WriteLine("Creating a IP Address");
                PublicIPAddressData ipAddressData = new PublicIPAddressData
                {
                    Location = location,
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                };

                PublicIPAddress ipAddress = await resourceGroup.GetPublicIPAddresses().CreateOrUpdate(pipDnsLabelLinuxVM, ipAddressData).WaitForCompletionAsync();

                Console.WriteLine("Created a IP Address");

                // Create VNet
                Console.WriteLine("Creating a VNet");
                VirtualNetworkData vnetData = new VirtualNetworkData
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
                VirtualNetwork vnet = await resourceGroup.GetVirtualNetworks().CreateOrUpdate(linuxVmName + "_vent", vnetData).WaitForCompletionAsync();

                Console.WriteLine("Created a VNet");

                // Create Network Interface
                Console.WriteLine("Creating a Network Interface");
                NetworkInterfaceData nicData = new NetworkInterfaceData
                {
                    Location = location,
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = vnet.Data.Subnets.First(),
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = ipAddress.Data
                        }
                    }
                };
                NetworkInterface nic = await resourceGroup.GetNetworkInterfaces().CreateOrUpdate(linuxVmName + "_nic", nicData).WaitForCompletionAsync();

                Console.WriteLine("Created a Network Interface");

                // Create VM
                Console.WriteLine("Creating a Linux VM");

                VirtualMachineData linuxVMData = new VirtualMachineData(location)
                {
                    NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile { NetworkInterfaces = { new NetworkInterfaceReference { Id = nic.Id } } },
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
                        }
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                VirtualMachine linuxVM = await resourceGroup.GetVirtualMachines().CreateOrUpdate(linuxVmName, linuxVMData).WaitForCompletionAsync();

                Console.WriteLine("Created a Linux VM:" + linuxVM.Id);

                // Add a second sudo user to Linux VM using VMAccess extension
                VirtualMachineExtensionData vmExtensionData = new VirtualMachineExtensionData(location)
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

                VirtualMachineExtensionVirtualMachine vmExtension = await linuxVM.GetVirtualMachineExtensionVirtualMachines().CreateOrUpdate(linuxVmAccessExtensionName, vmExtensionData).WaitForCompletionAsync();

                Console.WriteLine("Added a second sudo user to the Linux VM");

                // Add a third sudo user to Linux VM by updating VMAccess extension
                VirtualMachineExtensionUpdate vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", ThirdLinuxUserName },
                        { "password", ThirdLinuxUserPassword },
                        { "expiration", ThirdLinuxUserExpiration }
                    }
                };

                vmExtension = await vmExtension.Update(vmExtensionUpdate).WaitForCompletionAsync();

                Console.WriteLine("Added a third sudo user to the Linux VM");

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

                vmExtension = await vmExtension.Update(vmExtensionUpdate).WaitForCompletionAsync();

                Console.WriteLine("Password of first user of Linux VM has been updated");

                // Removes the second sudo user from Linux VM using VMAccess extension
                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "remove_user", SecondLinuxUserName },
                    }
                };

                vmExtension = await vmExtension.Update(vmExtensionUpdate).WaitForCompletionAsync();

                Console.WriteLine("Removed the second user from Linux VM using VMAccess extension");

                // Install MySQL in Linux VM using CustomScript extension
                vmExtensionData = new VirtualMachineExtensionData(location)
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

                VirtualMachineExtensionVirtualMachine vmCustomScriptExtension = await linuxVM.GetVirtualMachineExtensionVirtualMachines().CreateOrUpdate(LinuxCustomScriptExtensionName, vmExtensionData).WaitForCompletionAsync();

                Console.WriteLine("Installed MySql using custom script extension");

                // Removes the extensions from Linux VM
                await vmCustomScriptExtension.DeleteAsync();
                await vmExtension.DeleteAsync();

                Console.WriteLine("Removed the custom script and VM Access extensions from Linux VM");

                // Create a Windows VM with admin user
                // Create IP Address
                Console.WriteLine("Creating a IP Address");
                ipAddressData = new PublicIPAddressData
                {
                    PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                    Location = location,
                };

                PublicIPAddress windowsIPAddress = await resourceGroup.GetPublicIPAddresses().CreateOrUpdate(pipDnsLabelWindowsVM, ipAddressData).WaitForCompletionAsync();

                Console.WriteLine("Created a IP Address");

                // Create Network Interface #2
                Console.WriteLine("Creating a Network Interface #2");
                nicData = new NetworkInterfaceData
                {
                    Location = location,
                    IpConfigurations =
                    {
                        new NetworkInterfaceIPConfiguration
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = vnet.Data.Subnets.First(),
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            PublicIPAddress = windowsIPAddress.Data,
                        }
                    }
                };
                NetworkInterface windowsNIC = await resourceGroup.GetNetworkInterfaces().CreateOrUpdate(windowsVmName + "_nic", nicData).WaitForCompletionAsync();

                Console.WriteLine("Created a Network Interface");

                // Create Windows VM
                Console.WriteLine("Creating a Windows VM");

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
                    },
                    HardwareProfile = new HardwareProfile { VmSize = VirtualMachineSizeTypes.StandardD3V2 },
                };

                VirtualMachine windowsVM = await resourceGroup.GetVirtualMachines().CreateOrUpdate(windowsVmName, windowsVMData).WaitForCompletionAsync();

                vmExtensionData = new VirtualMachineExtensionData(location)
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

                VirtualMachineExtensionVirtualMachine windowsVMCustomScriptExtension = await windowsVM.GetVirtualMachineExtensionVirtualMachines().CreateOrUpdate(windowsCustomScriptExtensionName, vmExtensionData).WaitForCompletionAsync();

                Console.WriteLine("Created a Windows VM:" + windowsVM.Id);

                // Add a second admin user to Windows VM using VMAccess extension
                vmExtensionData = new VirtualMachineExtensionData(location)
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

                VirtualMachineExtensionVirtualMachine windowsVMAccessExtension = await windowsVM.GetVirtualMachineExtensionVirtualMachines().CreateOrUpdate(windowsVmAccessExtensionName, vmExtensionData).WaitForCompletionAsync();

                Console.WriteLine("Added a second admin user to the Windows VM");

                // Add a third admin user to Windows VM by updating VMAccess extension
                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", thirdWindowsUserName },
                        { "password", thirdWindowsUserPassword }
                    }
                };

                windowsVMAccessExtension = await windowsVMAccessExtension.Update(vmExtensionUpdate).WaitForCompletionAsync();

                Console.WriteLine("Added a third admin user to the Windows VM");

                // Reset admin password of first user of Windows VM by updating VMAccess extension
                vmExtensionUpdate = new VirtualMachineExtensionUpdate
                {
                    ProtectedSettings = new Dictionary<string, object>
                    {
                        { "username", firstWindowsUserName },
                        { "password", firstWindowsUserNewPassword }
                    }
                };

                windowsVMAccessExtension = await windowsVMAccessExtension.Update(vmExtensionUpdate).WaitForCompletionAsync();


                Console.WriteLine("Password of first user of Windows VM has been updated");

                // Removes the extensions from Linux VM
                await windowsVMAccessExtension.DeleteAsync();
                Console.WriteLine("Removed the VM Access extensions from Windows VM");
            }
            finally
            {
                try
                {
                    await resourceGroup.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
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
                Console.WriteLine(ex);
            }
        }
    }
}
