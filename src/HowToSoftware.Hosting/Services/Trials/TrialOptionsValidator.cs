using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services.Trials;

public sealed class TrialOptionsValidator(IOptions<PterodactylOptions>? panelOptions = null) : IValidateOptions<TrialOptions>
{
    public ValidateOptionsResult Validate(string? name, TrialOptions x)
    {
        var errors = new List<string>();
        if (x.DurationHours is < 1 or > 168) errors.Add("Trials:DurationHours must be between 1 and 168.");
        if (x.RetentionHours is < 0 or > 168) errors.Add("Trials:RetentionHours must be between 0 and 168.");
        if (x.MemoryMb is <= 0 or > 1048576 || x.DiskMb is <= 0 or > 1048576 || x.CpuPercent is < 0 or > 3600)
            errors.Add("Trials resource limits must be positive for memory/disk; CPU may be zero (unlimited).");
        if (x.BackupLimit is < 0 or > 10) errors.Add("Trials:BackupLimit must be between 0 and 10.");
        if (x.VerificationLifetimeMinutes is < 5 or > 1440) errors.Add("Trials:VerificationLifetimeMinutes must be between 5 and 1440.");
        if (x.ReservationLifetimeMinutes is < 30 or > 240) errors.Add("Trials:ReservationLifetimeMinutes must be between 30 and 240.");
        if (x.PollIntervalSeconds is < 5 or > 300 || x.RetryDelaySeconds is < 5 or > 3600 || x.LeaseMinutes is < 2 or > 30)
            errors.Add("Trials polling, retry and lease intervals are outside supported bounds.");
        if (x.LeaseMinutes * 60 <= (panelOptions?.Value.TimeoutSeconds ?? 30) + 30)
            errors.Add("Trials:LeaseMinutes must exceed Pterodactyl:TimeoutSeconds by more than 30 seconds.");
        var enabled = x.Profiles.Where(p => p.Enabled).ToArray();
        if (enabled.GroupBy(p => p.Id, StringComparer.Ordinal).Any(g => g.Count() > 1))
            errors.Add("Enabled Trials profile IDs must be unique.");
        foreach (var p in enabled)
        {
            if (string.IsNullOrWhiteSpace(p.Id) || p.Id.Length > 64 || p.GameSlug is not ("minecraft" or "project-zomboid")
                || p.NestId <= 0 || p.EggId <= 0)
                errors.Add("Enabled Trials profiles require a valid ID, supported game and configured nest/egg.");
            if (p.GameSlug == "minecraft" && (p.Edition is not ("java" or "bedrock") || string.IsNullOrWhiteSpace(p.Variant)
                || (p.Edition == "bedrock" && p.Variant != "vanilla") || p.AllowedVersions.Count == 0
                || p.AllowedVersions.Any(v => string.IsNullOrWhiteSpace(v) || v.Length > 64 || v.Any(char.IsControl))
                || string.IsNullOrWhiteSpace(p.VersionEnvironmentVariable)))
                errors.Add("Minecraft profiles require a compatible edition/variant, approved versions and a version environment variable.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
