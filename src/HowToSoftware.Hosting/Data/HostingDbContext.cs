using HowToSoftware.Hosting.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Data;

/// <summary>
/// The site's own database: orders and the Stripe events already handled.
/// </summary>
/// <remarks>
/// <para>
/// SQLite by default, chosen by connection string alone - the model uses nothing provider
/// specific, so pointing <c>ConnectionStrings:Hosting</c> at PostgreSQL later is a package
/// swap and a migration, not a rewrite.
/// </para>
/// <para>
/// Money is stored as <c>decimal</c> mapped to a TEXT column on SQLite. That is exact, which is
/// what matters; it is not sortable in SQL, which nothing here needs.
/// </para>
/// </remarks>
public sealed class HostingDbContext : DbContext
{
    /// <summary>Name of the connection string in configuration.</summary>
    public const string ConnectionName = "Hosting";

    /// <summary>Creates the context.</summary>
    public HostingDbContext(DbContextOptions<HostingDbContext> options) : base(options)
    {
    }

    /// <summary>Every order ever started.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Stripe event ids already acted on.</summary>
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);

            order.Property(o => o.GameId).HasMaxLength(64).IsRequired();
            order.Property(o => o.PlanId).HasMaxLength(64).IsRequired();
            order.Property(o => o.PlanName).HasMaxLength(128).IsRequired();
            order.Property(o => o.Currency).HasMaxLength(3).IsRequired();
            order.Property(o => o.UserId).HasMaxLength(128);
            order.Property(o => o.CustomerEmail).HasMaxLength(320);
            order.Property(o => o.StripeCheckoutSessionId).HasMaxLength(128);
            order.Property(o => o.StripeCustomerId).HasMaxLength(128);
            order.Property(o => o.StripeSubscriptionId).HasMaxLength(128);
            order.Property(o => o.SubscriptionStatus).HasMaxLength(32);
            order.Property(o => o.ServerIdentifier).HasMaxLength(64);
            order.Property(o => o.FailureReason).HasMaxLength(1024);

            order.Property(o => o.MonthlyPrice).HasPrecision(18, 2);
            order.Property(o => o.BaseAmount).HasPrecision(18, 2);
            order.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            order.Property(o => o.FinalAmount).HasPrecision(18, 2);

            // The webhook looks orders up by session and by subscription; both are unique
            // once set, and an index on each keeps the lookup honest as the table grows.
            order.HasIndex(o => o.StripeCheckoutSessionId).IsUnique();
            order.HasIndex(o => o.StripeSubscriptionId);
            order.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<ProcessedStripeEvent>(evt =>
        {
            evt.ToTable("ProcessedStripeEvents");
            evt.HasKey(e => e.Id);
            evt.Property(e => e.Id).HasMaxLength(128);
            evt.Property(e => e.Type).HasMaxLength(128).IsRequired();
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
