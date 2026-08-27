using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>Resolves Project Zomboid image slots.</summary>
public interface IZomboidPhotoLibrary
{
    /// <summary>Looks a slot up on disk.</summary>
    /// <param name="slot">Slot to resolve.</param>
    /// <returns>The asset, available or not.</returns>
    ZomboidPhotoAsset Resolve(ZomboidPhotoSlot slot);
}

/// <summary>
/// Resolves Project Zomboid image slots against <c>wwwroot/images/zomboid</c>.
/// </summary>
/// <remarks>
/// The same shape as <see cref="HardwarePhotoLibrary"/>, deliberately: the two libraries hold
/// material with very different provenance - one photographed by us, one belonging to a game
/// studio - and holding them in the same shape keeps that difference in the folder and the
/// README rather than in the components.
/// </remarks>
public sealed class ZomboidPhotoLibrary : IZomboidPhotoLibrary
{
    /// <summary>Folder, relative to the web root, that holds the game imagery.</summary>
    internal const string Folder = "images/zomboid";

    private static readonly IReadOnlyDictionary<ZomboidPhotoSlot, string> FileNames =
        new Dictionary<ZomboidPhotoSlot, string>
        {
            [ZomboidPhotoSlot.HeroWorld] = "hero-world",
            [ZomboidPhotoSlot.World] = "world",
            [ZomboidPhotoSlot.Workshop] = "workshop",
            [ZomboidPhotoSlot.Survivors] = "survivors",
            [ZomboidPhotoSlot.Closing] = "closing"
        };

    private readonly IWebHostEnvironment _environment;
    private readonly Dictionary<ZomboidPhotoSlot, ZomboidPhotoAsset> _cache = [];
    private readonly Lock _gate = new();

    /// <summary>Creates the library.</summary>
    /// <param name="environment">Used to reach the web root's file provider.</param>
    public ZomboidPhotoLibrary(IWebHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public ZomboidPhotoAsset Resolve(ZomboidPhotoSlot slot)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(slot, out var cached))
            {
                return cached;
            }

            var resolved = Probe(slot);
            _cache[slot] = resolved;
            return resolved;
        }
    }

    private ZomboidPhotoAsset Probe(ZomboidPhotoSlot slot)
    {
        var baseName = FileNames[slot];
        var webp = $"{Folder}/{baseName}.webp";
        var jpeg = $"{Folder}/{baseName}.jpg";

        var hasWebp = Exists(webp);
        var hasJpeg = Exists(jpeg);

        return new ZomboidPhotoAsset(
            slot,
            $"{baseName}.webp",
            hasWebp ? "/" + webp : hasJpeg ? "/" + jpeg : null,
            hasWebp && hasJpeg ? "/" + jpeg : null,
            $"src/HowToSoftware.Hosting/wwwroot/{Folder}/{baseName}.webp");
    }

    private bool Exists(string relativePath) =>
        _environment.WebRootFileProvider.GetFileInfo(relativePath)
            is { Exists: true, IsDirectory: false };
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
