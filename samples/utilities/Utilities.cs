// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Network.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Utilities
{
    public static class Utilities
    {
        public static bool IsRunningMocked { get; set; }
        public static Action<string> LoggerMethod { get; set; }
        public static Func<string> PauseMethod { get; set; }
        public static string ProjectPath { get; set; }

        static Utilities()
        {
            LoggerMethod = Console.WriteLine;
            PauseMethod = Console.ReadLine;
            ProjectPath = ".";
        }

        public static void Log(string message)
        {
            LoggerMethod.Invoke(message);
        }

        public static void Log(object obj)
        {
            if (obj != null)
            {
                LoggerMethod.Invoke(obj.ToString());
            }
            else
            {
                LoggerMethod.Invoke("(null)");
            }
        }

        public static void Log()
        {
            Utilities.Log(string.Empty);
        }

        public static string GetArmTemplate(string templateFileName)
        {
            var hostingPlanName = RandomResourceName("hpRSAT", 24);
            var webAppName = RandomResourceName("wnRSAT", 24);
            var armTemplateString = File.ReadAllText(Path.Combine(Utilities.ProjectPath, "Asset", templateFileName));

            if (string.Equals("ArmTemplate.json", templateFileName, StringComparison.OrdinalIgnoreCase))
            {
                armTemplateString = armTemplateString.Replace("\"hostingPlanName\": {\r\n      \"type\": \"string\",\r\n      \"defaultValue\": \"\"",
                   "\"hostingPlanName\": {\r\n      \"type\": \"string\",\r\n      \"defaultValue\": \"" + hostingPlanName + "\"");
                armTemplateString = armTemplateString.Replace("\"webSiteName\": {\r\n      \"type\": \"string\",\r\n      \"defaultValue\": \"\"",
                    "\"webSiteName\": {\r\n      \"type\": \"string\",\r\n      \"defaultValue\": \"" + webAppName + "\"");
            }
            
            return armTemplateString;
        }

        public static void Print(EHNamespace resource)
        {
            StringBuilder eh = new StringBuilder("Eventhub Namespace: ")
                .Append("Eventhub Namespace: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Name)
                    .Append("\n\tLocation: ").Append(resource.Location)
                    .Append("\n\tTags: ").Append(resource.Tags.ToString())
                    .Append("\n\tAzureInsightMetricId: ").Append(resource.MetricId)
                    .Append("\n\tIsAutoInflate enabled: ").Append(resource.IsAutoInflateEnabled)
                    .Append("\n\tServiceBus endpoint: ").Append(resource.ServiceBusEndpoint)
                    .Append("\n\tMaximum Throughput Units: ").Append(resource.MaximumThroughputUnits)
                    .Append("\n\tCreated time: ").Append(resource.CreatedAt)
                    .Append("\n\tUpdated time: ").Append(resource.UpdatedAt);
            Utilities.Log(eh.ToString());
        }

        public static void Print(Eventhub resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("Eventhub: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Name)
                    .Append("\n\tMessage retention in Days: ").Append(resource.MessageRetentionInDays)
                    .Append("\n\tPartition ids: ").Append(resource.PartitionIds);
            if (resource.CaptureDescription != null)
            {
                info.Append("\n\t\t\tSize limit in Bytes: ").Append(resource.CaptureDescription.SizeLimitInBytes);
                info.Append("\n\t\t\tInterval in seconds: ").Append(resource.CaptureDescription.IntervalInSeconds);
                if (resource.CaptureDescription.Destination != null)
                {
                    info.Append("\n\t\t\tData capture storage account: ").Append(resource.CaptureDescription.Destination.StorageAccountResourceId);
                    info.Append("\n\t\t\tData capture storage container: ").Append(resource.CaptureDescription.Destination.BlobContainer);
                }
            }
            Utilities.Log(info.ToString());
        }

        public static void Print(ConsumerGroup resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("Event hub consumer group: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Name)
                    .Append("\n\tUser metadata: ").Append(resource.UserMetadata);
            Utilities.Log(info.ToString());
        }

        public static void Print(ArmDisasterRecovery resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("DisasterRecoveryPairing: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Name)
                    .Append("\n\tAlternate name: ").Append(resource.AlternateName)
                    .Append("\n\tPartner namespace: ").Append(resource.PartnerNamespace)
                    .Append("\n\tNamespace role: ").Append(resource.Role);
            Utilities.Log(info.ToString());
        }

        public static void Print(AccessKeys resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("DisasterRecoveryPairing auth key: ")
                    .Append("\n\t Alias primary connection string: ").Append(resource.AliasPrimaryConnectionString)
                    .Append("\n\t Alias secondary connection string: ").Append(resource.AliasSecondaryConnectionString)
                    .Append("\n\t Primary key: ").Append(resource.PrimaryKey)
                    .Append("\n\t Secondary key: ").Append(resource.SecondaryKey)
                    .Append("\n\t Primary connection string: ").Append(resource.PrimaryConnectionString)
                    .Append("\n\t Secondary connection string: ").Append(resource.SecondaryConnectionString);
            Utilities.Log(info.ToString());
        }

        private static string FormatCollection(IEnumerable<string> collection)
        {
            return string.Join(", ", collection);
        }

        private static string FormatDictionary(IDictionary<string, string> dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

            var outputString = new StringBuilder();

            foreach (var entity in dictionary)
            {
                outputString.AppendLine($"{entity.Key}: {entity.Value}");
            }

            return outputString.ToString();
        }

        public static void PrintVault(Vault vault)
        {
            var info = new StringBuilder().Append("Key Vault: ").Append(vault.Id)
                .Append("Name: ").Append(vault.Name)
                .Append("\n\tLocation: ").Append(vault.Location)
                .Append("\n\tSku: ").Append(vault.Properties.Sku.Name).Append(" - ").Append(vault.Properties.Sku.Family)
                .Append("\n\tVault URI: ").Append(vault.Properties.VaultUri)
                .Append("\n\tAccess policies: ");
            foreach (var accessPolicy in vault.Properties.AccessPolicies)
            {
                info.Append("\n\t\tIdentity:").Append(accessPolicy.ObjectId);
                if (accessPolicy.Permissions.Keys != null)
                {
                    info.Append("\n\t\tKey permissions: ").Append(FormatCollection(accessPolicy.Permissions.Keys.Select(key => key.ToString())));
                }
                if (accessPolicy.Permissions.Secrets != null)
                {
                    info.Append("\n\t\tSecret permissions: ").Append(FormatCollection(accessPolicy.Permissions.Secrets.Select(secret => secret.ToString())));
                }      
            }

            Utilities.Log(info.ToString());
        }

        public static void PrintNetworkSecurityGroup(NetworkSecurityGroup resource)
        {
            var nsgOutput = new StringBuilder();
            nsgOutput.Append("NSG: ").Append(resource.Id)
                    .Append("Name: ").Append(resource.Name)
                    .Append("\n\tLocation: ").Append(resource.Location)
                    .Append("\n\tTags: ").Append(FormatDictionary(resource.Tags));

            // Output security rules
            foreach (var rule in resource.SecurityRules)
            {
                nsgOutput.Append("\n\tRule: ").Append(rule.Name)
                        .Append("\n\t\tAccess: ").Append(rule.Access)
                        .Append("\n\t\tDirection: ").Append(rule.Direction)
                        .Append("\n\t\tFrom address: ").Append(rule.SourceAddressPrefix)
                        .Append("\n\t\tFrom port range: ").Append(rule.SourcePortRange)
                        .Append("\n\t\tTo address: ").Append(rule.DestinationAddressPrefix)
                        .Append("\n\t\tTo port: ").Append(rule.DestinationPortRange)
                        .Append("\n\t\tProtocol: ").Append(rule.Protocol)
                        .Append("\n\t\tPriority: ").Append(rule.Priority);
            }
            Utilities.Log(nsgOutput.ToString());
        }

        public static void PrintVirtualNetwork(VirtualNetwork network)
        {
            var info = new StringBuilder();
            info.Append("Network: ").Append(network.Id)
                    .Append("Name: ").Append(network.Name)
                    .Append("\n\tLocation: ").Append(network.Location)
                    .Append("\n\tTags: ").Append(FormatDictionary(network.Tags))
                    .Append("\n\tAddress space: ").Append(network.AddressSpace);

            // Output subnets
            foreach (var subnet in network.Subnets)
            {
                info.Append("\n\tSubnet: ").Append(subnet.Name)
                        .Append("\n\t\tAddress prefix: ").Append(subnet.AddressPrefix);

                // Output associated NSG
                var subnetNsg = subnet.NetworkSecurityGroup;
                if (subnetNsg != null)
                {
                    info.Append("\n\t\tNetwork security group: ").Append(subnetNsg.Id);
                }

                // Output associated route table
                var routeTable = subnet.RouteTable;
                if (routeTable != null)
                {
                    info.Append("\n\tRoute table ID: ").Append(routeTable.Id);
                }
            }

            // Output peerings
            foreach (var peering in network.VirtualNetworkPeerings)
            {
                info.Append("\n\tPeering: ").Append(peering.Name)
                    .Append("\n\t\tRemote network ID: ").Append(peering.RemoteVirtualNetwork.Id)
                    .Append("\n\t\tPeering state: ").Append(peering.PeeringState)
                    .Append("\n\t\tIs traffic forwarded from remote network allowed? ").Append(peering.AllowForwardedTraffic)
                    .Append("\n\t\tGateway use: ").Append(peering.UseRemoteGateways);
            }

            Utilities.Log(info.ToString());
        }

        public static void PrintVirtualMachine(VirtualMachine virtualMachine)
        {
            var storageProfile = new StringBuilder().Append("\n\tStorageProfile: ");
            if (virtualMachine.StorageProfile.ImageReference != null)
            {
                storageProfile.Append("\n\t\tImageReference:");
                storageProfile.Append("\n\t\t\tPublisher: ").Append(virtualMachine.StorageProfile.ImageReference.Publisher);
                storageProfile.Append("\n\t\t\tOffer: ").Append(virtualMachine.StorageProfile.ImageReference.Offer);
                storageProfile.Append("\n\t\t\tSKU: ").Append(virtualMachine.StorageProfile.ImageReference.Sku);
                storageProfile.Append("\n\t\t\tVersion: ").Append(virtualMachine.StorageProfile.ImageReference.Version);
            }

            if (virtualMachine.StorageProfile.OsDisk != null)
            {
                storageProfile.Append("\n\t\tOSDisk:");
                storageProfile.Append("\n\t\t\tOSType: ").Append(virtualMachine.StorageProfile.OsDisk.OsType);
                storageProfile.Append("\n\t\t\tName: ").Append(virtualMachine.StorageProfile.OsDisk.Name);
                storageProfile.Append("\n\t\t\tCaching: ").Append(virtualMachine.StorageProfile.OsDisk.Caching);
                storageProfile.Append("\n\t\t\tCreateOption: ").Append(virtualMachine.StorageProfile.OsDisk.CreateOption);
                storageProfile.Append("\n\t\t\tDiskSizeGB: ").Append(virtualMachine.StorageProfile.OsDisk.DiskSizeGB);
                if (virtualMachine.StorageProfile.OsDisk.Image != null)
                {
                    storageProfile.Append("\n\t\t\tImage Uri: ").Append(virtualMachine.StorageProfile.OsDisk.Image.Uri);
                }
                if (virtualMachine.StorageProfile.OsDisk.Vhd != null)
                {
                    storageProfile.Append("\n\t\t\tVhd Uri: ").Append(virtualMachine.StorageProfile.OsDisk.Vhd.Uri);
                }
                if (virtualMachine.StorageProfile.OsDisk.EncryptionSettings != null)
                {
                    storageProfile.Append("\n\t\t\tEncryptionSettings: ");
                    storageProfile.Append("\n\t\t\t\tEnabled: ").Append(virtualMachine.StorageProfile.OsDisk.EncryptionSettings.Enabled);
                    storageProfile.Append("\n\t\t\t\tDiskEncryptionKey Uri: ").Append(virtualMachine
                            .StorageProfile
                            .OsDisk
                            .EncryptionSettings
                            .DiskEncryptionKey.SecretUrl);
                    storageProfile.Append("\n\t\t\t\tKeyEncryptionKey Uri: ").Append(virtualMachine
                            .StorageProfile
                            .OsDisk
                            .EncryptionSettings
                            .KeyEncryptionKey.KeyUrl);
                }
            }

            if (virtualMachine.StorageProfile.DataDisks != null)
            {
                var i = 0;
                foreach (var disk in virtualMachine.StorageProfile.DataDisks)
                {
                    storageProfile.Append("\n\t\tDataDisk: #").Append(i++);
                    storageProfile.Append("\n\t\t\tName: ").Append(disk.Name);
                    storageProfile.Append("\n\t\t\tCaching: ").Append(disk.Caching);
                    storageProfile.Append("\n\t\t\tCreateOption: ").Append(disk.CreateOption);
                    storageProfile.Append("\n\t\t\tDiskSizeGB: ").Append(disk.DiskSizeGB);
                    storageProfile.Append("\n\t\t\tLun: ").Append(disk.Lun);
                    if (disk.ManagedDisk != null)
                    {
                        if (disk.ManagedDisk != null)
                        {
                            storageProfile.Append("\n\t\t\tManaged Disk Id: ").Append(disk.ManagedDisk.Id);
                        }
                    }
                    else
                    {
                        if (disk.Vhd.Uri != null)
                        {
                            storageProfile.Append("\n\t\t\tVhd Uri: ").Append(disk.Vhd.Uri);
                        }
                    }
                    if (disk.Image != null)
                    {
                        storageProfile.Append("\n\t\t\tImage Uri: ").Append(disk.Image.Uri);
                    }
                }
            }
            StringBuilder osProfile;
            if (virtualMachine.OsProfile != null)
            {
                osProfile = new StringBuilder().Append("\n\tOSProfile: ");

                osProfile.Append("\n\t\tComputerName:").Append(virtualMachine.OsProfile.ComputerName);
                if (virtualMachine.OsProfile.WindowsConfiguration != null)
                {
                    osProfile.Append("\n\t\t\tWindowsConfiguration: ");
                    osProfile.Append("\n\t\t\t\tProvisionVMAgent: ")
                            .Append(virtualMachine.OsProfile.WindowsConfiguration.ProvisionVMAgent);
                    osProfile.Append("\n\t\t\t\tEnableAutomaticUpdates: ")
                            .Append(virtualMachine.OsProfile.WindowsConfiguration.EnableAutomaticUpdates);
                    osProfile.Append("\n\t\t\t\tTimeZone: ")
                            .Append(virtualMachine.OsProfile.WindowsConfiguration.TimeZone);
                }
                if (virtualMachine.OsProfile.LinuxConfiguration != null)
                {
                    osProfile.Append("\n\t\t\tLinuxConfiguration: ");
                    osProfile.Append("\n\t\t\t\tDisablePasswordAuthentication: ")
                            .Append(virtualMachine.OsProfile.LinuxConfiguration.DisablePasswordAuthentication);
                }
            }
            else
            {
                osProfile = new StringBuilder().Append("\n\tOSProfile: null");
            }


            var networkProfile = new StringBuilder().Append("\n\tNetworkProfile: ");
            foreach (var networkInterfaceId in virtualMachine.NetworkProfile.NetworkInterfaces)
            {
                networkProfile.Append("\n\t\tId:").Append(networkInterfaceId.Id);
            }

            var msi = new StringBuilder().Append("\n\tMSI: ");
            if (virtualMachine.Identity != null && virtualMachine.Identity.UserAssignedIdentities != null)
            {
                msi.Append("\n\t\tMSI enabled:").Append("True");
                foreach (var item in virtualMachine.Identity.UserAssignedIdentities)
                { 
                    msi.Append("\n\t\tMSI Active Directory Service Name:").Append(item.Key);
                    msi.Append("\n\t\tMSI Active Directory Service Principal Id:").Append(item.Value.PrincipalId);
                    msi.Append("\n\t\tMSI Active Directory Client Id:").Append(item.Value.PrincipalId);
                }
            }
            else
            {
                msi.Append("\n\t\tMSI enabled:").Append("False");
            }

            Utilities.Log(new StringBuilder().Append("Virtual Machine: ").Append(virtualMachine.Id)
                    .Append("Name: ").Append(virtualMachine.Name)
                    .Append("\n\tLocation: ").Append(virtualMachine.Location)
                    .Append("\n\tTags: ").Append(FormatDictionary(virtualMachine.Tags))
                    .Append("\n\tHardwareProfile: ")
                    .Append("\n\t\tSize: ").Append(virtualMachine.HardwareProfile.VmSize)
                    .Append(storageProfile)
                    .Append(osProfile)
                    .Append(networkProfile)
                    .Append(msi)
                    .ToString());
        }

        public static void PrintIPAddress(PublicIPAddress publicIPAddress)
        {
            Utilities.Log(new StringBuilder().Append("Public IP Address: ").Append(publicIPAddress.Id)
                .Append("Name: ").Append(publicIPAddress.Name)
                .Append("\n\tLocation: ").Append(publicIPAddress.Location)
                .Append("\n\tTags: ").Append(FormatDictionary(publicIPAddress.Tags))
                .Append("\n\tIP Address: ").Append(publicIPAddress.IpAddress)
                .Append("\n\tLeaf domain label: ").Append(publicIPAddress.DnsSettings.DomainNameLabel)
                .Append("\n\tFQDN: ").Append(publicIPAddress.DnsSettings.Fqdn)
                .Append("\n\tReverse FQDN: ").Append(publicIPAddress.DnsSettings.ReverseFqdn)
                .Append("\n\tIdle timeout (minutes): ").Append(publicIPAddress.IdleTimeoutInMinutes)
                .Append("\n\tIP allocation method: ").Append(publicIPAddress.PublicIPAllocationMethod)
                .ToString());
        }

        /// <summary>
        /// Generates the specified number of random resource names with the same prefix.
        /// </summary>
        /// <param name="prefix">the prefix to be used if possible</param>
        /// <param name="maxLen">the maximum length for the random generated name</param>
        /// <param name="count">the number of names to generate</param>
        /// <returns>random names</returns>
        public static string[] RandomResourceNames(string prefix, int maxLen, int count)
        {
            string[] names = new string[count];
            var resourceNamer = new ResourceNamer("");
            for (int i = 0; i < count; i++)
            {
                names[i] = resourceNamer.RandomName(prefix, maxLen);
            }
            return names;
        }

        public static string RandomResourceName(string prefix, int maxLen)
        {
            var namer = new ResourceNamer("");
            return namer.RandomName(prefix, maxLen);
        }
        public static string RandomGuid()
        {
            var namer = new ResourceNamer("");
            return namer.RandomGuid();
        }

        public static string CreateRandomName(string namePrefix)
        {
            return RandomResourceName(namePrefix, 15);
        }

        public static async Task<List<T>> ToEnumerableAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            List<T> list = new List<T>();
            await foreach (T item in asyncEnumerable)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
