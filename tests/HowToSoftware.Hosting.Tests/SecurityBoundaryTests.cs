using System.Net;
using System.Text.RegularExpressions;
using HowToSoftware.Hosting.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HowToSoftware.Hosting.Tests;

/// <summary>Integration checks for the boundaries that must survive refactors.</summary>
public sealed class SecurityBoundaryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hts-security-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityBoundaryTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Hosting", $"Data Source={_dbPath}"));
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HtmlCarriesStrictSecurityHeadersAndMatchingScriptNonces()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var csp = Assert.Single(
            response.Headers.GetValues("Content-Security-Policy"),
            value => value.Contains("nonce-", StringComparison.Ordinal));

        Assert.Contains("object-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("base-uri 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("connect-src 'self' ws://localhost", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("upgrade-insecure-requests", csp, StringComparison.Ordinal);
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));

        var match = Regex.Match(csp, "'nonce-([^']+)'", RegexOptions.CultureInvariant);
        Assert.True(match.Success);
        Assert.Contains(
            $"nonce=\"{match.Groups[1].Value}\"",
            WebUtility.HtmlDecode(html),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaymentPagesCannotBeCachedOrLeakThroughAReferrer()
    {
        using var response = await _client.GetAsync("/payment/success?session_id=cs_missing");
        using var statusResponse = await _client.GetAsync($"{SiteRoutes.OrderStatus}?session_id=cs_missing");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", statusResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
    }

    [Fact]
    public async Task CriticalBrowserAssetsStaySameOriginAndAreServed()
    {
        using var page = await _client.GetAsync("/");
        var html = await page.Content.ReadAsStringAsync();
        var references = Regex.Matches(
                html,
                "(?:src|href)=\"([^\"]+)\"",
                RegexOptions.CultureInvariant)
            .Select(match => WebUtility.HtmlDecode(match.Groups[1].Value))
            .ToArray();
        var expectedAssets = new[]
        {
            "_framework/blazor.web",
            "js/site.",
            "css/app.",
            "HowToSoftware.Hosting.",
            "fonts/archivo-latin.",
            "fonts/jetbrains-mono-latin."
        };

        foreach (var marker in expectedAssets)
        {
            var assetPath = Assert.Single(
                references,
                value => value.Contains(marker, StringComparison.Ordinal));

            Assert.False(
                Uri.TryCreate(assetPath, UriKind.Absolute, out _),
                $"Critical asset must stay same-origin, but was rendered as '{assetPath}'.");

            using var asset = await _client.GetAsync('/' + assetPath.TrimStart('/'));
            Assert.True(
                asset.IsSuccessStatusCode,
                $"Critical asset '{assetPath}' returned {(int)asset.StatusCode}.");
        }
    }

    [Fact]
    public async Task RepeatedStatusProbesAreRateLimited()
    {
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt < 61; attempt += 1)
        {
            response?.Dispose();
            response = await _client.GetAsync($"{SiteRoutes.OrderStatus}?session_id=cs_missing");
        }

        using (response)
        {
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.True(response.Headers.Contains("Retry-After"));
        }
    }

    [Fact]
    public async Task ProductionHttpsResponsesCarryOneYearHstsWithoutUnsafePreload()
    {
        var productionDb = Path.Combine(Path.GetTempPath(), $"hts-security-production-{Guid.NewGuid():N}.db");

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ConnectionStrings:Hosting", $"Data Source={productionDb}");
                builder.UseSetting("AllowedHosts", "secure.example");
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://secure.example")
            });

            using var response = await client.GetAsync("/");
            var hsts = Assert.Single(response.Headers.GetValues("Strict-Transport-Security"));
            var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));

            Assert.Contains("max-age=31536000", hsts, StringComparison.Ordinal);
            Assert.DoesNotContain("includeSubDomains", hsts, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("preload", hsts, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("connect-src 'self' wss://secure.example", csp, StringComparison.Ordinal);
            Assert.Contains("upgrade-insecure-requests", csp, StringComparison.Ordinal);
        }
        finally
        {
            foreach (var path in new[] { productionDb, productionDb + "-shm", productionDb + "-wal" })
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task ProductionHttpFallbackNeverUpgradesSameOriginAssetsToUnavailableTls()
    {
        var productionDb = Path.Combine(Path.GetTempPath(), $"hts-security-http-{Guid.NewGuid():N}.db");

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ConnectionStrings:Hosting", $"Data Source={productionDb}");
                builder.UseSetting("AllowedHosts", "lan.test");
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://lan.test")
            });

            using var response = await client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));

            Assert.DoesNotContain("upgrade-insecure-requests", csp, StringComparison.Ordinal);
            Assert.Contains("connect-src 'self' ws://lan.test", csp, StringComparison.Ordinal);
            Assert.DoesNotContain("https://lan.test", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            foreach (var path in new[] { productionDb, productionDb + "-shm", productionDb + "-wal" })
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();

        foreach (var path in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // SQLite can retain a pooled handle for a moment after the in-process host exits.
            }
        }
    }
}
