namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Minimal controllable clock, so the server-preview transitions can be tested without
/// sleeping and without pulling in an extra test package.
/// </summary>
internal sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Advance(TimeSpan amount) => _now = _now.Add(amount);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
