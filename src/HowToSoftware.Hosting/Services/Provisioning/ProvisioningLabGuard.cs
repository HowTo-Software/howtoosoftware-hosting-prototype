namespace HowToSoftware.Hosting.Services.Provisioning;

/// <summary>
/// Settings for the development-only provisioning lab.
/// </summary>
public sealed class ProvisioningLabOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ProvisioningTest";

    /// <summary>
    /// Opens the lab outside Development. Off unless deliberately switched on.
    /// </summary>
    /// <remarks>
    /// Exists so the pipeline can be exercised on a staging host that does not run in the
    /// Development environment. Turning it on in production would put a button that creates and
    /// deletes real servers on a public URL.
    /// </remarks>
    public bool Enabled { get; set; }
}

/// <summary>
/// Decides whether the provisioning lab may be used at all.
/// </summary>
public interface IProvisioningLabGuard
{
    /// <summary>Whether the lab is open on this host.</summary>
    bool IsAvailable { get; }

    /// <summary>Why it is closed, for a developer looking at a blank page.</summary>
    string ClosedReason { get; }

    /// <summary>
    /// Throws unless the lab is open.
    /// </summary>
    /// <exception cref="InvalidOperationException">The lab is closed on this host.</exception>
    /// <remarks>
    /// Called at the top of every action, not only when the page renders. A Blazor page that
    /// merely hides its buttons is still reachable over the circuit, so the check that matters is
    /// the one on the server immediately before the work.
    /// </remarks>
    void EnsureAvailable();
}

/// <summary>
/// Opens the lab in Development, or wherever <c>ProvisioningTest:Enabled</c> is explicitly set.
/// </summary>
public sealed class ProvisioningLabGuard : IProvisioningLabGuard
{
    private readonly IWebHostEnvironment _environment;
    private readonly ProvisioningLabOptions _options;

    /// <summary>Creates the guard.</summary>
    /// <param name="environment">Host environment.</param>
    /// <param name="options">Lab settings.</param>
    public ProvisioningLabGuard(
        IWebHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<ProvisioningLabOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool IsAvailable => _environment.IsDevelopment() || _options.Enabled;

    /// <inheritdoc />
    public string ClosedReason =>
        $"The provisioning lab is only available in Development, or where {ProvisioningLabOptions.SectionName}:Enabled is set. "
            + $"This host runs in the {_environment.EnvironmentName} environment.";

    /// <inheritdoc />
    public void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(ClosedReason);
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
