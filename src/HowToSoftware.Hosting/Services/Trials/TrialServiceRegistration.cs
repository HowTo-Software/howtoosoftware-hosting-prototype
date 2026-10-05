using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services.Trials;

public static class TrialServiceRegistration
{
    public static IServiceCollection AddServerTrials(this IServiceCollection services, IConfiguration configuration, bool databaseConfigured)
    {
        services.AddOptions<TrialOptions>().Bind(configuration.GetSection(TrialOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<TrialOptions>, TrialOptionsValidator>();
        if (databaseConfigured)
        {
            services.AddScoped<ITrialStore, SqlServerTrialStore>();
            services.AddScoped<ITrialPanelGateway, TrialPanelGateway>();
            services.AddScoped<ITrialService, TrialService>();
            services.AddHostedService<TrialLifecycleWorker>();
        }
        else
        {
            services.AddSingleton<ITrialService, UnavailableTrialService>();
        }
        return services;
    }
}
