using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Explicit permission to advertise an existing promotion. This configuration creates no
/// coupon and sets no discount; the commerce promotion validator remains authoritative.
/// </summary>
public sealed class PublicPromotionOptions
{
    public const string SectionName = "PublicPromotions";
    public const int MaximumCampaigns = 3;

    public List<PublicPromotionCampaign> Campaigns { get; set; } = [];
}

public sealed class PublicPromotionCampaign
{
    public string Code { get; set; } = string.Empty;
    public string GameSlug { get; set; } = string.Empty;
    public string PlanSlug { get; set; } = string.Empty;
    public string Period { get; set; } = "monthly";
}

public sealed partial class PublicPromotionOptionsValidator : IValidateOptions<PublicPromotionOptions>
{
    public ValidateOptionsResult Validate(string? name, PublicPromotionOptions options)
    {
        var failures = new List<string>();

        if (options.Campaigns.Count > PublicPromotionOptions.MaximumCampaigns)
        {
            failures.Add($"PublicPromotions:Campaigns may contain at most {PublicPromotionOptions.MaximumCampaigns} campaigns.");
        }

        for (var index = 0; index < options.Campaigns.Count; index++)
        {
            var campaign = options.Campaigns[index];
            var prefix = $"PublicPromotions:Campaigns:{index}";

            if (!CodePattern().IsMatch(campaign.Code.Trim()))
            {
                failures.Add($"{prefix}:Code must contain 1-64 letters, digits, underscores or hyphens.");
            }

            if (!SlugPattern().IsMatch(campaign.GameSlug) || !SlugPattern().IsMatch(campaign.PlanSlug))
            {
                failures.Add($"{prefix} must identify an existing game and plan by their lowercase slugs.");
            }

            if (!BillingPolicy.TryParse(campaign.Period, out _))
            {
                failures.Add($"{prefix}:Period must be monthly, quarterly or annual.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^[a-z0-9-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
