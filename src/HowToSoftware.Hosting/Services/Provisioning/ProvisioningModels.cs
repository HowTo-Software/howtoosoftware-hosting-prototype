using HowToSoftware.Hosting.Infrastructure.Pterodactyl;

namespace HowToSoftware.Hosting.Services.Provisioning;

/// <summary>
/// An order to turn into a running server.
/// </summary>
/// <remarks>
/// Deliberately shaped like something a payment webhook would hand over, not like something a
/// form posts. When checkout exists, the webhook builds one of these and calls the same method
/// the provisioning lab calls today - the trigger changes, the pipeline does not.
/// </remarks>
public sealed record ProvisioningRequest
{
    /// <summary>
    /// Identifies this provisioning attempt. It becomes the server's external id, which is what
    /// makes a retried or duplicated request reuse the existing server instead of creating a
    /// second one.
    /// </summary>
    public required Guid RequestId { get; init; }

    /// <summary>Our customer identifier, which maps to exactly one panel user.</summary>
    public required Guid CustomerId { get; init; }

    /// <summary>Customer email. Becomes the panel account's email.</summary>
    public required string CustomerEmail { get; init; }

    /// <summary>Slug of the plan being provisioned.</summary>
    public required string PlanSlug { get; init; }

    /// <summary>Name the server appears under in the panel.</summary>
    public required string ServerName { get; init; }

    /// <summary>Given name for the panel account.</summary>
    public string FirstName { get; init; } = "HowToSoftware";

    /// <summary>Family name for the panel account.</summary>
    public string LastName { get; init; } = "Customer";

    /// <summary>
    /// Whether this came from the provisioning lab rather than a real order.
    /// </summary>
    /// <remarks>
    /// Controls the external-id prefix, and therefore whether the result may later be deleted
    /// through the lab. A real order can never be cleaned up by the test workflow.
    /// </remarks>
    public bool IsTest { get; init; }

    /// <summary>Whether the panel should boot the server once installation finishes.</summary>
    public bool StartOnCompletion { get; init; } = true;
}

/// <summary>How a provisioning attempt ended.</summary>
public enum ProvisioningOutcome
{
    /// <summary>A new server was created.</summary>
    Created,

    /// <summary>A server already existed for this request id and was returned unchanged.</summary>
    AlreadyProvisioned,

    /// <summary>Nothing was created.</summary>
    Failed
}

/// <summary>
/// The result of a provisioning attempt, in a shape that is safe to render.
/// </summary>
/// <param name="Outcome">How it ended.</param>
/// <param name="RequestId">The provisioning request this describes.</param>
/// <param name="ServerId">Numeric panel server id, when one exists.</param>
/// <param name="ServerUuid">Panel UUID, when one exists.</param>
/// <param name="ServerIdentifier">Short identifier, the first eight characters of the UUID.</param>
/// <param name="ExternalId">External id written to the panel.</param>
/// <param name="NodeId">Node the panel placed the server on.</param>
/// <param name="AllocationId">Allocation the panel bound.</param>
/// <param name="PanelUserId">Numeric panel user id that owns the server.</param>
/// <param name="UserWasReused">Whether an existing panel user was reused rather than created.</param>
/// <param name="Status">Panel lifecycle status, <c>installing</c> immediately after creation.</param>
/// <param name="PlanSlug">Plan the server was built from.</param>
/// <param name="Failure">Failure category, when it failed.</param>
/// <param name="Message">Safe, human-readable summary.</param>
/// <remarks>
/// There is deliberately no field for a password, an API key or a raw panel response. Anything
/// placed here can end up on a screen.
/// </remarks>
public sealed record ProvisioningResult(
    ProvisioningOutcome Outcome,
    Guid RequestId,
    int? ServerId,
    string? ServerUuid,
    string? ServerIdentifier,
    string? ExternalId,
    int? NodeId,
    int? AllocationId,
    int? PanelUserId,
    bool UserWasReused,
    string? Status,
    string? PlanSlug,
    PterodactylFailure Failure,
    string Message)
{
    /// <summary>Whether a server exists as a result of this attempt.</summary>
    public bool IsSuccess => Outcome is ProvisioningOutcome.Created or ProvisioningOutcome.AlreadyProvisioned;

    /// <summary>Builds a failure result.</summary>
    /// <param name="requestId">The request that failed.</param>
    /// <param name="planSlug">Plan that was being provisioned.</param>
    /// <param name="failure">Failure category.</param>
    /// <param name="message">Safe summary.</param>
    public static ProvisioningResult Failed(
        Guid requestId,
        string? planSlug,
        PterodactylFailure failure,
        string message) =>
        new(ProvisioningOutcome.Failed, requestId, null, null, null, null, null, null, null, false,
            null, planSlug, failure, message);
}

/// <summary>
/// A read-only snapshot of the panel, for the provisioning lab's connection panel.
/// </summary>
/// <param name="IsConfigured">Whether a base URL and key were supplied.</param>
/// <param name="IsReachable">Whether the node list could be read.</param>
/// <param name="Failure">Failure category when it could not.</param>
/// <param name="Message">Safe summary of the failure, empty on success.</param>
/// <param name="Nodes">Nodes the panel reported.</param>
/// <param name="Locations">Locations the panel reported.</param>
/// <param name="EggName">Name of the configured egg, when it could be read.</param>
/// <param name="EggDockerImage">Default image the egg declares.</param>
/// <param name="RequiredVariables">Egg variables the panel will insist on.</param>
/// <param name="KeyDescription">Prefix and length of the configured key - never its value.</param>
public sealed record PanelSnapshot(
    bool IsConfigured,
    bool IsReachable,
    PterodactylFailure Failure,
    string Message,
    IReadOnlyList<PanelNodeSummary> Nodes,
    IReadOnlyList<PanelLocationSummary> Locations,
    string? EggName,
    string? EggDockerImage,
    IReadOnlyList<string> RequiredVariables,
    string KeyDescription);

/// <summary>A node, reduced to what the lab shows.</summary>
/// <param name="Id">Numeric node id.</param>
/// <param name="Name">Display name.</param>
/// <param name="LocationId">Location it sits in.</param>
/// <param name="IsPublic">Whether automatic deployment may use it.</param>
/// <param name="MaintenanceMode">Whether it is in maintenance.</param>
/// <param name="MemoryMib">Total memory, MiB.</param>
/// <param name="AllocatedMemoryMib">Memory already committed, MiB.</param>
/// <param name="DiskMib">Total disk, MiB.</param>
/// <param name="AllocatedDiskMib">Disk already committed, MiB.</param>
public sealed record PanelNodeSummary(
    int Id,
    string Name,
    int LocationId,
    bool IsPublic,
    bool MaintenanceMode,
    long MemoryMib,
    long AllocatedMemoryMib,
    long DiskMib,
    long AllocatedDiskMib)
{
    /// <summary>Memory still uncommitted, MiB. Negative when the node is over-allocated.</summary>
    public long FreeMemoryMib => MemoryMib - AllocatedMemoryMib;

    /// <summary>Whether automatic deployment will consider this node at all.</summary>
    /// <remarks>
    /// A private or maintenance node is skipped by the panel's own node selection, which is the
    /// usual explanation for "no viable node" while a node looks perfectly healthy.
    /// </remarks>
    public bool IsDeployable => IsPublic && !MaintenanceMode;
}

/// <summary>A location, reduced to what the lab shows.</summary>
/// <param name="Id">Numeric location id.</param>
/// <param name="Short">Short code.</param>
/// <param name="Long">Description.</param>
public sealed record PanelLocationSummary(int Id, string Short, string Long);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
