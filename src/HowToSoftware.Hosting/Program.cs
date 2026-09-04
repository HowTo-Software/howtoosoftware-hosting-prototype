using HowToSoftware.Hosting.Components;
using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Endpoints;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using HowToSoftware.Hosting.Services.Orders;
using HowToSoftware.Hosting.Services.Payments;
using HowToSoftware.Hosting.Services.Provisioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Blazor: the marketing pages render statically on the server for SEO and hydrate only the
// islands that genuinely need interactivity (navigation, FAQ, control-panel preview, sign-in).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Localisation. Resources live beside their marker types under Localization/, so
// IStringLocalizer<CommonText> reads Localization/CommonText.resx and its .pt-BR sibling.
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(SiteLocalization.Configure);

builder.Services.AddOptions<SiteOptions>()
    .Bind(builder.Configuration.GetSection(SiteOptions.SectionName));

// Plan prices are commercial values that change without the plans changing, so they arrive from
// configuration. A plan with no configured price renders as visibly unpriced rather than
// defaulting to a number nobody agreed to.
builder.Services.AddOptions<HostingPlanPricingOptions>()
    .Bind(builder.Configuration.GetSection(HostingPlanPricingOptions.SectionName));

// ── Pterodactyl ───────────────────────────────────────────────────────────
//
// The API key is NOT in appsettings.json and must never be. It is read from the environment
// (Pterodactyl__ApiKey) or from user-secrets in development. Options are validated on first
// use rather than at startup, so the site still serves its marketing pages on a host that has
// no panel credentials - only provisioning needs them.
builder.Services.AddOptions<PterodactylOptions>()
    .Bind(builder.Configuration.GetSection(PterodactylOptions.SectionName));
builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IValidateOptions<PterodactylOptions>,
    PterodactylOptionsValidator>();

builder.Services.AddOptions<ProvisioningLabOptions>()
    .Bind(builder.Configuration.GetSection(ProvisioningLabOptions.SectionName));

// ── Stripe ────────────────────────────────────────────────────────────────
//
// Same rule as the panel key: the secret key and the webhook secret are NOT in appsettings.json.
// They come from the environment (Stripe__SecretKey, Stripe__WebhookSecret) or user-secrets.
// Without them every page still renders; only "continue to payment" is switched off, and it
// says so.
builder.Services.AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IValidateOptions<StripeOptions>,
    StripeOptionsValidator>();

// ── Orders ────────────────────────────────────────────────────────────────
//
// SQLite by default, chosen by connection string. A factory rather than a scoped context,
// because the webhook and the fulfilment worker both need short units of work outside a
// component's scope.
builder.Services.AddDbContextFactory<HostingDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(HostingDbContext.ConnectionName)
            ?? "Data Source=hosting.db"));
builder.Services.AddSingleton<IOrderStore, EfOrderStore>();

builder.Services.AddHttpClient<IPterodactylClient, PterodactylClient>(PterodactylClient.HttpClientName,
    (provider, client) =>
    {
        var options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<PterodactylOptions>>()
            .CurrentValue;

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        // The Authorization header is attached per request inside the client, not here: it is
        // read from options each time so a rotated key takes effect without a restart, and it
        // stays in exactly one place in the codebase.
        client.DefaultRequestHeaders.Add("User-Agent", "HowToSoftware-Hosting/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Redirects are never legitimate on this API, and following one is actively dangerous:
        // the panel answers an unauthenticated request with a 302 to its HTML login page, so a
        // redirect-following client sees HTTP 200 and a page of HTML and calls it success.
        AllowAutoRedirect = false
    });

// Content and server telemetry are resolved through interfaces so the prototype's static and
// mock implementations can be replaced by CMS- and panel-backed services later on.
builder.Services.AddSingleton(TimeProvider.System);
// Marketing content is scoped rather than singleton now that its labels follow the request's
// culture; the data behind them is still compiled-in and allocation-cheap.
builder.Services.AddScoped<IMarketingContentService, StaticMarketingContentService>();
builder.Services.AddScoped<IPlanCatalogService, StaticPlanCatalogService>();
builder.Services.AddScoped<IGameCatalogService, StaticGameCatalogService>();
builder.Services.AddScoped<ICustomBuildService, RateCardBuildService>();
// The purchase flow. The browser sends a game, a plan and a period; everything with a
// currency sign on it is computed here, sent to Stripe from here, and checked here when
// Stripe's webhook reports it paid. Fulfilment runs off a queue so the webhook answers fast.
builder.Services.AddScoped<IOrderPricingService, OrderPricingService>();
builder.Services.AddSingleton<IStripeGateway, StripeGateway>();
builder.Services.AddSingleton<OrderFulfillmentQueue>();
builder.Services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
builder.Services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();
builder.Services.AddScoped<IOrderFulfillmentService, OrderFulfillmentService>();
builder.Services.AddHostedService<OrderFulfillmentWorker>();
builder.Services.AddSingleton<IInfrastructureContentService, StaticInfrastructureContentService>();
builder.Services.AddSingleton<IHardwarePhotoLibrary, HardwarePhotoLibrary>();
builder.Services.AddSingleton<IZomboidPhotoLibrary, ZomboidPhotoLibrary>();
builder.Services.AddSingleton<IGameTemplateCatalog, GameTemplateCatalog>();
builder.Services.AddScoped<IServerPreviewService, MockServerPreviewService>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddSingleton<IProvisioningLabGuard, ProvisioningLabGuard>();
// No identity provider exists yet. This gateway authenticates nobody and stores nothing; see
// PrototypeAuthenticationGateway for why that is the only honest stand-in.
builder.Services.AddScoped<IAuthenticationGateway, PrototypeAuthenticationGateway>();

var app = builder.Build();

// The order tables. Migrations rather than EnsureCreated, so the schema can change later
// without dropping the orders already in it.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HostingDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    // Default HSTS value is 30 days. Change this for production scenarios - see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Must run before the components: it sets the culture for the request that renders the page
// and, just as importantly, for the request that opens an interactive circuit, so both halves
// of a page speak the same language.
app.UseRequestLocalization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapCultureSelection();
app.MapPaymentEndpoints();

// The Project Zomboid page moved under /game-hosting. Links already out in the world keep
// working, and search engines are told the move is permanent.
app.MapGet(SiteRoutes.LegacyProjectZomboid, () => Results.Redirect(SiteRoutes.ProjectZomboid, permanent: true));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Makes the entry point visible to the integration tests, which host the whole application
/// in-process to exercise the webhook endpoint end to end.
/// </summary>
public partial class Program
{
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
