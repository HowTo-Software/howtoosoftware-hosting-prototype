using HowToSoftware.Hosting.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Data;

/// <summary>
/// The site's own database: orders and the Stripe events already handled.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server, reached by connection string alone - the model uses nothing provider specific,
/// so moving it to another host is a connection string change, not a rewrite.
/// </para>
/// <para>
/// Money is stored as <c>decimal(18,2)</c>, which is exact and sortable.
/// </para>
/// </remarks>
public sealed class HostingDbContext : DbContext
{
    /// <summary>Name of the connection string in configuration.</summary>
    public const string ConnectionName = "Hosting";

    /// <summary>Kept distinct so this schema can never share a history table with another app.</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Hosting";

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
            order.Property(o => o.StripePaymentIntentId).HasMaxLength(128);
            order.Property(o => o.StripeCustomerId).HasMaxLength(128);
            order.Property(o => o.StripeSubscriptionId).HasMaxLength(128);
            order.Property(o => o.StripeInvoiceId).HasMaxLength(128);
            order.Property(o => o.SubscriptionStatus).HasMaxLength(32);
            order.Property(o => o.ServerIdentifier).HasMaxLength(64);
            order.Property(o => o.FailureReason).HasMaxLength(1024);

            order.Property(o => o.MonthlyPrice).HasPrecision(18, 2);
            order.Property(o => o.BaseAmount).HasPrecision(18, 2);
            order.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            order.Property(o => o.FinalAmount).HasPrecision(18, 2);

            // The webhook looks orders up by session and by subscription; both are unique
            // once set, and an index on each keeps the lookup honest as the table grows.
            // The filter matters: SQL Server treats NULLs as equal in a unique index, so without
            // it only one order could ever sit unpaid with no session id.
            order.HasIndex(o => o.StripeCheckoutSessionId).IsUnique().HasFilter("[StripeCheckoutSessionId] IS NOT NULL");
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

        // Stripe identifiers are case-sensitive keys minted elsewhere, and SQL Server's usual
        // default collation is not. Without this, two distinct identifiers compare equal: the
        // unique index rejects a legitimate row, and a lookup can return the wrong order.
        // Applied only on SQL Server, because the hermetic test provider has no such collation.
        if (Database.IsSqlServer())
        {
            // ProcessedStripeEvent.Id is itself a Stripe event id, so it is included by type
            // rather than by name: it is the key the replay guard compares on.
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(x => x.GetProperties())
                .Where(x => x.ClrType == typeof(string)
                    && (x.Name.StartsWith("Stripe", StringComparison.Ordinal)
                        || x.Name == "UserId"
                        || x.DeclaringType.ClrType == typeof(ProcessedStripeEvent))))
            {
                property.SetCollation("Latin1_General_100_BIN2");
            }
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
