using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The infrastructure photography: whether the files the site asks for are the files that were
/// supplied, and whether every slot has a label to show under it.
/// </summary>
/// <remarks>
/// These exist because the slot enum is read three ways that a compiler cannot cross-check: as a
/// dictionary key for a file name, as a file on disk, and as a resource key built by string
/// interpolation in the component. Renaming a member satisfies the build and still ships a page
/// captioned "Photo.Slot.RackOpen" over an empty frame.
/// </remarks>
public class HardwarePhotoTests
{
    public static TheoryData<HardwarePhotoSlot> AllSlots()
    {
        var data = new TheoryData<HardwarePhotoSlot>();

        foreach (var slot in Enum.GetValues<HardwarePhotoSlot>())
        {
            data.Add(slot);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public void EverySlot_ResolvesToASuppliedPhotograph(HardwarePhotoSlot slot)
    {
        var library = new HardwarePhotoLibrary(RealWebRoot());

        var resolved = library.Resolve(slot);

        Assert.True(
            resolved.IsAvailable,
            $"No photograph for {slot}. Expected a file at {resolved.DropPath}.");
    }

    /// <summary>
    /// Both derivatives, so a browser without WebP is served the JPEG rather than an empty
    /// <c>picture</c> element.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllSlots))]
    public void EverySlot_HasBothAWebPAndAJpeg(HardwarePhotoSlot slot)
    {
        var resolved = new HardwarePhotoLibrary(RealWebRoot()).Resolve(slot);

        Assert.EndsWith(".webp", resolved.WebPath);
        Assert.EndsWith(".jpg", resolved.FallbackWebPath);
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public void EverySlot_HasALabelInBothLanguages(HardwarePhotoSlot slot)
    {
        foreach (var culture in new[] { "en", "pt-BR" })
        {
            using var scope = new CultureScope(culture);

            var label = TestLocalizer.For<HardwareText>()[$"Photo.Slot.{slot}"];

            Assert.False(
                label.ResourceNotFound,
                $"HardwareText is missing Photo.Slot.{slot} in {culture}.");
        }
    }

    /// <summary>
    /// A slot with nothing supplied says so, and says where the file goes. It does not quietly
    /// borrow another slot's photograph, and it does not fall back to stock imagery.
    /// </summary>
    [Fact]
    public void AMissingPhotograph_ReportsWhereToDropTheFile()
    {
        var library = new HardwarePhotoLibrary(EmptyWebRoot());

        var resolved = library.Resolve(HardwarePhotoSlot.RackOpen);

        Assert.False(resolved.IsAvailable);
        Assert.Null(resolved.WebPath);
        Assert.Equal(
            "src/HowToSoftware.Hosting/wwwroot/images/infrastructure/rack-open.webp",
            resolved.DropPath);
    }

    [Fact]
    public void NoTwoSlots_ShareTheSameFile()
    {
        var library = new HardwarePhotoLibrary(EmptyWebRoot());

        var names = Enum.GetValues<HardwarePhotoSlot>()
            .Select(slot => library.Resolve(slot).FileName)
            .ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    private static IWebHostEnvironment RealWebRoot() =>
        new StubWebHost(new PhysicalFileProvider(LocateWebRoot()));

    private static IWebHostEnvironment EmptyWebRoot() => new StubWebHost(new NullFileProvider());

    /// <summary>
    /// Walks up from the test binaries to the web project's <c>wwwroot</c>. The photographs are
    /// content files rather than build output, so they are only ever in the source tree.
    /// </summary>
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
