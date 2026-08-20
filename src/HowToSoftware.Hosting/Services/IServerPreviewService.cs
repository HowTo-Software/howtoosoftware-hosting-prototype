using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Provides the server state rendered by the control-panel preview.
/// </summary>
/// <remarks>
/// The contract is asynchronous and command-based on purpose: the prototype is backed by an
/// in-memory implementation, but the same interface can be implemented against a game panel
/// API (Pterodactyl) later without changing <c>ServerPreviewSection</c>.
/// </remarks>
public interface IServerPreviewService
{
    /// <summary>Reads the current state of the demonstration server.</summary>
    ValueTask<ServerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a power action and returns the resulting state.
    /// </summary>
    /// <param name="command">The power action to perform.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask<ServerSnapshot> SendCommandAsync(
        ServerCommand command,
        CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
