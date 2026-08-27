namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// What went wrong talking to the panel, at the level a caller can actually act on.
/// </summary>
/// <remarks>
/// The panel's own error codes are too fine-grained to branch on and its HTTP statuses too
/// coarse. This is the middle: enough to decide whether to retry, to tell an operator to fix
/// configuration, or to tell a customer to wait.
/// </remarks>
public enum PterodactylFailure
{
    /// <summary>No failure.</summary>
    None = 0,

    /// <summary>No panel URL or API key is configured. An operator problem, not a runtime one.</summary>
    NotConfigured,

    /// <summary>The panel could not be reached at all: DNS, TCP, or TLS.</summary>
    Unreachable,

    /// <summary>The request took longer than the configured timeout.</summary>
    Timeout,

    /// <summary>401. The key is missing, malformed, or revoked.</summary>
    Unauthorized,

    /// <summary>403. The key is valid but lacks the permission this endpoint needs.</summary>
    Forbidden,

    /// <summary>404. The egg, location, user or server does not exist.</summary>
    NotFound,

    /// <summary>422. The panel rejected the request body.</summary>
    ValidationFailed,

    /// <summary>
    /// 422 on a unique external id: something already holds it. Two concurrent attempts at the
    /// same provisioning request produce this, and the answer is to re-read rather than retry.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// 400 from the automatic-deployment path: no node had room, or no allocation was free.
    /// </summary>
    NoCapacity,

    /// <summary>429. The panel is rate-limiting us.</summary>
    RateLimited,

    /// <summary>5xx, or a response that could not be parsed.</summary>
    PanelError
}

/// <summary>
/// A call to the Pterodactyl Application API that did not succeed.
/// </summary>
/// <remarks>
/// <para>
/// Carries the panel's own <c>errors[]</c> entries when it returned any, so a developer can see
/// which field was rejected. The message is written to be safe to surface in a development-only
/// diagnostic view; it never contains the API key, because the key is never placed anywhere it
/// could be interpolated from.
/// </para>
/// </remarks>
public sealed class PterodactylApiException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="failure">Category the caller branches on.</param>
    /// <param name="message">Safe, human-readable summary.</param>
    /// <param name="statusCode">HTTP status the panel returned, when there was one.</param>
    /// <param name="errors">Parsed <c>errors[]</c> entries from the panel.</param>
    /// <param name="innerException">Transport-level cause, when there was one.</param>
    public PterodactylApiException(
        PterodactylFailure failure,
        string message,
        int? statusCode = null,
        IReadOnlyList<PterodactylErrorDetail>? errors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        StatusCode = statusCode;
        Errors = errors ?? [];
    }

    /// <summary>Category of the failure.</summary>
    public PterodactylFailure Failure { get; }

    /// <summary>HTTP status the panel returned, when the call reached it.</summary>
    public int? StatusCode { get; }

    /// <summary>Field-level errors the panel reported.</summary>
    public IReadOnlyList<PterodactylErrorDetail> Errors { get; }

    /// <summary>
    /// A one-line summary of the field errors, for a development diagnostic panel.
    /// </summary>
    public string DescribeErrors() =>
        Errors.Count == 0
            ? string.Empty
            : string.Join(" · ", Errors.Select(error =>
                string.IsNullOrEmpty(error.SourceField)
                    ? error.Detail
                    : $"{error.SourceField}: {error.Detail}"));
}

/// <summary>
/// One entry from the panel's <c>errors[]</c> array.
/// </summary>
/// <param name="Code">Panel exception name, e.g. <c>ValidationException</c>.</param>
/// <param name="Status">HTTP status as a string, as the panel sends it.</param>
/// <param name="Detail">Human-readable message.</param>
/// <param name="SourceField">
/// Field the error refers to, e.g. <c>limits.memory</c>, when the panel names one.
/// </param>
public sealed record PterodactylErrorDetail(
    string Code,
    string Status,
    string Detail,
    string? SourceField);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
