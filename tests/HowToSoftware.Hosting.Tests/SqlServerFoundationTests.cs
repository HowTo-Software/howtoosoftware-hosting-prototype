using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Infrastructure.Database;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HowToSoftware.Hosting.Tests;

public sealed class SqlServerFoundationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Server=HOST,1433;Database=DB_AQUI;User Id=USER_AQUI;Password=SENHA_AQUI")]
    [InlineData("Server=YOUR-HOST,1433;Database=REPLACE_ME;User Id=sa;Password=REPLACE_ME")]
    public void PlaceholdersNeverEnableTheRemoteDatabase(string value)
    {
        var options = new SqlServerOptions { ConnectionString = value };
        Assert.False(options.IsDatabaseConfigured);
    }

    [Theory]
    [InlineData("Server=db.example,1433;User Id=app;Password=secret")]
    [InlineData("Database=hosting;User Id=app;Password=secret")]
    public void AConnectionStringMissingAServerOrDatabaseIsRejected(string value)
    {
        Assert.False(new SqlServerOptions { ConnectionString = value }.IsDatabaseConfigured);
    }

    /// <summary>
    /// Encryption is the one setting an operator must not be able to switch off by omission:
    /// without it the credentials and every order row cross the network in the clear.
    /// </summary>
    [Fact]
    public void EncryptionIsForcedOnEvenWhenTheSuppliedStringDisablesIt()
    {
        var options = new SqlServerOptions
        {
            ConnectionString = "Server=db.example,1433;Database=hosting;User Id=app;Password=p@ss;Encrypt=False"
        };

        Assert.True(options.IsDatabaseConfigured);
        var parsed = new SqlConnectionStringBuilder(options.GetConnectionString());
        Assert.True(parsed.Encrypt);
        Assert.Equal("p@ss", parsed.Password);
        Assert.Equal("HowToSoftware.Hosting", parsed.ApplicationName);
    }

    /// <summary>
    /// Certificate validation stays on unless the operator turns it off deliberately, because
    /// trusting any presented certificate still permits a machine-in-the-middle.
    /// </summary>
    [Fact]
    public void CertificateValidationIsOnUnlessExplicitlyWaived()
    {
        var strict = new SqlServerOptions
        {
            ConnectionString = "Server=db.example,1433;Database=hosting;User Id=app;Password=p@ss"
        };
        Assert.False(new SqlConnectionStringBuilder(strict.GetConnectionString()).TrustServerCertificate);

        var waived = new SqlServerOptions
        {
            ConnectionString = "Server=db.example,1433;Database=hosting;User Id=app;Password=p@ss;TrustServerCertificate=True"
        };
        Assert.True(new SqlConnectionStringBuilder(waived.GetConnectionString()).TrustServerCertificate);
    }

    [Fact]
    public async Task PlaceholderConfigurationReportsNotConfiguredWithoutConnecting()
    {
        var check = new SqlServerHealthCheck(new SqlServerOptions
        {
            ConnectionString = "Server=HOST,1433;Database=DB_AQUI;User Id=USER_AQUI;Password=SENHA_AQUI"
        });

        var result = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("No database configured", result.Description);
    }

    [Fact]
    public void CommerceModelUsesSqlServerMoneyAndSnakeCaseTables()
    {
        using var db = new CommerceDbContext(CommerceOptions());

        var order = db.Model.FindEntityType("HowToSoftware.Hosting.Models.Commerce.CommerceOrder")!;
        Assert.Equal("orders", order.GetTableName());
        Assert.Equal("bigint", order.FindProperty("FinalAmountCents")!.GetColumnType());
        Assert.Equal("final_amount_cents", order.FindProperty("FinalAmountCents")!.GetColumnName());
    }

    /// <summary>
    /// SQL Server treats NULLs as equal in a unique index, so an unfiltered unique index on an
    /// optional column would cap the table at one not-yet-linked row. Postgres did not, which is
    /// exactly the kind of difference that survives a migration and surfaces in production.
    /// </summary>
    [Theory]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.CommerceOrder", "stripe_checkout_session_id")]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.CustomerProfile", "stripe_customer_id")]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.PlanBillingPrice", "stripe_price_id")]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.HostingServiceRecord", "stripe_subscription_id")]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.HostingServiceRecord", "pterodactyl_server_id")]
    [InlineData("HowToSoftware.Hosting.Models.Commerce.HostingServiceRecord", "pterodactyl_server_uuid")]
    public void UniqueIndexesOnOptionalColumnsExcludeNulls(string entityName, string columnName)
    {
        using var db = new CommerceDbContext(CommerceOptions());
        var entity = db.Model.FindEntityType(entityName)!;

        var index = entity.GetIndexes().Single(x =>
            x.IsUnique && x.Properties.Count == 1 && x.Properties[0].GetColumnName() == columnName);

        Assert.Equal($"[{columnName}] IS NOT NULL", index.GetFilter());
    }

    /// <summary>
    /// SQL Server refuses a schema where one delete can reach the same table two ways
    /// (error 1785), so the second edge into deployment_events must not cascade.
    /// </summary>
    [Fact]
    public void DeploymentEventsHaveASingleCascadePath()
    {
        using var db = new CommerceDbContext(CommerceOptions());
        var entity = db.Model.FindEntityType("HowToSoftware.Hosting.Models.Commerce.DeploymentEventRecord")!;

        var cascading = entity.GetForeignKeys().Where(x => x.DeleteBehavior == DeleteBehavior.Cascade).ToList();
        Assert.Single(cascading);
        Assert.Equal("ProvisioningJobId", Assert.Single(cascading[0].Properties).Name);
    }

    /// <summary>Two schemas on one server must never share a migrations history table.</summary>
    [Fact]
    public void EachContextOwnsADistinctMigrationsHistoryTable()
    {
        Assert.NotEqual(CommerceDbContext.MigrationsHistoryTable, HostingDbContext.MigrationsHistoryTable);
    }

    /// <summary>
    /// Stripe and panel identifiers are case-sensitive keys minted elsewhere. SQL Server defaults
    /// to a case-insensitive collation, under which two distinct identifiers compare equal: the
    /// unique index would reject a legitimate row, and a webhook lookup could return a different
    /// customer's order. This is the difference most likely to be missed in a port from Postgres.
    /// </summary>
    [Theory]
    [InlineData("stripe_checkout_session_id")]
    [InlineData("stripe_subscription_id")]
    [InlineData("stripe_event_id")]
    [InlineData("hts_user_id")]
    [InlineData("stripe_customer_id")]
    [InlineData("pterodactyl_server_uuid")]
    public void CommerceExternalIdentifiersAreComparedCaseSensitively(string column)
    {
        using var db = new CommerceDbContext(CommerceOptions());
        AssertColumnIsCaseSensitive(db.Database.GenerateCreateScript(), column);
    }

    [Theory]
    [InlineData("StripeCheckoutSessionId")]
    [InlineData("StripeSubscriptionId")]
    [InlineData("UserId")]
    public void HostingExternalIdentifiersAreComparedCaseSensitively(string column)
    {
        var options = new DbContextOptionsBuilder<HostingDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=test;User Id=test;Password=test")
            .Options;
        using var db = new HostingDbContext(options);
        AssertColumnIsCaseSensitive(db.Database.GenerateCreateScript(), column);
    }

    private static void AssertColumnIsCaseSensitive(string script, string column)
    {
        // The first mention of a column in the script is its definition; later ones are indexes.
        var definition = script
            .Split('\n')
            .First(line => line.Contains($"[{column}]", StringComparison.Ordinal));

        Assert.Contains("COLLATE Latin1_General_100_BIN2", definition, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsNeverStringifySecrets()
    {
        var options = new SqlServerOptions
        {
            ConnectionString = "Server=db.example,1433;Database=hosting;User Id=app;Password=never_print_this"
        };

        Assert.Equal(nameof(SqlServerOptions), options.ToString());
        Assert.DoesNotContain("never_print_this", options.DescribeTarget(), StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentedPlaceholdersNeverEnableExternalIntegrations()
    {
        Assert.False(new StripeOptions { SecretKey = "sk_test_API_AQUI" }.IsConfigured);
        Assert.False(new StripeOptions { WebhookSecret = "whsec_API_AQUI" }.IsWebhookConfigured);
        Assert.False(new PterodactylOptions
        {
            BaseUrl = "https://PANEL_AQUI",
            ApiKey = "ptla_API_AQUI"
        }.IsConfigured);
    }

    private static DbContextOptions<CommerceDbContext> CommerceOptions() =>
        new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=test;User Id=test;Password=test")
            .Options;
}
