namespace HowToSoftware.Hosting.Models;

/// <summary>
/// A photograph slot on the infrastructure page.
/// </summary>
/// <remarks>
/// An enum rather than free-form paths, so a component cannot ask for a file that the photo
/// README does not tell someone to supply.
/// </remarks>
public enum HardwarePhotoSlot
{
    /// <summary>The rack in the room, doors closed - the establishing shot.</summary>
    RackFront,

    /// <summary>The rack straight on, doors closed.</summary>
    RackElevation,

    /// <summary>The rack with the door open, the machines visible.</summary>
    RackOpen,

    /// <summary>Inside the rack: switching, cabling and the mounted machines.</summary>
    RackInterior,

    /// <summary>Close detail of the upper rack.</summary>
    RackDetailTop,

    /// <summary>Close detail of the lower rack.</summary>
    RackDetailBottom
}

/// <summary>
/// The result of looking a photo slot up on disk.
/// </summary>
/// <param name="Slot">The slot that was resolved.</param>
/// <param name="FileName">Base file name the slot expects, e.g. <c>node-01-front.webp</c>.</param>
/// <param name="WebPath">
/// Site-relative path to the WebP, or <see langword="null"/> when it has not been supplied yet.
/// </param>
/// <param name="FallbackWebPath">
/// Site-relative path to a JPEG of the same name when one exists, for browsers without WebP.
/// </param>
/// <param name="DropPath">
/// Repository-relative path a photographer should place the file at. Shown in the placeholder so
/// the instruction is on the page itself rather than in a document nobody opens.
/// </param>
public sealed record HardwarePhotoAsset(
    HardwarePhotoSlot Slot,
    string FileName,
    string? WebPath,
    string? FallbackWebPath,
    string DropPath)
{
    /// <summary>Whether a real photograph is available for this slot.</summary>
    public bool IsAvailable => WebPath is not null;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
