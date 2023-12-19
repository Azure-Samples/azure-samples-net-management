// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.ResourceManager.AppConfiguration.Models;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Communication.Models;
using Azure.ResourceManager.EventHubs;

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

        public static void PrintNameSpace(EventHubsNamespaceResource resource)
        {
            StringBuilder eh = new StringBuilder("Eventhub Namespace: ")
                .Append("Eventhub Namespace: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Data.Name)
                    .Append("\n\tLocation: ").Append(resource.Data.Location)
                    .Append("\n\tTags: ").Append(resource.Data.Tags.ToString())
                    .Append("\n\tAzureInsightMetricId: ").Append(resource.Data.MetricId)
                    .Append("\n\tIsAutoInflate enabled: ").Append(resource.Data.IsAutoInflateEnabled)
                    .Append("\n\tServiceBus endpoint: ").Append(resource.Data.ServiceBusEndpoint)
                    .Append("\n\tMaximum Throughput Units: ").Append(resource.Data.MaximumThroughputUnits)
                    .Append("\n\tCreated time: ").Append(resource.Data.CreatedOn)
                    .Append("\n\tUpdated time: ").Append(resource.Data.UpdatedOn);
            Utilities.Log(eh.ToString());
        }

        public static void PrintEventHub(EventHubResource resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("Eventhub: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Data.Name)
                    .Append("\n\tMessage retention in Days: ").Append(resource.Data.MessageRetentionInDays)
                    .Append("\n\tPartition ids: ").Append(resource.Data.PartitionIds);
            if (resource.Data.CaptureDescription != null)
            {
                info.Append("\n\t\t\tSize limit in Bytes: ").Append(resource.Data.CaptureDescription.SizeLimitInBytes);
                info.Append("\n\t\t\tInterval in seconds: ").Append(resource.Data.CaptureDescription.IntervalInSeconds);
                if (resource.Data.CaptureDescription.Destination != null)
                {
                    info.Append("\n\t\t\tData capture storage account: ").Append(resource.Data.CaptureDescription.Destination.StorageAccountResourceId);
                    info.Append("\n\t\t\tData capture storage container: ").Append(resource.Data.CaptureDescription.Destination.BlobContainer);
                }
            }
            Utilities.Log(info.ToString());
        }

        public static void PrintConsumerGroup(EventHubsConsumerGroupResource resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("Event hub consumer group: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Data.Name)
                    .Append("\n\tUser metadata: ").Append(resource.Data.UserMetadata);
            Utilities.Log(info.ToString());
        }

        public static void PrintDisasterRecovery(EventHubsDisasterRecoveryResource resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("DisasterRecoveryPairing: ").Append(resource.Id)
                    .Append("\n\tName: ").Append(resource.Data.Name)
                    .Append("\n\tAlternate name: ").Append(resource.Data.AlternateName)
                    .Append("\n\tPartner namespace: ").Append(resource.Data.PartnerNamespace)
                    .Append("\n\tNamespace role: ").Append(resource.Data.Role);
            Utilities.Log(info.ToString());
        }

        public static void PrintAccessKey(EventHubsAccessKeys resource)
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

        public static void Print(CommunicationServiceResource resource)
        {
            StringBuilder info = new StringBuilder();
            info.Append("CommunicationServiceResource")
                    .Append("\n\t Name: ").Append(resource.Name)
                    .Append("\n\t ProvisioningState: ").Append(resource.ProvisioningState)
                    .Append("\n\t HostName: ").Append(resource.HostName)
                    .Append("\n\t DataLocation: ").Append(resource.DataLocation)
                    .Append("\n\t NotificationHubId: ").Append(resource.NotificationHubId)
                    .Append("\n\t ImmutableResourceId: ").Append(resource.ImmutableResourceId)
                    .Append("\n\t Location: ").Append(resource.Location);

            string tags = "None";
            if (resource.Tags != null)
            {
                tags = string.Join(", ", resource.Tags.Select(kvp => kvp.Key + ": " + kvp.Value.ToString()));
            }
            info.Append("\n\t Tags: " + tags);

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

        public static void PrintAppConfiguration(ConfigurationStore configurationStore)
        {
            var info = new StringBuilder().Append("App Configuration: ").Append(configurationStore.Id)
                .Append("Name: ").Append(configurationStore.Name)
                .Append("\n\tLocation: ").Append(configurationStore.Location)
                .Append("\n\tSku: ").Append(configurationStore.Sku.Name);

            Utilities.Log(info.ToString());
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
