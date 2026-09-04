using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using HowToSoftware.Hosting.Infrastructure.Supabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace HowToSoftware.Hosting.Tests;

public sealed class SupabaseFoundationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("postgresql://USER:PASSWORD@HOST:5432/postgres")]
    [InlineData("postgresql://postgres:REPLACE_ME@db.PROJECT_ID.supabase.co:5432/postgres")]
    public void PlaceholdersNeverEnableTheRemoteDatabase(string value)
    {
        var options = new SupabaseOptions { DbConnectionString = value };
        Assert.False(options.IsDatabaseConfigured);
    }

    [Fact]
    public void SupabaseUriIsConvertedToASecureNpgsqlConnectionString()
    {
        var options = new SupabaseOptions
        {
            DbConnectionString = "postgresql://postgres.project:p%40ss@aws-0-us-east-1.pooler.supabase.com:5432/postgres"
        };

        Assert.True(options.IsDatabaseConfigured);
        var parsed = new NpgsqlConnectionStringBuilder(options.GetNpgsqlConnectionString());
        Assert.Equal("postgres.project", parsed.Username);
        Assert.Equal("p@ss", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public async Task PlaceholderConfigurationReportsNotConfiguredWithoutConnecting()
    {
        var check = new SupabaseHealthCheck(new SupabaseOptions
        {
            Url = "https://PROJECT_ID.supabase.co",
            SecretKey = "sb_secret_API_AQUI",
            DbConnectionString = "postgresql://USER:PASSWORD@HOST:5432/postgres"
        });

        var result = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Supabase not configured", result.Description);
    }

    [Fact]
    public void CommerceModelUsesPostgresMoneyAndSnakeCaseTables()
    {
        var dbOptions = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;
        using var db = new CommerceDbContext(dbOptions);

        var order = db.Model.FindEntityType("HowToSoftware.Hosting.Models.Commerce.CommerceOrder")!;
        Assert.Equal("orders", order.GetTableName());
        Assert.Equal("bigint", order.FindProperty("FinalAmountCents")!.GetColumnType());
        Assert.Equal("final_amount_cents", order.FindProperty("FinalAmountCents")!.GetColumnName());
    }

    [Fact]
    public void DiagnosticsNeverStringifySecrets()
    {
        var options = new SupabaseOptions
        {
            SecretKey = "sb_secret_never_print_this",
            DbConnectionString = "Host=db.example;Database=postgres;Username=user;Password=never_print_this"
        };

        Assert.Equal(nameof(SupabaseOptions), options.ToString());
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
}
