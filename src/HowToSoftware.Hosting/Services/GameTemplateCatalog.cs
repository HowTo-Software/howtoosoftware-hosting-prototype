using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Resolves the game template a plan is built from.
/// </summary>
public interface IGameTemplateCatalog
{
    /// <summary>Every template the platform knows how to deploy.</summary>
    IReadOnlyList<GameTemplate> Templates { get; }

    /// <summary>Finds a template by id.</summary>
    /// <param name="id">Template id, e.g. <c>project-zomboid</c>.</param>
    /// <returns>The template, or <see langword="null"/> when unknown.</returns>
    GameTemplate? Find(string? id);
}

/// <summary>
/// The single game the platform deploys today, assembled from configuration.
/// </summary>
/// <remarks>
/// <para>
/// The egg, nest and location ids are deployment facts, not product facts: they differ between a
/// staging panel and the production one, so they are read from <see cref="PterodactylOptions"/>
/// rather than compiled in.
/// </para>
/// <para>
/// Adding a second game means adding an entry here and a set of plans that name it. Nothing in
/// the UI needs to change - a component asks for a plan, the plan names a template, and the
/// provisioner resolves it.
/// </para>
/// </remarks>
public sealed class GameTemplateCatalog : IGameTemplateCatalog
{
    /// <summary>Identifier every Project Zomboid plan points at.</summary>
    public const string ProjectZomboidId = "project-zomboid";

    private readonly IOptionsMonitor<PterodactylOptions> _options;

    /// <summary>Creates the catalogue.</summary>
    /// <param name="options">Panel deployment settings.</param>
    public GameTemplateCatalog(IOptionsMonitor<PterodactylOptions> options) => _options = options;

    /// <inheritdoc />
    public IReadOnlyList<GameTemplate> Templates => [ProjectZomboid];

    /// <inheritdoc />
    public GameTemplate? Find(string? id) =>
        string.Equals(id, ProjectZomboidId, StringComparison.OrdinalIgnoreCase) ? ProjectZomboid : null;

    private GameTemplate ProjectZomboid
    {
        get
        {
            var panel = _options.CurrentValue;

            return new GameTemplate
            {
                Id = ProjectZomboidId,
                GameName = "Project Zomboid",
                NestId = panel.NestId,
                EggId = panel.EggId,
                LocationId = panel.LocationId,
                DockerImage = string.IsNullOrWhiteSpace(panel.DockerImage) ? null : panel.DockerImage,
                StartupCommand = string.IsNullOrWhiteSpace(panel.StartupCommand) ? null : panel.StartupCommand,
                Environment = panel.Environment,
                PortRange = panel.PortRange
            };
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
