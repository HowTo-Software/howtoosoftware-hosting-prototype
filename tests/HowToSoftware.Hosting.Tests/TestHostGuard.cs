using System.Runtime.CompilerServices;
using HowToSoftware.Hosting.Infrastructure.Configuration;

namespace HowToSoftware.Hosting.Tests;

internal static class TestHostGuard
{
    /// <summary>
    /// The in-process tests boot the real <c>Program</c>, which loads the repository's <c>.env</c>.
    /// On a developer machine that file names a real database, so the suite silently wrote orders
    /// and Stripe events into it. This runs before any test and closes that path for the whole
    /// assembly rather than relying on each fixture to remember.
    /// </summary>
    [ModuleInitializer]
    internal static void SuppressDeveloperEnvironmentFile() =>
        Environment.SetEnvironmentVariable(EnvironmentFile.SuppressionVariableName, "true");
}
