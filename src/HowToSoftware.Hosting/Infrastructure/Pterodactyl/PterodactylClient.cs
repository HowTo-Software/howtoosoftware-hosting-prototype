using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Models;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Requests;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// HTTP client for the Pterodactyl Application API.
/// </summary>
/// <remarks>
/// <para>
/// All HTTP behaviour lives here: the base address, the headers, JSON options, status mapping
/// and error parsing. Nothing else in the application constructs a request to the panel, and
/// nothing else needs to know the panel's error envelope.
/// </para>
/// <para>
/// The <see cref="HttpClient"/> is supplied by <c>IHttpClientFactory</c>, so its handler is
/// pooled and its lifetime managed. The API key is attached per request from options rather than
/// baked into the handler, so rotating it does not need a restart - and it exists in exactly one
/// place in this file.
/// </para>
/// </remarks>
public sealed class PterodactylClient : IPterodactylClient
{
    /// <summary>Name the typed client is registered under.</summary>
    public const string HttpClientName = "pterodactyl";

    private const string ApplicationApiPath = "/api/application";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // The panel sends every field it knows about; we deserialise the subset we declared and
        // ignore the rest, so a panel upgrade that adds a field cannot break provisioning.
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<PterodactylOptions> _options;
    private readonly ILogger<PterodactylClient> _logger;

    /// <summary>Creates the client.</summary>
    /// <param name="http">Client supplied by the factory, already carrying the base address.</param>
    /// <param name="options">Panel settings, re-read per call so a key rotation takes effect.</param>
    /// <param name="logger">Structured log sink. Never receives the key or a password.</param>
    public PterodactylClient(
        HttpClient http,
        IOptionsMonitor<PterodactylOptions> options,
        ILogger<PterodactylClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured => _options.CurrentValue.IsConfigured;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PterodactylNode>> GetNodesAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await SendAsync<PterodactylList<PterodactylNode>>(
            HttpMethod.Get,
            "/nodes?per_page=100",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        return list.Items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PterodactylLocation>> GetLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await SendAsync<PterodactylList<PterodactylLocation>>(
            HttpMethod.Get,
            "/locations?per_page=100",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        return list.Items;
    }

    /// <inheritdoc />
    public async Task<PterodactylEgg> GetEggAsync(
        int nestId,
        int eggId,
        CancellationToken cancellationToken = default)
    {
        var item = await SendAsync<PterodactylItem<PterodactylEgg>>(
            HttpMethod.Get,
            $"/nests/{nestId}/eggs/{eggId}?include=variables",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        return item.Attributes
            ?? throw new PterodactylApiException(
                PterodactylFailure.PanelError,
                $"The panel returned no egg body for nest {nestId} / egg {eggId}.");
    }

    /// <inheritdoc />
    public async Task<PterodactylUser?> FindUserByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        var item = await SendOrNullAsync<PterodactylItem<PterodactylUser>>(
            HttpMethod.Get,
            $"/users/external/{Uri.EscapeDataString(externalId)}",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        return item?.Attributes;
    }

    /// <inheritdoc />
    public async Task<PterodactylUser> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await SendAsync<PterodactylItem<PterodactylUser>>(
            HttpMethod.Post,
            "/users",
            request,
            cancellationToken).ConfigureAwait(false);

        return item.Attributes
            ?? throw new PterodactylApiException(
                PterodactylFailure.PanelError,
                "The panel accepted the user but returned no body.");
    }

    /// <inheritdoc />
    public async Task<PterodactylServer?> FindServerByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        var item = await SendOrNullAsync<PterodactylItem<PterodactylServer>>(
            HttpMethod.Get,
            $"/servers/external/{Uri.EscapeDataString(externalId)}",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        return item?.Attributes;
    }

    /// <inheritdoc />
    public async Task<PterodactylServer> CreateServerAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await SendAsync<PterodactylItem<PterodactylServer>>(
            HttpMethod.Post,
            "/servers",
            request,
            cancellationToken).ConfigureAwait(false);

        return item.Attributes
            ?? throw new PterodactylApiException(
                PterodactylFailure.PanelError,
                "The panel accepted the server but returned no body.");
    }

    /// <inheritdoc />
    public async Task DeleteServerAsync(
        int serverId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        // 204 with an empty body, so there is nothing to deserialise.
        using var response = await ExecuteAsync(
            HttpMethod.Delete,
            force ? $"/servers/{serverId}/force" : $"/servers/{serverId}",
            payload: null,
            cancellationToken).ConfigureAwait(false);

        // The panel answers a successful delete with 204 and an empty body, and only that.
        // Anything else - including a 2xx that is not 204 - means the server still exists.
        if (response.StatusCode is not System.Net.HttpStatusCode.NoContent)
        {
            throw await BuildFailureAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    // ── Transport ─────────────────────────────────────────────────────────

    /// <summary>Sends a request and deserialises the response, throwing on any failure.</summary>
    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var response = await ExecuteAsync(method, path, payload, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildFailureAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return await ReadBodyAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// As <see cref="SendAsync{T}"/>, but treats 404 as "no such record" rather than an error.
    /// </summary>
    private async Task<T?> SendOrNullAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
        where T : class
    {
        using var response = await ExecuteAsync(method, path, payload, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildFailureAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return await ReadBodyAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds and sends the request, translating transport faults into typed failures.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;

        if (!options.IsConfigured)
        {
            throw new PterodactylApiException(
                PterodactylFailure.NotConfigured,
                "Pterodactyl is not configured. Set Pterodactyl:BaseUrl and Pterodactyl:ApiKey "
                    + "(environment variables Pterodactyl__BaseUrl / Pterodactyl__ApiKey, or dotnet user-secrets).");
        }

        using var request = new HttpRequestMessage(method, options.NormalisedBaseUrl + ApplicationApiPath + path);

        // The one place the key is read, and the only place it is attached. It is never written
        // to a log, an exception message or a rendered page.
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + options.ApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, payload.GetType(), options: JsonOptions);
        }

        try
        {
            _logger.LogDebug(
                "Pterodactyl request {Method} {Path}",
                method.Method,
                path);

            return await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient surfaces its own timeout as a cancellation, which is not the caller's.
            throw new PterodactylApiException(
                PterodactylFailure.Timeout,
                $"The panel did not respond within {options.TimeoutSeconds} seconds.",
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new PterodactylApiException(
                PterodactylFailure.Unreachable,
                "The panel could not be reached. Check Pterodactyl:BaseUrl, DNS and the TLS certificate.",
                innerException: exception);
        }
    }

    /// <summary>Deserialises a success body, turning malformed JSON into a typed failure.</summary>
    private static async Task<T> ReadBodyAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content
                .ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return body
                ?? throw new PterodactylApiException(
                    PterodactylFailure.PanelError,
                    "The panel returned an empty body where a resource was expected.");
        }
        catch (JsonException exception)
        {
            // Usually the panel returning an HTML error page - a reverse proxy in front of it,
            // or a base URL pointing at something that is not a panel at all.
            throw new PterodactylApiException(
                PterodactylFailure.PanelError,
                "The panel returned a response that could not be parsed as JSON. "
                    + "Check that Pterodactyl:BaseUrl points at the panel root.",
                (int)response.StatusCode,
                innerException: exception);
        }
    }

    /// <summary>
    /// Turns a non-success response into a typed exception, parsing the panel's error envelope
    /// when there is one.
    /// </summary>
    private async Task<PterodactylApiException> BuildFailureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var errors = await TryReadErrorsAsync(response, cancellationToken).ConfigureAwait(false);

        var failure = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => PterodactylFailure.Unauthorized,
            HttpStatusCode.Forbidden => PterodactylFailure.Forbidden,
            HttpStatusCode.NotFound => PterodactylFailure.NotFound,
            HttpStatusCode.UnprocessableContent when IsDuplicateExternalId(errors) =>
                PterodactylFailure.AlreadyExists,
            HttpStatusCode.UnprocessableContent => PterodactylFailure.ValidationFailed,
            HttpStatusCode.TooManyRequests => PterodactylFailure.RateLimited,
            // The panel reports "no node had room" and "no allocation was free" as 400s carrying
            // a NoViableNodeException / NoViableAllocationException code.
            HttpStatusCode.BadRequest when IsCapacityFailure(errors) => PterodactylFailure.NoCapacity,
            // A panel that enforces 2FA rejects a key whose owner has none. That is an operator
            // fix, not a malformed request, so it must not be reported as a validation failure.
            HttpStatusCode.BadRequest when HasCode(errors, "TwoFactorAuthRequired") =>
                PterodactylFailure.Forbidden,
            HttpStatusCode.BadRequest => PterodactylFailure.ValidationFailed,
            _ => PterodactylFailure.PanelError
        };

        var summary = failure switch
        {
            PterodactylFailure.Unauthorized =>
                "The panel rejected the API key. Check that Pterodactyl:ApiKey is an Application key "
                    + "(prefix ptla_), that it has not been revoked, and that it is not a client key (ptlc_).",
            PterodactylFailure.Forbidden =>
                "The API key authenticated but lacks permission for this endpoint. Grant it read/write "
                    + "on the resources it needs in the panel's API key settings.",
            PterodactylFailure.NoCapacity =>
                "The panel found no node with room, or no free allocation, for this deployment.",
            PterodactylFailure.ValidationFailed =>
                "The panel rejected the request body.",
            PterodactylFailure.NotFound =>
                "The panel has no such record.",
            PterodactylFailure.AlreadyExists =>
                "The panel already holds a record with that external id.",
            PterodactylFailure.RateLimited =>
                DescribeRateLimit(response),
            _ => $"The panel returned HTTP {status}."
        };

        var detail = errors.Count > 0
            ? $"{summary} {string.Join(" · ", errors.Select(Describe))}"
            : summary;

        // Panel details can echo submitted fields (including customer data). Keep them in the
        // typed exception for the Development-only lab, but never send them to shared logs.
        _logger.LogWarning(
            "Pterodactyl call failed with {Failure} (HTTP {Status}, {ErrorCount} panel errors).",
            failure,
            status,
            errors.Count);

        return new PterodactylApiException(failure, detail, status, errors);
    }

    /// <summary>Reads the panel's <c>errors[]</c> array, tolerating a body that has none.</summary>
    private static async Task<IReadOnlyList<PterodactylErrorDetail>> TryReadErrorsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content
                .ReadFromJsonAsync<PterodactylErrorEnvelope>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (envelope?.Errors is not { Count: > 0 } entries)
            {
                return [];
            }

            return
            [
                .. entries.Select(entry => new PterodactylErrorDetail(
                    entry.Code,
                    entry.Status,
                    entry.Detail,
                    entry.Meta?.SourceField))
            ];
        }
        catch (JsonException)
        {
            // An HTML error page, or an empty body. Nothing to add beyond the status.
            return [];
        }
        catch (NotSupportedException)
        {
            return [];
        }
    }

    private static bool IsCapacityFailure(IReadOnlyList<PterodactylErrorDetail> errors) =>
        errors.Any(error =>
            error.Code.Contains("NoViableNode", StringComparison.OrdinalIgnoreCase)
            || error.Code.Contains("NoViableAllocation", StringComparison.OrdinalIgnoreCase));

    private static bool HasCode(IReadOnlyList<PterodactylErrorDetail> errors, string fragment) =>
        errors.Any(error => error.Code.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Whether a 422 is the panel refusing a second record with an external id we already used.
    /// </summary>
    /// <remarks>
    /// external_id carries a uniqueness rule, so two concurrent provisioning attempts for the
    /// same request lose this race rather than creating two servers. The caller re-reads the
    /// record instead of treating it as a rejected body.
    /// </remarks>
    private static bool IsDuplicateExternalId(IReadOnlyList<PterodactylErrorDetail> errors) =>
        errors.Any(error =>
            string.Equals(error.SourceField, "external_id", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reads the panel's own throttle headers rather than assuming a limit. The default is
    /// operator-configurable, and a proxy in front of the panel may impose a different one.
    /// </summary>
    private static string DescribeRateLimit(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds
            ?? (response.Headers.TryGetValues("Retry-After", out var values)
                && int.TryParse(values.FirstOrDefault(), out var seconds) ? seconds : (double?)null);

        var limit = response.Headers.TryGetValues("X-RateLimit-Limit", out var limits)
            ? limits.FirstOrDefault()
            : null;

        var suffix = retryAfter is { } wait ? $" Retry in {wait:0} s." : string.Empty;
        var ceiling = limit is not null ? $" The panel allows {limit} requests per minute." : string.Empty;

        return $"The panel is rate-limiting this key.{ceiling}{suffix}";
    }

    private static string Describe(PterodactylErrorDetail error) =>
        string.IsNullOrEmpty(error.SourceField) ? error.Detail : $"{error.SourceField}: {error.Detail}";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
