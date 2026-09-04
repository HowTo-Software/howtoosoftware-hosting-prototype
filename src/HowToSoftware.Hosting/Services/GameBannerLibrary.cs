using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The banner file a game's catalogue card shows, resolved on disk.
/// </summary>
/// <param name="GameSlug">The game.</param>
/// <param name="FileName">The exact file name the card looks for.</param>
/// <param name="WebPath">Site-relative path to the image, or <see langword="null"/> when absent.</param>
/// <param name="DropPath">Repository-relative path the file should be placed at.</param>
public sealed record GameBanner(string GameSlug, string FileName, string? WebPath, string DropPath)
{
    /// <summary>Whether the file exists and the card can show it.</summary>
    public bool IsAvailable => WebPath is not null;
}

/// <summary>
/// Finds the banner image for a game's catalogue card.
/// </summary>
public interface IGameBannerLibrary
{
    /// <summary>Resolves a game's banner. Never throws for an unknown game; it is simply absent.</summary>
    GameBanner Resolve(string gameSlug);
}

/// <summary>
/// <see cref="IGameBannerLibrary"/> over <c>wwwroot/images/games</c>.
/// </summary>
/// <remarks>
/// <para>
/// One fixed file name per game, decided here rather than discovered, so the person supplying
/// the artwork knows exactly what to name it: drop <c>Zomboid_Banner.png</c> or
/// <c>Mine_banner.png</c> into the folder and the card shows it on the next request. While a
/// file is missing the card draws its typographic plate instead.
/// </para>
/// <para>
/// Checked on every resolve, not cached: the point is that adding a file needs no restart, and
/// a stat call per card render is nothing.
/// </para>
/// </remarks>
public sealed class GameBannerLibrary : IGameBannerLibrary
{
    /// <summary>Folder under <c>wwwroot</c> the banners live in.</summary>
    public const string Folder = "images/games";

    /// <summary>The file name each game's card looks for. Case matters on a Linux host.</summary>
    public static IReadOnlyDictionary<string, string> FileNames { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [StaticGameCatalogService.ProjectZomboidSlug] = "Zomboid_Banner.png",
            [StaticGameCatalogService.MinecraftSlug] = "Mine_banner.png"
        };

    private readonly IWebHostEnvironment _environment;

    /// <summary>Creates the library.</summary>
    public GameBannerLibrary(IWebHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public GameBanner Resolve(string gameSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameSlug);

        if (!FileNames.TryGetValue(gameSlug.Trim(), out var fileName))
        {
            return new GameBanner(gameSlug, string.Empty, null, $"src/HowToSoftware.Hosting/wwwroot/{Folder}/");
        }

        var dropPath = $"src/HowToSoftware.Hosting/wwwroot/{Folder}/{fileName}";
        var root = _environment.WebRootPath;

        var exists = !string.IsNullOrEmpty(root)
            && File.Exists(Path.Combine(root, "images", "games", fileName));

        return new GameBanner(gameSlug, fileName, exists ? $"{Folder}/{fileName}" : null, dropPath);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
