using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The Project Zomboid image slots.
/// </summary>
/// <remarks>
/// These slots are expected to be <b>empty</b>. Game imagery belongs to The Indie Stone, and
/// nothing goes in the folder until somebody confirms it may. What is tested here is that the
/// site behaves correctly while that is true: it says where a file goes, it never substitutes
/// anything, and every slot has a label in both languages the moment one arrives.
/// </remarks>
public class ZomboidPhotoTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);

    public void Dispose() => _culture.Dispose();

    public static TheoryData<ZomboidPhotoSlot> AllSlots()
    {
        var data = new TheoryData<ZomboidPhotoSlot>();

        foreach (var slot in Enum.GetValues<ZomboidPhotoSlot>())
        {
            data.Add(slot);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public void AnEmptySlotSaysWhereTheFileGoes(ZomboidPhotoSlot slot)
    {
        var resolved = new ZomboidPhotoLibrary(EmptyWebRoot()).Resolve(slot);

        Assert.False(resolved.IsAvailable);
        Assert.Null(resolved.WebPath);
        Assert.StartsWith(
            "src/HowToSoftware.Hosting/wwwroot/images/zomboid/",
            resolved.DropPath,
            StringComparison.Ordinal);
        Assert.EndsWith(".webp", resolved.DropPath, StringComparison.Ordinal);
    }

    [Fact]
    public void NoTwoSlotsShareAFile()
    {
        var library = new ZomboidPhotoLibrary(EmptyWebRoot());

        var names = Enum.GetValues<ZomboidPhotoSlot>()
            .Select(slot => library.Resolve(slot).FileName)
            .ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    /// <summary>
    /// A slot resolves against its own folder and nothing else - the hardware photography and
    /// the game imagery must never be able to stand in for each other.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllSlots))]
    public void EverySlotResolvesInsideTheGameImageFolder(ZomboidPhotoSlot slot)
    {
        var resolved = new ZomboidPhotoLibrary(EmptyWebRoot()).Resolve(slot);

        Assert.Contains("images/zomboid/", resolved.DropPath, StringComparison.Ordinal);
        Assert.DoesNotContain("infrastructure", resolved.DropPath, StringComparison.Ordinal);
    }

    /// <summary>
    /// The folder ships a README saying why it is empty and what has to be true before a file
    /// goes in it. Deleting that leaves the next person to guess at a licensing question.
    /// </summary>
    [Fact]
    public void TheFolderExplainsWhyItIsEmpty()
    {
        var readme = Path.Combine(LocateWebRoot(), "images", "zomboid", "README.md");

        Assert.True(File.Exists(readme), $"No README at {readme}");

        var text = File.ReadAllText(readme);

        Assert.Contains("Indie Stone", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permission", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// If somebody does drop files in, the site must not start serving them from a folder whose
    /// provenance was never recorded. This is a reminder in test form rather than a guard: it
    /// fails loudly the day the folder stops being empty, so the README gets updated with it.
    /// </summary>
    [Fact]
    public void TheEditorialPhotoSlotsAreStillEmpty_SoTheLicenceNoteIsStillCurrent()
    {
        var folder = Path.Combine(LocateWebRoot(), "images", "zomboid");
        var editorialSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hero-world.webp", "hero-world.jpg",
            "world.webp", "world.jpg",
            "workshop.webp", "workshop.jpg",
            "survivors.webp", "survivors.jpg",
            "closing.webp", "closing.jpg"
        };
        var images = Directory.EnumerateFiles(folder)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(editorialSlots.Contains)
            .ToArray();

        Assert.True(
            images.Length == 0,
            "Editorial game imagery has been added: " + string.Join(", ", images) +
            ". Record where each file came from and what permits its use in " +
            "wwwroot/images/zomboid/README.md, then update this test.");
    }

    private static IWebHostEnvironment EmptyWebRoot() => new StubWebHost(new NullFileProvider());

    private static string LocateWebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "HowToSoftware.Hosting", "wwwroot");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the web root above {AppContext.BaseDirectory}.");
    }

    private sealed class StubWebHost(IFileProvider webRoot) : IWebHostEnvironment
    {
        public IFileProvider WebRootFileProvider { get; set; } = webRoot;

        public string WebRootPath { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = "HowToSoftware.Hosting.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = Environments.Development;
    }
}

// =============================================================
// (c) 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
