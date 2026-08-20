using HowToSoftware.Hosting.Components;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor: the marketing pages render statically on the server for SEO and hydrate only the
// islands that genuinely need interactivity (navigation, FAQ, control-panel preview).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOptions<SiteOptions>()
    .Bind(builder.Configuration.GetSection(SiteOptions.SectionName));

// Content and server telemetry are resolved through interfaces so the prototype's static and
// mock implementations can be replaced by CMS- and panel-backed services later on.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMarketingContentService, StaticMarketingContentService>();
builder.Services.AddSingleton<IPlanCatalogService, StaticPlanCatalogService>();
builder.Services.AddScoped<IServerPreviewService, MockServerPreviewService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    // Default HSTS value is 30 days. Change this for production scenarios - see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
