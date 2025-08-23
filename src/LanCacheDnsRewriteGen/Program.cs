using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleAppFramework;

namespace LanCacheDnsRewriteGen;

internal static class Program
{
    private const string _cacheDomainsFileName = "cache_domains.json";
    private const string _lancacheDnsRewriteFileName = "lancache.txt";
    private const int _expectedMaximumGeneratedCharaterCount = 15_000;

    private static readonly StringBuilder _stringBuilder = new(_expectedMaximumGeneratedCharaterCount);

    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, GenerateAdGuardHomeRules);
    }

    private static void GenerateAdGuardHomeRules(string repositoryPath, string lancacheIpv4, string lastModified)
    {
        string cacheDomainsFilePath = Path.Combine(repositoryPath, _cacheDomainsFileName);
        if (!File.Exists(cacheDomainsFilePath))
        {
            Console.Error.WriteLine($"Error: {cacheDomainsFilePath} does not exist.");
            Environment.ExitCode = 1;
            return;
        }

        if (!IPAddress.TryParse(lancacheIpv4, out _))
        {
            Console.Error.WriteLine($"Error: Invalid IPv4 address: {lancacheIpv4}");
            Environment.ExitCode = 1;
            return;
        }
        
        if (!DateTimeOffset.TryParse(lastModified, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset repositoryLastModified))
        {
            Console.Error.WriteLine($"Error: Invalid ISO 8601 date format: {lastModified}");
            Environment.ExitCode = 1;
            return;
        }

        using FileStream utf8Json = new(cacheDomainsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        UklansCacheDomains? cacheDomains = JsonSerializer.Deserialize<UklansCacheDomains>(utf8Json);
        if (cacheDomains is null)
        {
            Console.Error.WriteLine($"Error: Unable to deserialize {_cacheDomainsFileName}.");
            Environment.ExitCode = 1;
            return;
        }

        GenerateFileHeader(repositoryLastModified);

        foreach (UklansCacheDomain cacheDomain in cacheDomains.CacheDomains)
        {
            GenerateDnsRewriteRules(cacheDomain, repositoryPath, lancacheIpv4);
        }

        File.WriteAllText(_lancacheDnsRewriteFileName, _stringBuilder.ToString());
        Console.WriteLine("LanCache DNS rewrite rules successfully generated.");
    }

    private static void GenerateFileHeader(DateTimeOffset repositoryLastModified)
    {
        string lastModifiedIso8601 = repositoryLastModified.ToString("o");
        string generatedAtIso8601 = DateTimeOffset.Now.ToString("o");

        _ = _stringBuilder.AppendLine("! Title: LanCache DNS rewrite")
                        .AppendLine("! Description: AdGuard DNS filtering rules for redirecting download requests to LanCache caching proxy server.")
                        .AppendLine($"! Version: {lastModifiedIso8601}")
                        .AppendLine("! Homepage: https://github.com/uklans/cache-domains")
                        .AppendLine($"! Last modified: {lastModifiedIso8601}")
                        .AppendLine($"! Generated at: {generatedAtIso8601}")
                        .AppendLine("!");
    }

    private static void GenerateDnsRewriteRules(UklansCacheDomain cacheDomain, string repositoryPath, string lancacheIpv4)
    {
        GenerateSectionHeader(cacheDomain);
        GenerateRules(cacheDomain, repositoryPath, lancacheIpv4);
    }

    private static void GenerateSectionHeader(UklansCacheDomain cacheDomain)
    {
        _ = _stringBuilder.AppendLine($"! === {cacheDomain.Name} ===")
            .AppendLine($"! {cacheDomain.Description}");

        if (!string.IsNullOrEmpty(cacheDomain.Notes))
        {
            _ = _stringBuilder.AppendLine($"! Notes: {cacheDomain.Notes}");
        }
    }
            
    private static void GenerateRules(UklansCacheDomain cacheDomain, string repositoryPath, string lancacheIpv4)
    {
        foreach (string domainFile in cacheDomain.DomainFiles)
        {
            string domainFilePath = Path.Combine(repositoryPath, domainFile);

            if (!File.Exists(domainFilePath))
            {
                Console.Error.WriteLine($"Error: {domainFilePath} doesn't exist.");
                continue;
            }

            ProcessDomainFile(lancacheIpv4, domainFilePath);
        }
    }

    // Input text:
    // *.cdn.blizzard.com
    //
    // Output text:
    // ||cdn.blizzard.com^$dnsrewrite=192.168.0.4
    // ||cdn.blizzard.com^$dnstype=AAAA
    private static void ProcessDomainFile(string lancacheIpv4, string domainFilePath)
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

            GenerateIpv4Rule(numberOfStartAnchors, domain, lancacheIpv4);
            GenerateIpv6Rule(numberOfStartAnchors, domain);
        }
    }

    private static void GenerateIpv4Rule(int numberOfStartAnchors, in ReadOnlySpan<char> domain, string lancacheIpv4)
    {
        _ = _stringBuilder.Append('|', numberOfStartAnchors)
            .Append(domain)
            .Append($"^$dnsrewrite={lancacheIpv4}")
            .AppendLine();
    }

    private static void GenerateIpv6Rule(int numberOfStartAnchors, in ReadOnlySpan<char> domain)
    {
        _ = _stringBuilder.Append('|', numberOfStartAnchors)
            .Append(domain)
            .Append("^$dnstype=AAAA")
            .AppendLine();
    }
}
