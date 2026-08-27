namespace HowToSoftware.Hosting.Services.Provisioning;

/// <summary>
/// Turns an order into a running Project Zomboid server.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam the payment webhook will call. Today the only caller is the development
/// provisioning lab, pressing a button; when checkout exists, the webhook builds the same
/// <see cref="ProvisioningRequest"/> and calls the same method. Nothing about the pipeline
/// changes, which is why it is worth building it before the payments are.
/// </para>
/// <para>
/// <see cref="ProvisionAsync"/> is idempotent on <see cref="ProvisioningRequest.RequestId"/>:
/// calling it twice with the same id returns the existing server rather than creating a second
/// one. Payment providers retry webhooks, so this is not a nicety.
/// </para>
/// </remarks>
public interface IProvisioningService
{
    /// <summary>
    /// Reads the panel's current state for the provisioning lab: reachability, nodes, locations
    /// and the configured egg.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the calls.</param>
    /// <remarks>Read-only. Nothing is created, and nothing sensitive is returned.</remarks>
    Task<PanelSnapshot> InspectPanelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the server for an order, reusing the customer's panel account and any server
    /// already provisioned for the same request id.
    /// </summary>
    /// <param name="request">The order to fulfil.</param>
    /// <param name="cancellationToken">Token used to cancel the calls.</param>
    Task<ProvisioningResult> ProvisionAsync(
        ProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a server created by the provisioning lab.
    /// </summary>
    /// <param name="requestId">Request id of the test deployment to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the calls.</param>
    /// <returns>A result describing what happened.</returns>
    /// <remarks>
    /// Takes a request id rather than a server id, so there is no form field anywhere that can
    /// name an arbitrary server. The id is turned into the test external id, the server is looked
    /// up by it, and the external id is checked again before anything is deleted.
    /// </remarks>
    Task<ProvisioningResult> DeleteTestServerAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
