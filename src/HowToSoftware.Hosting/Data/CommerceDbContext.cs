using HowToSoftware.Hosting.Models.Commerce;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Data;

/// <summary>
/// Independent PostgreSQL commerce database. Business services depend on repository interfaces,
/// so this provider can later move from Supabase to the primary HTS database without a rewrite.
/// </summary>
public sealed class CommerceDbContext(DbContextOptions<CommerceDbContext> options) : DbContext(options)
{
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<CommerceGame> Games => Set<CommerceGame>();
    public DbSet<CommerceHostingPlan> HostingPlans => Set<CommerceHostingPlan>();
    public DbSet<PlanBillingPrice> PlanBillingPrices => Set<PlanBillingPrice>();
    public DbSet<CommerceOrder> Orders => Set<CommerceOrder>();
    public DbSet<HostingServiceRecord> HostingServices => Set<HostingServiceRecord>();
    public DbSet<StripeEventRecord> StripeEvents => Set<StripeEventRecord>();
    public DbSet<ProvisioningJobRecord> ProvisioningJobs => Set<ProvisioningJobRecord>();
    public DbSet<HostingNodeRecord> HostingNodes => Set<HostingNodeRecord>();
    public DbSet<GameDeploymentProfileRecord> GameDeploymentProfiles => Set<GameDeploymentProfileRecord>();
    public DbSet<DeploymentEventRecord> DeploymentEvents => Set<DeploymentEventRecord>();
    public DbSet<BillingInvoiceReference> BillingInvoiceRefs => Set<BillingInvoiceReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HtsUserId).HasMaxLength(128);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.StripeCustomerId).HasMaxLength(128);
            entity.HasIndex(x => x.HtsUserId).IsUnique();
            entity.HasIndex(x => x.StripeCustomerId).IsUnique();
        });

        modelBuilder.Entity<CommerceGame>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.ArtworkUrl).HasMaxLength(2048);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.Active, x.PrimaryGame });
        });

        modelBuilder.Entity<CommerceHostingPlan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_hosting_plans_resources", "ram_mb > 0 AND cpu_percent > 0 AND disk_mb > 0");
                table.HasCheckConstraint("ck_hosting_plans_limits", "allocation_limit >= 0 AND database_limit >= 0 AND backup_limit >= 0");
                table.HasCheckConstraint("ck_hosting_plans_price", "monthly_price_cents >= 0");
            });
            entity.Property(x => x.Slug).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.GameId, x.Active, x.SortOrder });
            entity.HasOne<CommerceGame>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlanBillingPrice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_plan_billing_prices_period", "billing_period IN ('monthly', 'quarterly', 'annual')");
                table.HasCheckConstraint("ck_plan_billing_prices_discount", "discount_percent BETWEEN 0 AND 100");
                table.HasCheckConstraint("ck_plan_billing_prices_amount", "amount_cents >= 0");
            });
            entity.Property(x => x.BillingPeriod).HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.StripeProductId).HasMaxLength(128);
            entity.Property(x => x.StripePriceId).HasMaxLength(128);
            entity.HasIndex(x => new { x.PlanId, x.BillingPeriod }).IsUnique();
            entity.HasIndex(x => x.StripePriceId).IsUnique();
            entity.HasOne<CommerceHostingPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommerceOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_orders_amounts", "monthly_price_cents >= 0 AND base_amount_cents >= 0 AND discount_amount_cents >= 0 AND final_amount_cents >= 0");
                table.HasCheckConstraint("ck_orders_discount", "discount_percent BETWEEN 0 AND 100");
            });
            entity.Property(x => x.HtsUserId).HasMaxLength(128);
            entity.Property(x => x.CustomerEmail).HasMaxLength(320);
            entity.Property(x => x.GameSlug).HasMaxLength(64);
            entity.Property(x => x.PlanSlug).HasMaxLength(64);
            entity.Property(x => x.PlanName).HasMaxLength(128);
            entity.Property(x => x.BillingPeriod).HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ProvisioningStage).HasMaxLength(32);
            entity.Property(x => x.StripeCustomerId).HasMaxLength(128);
            entity.Property(x => x.StripeCheckoutSessionId).HasMaxLength(128);
            entity.Property(x => x.StripePaymentIntentId).HasMaxLength(128);
            entity.Property(x => x.StripeSubscriptionId).HasMaxLength(128);
            entity.Property(x => x.StripeInvoiceId).HasMaxLength(128);
            entity.Property(x => x.SubscriptionStatus).HasMaxLength(32);
            entity.Property(x => x.ServerIdentifier).HasMaxLength(64);
            entity.Property(x => x.FailureReason).HasMaxLength(2048);
            entity.HasIndex(x => x.StripeCheckoutSessionId).IsUnique();
            entity.HasIndex(x => x.StripeSubscriptionId);
            entity.HasIndex(x => x.CustomerProfileId);
            entity.HasIndex(x => x.Status);
            entity.HasOne<CustomerProfile>().WithMany().HasForeignKey(x => x.CustomerProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CommerceGame>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CommerceHostingPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HostingServiceRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.StripeSubscriptionId).HasMaxLength(128);
            entity.Property(x => x.PterodactylServerUuid).HasMaxLength(64);
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasIndex(x => x.StripeSubscriptionId).IsUnique();
            entity.HasIndex(x => x.PterodactylServerId).IsUnique();
            entity.HasIndex(x => x.PterodactylServerUuid).IsUnique();
            entity.HasIndex(x => new { x.CustomerProfileId, x.Status });
            entity.HasOne<CommerceOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerProfile>().WithMany().HasForeignKey(x => x.CustomerProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CommerceGame>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CommerceHostingPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StripeEventRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StripeEventId).HasMaxLength(128);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.Property(x => x.ProcessingError).HasMaxLength(2048);
            entity.HasIndex(x => x.StripeEventId).IsUnique();
            entity.HasIndex(x => new { x.Processed, x.ReceivedAt });
        });

        modelBuilder.Entity<ProvisioningJobRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table => table.HasCheckConstraint("ck_provisioning_jobs_attempts", "attempt_count >= 0"));
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.PterodactylServerUuid).HasMaxLength(64);
            entity.Property(x => x.LastError).HasMaxLength(4096);
            entity.HasIndex(x => x.HostingServiceId).IsUnique();
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.HasOne<HostingServiceRecord>().WithMany().HasForeignKey(x => x.HostingServiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CommerceOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HostingNodeRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Region).HasMaxLength(64);
            entity.Property(x => x.Location).HasMaxLength(128);
            entity.HasIndex(x => x.PterodactylNodeId).IsUnique();
            entity.HasIndex(x => new { x.Enabled, x.Maintenance, x.Priority });
        });

        modelBuilder.Entity<GameDeploymentProfileRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DockerImage).HasMaxLength(512);
            entity.Property(x => x.DefaultStartup).HasMaxLength(2048);
            entity.Property(x => x.DeploymentConfigJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.GameId).IsUnique().HasFilter("active");
            entity.HasOne<CommerceGame>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeploymentEventRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64);
            entity.Property(x => x.Message).HasMaxLength(2048);
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(x => new { x.ProvisioningJobId, x.CreatedAt });
            entity.HasOne<ProvisioningJobRecord>().WithMany().HasForeignKey(x => x.ProvisioningJobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<HostingServiceRecord>().WithMany().HasForeignKey(x => x.HostingServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillingInvoiceReference>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_billing_invoice_refs_amounts",
                "amount_due_cents >= 0 AND amount_paid_cents >= 0"));
            entity.Property(x => x.StripeInvoiceId).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.HostedInvoiceUrl).HasMaxLength(2048);
            entity.Property(x => x.InvoicePdfUrl).HasMaxLength(2048);
            entity.HasIndex(x => x.StripeInvoiceId).IsUnique();
            entity.HasIndex(x => new { x.CustomerProfileId, x.InvoiceDate });
            entity.HasOne<CustomerProfile>().WithMany().HasForeignKey(x => x.CustomerProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HostingServiceRecord>().WithMany().HasForeignKey(x => x.HostingServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(ToSnakeCase(entityType.GetTableName()!));
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value) => string.Concat(
        value.Select((character, index) => char.IsUpper(character) && index > 0
            ? $"_{char.ToLowerInvariant(character)}"
            : char.ToLowerInvariant(character).ToString()));
}
