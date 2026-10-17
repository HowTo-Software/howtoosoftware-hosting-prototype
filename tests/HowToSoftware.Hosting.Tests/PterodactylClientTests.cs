using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace HowToSoftware.Hosting.Tests;

public sealed class PterodactylClientTests
{
    [Fact]
    public async Task NodeAndLocationListsFollowPaginationMetadata()
    {
        using var handler = new PagedResponseHandler();
        var (client, httpClient) = CreateClient(handler);
        using var _ = httpClient;

        var nodes = await client.GetNodesAsync();
        var locations = await client.GetLocationsAsync();

        Assert.Equal([1, 2], nodes.Select(node => node.Id));
        Assert.Equal([1, 2], locations.Select(location => location.Id));
        Assert.Equal(
            ["/api/application/nodes?per_page=100&page=1", "/api/application/nodes?per_page=100&page=2"],
            handler.RequestPaths.Where(path => path.Contains("/nodes?", StringComparison.Ordinal)));
        Assert.Equal(
            ["/api/application/locations?per_page=100&page=1", "/api/application/locations?per_page=100&page=2"],
            handler.RequestPaths.Where(path => path.Contains("/locations?", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task RateLimitDescriptionIncludesDateBasedRetryAfter()
    {
        using var handler = new RateLimitedResponseHandler();
        var (client, httpClient) = CreateClient(handler);
        using var _ = httpClient;

        var error = await Assert.ThrowsAsync<PterodactylApiException>(() => client.GetNodesAsync());

        Assert.Equal(PterodactylFailure.RateLimited, error.Failure);
        Assert.Contains("Retry in", error.Message, StringComparison.Ordinal);
    }

    private static (PterodactylClient Client, HttpClient HttpClient) CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var client = new PterodactylClient(
            httpClient,
            new StaticOptionsMonitor<PterodactylOptions>(new PterodactylOptions
            {
                BaseUrl = "https://panel.example.com",
                ApiKey = "ptla_" + new string('a', 43)
            }),
            NullLogger<PterodactylClient>.Instance);
        return (client, httpClient);
    }

    private sealed class PagedResponseHandler : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            RequestPaths.Add(path);
            var page = path.EndsWith("page=2", StringComparison.Ordinal) ? 2 : 1;
            var json = path.Contains("/nodes?", StringComparison.Ordinal)
                ? JsonSerializer.Serialize(new PterodactylList<PterodactylNode>
                {
                    Object = "list",
                    Data = [new PterodactylItem<PterodactylNode>
                    {
                        Object = "node",
                        Attributes = new PterodactylNode { Id = page }
                    }],
                    Meta = new PterodactylListMeta
                    {
                        Pagination = new PterodactylPagination { CurrentPage = page, TotalPages = 2 }
                    }
                })
                : JsonSerializer.Serialize(new PterodactylList<PterodactylLocation>
                {
                    Object = "list",
                    Data = [new PterodactylItem<PterodactylLocation>
                    {
                        Object = "location",
                        Attributes = new PterodactylLocation { Id = page }
                    }],
                    Meta = new PterodactylListMeta
                    {
                        Pagination = new PterodactylPagination { CurrentPage = page, TotalPages = 2 }
                    }
                });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class RateLimitedResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("""{"errors":[]}""", Encoding.UTF8, "application/json")
            };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(30));
            return Task.FromResult(response);
        }
    }
}
