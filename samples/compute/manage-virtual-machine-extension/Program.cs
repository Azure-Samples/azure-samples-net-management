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

        public static async Task RunSample(ArmClient client)
        {
            string rgName = Utilities.RandomResourceName("rgCOVE", 15);
            string publicIpDnsLabel = Utilities.CreateRandomName("pip" + "-");
            string nicName = Utilities.CreateRandomName("nic");
            string linuxVmName = Utilities.RandomResourceName("lVM", 10);
            string windowsVmName = Utilities.RandomResourceName("wVM", 10);
            string pipDnsLabelLinuxVM = Utilities.RandomResourceName("rgPip1", 25);
            string pipDnsLabelWindowsVM = Utilities.RandomResourceName("rgPip2", 25);
            string location = AzureLocation.EastUS;
            string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            try
            {
                ResourceGroupResource resourceGroup = (await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(location))).Value;

                // Create a Linux VM with root (sudo) user

                // Create IP Address
                Utilities.Log("Creating a IP Address");
                var publicIpAddressCollection = resourceGroup.GetPublicIPAddresses();
                var publicIPAddressData = new PublicIPAddressData()
                {
                    DnsSettings =
                            {
                                DomainNameLabel = publicIpDnsLabel
                            }
                };
                var ipAddress = await publicIpAddressCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, pipDnsLabelLinuxVM, publicIPAddressData);

                Utilities.Log("Created a IP Address");

                // Create VNet
                Utilities.Log("Creating a VNet");
                var vnetData = new VirtualNetworkData()
                {
                    Location = location,
                    AddressPrefixes = { "10.0.0.0/16" },
                    Subnets = { new SubnetData() { Name = "SubnetSampleName", AddressPrefix = "10.0.0.0/28" } }
                };
                var vnetCollection = resourceGroup.GetVirtualNetworks();

                var vnet = (await vnetCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, "vnetSample", vnetData)).Value;

                Utilities.Log("Created a VNet");

                // Create Network Interface
                Utilities.Log("Creating a Network Interface");
                var nicData = new NetworkInterfaceData()
                {
                    Location = location,
                    IPConfigurations = {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "SampleIpConfigName",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Primary = true,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Data.Subnets.ElementAt(0).Id
                            },
                            PrivateIPAddress = ipAddress.Id
                        }
                    }
                };
                var nicCollection = resourceGroup.GetNetworkInterfaces();
                var nic = (await nicCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, nicName, nicData)).Value;

                Utilities.Log("Created a Network Interface");

                // Create VM
                Utilities.Log("Creating a Linux VM");

                var linuxVMData = new VirtualMachineData(location)
                {
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
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = linuxVmName,
                        AdminUsername = FirstLinuxUserName,
                        AdminPassword = FirstLinuxUserNewPassword,
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
                var vmCollection = resourceGroup.GetVirtualMachines();
                var linuxVM = (await vmCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, linuxVmName, linuxVMData)).Value;

                Utilities.Log("Created a Linux VM:" + linuxVM.Id);
                Utilities.PrintVirtualMachine(linuxVM);

                // Add a second sudo user to Linux VM using VMAccess extension

                var virtualMachineExtensionCollection = linuxVM.GetVirtualMachineExtensions();
                var linuxSettings = new
                {
                    username = SecondLinuxUserName,
                    password = SecondLinuxUserPassword,
                    expiration = SecondLinuxUserExpiration
                };
                var linuxBinaryData = BinaryData.FromObjectAsJson(linuxSettings);
                var vmExtensionData = new VirtualMachineExtensionData(location)
                {
                    Publisher = linuxVmAccessExtensionPublisherName,
                    ExtensionType = linuxVmAccessExtensionTypeName,
                    TypeHandlerVersion = linuxVmAccessExtensionVersionName,
                    ProtectedSettings = linuxBinaryData
                };

                var vmExtension = (await virtualMachineExtensionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, linuxVmAccessExtensionName, vmExtensionData)).Value;

                Utilities.Log("Added a second sudo user to the Linux VM");

                // Add a third sudo user to Linux VM by updating VMAccess extension

                var linuxSettings1 = new
                {
                    username = SecondLinuxUserName,
                    password = SecondLinuxUserPassword,
                    expiration = SecondLinuxUserExpiration
                };
                var linuxBinaryData1 = BinaryData.FromObjectAsJson(linuxSettings1);
                var vmExtensionUpdatePatch = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = linuxBinaryData1
                };

                var vmExtensionUpdate = (await vmExtension.UpdateAsync(Azure.WaitUntil.Completed, vmExtensionUpdatePatch)).Value;

                var linuxSettings2 = new
                {
                    username = ThirdLinuxUserName,
                    password = ThirdLinuxUserPassword,
                    expiration = ThirdLinuxUserExpiration
                };
                var linuxBinaryData2 = BinaryData.FromObjectAsJson(linuxSettings2);
                var vmExtensionUpdatePatch2 = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = linuxBinaryData2
                };

                var vmExtensionUpdate2 = (await vmExtension.UpdateAsync(Azure.WaitUntil.Completed, vmExtensionUpdatePatch2)).Value;

                Utilities.Log("Added a third sudo user to the Linux VM");

                // Reset ssh password of first user of Linux VM by updating VMAccess extension

                var linuxSettings3 = new
                {
                    username = FirstLinuxUserName,
                    password = FirstLinuxUserNewPassword,
                    reset_ssh = true
                };
                var linuxBinaryData3 = BinaryData.FromObjectAsJson(linuxSettings3);
                var vmExtensionUpdatePatch3 = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = linuxBinaryData3
                };

                var vmExtensionUpdate3 = (await vmExtension.UpdateAsync(Azure.WaitUntil.Completed, vmExtensionUpdatePatch3)).Value;

                Utilities.Log("Password of first user of Linux VM has been updated");

                // Removes the second sudo user from Linux VM using VMAccess extension

                var linuxSettings4 = new
                {
                    remove_user = SecondLinuxUserName
                };
                var linuxBinaryData4 = BinaryData.FromObjectAsJson(linuxSettings4);
                var vmExtensionUpdatePatch4 = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = linuxBinaryData4
                };

                var vmExtensionUpdate4 = (await vmExtension.UpdateAsync(Azure.WaitUntil.Completed, vmExtensionUpdatePatch4)).Value;

                Utilities.Log("Removed the second user from Linux VM using VMAccess extension");

                // Install MySQL in Linux VM using CustomScript extension

                var linuxSettings5 = new
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
                var linuxBinaryData5 = BinaryData.FromObjectAsJson(linuxSettings5);
                var vmExtensionUpdatePatch5 = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = linuxBinaryData5
                };

                var vmExtensionUpdate5 = (await vmExtension.UpdateAsync(Azure.WaitUntil.Completed, vmExtensionUpdatePatch5)).Value;

                Utilities.Log("Installed MySql using custom script extension");
                Utilities.PrintVirtualMachine(linuxVM);

                // Removes the extensions from Linux VM

                await vmExtensionUpdate5.DeleteAsync(Azure.WaitUntil.Completed);
                await vmExtensionUpdate4.DeleteAsync(Azure.WaitUntil.Completed);

                Utilities.Log("Removed the custom script and VM Access extensions from Linux VM");
                Utilities.PrintVirtualMachine(linuxVM);

                // Create a Windows VM with admin user

                // Create IP Address

                Utilities.Log("Creating a IP Address");
                var windowsIpAddressData = new PublicIPAddressData()
                {
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Location = location,
                };

                var windowsIpAddress = await publicIpAddressCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, pipDnsLabelWindowsVM, windowsIpAddressData);

                Utilities.Log("Created a IP Address");

                // Create Network Interface #2

                Utilities.Log("Creating a Network Interface #2");
                var nicData2 = new NetworkInterfaceData()
                {
                    Location = location,
                    IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "Primary",
                        Primary = true,
                            Subnet = new SubnetData()
                            {
                                Id = vnet.Data.Subnets.ElementAt(0).Id
                            },
                            PrivateIPAddress = ipAddress.Id
                    }
                }
                };
                var nic2 = (await nicCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, windowsVmName + "_nic", nicData2)).Value;

                Utilities.Log("Created a Network Interface");

                // Create Windows VM

                Utilities.Log("Creating a Windows VM");

                var windowsVMData = new VirtualMachineData(location)
                {
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic.Id,
                            }
                        }
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = windowsVmName,
                        AdminUsername = firstWindowsUserName,
                        AdminPassword = firstWindowsUserPassword,
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
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
                        }
                    },
                    HardwareProfile = new VirtualMachineHardwareProfile() { VmSize = VirtualMachineSizeType.StandardD3V2 },
                };

                var windowsVM = (await vmCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, windowsVmName, windowsVMData)).Value;
                var windowsVirtualMachineExtensionCollection = windowsVM.GetVirtualMachineExtensions();
                var windowsSettings = new
                {
                    fileUris = mySQLWindowsInstallScriptFileUris,
                    commandToExecute = mySqlScriptWindowsInstallCommand,
                };
                var windowsBinaryData = BinaryData.FromObjectAsJson(windowsSettings);
                var vmExtensionData2 = new VirtualMachineExtensionData(location)
                {
                    Publisher = windowsCustomScriptExtensionPublisherName,
                    ExtensionType = windowsCustomScriptExtensionTypeName,
                    TypeHandlerVersion = windowsCustomScriptExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    Settings = windowsBinaryData
                };

                var windowsExtension = (await windowsVirtualMachineExtensionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, windowsCustomScriptExtensionName, vmExtensionData)).Value;

                Utilities.Log("Created a Windows VM:" + windowsVM.Id);
                Utilities.PrintVirtualMachine(windowsVM);

                // Add a second admin user to Windows VM using VMAccess extension

                var windowsSettings1 = new
                {
                    username = secondWindowsUserName,
                    password = secondWindowsUserPassword,
                };
                var windowsBinaryData1 = BinaryData.FromObjectAsJson(windowsSettings1);
                var vmExtensionWindowsData = new VirtualMachineExtensionData(location)
                {
                    Publisher = windowsVmAccessExtensionPublisherName,
                    ExtensionType = windowsVmAccessExtensionTypeName,
                    TypeHandlerVersion = windowsVmAccessExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    ProtectedSettings = windowsBinaryData1
                };

                var windowsExtension1 = (await windowsVirtualMachineExtensionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, windowsVmAccessExtensionName, vmExtensionData)).Value;

                Utilities.Log("Added a second admin user to the Windows VM");

                // Add a third admin user to Windows VM by updating VMAccess extension

                var windowsSettings2 = new
                {
                    username = thirdWindowsUserName,
                    password = thirdWindowsUserPassword,
                };
                var windowsBinaryData2 = BinaryData.FromObjectAsJson(windowsSettings2);
                var windowsExtensionUpdatePatch = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = windowsBinaryData2
                };

                var windowsExtensionUpdate = (await windowsExtension.UpdateAsync(Azure.WaitUntil.Completed, windowsExtensionUpdatePatch)).Value;

                Utilities.Log("Added a third admin user to the Windows VM");

                // Reset admin password of first user of Windows VM by updating VMAccess extension

                var windowsSettings3 = new
                {
                    username = firstWindowsUserName,
                    password = firstWindowsUserNewPassword,
                };
                var windowsBinaryData3 = BinaryData.FromObjectAsJson(windowsSettings3);
                var windowsExtensionUpdatePatch2 = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = windowsBinaryData3
                };

                var windowsExtensionUpdate2 = (await windowsExtension.UpdateAsync(Azure.WaitUntil.Completed, windowsExtensionUpdatePatch2)).Value;


                Utilities.Log("Password of first user of Windows VM has been updated");

                // Removes the extensions from Linux VM

                await windowsExtension1.DeleteAsync(Azure.WaitUntil.Completed);

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
