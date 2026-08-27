using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Resolves infrastructure photo slots against <c>wwwroot/images/infrastructure</c>.
/// </summary>
/// <remarks>
/// <para>
/// Existence is checked once per slot and cached for the process. These files are added by
/// hand and then deployed; re-stating the question on every render of every card would be a
/// filesystem hit per image per request for an answer that changes when someone restarts the
/// site anyway.
/// </para>
/// <para>
/// Nothing here can be pointed at an arbitrary path: the slot enum is the whole input surface.
/// </para>
/// </remarks>
public sealed class HardwarePhotoLibrary : IHardwarePhotoLibrary
{
    /// <summary>Folder, relative to the web root, that holds the photography.</summary>
    internal const string Folder = "images/infrastructure";

    private static readonly IReadOnlyDictionary<HardwarePhotoSlot, string> FileNames =
        new Dictionary<HardwarePhotoSlot, string>
        {
            [HardwarePhotoSlot.RackFront] = "rack-front",
            [HardwarePhotoSlot.RackElevation] = "rack-elevation",
            [HardwarePhotoSlot.RackOpen] = "rack-open",
            [HardwarePhotoSlot.RackInterior] = "rack-interior",
            [HardwarePhotoSlot.RackDetailTop] = "rack-detail-top",
            [HardwarePhotoSlot.RackDetailBottom] = "rack-detail-bottom"
        };

    private readonly IWebHostEnvironment _environment;
    private readonly Dictionary<HardwarePhotoSlot, HardwarePhotoAsset> _cache = [];
    private readonly Lock _gate = new();

    /// <summary>Creates the library.</summary>
    /// <param name="environment">Used to reach the web root's file provider.</param>
    public HardwarePhotoLibrary(IWebHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public HardwarePhotoAsset Resolve(HardwarePhotoSlot slot)
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

    private HardwarePhotoAsset Probe(HardwarePhotoSlot slot)
    {
        var baseName = FileNames[slot];
        var webp = $"{Folder}/{baseName}.webp";
        var jpeg = $"{Folder}/{baseName}.jpg";

        var hasWebp = Exists(webp);
        var hasJpeg = Exists(jpeg);

        return new HardwarePhotoAsset(
            slot,
            $"{baseName}.webp",
            // A JPEG on its own is still a usable photograph, so it is accepted as the primary
            // source rather than being ignored for not being WebP.
            hasWebp ? "/" + webp : hasJpeg ? "/" + jpeg : null,
            hasWebp && hasJpeg ? "/" + jpeg : null,
            $"src/HowToSoftware.Hosting/wwwroot/{Folder}/{baseName}.webp");
    }

    private bool Exists(string relativePath)
    {
        var fileProvider = _environment.WebRootFileProvider;

        return fileProvider.GetFileInfo(relativePath) is { Exists: true, IsDirectory: false };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
