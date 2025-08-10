using System.Text.Json.Serialization;

namespace LanCacheDnsRewriteGen;

internal sealed class UklansCacheDomain
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("mixed_content")]
    public bool MixedContent { get; set; }

    [JsonPropertyName("domain_files")]
    public required string[] DomainFiles { get; set; }
}
