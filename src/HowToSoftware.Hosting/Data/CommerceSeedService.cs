using System.Text.Json;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Commerce;
using HowToSoftware.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Data;

/// <summary>Idempotent development seed for the first game and its commercial plans.</summary>
public sealed class CommerceSeedService(
    IDbContextFactory<CommerceDbContext> factory,
    IGameCatalogService games,
    IOptions<HostingPlanPricingOptions> pricing,
    IOptionsMonitor<PterodactylOptions> pterodactyl,
    IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Commerce seed data may only be applied in Development.");
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // The connection retries transient faults, and a retrying strategy will not drive a
        // transaction it did not open: the whole unit has to be replayable as one.
        await db.Database.CreateExecutionStrategy()
            .ExecuteAsync(token => SeedCoreAsync(db, token), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SeedCoreAsync(CommerceDbContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        var sourceGame = games.Primary;
        var game = await db.Games.SingleOrDefaultAsync(x => x.Slug == sourceGame.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (game is null)
        {
            game = new CommerceGame
            {
                Id = Guid.NewGuid(),
                Slug = sourceGame.Slug,
                Name = sourceGame.Name,
                Description = sourceGame.Summary,
                Active = true,
                PrimaryGame = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Games.Add(game);
        }

        foreach (var (sourcePlan, index) in games.PlansFor(sourceGame).Select((plan, index) => (plan, index)))
        {
            var plan = await db.HostingPlans.SingleOrDefaultAsync(x => x.Slug == sourcePlan.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (plan is null)
            {
                if (sourcePlan.PriceMonthly is not { } monthlyPrice)
                {
                    throw new InvalidOperationException($"Plan {sourcePlan.Slug} has no configured monthly price.");
                }

                plan = new CommerceHostingPlan
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    Slug = sourcePlan.Slug,
                    Name = sourcePlan.Name,
                    Description = sourcePlan.Tagline,
                    RamMb = sourcePlan.MemoryMib,
                    CpuPercent = sourcePlan.CpuPercent,
                    DiskMb = sourcePlan.DiskMib,
                    AllocationLimit = sourcePlan.AllocationLimit,
                    DatabaseLimit = sourcePlan.DatabaseLimit,
                    BackupLimit = sourcePlan.BackupLimit,
                    MonthlyPriceCents = ToCents(monthlyPrice),
                    Currency = pricing.Value.CurrencyCode.ToUpperInvariant(),
                    Active = true,
                    SortOrder = index,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.HostingPlans.Add(plan);
            }
            else
            {
                var monthlyPrice = sourcePlan.PriceMonthly
                    ?? throw new InvalidOperationException($"Plan {sourcePlan.Slug} has no configured monthly price.");
                plan.GameId = game.Id;
                plan.Name = sourcePlan.Name;
                plan.Description = sourcePlan.Tagline;
                plan.RamMb = sourcePlan.MemoryMib;
                plan.CpuPercent = sourcePlan.CpuPercent;
                plan.DiskMb = sourcePlan.DiskMib;
                plan.AllocationLimit = sourcePlan.AllocationLimit;
                plan.DatabaseLimit = sourcePlan.DatabaseLimit;
                plan.BackupLimit = sourcePlan.BackupLimit;
                plan.MonthlyPriceCents = ToCents(monthlyPrice);
                plan.Currency = pricing.Value.CurrencyCode.ToUpperInvariant();
                plan.Active = true;
                plan.SortOrder = index;
                plan.UpdatedAt = now;
            }

            foreach (var period in BillingPolicy.Periods)
            {
                var periodSlug = BillingPolicy.Slug(period);
                var billingPrice = await db.PlanBillingPrices.SingleOrDefaultAsync(
                    x => x.PlanId == plan.Id && x.BillingPeriod == periodSlug,
                    cancellationToken).ConfigureAwait(false);

                var quote = BillingPolicy.Quote(sourcePlan.PriceMonthly!.Value, period);
                if (billingPrice is null)
                {
                    billingPrice = new PlanBillingPrice
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        BillingPeriod = periodSlug,
                        DiscountPercent = quote.DiscountPercent,
                        AmountCents = quote.FinalAmountMinor,
                        Currency = pricing.Value.CurrencyCode.ToUpperInvariant(),
                        Active = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.PlanBillingPrices.Add(billingPrice);
                }
                else
                {
                    // Preserve dashboard-managed Stripe IDs while refreshing the commercial
                    // calculation. This also upgrades an earlier 15% annual seed to 10%.
                    billingPrice.DiscountPercent = quote.DiscountPercent;
                    billingPrice.AmountCents = quote.FinalAmountMinor;
                    billingPrice.Currency = pricing.Value.CurrencyCode.ToUpperInvariant();
                    billingPrice.Active = true;
                    billingPrice.UpdatedAt = now;
                }
            }
        }

        var hasDeploymentProfile = await db.GameDeploymentProfiles
            .AnyAsync(x => x.GameId == game.Id && x.Active, cancellationToken)
            .ConfigureAwait(false);

        if (!hasDeploymentProfile)
        {
            var panel = pterodactyl.CurrentValue;
            db.GameDeploymentProfiles.Add(new GameDeploymentProfileRecord
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                PterodactylNestId = panel.NestId,
                PterodactylEggId = panel.EggId,
                DockerImage = panel.DockerImage,
                DefaultStartup = panel.StartupCommand,
                DeploymentConfigJson = JsonSerializer.Serialize(new
                {
                    panel.LocationId,
                    panel.PortRange,
                    EnvironmentKeys = panel.Environment.Keys.OrderBy(key => key).ToArray()
                }),
                Active = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static long ToCents(decimal amount) =>
        checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
}
