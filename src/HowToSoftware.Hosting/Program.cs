using HowToSoftware.Hosting.Components;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

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

// Content and server telemetry are resolved through interfaces so the prototype's static and
// mock implementations can be replaced by CMS- and panel-backed services later on.
builder.Services.AddSingleton(TimeProvider.System);
// Marketing content is scoped rather than singleton now that its labels follow the request's
// culture; the data behind them is still compiled-in and allocation-cheap.
builder.Services.AddScoped<IMarketingContentService, StaticMarketingContentService>();
builder.Services.AddSingleton<IPlanCatalogService, StaticPlanCatalogService>();
builder.Services.AddSingleton<IInfrastructureContentService, StaticInfrastructureContentService>();
builder.Services.AddScoped<IServerPreviewService, MockServerPreviewService>();
// No identity provider exists yet. This gateway authenticates nobody and stores nothing; see
// PrototypeAuthenticationGateway for why that is the only honest stand-in.
builder.Services.AddScoped<IAuthenticationGateway, PrototypeAuthenticationGateway>();

var app = builder.Build();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
