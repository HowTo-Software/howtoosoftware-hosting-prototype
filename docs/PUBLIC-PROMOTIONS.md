# Public promotion announcements

Status: Implemented; public campaigns are disabled by default.

Owner: HTS maintainers.

Last updated: 2026-10-05.

The storefront can announce up to three existing promotion codes in a compact rail. This is
an explicit publication list, not a list of every coupon in the commerce database. Private,
customer-specific or unpublished codes remain private unless an operator adds them to this
configuration. No coupon, redemption or discount value is created by the announcement system.

## Configuration

`PublicPromotions:Campaigns` defaults to an empty array. Each entry identifies an existing
promotion code, game slug, plan slug and billing period. The only accepted periods are
`monthly`, `quarterly` and `annual`. Current catalogue examples are `project-zomboid` and
`zomboid-4gb`; choose the plan to which the actual campaign applies.

Set these environment keys for the first entry, supplying the actual code already managed in
the commerce database:

| Key | Value |
| --- | --- |
| `PublicPromotions__Campaigns__0__Code` | An existing code explicitly approved for public display |
| `PublicPromotions__Campaigns__0__GameSlug` | The eligible game's existing slug |
| `PublicPromotions__Campaigns__0__PlanSlug` | The eligible plan's existing slug |
| `PublicPromotions__Campaigns__0__Period` | `monthly`, `quarterly` or `annual` |

Use indices `1` and `2` for additional campaigns. Do not configure empty entries. Codes must
contain 1–64 letters, digits, underscores or hyphens; slugs must contain 1–64 lowercase letters,
digits or hyphens. Invalid configuration fails startup rather than silently publishing an
ambiguous campaign. Configuration changes require an application restart.

## Eligibility and pricing

`PublicPromotionService` resolves each candidate through `IOrderPricingService.Price`, then
calls the same `IPromotionService.ValidateAsync` used by Checkout. Active state, start and
expiry dates, game and plan restrictions, redemption limits, minimum order amount, currency
and stacking rules remain controlled by the existing promotion service. The announcement
displays the validated saving for the selected plan and period; it never invents a percentage.

The current promotion validator supports USD. Campaigns for another catalogue currency are
omitted. Without the commerce database, `NullPromotionService` returns no eligible promotions,
so configuring a code cannot bypass validation. Duplicate entries for the same code, target
and period are shown once. Database availability failures or validation exceeding the two-second
total budget omit optional announcements while allowing the storefront to render.

Following an offer opens the existing review URL with `promo` populated. Checkout checks the
code again at submission; an announcement is not a reservation or a guarantee of remaining
redemptions. This operation does not redeem a code or create an order.

## Code and validation

- [Publication configuration](../src/HowToSoftware.Hosting/Models/PublicPromotionOptions.cs).
- [Server-side eligibility](../src/HowToSoftware.Hosting/Services/Payments/PublicPromotionService.cs).
- [Announcement component](../src/HowToSoftware.Hosting/Components/Shared/PromotionAnnouncements.razor).
- [Existing coupon rules](../src/HowToSoftware.Hosting/Services/Payments/PromotionService.cs).
- [Publication boundary tests](../tests/HowToSoftware.Hosting.Tests/PublicPromotionTests.cs).

The rail renders as static SSR, uses native review links and supports EN/PT-BR. With no valid
public campaign it renders no placeholder, ticker, popup or empty banner.
