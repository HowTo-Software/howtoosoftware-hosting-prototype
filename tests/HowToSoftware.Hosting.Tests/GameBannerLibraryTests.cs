using HowToSoftware.Hosting.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The banner files the catalogue cards look for: fixed names, resolved on disk, absent
/// without fuss.
/// </summary>
public sealed class GameBannerLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hts-banners-{Guid.NewGuid():N}");

    public GameBannerLibraryTests() => Directory.CreateDirectory(Path.Combine(_root, "images", "games"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private GameBannerLibrary Library() => new(new StubEnvironment(_root));

    [Theory]
    [InlineData("project-zomboid", "Zomboid_Banner.png")]
    [InlineData("minecraft", "Mine_banner.png")]
    public void EachGameLooksForItsAgreedFileName(string slug, string fileName)
    {
        var banner = Library().Resolve(slug);

        Assert.Equal(fileName, banner.FileName);
        Assert.False(banner.IsAvailable);
        Assert.EndsWith($"wwwroot/images/games/{fileName}", banner.DropPath, StringComparison.Ordinal);
    }

    [Fact]
    public void ABannerAppearsTheMomentTheFileExists()
    {
        var library = Library();
        Assert.False(library.Resolve("project-zomboid").IsAvailable);

        File.WriteAllBytes(Path.Combine(_root, "images", "games", "Zomboid_Banner.png"), [0x89, 0x50, 0x4E, 0x47]);

        var banner = library.Resolve("project-zomboid");
        Assert.True(banner.IsAvailable);
        Assert.Equal("images/games/Zomboid_Banner.png", banner.WebPath);
    }

    [Fact]
    public void AnUnknownGameIsSimplyAbsent()
    {
        var banner = Library().Resolve("palworld");

        Assert.False(banner.IsAvailable);
        Assert.Null(banner.WebPath);
    }

    [Fact]
    public void EveryCatalogueGameHasABannerName()
    {
        foreach (var slug in new[] { StaticGameCatalogService.ProjectZomboidSlug, StaticGameCatalogService.MinecraftSlug })
        {
            Assert.True(GameBannerLibrary.FileNames.ContainsKey(slug), $"{slug} has no banner file name");
        }
    }

    private sealed class StubEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
