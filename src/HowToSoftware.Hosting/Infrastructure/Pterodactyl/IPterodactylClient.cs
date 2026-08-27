using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Models;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Requests;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// The Pterodactyl Application API, narrowed to what provisioning actually needs.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not a general-purpose panel SDK. Every method here exists because a step of
/// provisioning needs it, and there is no method that could delete or modify something the
/// provisioning workflow did not create. A thin client is also a small attack surface: nothing
/// on this site can be talked into an arbitrary panel call.
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
    /// This method does not decide <em>whether</em> a server may be deleted. That guard lives in
    /// the provisioning service, which refuses anything whose external id is not a lab server.
    /// </remarks>
    Task DeleteServerAsync(int serverId, bool force = false, CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
