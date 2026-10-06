using System.Globalization;

namespace HowToSoftware.Hosting.Models;

/// <summary>Operator-approved paid tiers for conversion of an existing Minecraft trial.</summary>
public sealed class MinecraftPlanOptions
{
    public const string SectionName = "MinecraftPlans";
    public List<MinecraftPlanTier> Tiers { get; set; } = [];
}

public sealed class MinecraftPlanTier
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MemoryMb { get; set; }
    public int CpuPercent { get; set; }
    public int DiskMb { get; set; }
    public int BackupLimit { get; set; } = 1;
    public int AllocationLimit { get; set; } = 1;
    public string MonthlyPrice { get; set; } = string.Empty;
    public decimal? Price => decimal.TryParse(MonthlyPrice, NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture, out var value) && value > 0 ? value : null;
    public bool IsValid => Slug.StartsWith("minecraft-", StringComparison.Ordinal)
        && Slug.Length <= 80 && Slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
        && Name.Length is > 0 and <= 80 && Description.Length <= 500
        && MemoryMb is > 0 and <= 65536 && CpuPercent is >= 0 and <= 3600
        && DiskMb is > 0 and <= 512000 && BackupLimit is >= 0 and <= 100
        && AllocationLimit is >= 1 and <= 20 && Price is not null;
}
