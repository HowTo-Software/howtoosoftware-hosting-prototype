namespace HowToSoftware.Hosting.Services.Trials;

/// <summary>Trial settings are operator-owned; no client value can become a deployment command.</summary>
public sealed class TrialOptions
{
    public const string SectionName = "Trials";
    public bool Enabled { get; set; }
    public int DurationHours { get; set; } = 24;
    public int RetentionHours { get; set; } = 72;
    public int MemoryMb { get; set; } = 6144;
    public int CpuPercent { get; set; }
    public int DiskMb { get; set; } = 25600;
    public int BackupLimit { get; set; } = 1;
    public int VerificationLifetimeMinutes { get; set; } = 60;
    public int ReservationLifetimeMinutes { get; set; } = 120;
    public int PollIntervalSeconds { get; set; } = 30;
    public int RetryDelaySeconds { get; set; } = 60;
    public int LeaseMinutes { get; set; } = 5;
    public List<TrialGameProfile> Profiles { get; set; } = [];
    public TrialResourceLimits Limits => new(MemoryMb, CpuPercent, DiskMb, BackupLimit);
}

/// <summary>Public selectors describe approved variants; deployment fields stay on the server.</summary>
public sealed class TrialGameProfile
{
    public string Id { get; set; } = string.Empty;
    public string GameSlug { get; set; } = "minecraft";
    public string Edition { get; set; } = "java";
    public string Variant { get; set; } = "vanilla";
    public bool Enabled { get; set; }
    public int NestId { get; set; }
    public int EggId { get; set; }
    public int LocationId { get; set; }
    public string? DockerImage { get; set; }
    public string? StartupCommand { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.Ordinal);
    public List<string> PortRange { get; set; } = [];
    public List<string> AllowedVersions { get; set; } = [];
    public string? VersionEnvironmentVariable { get; set; }
}
