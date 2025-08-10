using ConsoleAppFramework;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LanCacheDnsRewriteGen;

internal class Program
{
    private const string _cacheDomainsFileName = "cache_domains.json";
    private const string _lancacheDnsRewriteFileName = "lancache.txt";
    private const int _expectedMaximumGeneratedCharaterCount = 150000;

    public static async Task Main(string[] args)
    {
        await ConsoleApp.RunAsync(args, GenerateAdGuardHomeRules);
    }

    private static async Task GenerateAdGuardHomeRules(string repositoryPath, string lancacheIpv4, DateTimeOffset lastModified, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(lancacheIpv4, out _))
        {
            Console.Error.WriteLine($"Error: Invalid IPv4 address: {lancacheIpv4}");
            Environment.ExitCode = 1;
            return;
        }

        string cacheDomainsFilePath = Path.Combine(repositoryPath, _cacheDomainsFileName);
        if (!File.Exists(cacheDomainsFilePath))
        {
            Console.Error.WriteLine($"Error: {cacheDomainsFilePath} does not exist.");
            Environment.ExitCode = 1;
            return;
        }

        await using FileStream utf8Json = new(cacheDomainsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        UklansCacheDomains? cacheDomains = await JsonSerializer.DeserializeAsync<UklansCacheDomains>(utf8Json, cancellationToken: cancellationToken);
        if (cacheDomains is null)
        {
            Console.Error.WriteLine($"Error: Unable to deserialize {_cacheDomainsFileName}.");
            Environment.ExitCode = 1;
            return;
        }

        StringBuilder stringBuilder = new(_expectedMaximumGeneratedCharaterCount);
        GenerateFileHeader(stringBuilder, lastModified);

        foreach (UklansCacheDomain cacheDomain in cacheDomains.CacheDomains)
        {
            GenerateDnsRewriteRules(stringBuilder, cacheDomain, repositoryPath, lancacheIpv4);
        }

        await File.WriteAllTextAsync(_lancacheDnsRewriteFileName, stringBuilder.ToString(), cancellationToken);
        Console.WriteLine("LanCache DNS rewrite rules successfully generated.");
    }

    private static void GenerateFileHeader(StringBuilder stringBuilder, DateTimeOffset lastModified)
    {
        string lastModifiedIso8601 = lastModified.ToString("o");
        string generatedAtIso8601 = DateTimeOffset.Now.ToString("o");

        _ = stringBuilder.AppendLine("! Title: LanCache DNS rewrite")
                        .AppendLine("! Description: AdGuard DNS filtering rules for redirecting download requests to LanCache caching proxy server.")
                        .AppendLine($"! Version: {lastModifiedIso8601}")
                        .AppendLine("! Homepage: https://github.com/uklans/cache-domains")
                        .AppendLine($"! Last modified: {lastModifiedIso8601}")
                        .AppendLine($"! Generated at: {generatedAtIso8601}")
                        .AppendLine("!");
    }

    private static void GenerateDnsRewriteRules(StringBuilder stringBuilder, UklansCacheDomain cacheDomain, string repositoryPath, string lancacheIpv4)
    {
        GenerateSectionHeader(stringBuilder, cacheDomain);
        GenerateRules(stringBuilder, cacheDomain, repositoryPath, lancacheIpv4);
    }

    private static void GenerateSectionHeader(StringBuilder stringBuilder, UklansCacheDomain cacheDomain)
    {
        _ = stringBuilder.AppendLine($"! === {cacheDomain.Name} ===")
            .AppendLine($"! {cacheDomain.Description}");

        if (!string.IsNullOrEmpty(cacheDomain.Notes))
        {
            _ = stringBuilder.AppendLine($"! Notes: {cacheDomain.Notes}");
        }
    }
            
    private static void GenerateRules(StringBuilder stringBuilder, UklansCacheDomain cacheDomain, string repositoryPath, string lancacheIpv4)
    {
        foreach (string domainFile in cacheDomain.DomainFiles)
        {
            string domainFilePath = Path.Combine(repositoryPath, domainFile);

            if (!File.Exists(domainFilePath))
            {
                Console.Error.WriteLine($"Error: {domainFilePath} doesn't exist.");
                continue;
            }

            ProcessDomainFile(stringBuilder, lancacheIpv4, domainFilePath);
        }
    }

    // Input text:
    // *.cdn.blizzard.com
    //
    // Output text:
    // ||cdn.blizzard.com^$dnsrewrite=192.168.0.4
    // ||cdn.blizzard.com^$dnstype=AAAA
    private static void ProcessDomainFile(StringBuilder stringBuilder, string lancacheIpv4, string domainFilePath)
    {
        foreach (string line in File.ReadLines(domainFilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue; // Skip empty lines and comments
            }

            ReadOnlySpan<char> lineSpan = line.AsSpan().Trim();
            int numberOfStartAnchors;
            ReadOnlySpan<char> domain;

            if (lineSpan.StartsWith("*.", StringComparison.Ordinal))
            {
                numberOfStartAnchors = 2;
                domain = lineSpan[2..];
            }
            else
            {
                numberOfStartAnchors = 1;
                domain = lineSpan;
            }

            GenerateIpv4Rule(stringBuilder, numberOfStartAnchors, domain, lancacheIpv4);
            GenerateIpv6Rule(stringBuilder, numberOfStartAnchors, domain);
        }
    }

    private static void GenerateIpv4Rule(StringBuilder stringBuilder, int numberOfStartAnchors, in ReadOnlySpan<char> domain, string lancacheIpv4)
    {
        _ = stringBuilder.Append('|', numberOfStartAnchors)
            .Append(domain)
            .Append($"^$dnsrewrite={lancacheIpv4}")
            .AppendLine();
    }

    private static void GenerateIpv6Rule(StringBuilder stringBuilder, int numberOfStartAnchors, in ReadOnlySpan<char> domain)
    {
        _ = stringBuilder.Append('|', numberOfStartAnchors)
            .Append(domain)
            .Append("^$dnstype=AAAA")
            .AppendLine();
    }
}
