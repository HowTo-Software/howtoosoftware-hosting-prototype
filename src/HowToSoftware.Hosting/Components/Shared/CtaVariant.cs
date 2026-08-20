namespace HowToSoftware.Hosting.Components.Shared;

/// <summary>
/// Visual weight of a call-to-action rendered by <c>CtaButton</c>.
/// </summary>
public enum CtaVariant
{
    /// <summary>Filled gradient button - one per view, reserved for the main action.</summary>
    Primary,

    /// <summary>Outlined button on a light surface.</summary>
    Secondary,

    /// <summary>Text-only action with an underline on hover.</summary>
    Ghost,

    /// <summary>Filled white button, for use on top of a saturated background.</summary>
    Inverse,

    /// <summary>Outlined white button, for use on top of a saturated background.</summary>
    OutlineInverse
}

/// <summary>
/// Size of a call-to-action rendered by <c>CtaButton</c>.
/// </summary>
public enum CtaSize
{
    /// <summary>Compact, used in the header and inline with body copy.</summary>
    Small,

    /// <summary>Default size for section actions.</summary>
    Medium,

    /// <summary>Prominent size for the hero and closing call-to-action.</summary>
    Large
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
