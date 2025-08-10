using System.Text.Json.Serialization;

namespace LanCacheDnsRewriteGen;

internal sealed class UklansCacheDomains
{
    [JsonPropertyName("cache_domains")]
    public UklansCacheDomain[] CacheDomains { get; set; } = [];
}
