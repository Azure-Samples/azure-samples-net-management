using System;
using Azure.Core;
using Azure.ResourceManager.Storage.Models;
using CreateStorageSample;
using Newtonsoft.Json;

public class StorageAccountCreateParametersWithVrs : StorageAccountCreateOrUpdateContent
{
    public StorageAccountCreateParametersWithVrs(StorageSku sku, StorageKind kind, AzureLocation location)
    : base(sku, kind, location)
    {
    }

    /// <summary>
    /// Gets or sets the residency boundary for VRS feature.
    /// </summary>
    [JsonProperty(PropertyName = "properties.residencyBoundary")]
    public string ResidencyBoundary { get; set; }

    /// <summary>
    /// Gets or sets the residency minimum for VRS feature.
    /// </summary>
    [JsonProperty(PropertyName = "properties.variableResiliency.resiliencyMinimum")]
    public string ResiliencyMinimum { get; set; }

    /// <summary>
    /// Gets or sets the residency maximum for VRS feature.
    /// </summary>
    [JsonProperty(PropertyName = "properties.variableResiliency.resiliencyMaximum")]
    public string ResiliencyMaximum { get; set; }

    /// <summary>
    /// Gets or sets the residency progression id for VRS feature.
    /// </summary>
    [JsonProperty(PropertyName = "properties.variableResiliency.resilienciesProgressionId")]
    public string ResilienciesProgressionId { get; set; }

    /// <summary>
    /// Gets or sets the additional locations for VRS feature.
    /// </summary>
    [JsonProperty(PropertyName = "properties.variableResiliency.additionalLocations")]
    public AdditionalLocation[] AdditionalLocations { get; set; }
}
