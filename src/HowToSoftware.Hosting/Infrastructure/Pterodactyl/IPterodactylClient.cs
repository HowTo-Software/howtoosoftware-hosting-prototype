using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Models;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Requests;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// The Pterodactyl Application API, narrowed to provisioning and trial lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Each method supports the provisioner or a durable trial transition. Customer ownership,
/// verified identity and trial external-id guards belong to those services; this transport
/// cannot authorize a server mutation on its own. No arbitrary URL or panel operation is
/// accepted from a public form.
/// </para>
/// <para>
/// Failures surface as <see cref="PterodactylApiException"/> carrying a
/// <see cref="PterodactylFailure"/> category. Lookups that legitimately come back empty return
/// <see langword="null"/> instead, because "this customer does not exist yet" is an expected
/// answer, not an error.
/// </para>
/// </remarks>
public interface IPterodactylClient
{
    /// <summary>Whether a panel URL and API key have been supplied.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Reads the node list. This is the connectivity test: it proves the panel is reachable, the
    /// key authenticates, and the response parses.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    Task<IReadOnlyList<PterodactylNode>> GetNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the location list.</summary>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    Task<IReadOnlyList<PterodactylLocation>> GetLocationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one egg, including its variables.
    /// </summary>
    /// <param name="nestId">Nest the egg lives in.</param>
    /// <param name="eggId">Egg to read.</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    /// <remarks>
    /// Server creation requires <c>docker_image</c> and <c>startup</c>, and the panel rejects a
    /// request that omits a variable the egg marks required. Both come from here rather than
    /// being guessed, which is the whole reason the nest id is configured at all.
    /// </remarks>
    Task<PterodactylEgg> GetEggAsync(int nestId, int eggId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a panel user by the external id we assigned it.
    /// </summary>
    /// <param name="externalId">External id to look up.</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    /// <returns>The user, or <see langword="null"/> when the panel has none.</returns>
    Task<PterodactylUser?> FindUserByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an exact email match; the panel's partial filter is never trusted as identity.</summary>
    Task<PterodactylUser?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a panel user.</summary>
    /// <param name="request">User to create.</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    Task<PterodactylUser> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a server by the external id we assigned it.
    /// </summary>
    /// <param name="externalId">External id to look up.</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    /// <returns>The server, or <see langword="null"/> when the panel has none.</returns>
    Task<PterodactylServer?> FindServerByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a server.</summary>
    /// <param name="request">Server to create.</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    Task<PterodactylServer> CreateServerAsync(
        CreateServerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one server; returns null when it no longer exists.</summary>
    Task<PterodactylServer?> GetServerAsync(int serverId, CancellationToken cancellationToken = default);

    /// <summary>Suspends a server without removing its files or allocation.</summary>
    Task SuspendServerAsync(int serverId, CancellationToken cancellationToken = default);

    /// <summary>Restores panel access after suspension without reinstalling the server.</summary>
    Task UnsuspendServerAsync(int serverId, CancellationToken cancellationToken = default);

    /// <summary>Changes resource ceilings on the same server; never reinstalls or recreates it.</summary>
    Task<PterodactylServer> UpdateServerBuildAsync(
        int serverId,
        UpdateServerBuildRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a server by its numeric panel id.
    /// </summary>
    /// <param name="serverId">Numeric panel id - not the short identifier and not the UUID.</param>
    /// <param name="force">
    /// Whether to continue when the node cannot be reached. A non-forced delete aborts if wings
    /// errors, which leaves the panel and the node consistent.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    /// <remarks>
    /// This method does not decide whether a server may be deleted. The provisioning lab
    /// accepts its own test prefix; the trial gateway accepts a validated trial prefix, with
    /// retention and paid-conversion state checked by the durable trial service.
    /// </remarks>
    Task DeleteServerAsync(int serverId, bool force = false, CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
