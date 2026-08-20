using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

namespace HowToSoftware.Hosting.Tests;

public class MockServerPreviewServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 20, 21, 0, 0, TimeSpan.Zero);

    private readonly TestTimeProvider _clock = new(Start);
    private readonly MockServerPreviewService _sut;

    public MockServerPreviewServiceTests()
    {
        _sut = new MockServerPreviewService(_clock);
    }

    [Fact]
    public async Task GetSnapshot_StartsOnlineWithSeededConsole()
    {
        var snapshot = await _sut.GetSnapshotAsync();

        Assert.Equal(ServerState.Online, snapshot.State);
        Assert.False(snapshot.IsTransitioning);
        Assert.NotEmpty(snapshot.Metrics);
        Assert.NotEmpty(snapshot.Console);
    }

    [Fact]
    public async Task Stop_TransitionsThroughStopping_ThenSettlesOffline()
    {
        var stopping = await _sut.SendCommandAsync(ServerCommand.Stop);

        Assert.Equal(ServerState.Stopping, stopping.State);
        Assert.True(stopping.IsTransitioning);

        _clock.Advance(TimeSpan.FromSeconds(5));
        var settled = await _sut.GetSnapshotAsync();

        Assert.Equal(ServerState.Offline, settled.State);
        Assert.False(settled.IsTransitioning);
    }

    [Fact]
    public async Task Restart_ReturnsToOnline()
    {
        await _sut.SendCommandAsync(ServerCommand.Restart);
        _clock.Advance(TimeSpan.FromSeconds(5));

        var settled = await _sut.GetSnapshotAsync();

        Assert.Equal(ServerState.Online, settled.State);
    }

    [Fact]
    public async Task Start_AfterStop_BringsTheInstanceBackOnline()
    {
        await _sut.SendCommandAsync(ServerCommand.Stop);
        _clock.Advance(TimeSpan.FromSeconds(5));
        await _sut.GetSnapshotAsync();

        await _sut.SendCommandAsync(ServerCommand.Start);
        _clock.Advance(TimeSpan.FromSeconds(5));
        var settled = await _sut.GetSnapshotAsync();

        Assert.Equal(ServerState.Online, settled.State);
    }

    [Fact]
    public async Task Start_WhileAlreadyOnline_IsIgnored()
    {
        var before = await _sut.GetSnapshotAsync();

        var after = await _sut.SendCommandAsync(ServerCommand.Start);

        Assert.Equal(ServerState.Online, after.State);
        Assert.Equal(before.Console.Count, after.Console.Count);
    }

    [Fact]
    public async Task Offline_ReportsNoPlayersAndZeroedLiveMetrics()
    {
        await _sut.SendCommandAsync(ServerCommand.Stop);
        _clock.Advance(TimeSpan.FromSeconds(5));

        var settled = await _sut.GetSnapshotAsync();

        Assert.Equal(0, settled.PlayersOnline);
        Assert.Equal(0, settled.Metrics.Single(m => m.Label == "CPU").Percent);
        Assert.Equal(0, settled.Metrics.Single(m => m.Label == "MEMORY").Percent);
    }

    [Fact]
    public async Task Console_IsCappedSoItCannotGrowWithoutBound()
    {
        // Each stop/start round trip appends two lines; run well past the buffer size.
        for (var i = 0; i < 20; i++)
        {
            await _sut.SendCommandAsync(ServerCommand.Stop);
            _clock.Advance(TimeSpan.FromSeconds(5));
            await _sut.GetSnapshotAsync();

            await _sut.SendCommandAsync(ServerCommand.Start);
            _clock.Advance(TimeSpan.FromSeconds(5));
            await _sut.GetSnapshotAsync();
        }

        var snapshot = await _sut.GetSnapshotAsync();

        Assert.True(snapshot.Console.Count <= 18, $"Console grew to {snapshot.Console.Count} entries.");
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
