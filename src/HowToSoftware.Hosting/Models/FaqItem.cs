namespace HowToSoftware.Hosting.Models;

/// <summary>
/// A question/answer pair rendered by the accessible FAQ accordion.
/// </summary>
/// <param name="Id">Stable slug used to build the <c>aria-controls</c> relationship.</param>
/// <param name="Question">The question text.</param>
/// <param name="Answer">Placeholder answer copy, safe to reword without touching markup.</param>
public sealed record FaqItem(string Id, string Question, string Answer);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
