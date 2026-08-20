using System.Globalization;
using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// In-memory implementation of <see cref="IServerPreviewService"/> used by the control-panel
/// section on the landing page.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here talks to a real game server. State is held per Blazor circuit so a visitor can
/// press the power controls and watch a believable transition, and it resets when the page is
/// reloaded.
/// </para>
/// <para>
/// The transition model deliberately mirrors how a game panel API behaves: a command returns
/// immediately with a transitional state, and the caller polls until the instance settles. A
/// future panel-backed implementation can therefore replace this class without any change to
/// the components that consume it.
/// </para>
/// </remarks>
public sealed class MockServerPreviewService : IServerPreviewService
{
    private const int ConsoleCapacity = 18;

    private readonly TimeProvider _timeProvider;
    private readonly List<ConsoleEntry> _console;

    private ServerState _state = ServerState.Online;
    private ServerState? _pendingState;
    private DateTimeOffset _settlesAt;

    /// <summary>Creates the service.</summary>
    /// <param name="timeProvider">Clock used for console timestamps and transition timing.</param>
    public MockServerPreviewService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        // Seeded with descending offsets so the backlog reads like a real session rather than
        // a block of entries stamped at the same instant.
        _console =
        [
            Seed(-1_840, "SERVER", "World initialised from persistent volume", ConsoleSeverity.Success),
            Seed(-1_795, "WORKSHOP", "Resolving collection 3021845517", ConsoleSeverity.Info),
            Seed(-1_762, "WORKSHOP", "82 mods mounted, load order verified", ConsoleSeverity.Success),
            Seed(-1_698, "SERVER", "Sandbox configuration applied", ConsoleSeverity.Info),
            Seed(-1_540, "NETWORK", "Listening on allocation 203.0.113.24:16261", ConsoleSeverity.Success),
            Seed(-980, "PLAYER", "survivor_04 connected", ConsoleSeverity.Info),
            Seed(-624, "BACKUP", "Scheduled snapshot completed", ConsoleSeverity.Success),
            Seed(-97, "PLAYER", "survivor_11 connected", ConsoleSeverity.Info)
        ];
    }

    /// <inheritdoc />
    public ValueTask<ServerSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        SettleIfDue();
        return ValueTask.FromResult(BuildSnapshot());
    }

    /// <inheritdoc />
    public ValueTask<ServerSnapshot> SendCommandAsync(
        ServerCommand command,
        CancellationToken cancellationToken = default)
    {
        SettleIfDue();

        // Ignore commands that are meaningless for the current state, exactly as a panel API
        // would reject starting an already-running instance.
        var accepted = command switch
        {
            ServerCommand.Start => _state is ServerState.Offline,
            ServerCommand.Stop => _state is ServerState.Online,
            ServerCommand.Restart => _state is ServerState.Online,
            _ => false
        };

        if (!accepted)
        {
            return ValueTask.FromResult(BuildSnapshot());
        }

        (_state, _pendingState) = command switch
        {
            ServerCommand.Start => (ServerState.Starting, ServerState.Online),
            ServerCommand.Restart => (ServerState.Restarting, ServerState.Online),
            _ => (ServerState.Stopping, ServerState.Offline)
        };

        _settlesAt = _timeProvider.GetUtcNow().AddSeconds(2.4);

        Append(command switch
        {
            ServerCommand.Start => Line("POWER", "Start requested", ConsoleSeverity.Info),
            ServerCommand.Restart => Line("POWER", "Restart requested, saving world", ConsoleSeverity.Warning),
            _ => Line("POWER", "Stop requested, saving world", ConsoleSeverity.Warning)
        });

        return ValueTask.FromResult(BuildSnapshot());
    }

    /// <summary>Applies a pending transition once its simulated duration has elapsed.</summary>
    private void SettleIfDue()
    {
        if (_pendingState is not { } target || _timeProvider.GetUtcNow() < _settlesAt)
        {
            return;
        }

        _state = target;
        _pendingState = null;

        if (target is ServerState.Online)
        {
            Append(Line("WORKSHOP", "82 mods mounted, load order verified", ConsoleSeverity.Info));
            Append(Line("SERVER", "Health check passed, instance online", ConsoleSeverity.Success));
        }
        else
        {
            Append(Line("SERVER", "Instance stopped cleanly, world saved", ConsoleSeverity.Info));
        }
    }

    private void Append(ConsoleEntry entry)
    {
        _console.Add(entry);

        if (_console.Count > ConsoleCapacity)
        {
            _console.RemoveRange(0, _console.Count - ConsoleCapacity);
        }
    }

    private ConsoleEntry Line(string source, string message, ConsoleSeverity severity) =>
        Seed(0, source, message, severity);

    /// <summary>Builds a console entry stamped <paramref name="offsetSeconds"/> from now.</summary>
    private ConsoleEntry Seed(int offsetSeconds, string source, string message, ConsoleSeverity severity) =>
        new(
            _timeProvider.GetLocalNow().AddSeconds(offsetSeconds).ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            source,
            severity,
            message);

    private ServerSnapshot BuildSnapshot()
    {
        var running = _state is ServerState.Online or ServerState.Restarting;
        var players = running ? 27 : 0;

        return new ServerSnapshot
        {
            Name = "knox-county-01",
            // RFC 5737 documentation address - never a routable host.
            Address = "203.0.113.24:16261",
            State = _state,
            Build = "Build 42",
            Region = "EU West",
            PlayersOnline = players,
            PlayerCapacity = 64,
            Uptime = running ? "06D 14H" : "-",
            Metrics =
            [
                new("CPU", IconName.Cpu, running ? 34 : 0, running ? "34%" : "0%", "of 8 vCPU allocated"),
                new("MEMORY", IconName.Memory, running ? 56 : 0, running ? "11.2 GB" : "0 GB", "of 20 GB allocated"),
                new("STORAGE", IconName.HardDrive, 41, "24.6 GB", "of 60 GB volume"),
                new("PLAYERS", IconName.Users, players * 100 / 64, $"{players} / 64", "slots in use")
            ],
            Console = _console.ToArray()
        };
    }
}
