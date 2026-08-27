namespace HowToSoftware.Hosting.Models;

/// <summary>
/// A Project Zomboid image slot on the marketing pages.
/// </summary>
/// <remarks>
/// <para>
/// An enum rather than free-form paths, for the same reason the hardware photography uses one:
/// a component cannot ask for a file that nothing tells anybody to supply.
/// </para>
/// <para>
/// <b>These slots are empty on purpose.</b> Project Zomboid screenshots belong to The Indie
/// Stone, and their licence for promotional reuse by a hosting company is not something to
/// assume. Until someone drops in material that is confirmed cleared, every slot renders a
/// composed placeholder that names the exact path - and no slot ever falls back to stock
/// photography pretending to be the game.
/// </para>
/// </remarks>
public enum ZomboidPhotoSlot
{
    /// <summary>Wide atmospheric world shot behind the hero.</summary>
    HeroWorld,

    /// <summary>A persistent world in play - the world section.</summary>
    World,

    /// <summary>A modded world, for the Workshop section.</summary>
    Workshop,

    /// <summary>Survivors, for the community-scale sections.</summary>
    Survivors,

    /// <summary>Wide shot for the closing call to action.</summary>
    Closing
}

/// <summary>
/// The result of looking a Project Zomboid slot up on disk.
/// </summary>
/// <param name="Slot">The slot that was resolved.</param>
/// <param name="FileName">Base file name the slot expects.</param>
/// <param name="WebPath">Site-relative path to the image, or <see langword="null"/> when absent.</param>
/// <param name="FallbackWebPath">Site-relative JPEG for browsers without WebP, when present.</param>
/// <param name="DropPath">Repository-relative path a file should be placed at.</param>
public sealed record ZomboidPhotoAsset(
    ZomboidPhotoSlot Slot,
    string FileName,
    string? WebPath,
    string? FallbackWebPath,
    string DropPath)
{
    /// <summary>Whether an image is available for this slot.</summary>
    public bool IsAvailable => WebPath is not null;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
