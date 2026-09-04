using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Supabase;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Commerce;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Orders;
using HowToSoftware.Hosting.Services.Provisioning;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Opt-in PostgreSQL contract test. It never reads the application's production connection
/// variable: a developer must deliberately enable it and provide the dedicated TEST variable.
/// </summary>
public sealed class SupabaseIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CommerceLifecyclePersistsAndRemainsIdempotent()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("SUPABASE_INTEGRATION_TESTS_ENABLED"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var raw = Environment.GetEnvironmentVariable("SUPABASE_TEST_DB_CONNECTION_STRING");
        Assert.False(string.IsNullOrWhiteSpace(raw),
            "Set SUPABASE_TEST_DB_CONNECTION_STRING to a dedicated non-production Supabase project.");

        var supabase = new SupabaseOptions { DbConnectionString = raw! };
        Assert.True(supabase.IsDatabaseConfigured);

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(supabase.GetNpgsqlConnectionString())
            .Options;
        var factory = new TestCommerceFactory(options);

        await using (var db = await factory.CreateDbContextAsync())
        {
            Assert.True(await db.Database.CanConnectAsync());
            await db.Database.MigrateAsync();
        }

        var suffix = Guid.NewGuid().ToString("N");
        var gameId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var userId = $"integration-user-{suffix}";
        var gameSlug = $"integration-game-{suffix}";
        var planSlug = $"integration-plan-{suffix}";
        var now = DateTimeOffset.UtcNow;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Games.Add(new CommerceGame
            {
                Id = gameId,
                Slug = gameSlug,
                Name = "Integration Game",
                Active = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.HostingPlans.Add(new CommerceHostingPlan
            {
                Id = planId,
                GameId = gameId,
                Slug = planSlug,
                Name = "Integration Plan",
                RamMb = 4096,
                CpuPercent = 300,
                DiskMb = 25600,
                AllocationLimit = 1,
                DatabaseLimit = 0,
                BackupLimit = 1,
                MonthlyPriceCents = 799,
                Currency = "USD",
                Active = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        var orders = new PostgresOrderStore(factory);
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            CustomerEmail = $"{suffix}@example.test",
            GameId = gameSlug,
            PlanId = planSlug,
            PlanName = "Integration Plan",
            BillingPeriod = BillingPeriod.Annual,
            MonthlyPrice = 7.99m,
            BaseAmount = 95.88m,
            DiscountPercentage = 10,
            DiscountAmount = 9.59m,
            FinalAmount = 86.29m,
            Currency = "usd",
            Status = OrderStatus.Provisioning,
            ProvisioningStage = FulfilmentStage.Preparing,
            StripeCustomerId = $"cus_{suffix}",
            StripeSubscriptionId = $"sub_{suffix}",
            CreatedAt = now,
            UpdatedAt = now,
            PaidAt = now
        };

        Guid? provisioningJobId = null;

        try
        {
            await orders.AddAsync(order);
            Assert.True(await orders.TryRecordEventAsync($"evt_{suffix}", "checkout.session.completed"));
            Assert.False(await orders.TryRecordEventAsync($"evt_{suffix}", "checkout.session.completed"));

            var state = new PostgresProvisioningStateStore(factory, TimeProvider.System);
            await state.BeginAsync(orderId);
            await state.MarkCreatedAsync(orderId, new ProvisioningResult(
                ProvisioningOutcome.Created,
                orderId,
                101,
                $"uuid-{suffix}",
                suffix[..8],
                $"hts-order-{orderId:N}",
                3,
                12,
                44,
                false,
                "installing",
                planSlug,
                PterodactylFailure.None,
                "Created"));
            await state.MarkOnlineAsync(orderId);

            await using var verify = await factory.CreateDbContextAsync();
            var saved = await verify.Orders.SingleAsync(x => x.Id == orderId);
            var profile = await verify.CustomerProfiles.SingleAsync(x => x.HtsUserId == userId);
            var service = await verify.HostingServices.SingleAsync(x => x.OrderId == orderId);
            var job = await verify.ProvisioningJobs.SingleAsync(x => x.OrderId == orderId);
            provisioningJobId = job.Id;

            Assert.Equal(profile.Id, saved.CustomerProfileId);
            Assert.Equal($"sub_{suffix}", saved.StripeSubscriptionId);
            Assert.Equal(101, service.PterodactylServerId);
            Assert.Equal(3, service.PterodactylNodeId);
            Assert.Equal("active", service.Status);
            Assert.Equal("online", job.Status);
            Assert.True(await verify.DeploymentEvents.AnyAsync(x => x.ProvisioningJobId == job.Id));
        }
        finally
        {
            await using var cleanup = await factory.CreateDbContextAsync();
            if (provisioningJobId is { } jobId)
            {
                await cleanup.DeploymentEvents.Where(x => x.ProvisioningJobId == jobId).ExecuteDeleteAsync();
            }
            await cleanup.ProvisioningJobs.Where(x => x.OrderId == orderId).ExecuteDeleteAsync();
            await cleanup.HostingServices.Where(x => x.OrderId == orderId).ExecuteDeleteAsync();
            await cleanup.StripeEvents.Where(x => x.StripeEventId == $"evt_{suffix}").ExecuteDeleteAsync();
            await cleanup.Orders.Where(x => x.Id == orderId).ExecuteDeleteAsync();
            await cleanup.CustomerProfiles.Where(x => x.HtsUserId == userId).ExecuteDeleteAsync();
            await cleanup.HostingPlans.Where(x => x.Id == planId).ExecuteDeleteAsync();
            await cleanup.Games.Where(x => x.Id == gameId).ExecuteDeleteAsync();
        }
    }

    private sealed class TestCommerceFactory(DbContextOptions<CommerceDbContext> options)
        : IDbContextFactory<CommerceDbContext>
    {
        public CommerceDbContext CreateDbContext() => new(options);

        public Task<CommerceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
