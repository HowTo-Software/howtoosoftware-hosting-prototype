using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Knows which infrastructure photographs actually exist on disk.
/// </summary>
/// <remarks>
/// The infrastructure page presents these as photographs of our own machines, so it must never
/// substitute stock imagery when one is missing. Instead it asks here, and renders a labelled
/// placeholder naming the path to drop the file at. Adding the file is the only step needed -
/// the next render picks it up.
/// </remarks>
public interface IHardwarePhotoLibrary
{
    /// <summary>Resolves a photo slot to whatever is currently on disk for it.</summary>
    /// <param name="slot">The slot to resolve.</param>
    HardwarePhotoAsset Resolve(HardwarePhotoSlot slot);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
