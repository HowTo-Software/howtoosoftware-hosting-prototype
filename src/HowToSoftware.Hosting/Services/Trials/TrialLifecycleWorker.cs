using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services.Trials;

/// <summary>SQL state is the queue; restarts and multiple application replicas share the same leases.</summary>
public sealed class TrialLifecycleWorker(
    IServiceScopeFactory scopes, IOptionsMonitor<TrialOptions> options, TimeProvider clock,
    ILogger<TrialLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ITrialService>().ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                logger.LogWarning("Trial lifecycle scan failed ({FailureClass}); the next scan will retry.", error.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.PollIntervalSeconds), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
