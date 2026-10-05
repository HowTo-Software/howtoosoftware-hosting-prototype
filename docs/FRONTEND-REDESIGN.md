# HTS Hosting — frontend redesign

> **Status:** Local visual review completed; publication unconfirmed
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

[Documentation index](README.md) · [Architecture](phase-2-design/architecture.md) · [DOM and animation ADR](phase-2-design/adr/0004-preserve-blazor-dom.md) · [Test plan](phase-4-testing/test-plan.md)

Redesign started on October 4, 2026 for the existing hosting storefront. The project retains Blazor and .NET 10 and uses the application's catalogs, prices, routes, and services. The current composition is the third visual iteration. Earlier sections and validation figures describe the second iteration and are retained as historical records.

## Baseline and preservation of local changes

- Repository: [HowTo-Software/howtoosoftware-hosting-prototype](https://github.com/HowTo-Software/howtoosoftware-hosting-prototype).
- Local `main` was updated to `a7ac383`, `Deploy to /opt/howtoosoftware-hosting-prototype instead of ~/hts-hosting`, before visual implementation.
- Before publication on October 5, the redesign branch was fast-forwarded to `277e7b1`, incorporating the current main dependency updates: Stripe.net 53.0.0, xunit.runner.visualstudio 4.0.0, Microsoft.NET.Test.Sdk 18.10.1 and Microsoft.Extensions.TimeProvider.Testing 10.10.0.
- Redesign work uses `feat/hosting-frontend-redesign`.
- Previous local work was preserved in `preserve-local-before-hosting-redesign-2026-10-04`. The snapshot includes promotion changes and `TrialServerBanner.razor`/`TrialServerBanner.razor.css`, including untracked files. Changes were restored into the working tree for integration; the stash remains as a backup.
- Updating and redesigning do not deploy to the production machine. Publishing remains separate and is documented in [DEPLOYMENT.md](DEPLOYMENT.md).

## Third iteration: centered branding and compact infrastructure

The hero centers a large HTS Vector Wordmark above the heading and actions. The brand stays in the opening section and scrolls away with it. It keeps its supplied pointer animation, bounded drawing and static reduced-motion fallback. GPU resources are disposed on navigation. Following visual review, the owner chose this opening composition instead of a persistent background; no fixed ambient layer or scroll-fade script remains.

The large Project Zomboid configurator and the control-panel demonstration were removed from the homepage. Existing plan selection remains on the product page. The primary action leads to the 24-hour trial; the trial banner follows the hero. A compact promotion rail renders only configured public codes that pass the existing database promotion validator. No available promotion means no empty or fictional announcement.

Infrastructure now uses four sections: editorial hero and one factual specification sheet, three real photographs, native operational disclosures and a short closing action. Synthetic pixel grids, storage chains, illustrated topology and static health badges no longer render on the route. Native details support keyboard use, no JavaScript and reduced motion.

Trial duration was corrected by the owner to **24 hours**, with CPU **0**, RAM **6 GiB** and disk **25 GiB** across Project Zomboid and Minecraft. Email confirmation precedes provisioning; expiration suspends the server and retains its save for 72 hours. Paid conversion resizes and resumes the existing server. Minecraft offers operator-approved edition, variant and version selectors. See [trial implementation and configuration](TRIAL-SERVERS.md).

Jetpack Compose Preview was requested for mobile review. It was unavailable in this session and targets Android Compose, so mobile validation uses the actual Blazor pages through Chromium device emulation. No Compose implementation or tool execution is claimed.

## Third iteration — validation and previews

Final local validation on October 5, 2026: **627 automated tests passed**, with zero failures or skipped tests. Release compilation completed without warnings or errors. Tests cover trial ownership and lifecycle, panel API requests, SMTP messages, public promotions, preserved upgrade intent and payment conversion on the existing server.

Browser review passed **70 main checks** in [qa-v3.json](frontend/qa-v3.json) and **7 supplemental trial checks** in [qa-v3-trial.json](frontend/qa-v3-trial.json), with no runtime exceptions or missing assets. Home and infrastructure were checked at 320, 390, 768, 1024 and 1440 px in EN/PT-BR and both themes. Trial checks include 390/1440 px, Java/Bedrock entry, unavailable configurations and explicit confirmation POST. Additional checks cover wordmark centering, removal of the homepage configurator/demo, offscreen pausing, disposal after navigation, keyboard disclosures, reduced motion and no JavaScript.

The preview disables dotenv loading and external integrations. Browser review covers the public interface and unconfigured trial states; approved versions and panel provisioning are tested with doubles. Production SQL, SMTP delivery, Stripe payment and Pterodactyl mutations remain unverified. The commerce migration was generated, not applied. Device emulation and SwiftShader do not constitute a mobile hardware benchmark.

| Current preview | File |
| --- | --- |
| Large centered HTS, desktop | [hero-v3-dark.png](frontend/hero-v3-dark.png) |
| Large centered HTS, mobile | [hero-v3-mobile.png](frontend/hero-v3-mobile.png) |
| Mobile reduced-motion fallback | [hero-v3-reduced.png](frontend/hero-v3-reduced.png) |
| Full homepage | [home-v3-dark.png](frontend/home-v3-dark.png) |
| Compact infrastructure, desktop | [infrastructure-v3-dark.png](frontend/infrastructure-v3-dark.png) |
| Compact infrastructure, mobile | [infrastructure-v3-mobile.png](frontend/infrastructure-v3-mobile.png) |
| Trial entry, mobile | [trial-v3-mobile.png](frontend/trial-v3-mobile.png) |
| Minecraft trial entry, mobile | [trial-minecraft-v3-mobile.png](frontend/trial-minecraft-v3-mobile.png) |

## Second iteration — historical visual direction

The second iteration presents a technical hosting storefront: choose a game, compare resources, review configuration, and proceed to payment. Identity uses graphite in the dark theme, neutral paper in the light theme, and cobalt for actions/highlights: `#91b2ff` in dark and `#2c55c3` in light. Subtle surfaces, 6–10 px corners, separators, and readable typography replace the first composition's large pastel blocks and rounded panels.

The hero displays a real HTS hardware photograph resolved by the existing library. The fictional rack composition was removed. CPU, storage, plans, and prices come from application services; visuals organize this data rather than inventing servers or telemetry.

| Public reference | Observed aspect applied to HTS |
| --- | --- |
| [Nodecraft: pricing](https://nodecraft.com/pricing) | Game-based catalog and game → plan → start journey. HTS uses its own games, availability, and routes. |
| [BisectHosting: games](https://www.bisecthosting.com/game-server-hosting), [panel](https://www.bisecthosting.com/control-panel) | Game-based offers, direct plan access, and panel-feature presentation. HTS highlights only catalog products/features. |
| [Hetzner: cloud](https://www.hetzner.com/cloud/) | Comparable specifications/configurations with direct memory, CPU, storage, and pricing presentation. |

These pages are structural and presentation references. No provider code, illustrations, images, logos, or commercial copy were copied. Game, hardware, and panel images are project assets.

## Skiper UI: collected sources, adaptations, and credits

The three researched components were collected from official public registries without authentication, returning HTTP 200. Original JSON and `files[].content` fields are preserved in [reference-sources](reference-sources). These references do not execute as React or ship to browsers.

| Component | Documentation and original registry | Project adaptation |
| --- | --- | --- |
| CssLink / Skiper40 | [Documentation](https://skiper-ui.com/v1/skiper40) · [JSON](https://skiper-ui.com/r/skiper40.json) · [Collected TSX](reference-sources/skiper40.tsx) | `.hts-text-link` in `wwwroot/css/storefront.css`: directional underline and short arrow movement, with an equivalent keyboard-focus state. |
| Text roll navigation / Skiper58 | [Documentation](https://skiper-ui.com/v1/skiper58) · [JSON](https://skiper-ui.com/r/skiper58.json) · [Collected TSX](reference-sources/skiper58.tsx) | `RollingNavText.razor` and isolated CSS: two character layers, vertical movement, and center-out delay. Accessible text is unique; decorative layers use `aria-hidden`. |
| ExpandOnHover / Skiper52 | [Documentation](https://skiper-ui.com/v1/skiper52) · [JSON](https://skiper-ui.com/r/skiper52.json) · [Collected TSX](reference-sources/skiper52.tsx) | First-gallery reference. The second iteration replaces width expansion with border emphasis and light image cropping through `transform`, keeping dimensions, text, and actions stable on hover/focus. |

Products, prices, and purchase actions remain visible without hover. Motion confirms focus without moving neighboring products.

### License and attribution

[Skiper UI's official usage documentation](https://skiper-ui.com/docs/quick-start) and these component pages permit personal/commercial use and modification. Free usage requires **Skiper UI** attribution; the footer includes a visible library link. These are the supplier's published conditions; files are not presented as MIT-licensed.

CssLink credits [Cursor](https://cursor.com/) as its original inspiration. ExpandOnHover's demo uses AarzooAly illustrations; those images were not collected/reused. Original authorship and usage comments remain in the references.

### Portability

Original examples use React and Tailwind/`cn` utilities. CssLink imports `next/link`; TextRoll and ExpandOnHover use Framer Motion, and the ExpandOnHover demonstration includes Swiper CSS. Adaptations replace these dependencies with project Razor, CSS, and JavaScript. React, Next.js, Tailwind, Framer Motion, and Swiper were not added to the Blazor runtime. Skiper37 was evaluated and not used.

## OriginKit: initial access and owner-supplied code

In the first iteration, [Mask Text Reveal](https://www.originkit.dev/components/mask-text-reveal) was a public behavior reference. The [component guide](https://www.originkit.dev/docs/components) states that copying/installing code requires login; the session had no authorized account/key. No authenticated code was collected or access mechanism bypassed. `MaskReveal.razor` was independently implemented with CSS and the existing observer.

In the second iteration, the project owner supplied original **Vector Wordmark / OriginKit** code as an attachment and authorized its HTS adaptation. This implementation comes from the user-supplied file, not subsequent collection from a restricted endpoint. GLSL, the red/green-channel glyph atlas, and grid/displacement equations were adapted to HTS in `VectorWordmark.razor`, its isolated CSS, and `wwwroot/js/vector-wordmark.js`.

The adaptation uses WebGL 1 and its own DOM lifecycle without React. Drawing is limited to **30 fps**, with maximum DPR **1.5 on desktop** and **1 on mobile**. Introduction and replay each run once for **4.4 seconds**, below five seconds. Pointer reaction pauses **1.2 seconds after the last event**; there is no continuous idle loop. Drawing is suspended offscreen, in hidden tabs, or with reduced motion. Static text remains the fallback for reduced motion or unavailable WebGL. The wordmark is decorative, not a keyboard control: its container has no `tabindex` or keyboard replay action.

The supplied code is used within this application. The original attachment is not republished as a component catalog, UI kit, or snippet collection. [OriginKit licensing](https://www.originkit.dev/docs/licensing) permits adapting legitimately obtained components for real applications and restricts kit redistribution. Documentation distinguishes the owner-supplied code from the earlier independent implementation.

Heading masks animate the `.ht-mask-content` child. `IntersectionObserver` tracks the outer box and applies `.is-revealed`, avoiding observation of an element whose own mask prevents detection. Text is server-rendered and visible without animation activation.

Other public references: [Reactive Lines](https://www.originkit.dev/components/reactive-lines) for lines/offscreen behavior, and [Features 01](https://www.originkit.dev/sections/features-01) for feature organization. Their sources were not collected, and the original Reactive Lines canvas was not incorporated.

## Second-iteration architecture and behavior

- **Hero and native configuration:** `HeroExperience.razor` combines the HTS wordmark, masked headings, real photography, and native radio plan selection. All configurations/quotes are server-rendered; CSS `:has()` displays selection. The hero is no longer an `InteractiveServer` island: selection requires no Blazor circuit or JavaScript. Options are deduplicated from the catalog's first, recommended, and last plans. Unpublished prices retain an explicit state; purchasable configurations link to existing review.
- **Homepage games and plans:** `GameGateway.razor` uses stable product rows and distinguishes available/planned games. `PlansTeaser.razor` presents real configurations side by side without turning recommendations into a large pastel panel. Quotes, resources, and purchase links remain catalog-derived.
- **Purchase and comparison:** `PlanPicker.razor` preserves native billing-period radios and card/comparison switching. The same list reorganizes for comparison without duplicating prices. Links carry game, plan, and period; the server calculates orders before payment.
- **Capabilities and evidence:** `HostingBenefits.razor` uses three technical blocks and a labeled world/backups/control flow that works without JavaScript. `InfrastructureProof.razor` shows HTS rack photography and the infrastructure service's `SpecSheet`. These explain the product without simulated telemetry.
- **Navigation and states:** entrance masks, lines, corners, underlines, and image crops share short timings and mouse/keyboard states. Entrances can use subtle staggering. Monetary values do not count progressively, and information never depends on animation completion. Theme/language preferences remain available; Skiper attribution remains in the footer.
- **Demonstration panel:** surfaces use 8 px corners; busy state uses amber to distinguish it from cobalt highlights. The demonstration explains controls/states and must not be interpreted as a visitor's server telemetry.
- **Custom configuration:** retains existing sliders, limits, normalization, estimates, and `mailto` drafts. Controls/quotes remain readable, without effects splitting/replacing this interactive island's DOM. Removing `InteractiveServer` applies to the hero, not the custom configurator.
- **Compatibility:** trial banner, promotions, checkout, authentication, payments, and provisioning retain their services/routes. Changes concern storefront presentation and interaction.

### Scheduling and lifecycle

`wwwroot/js/site.js` centralizes visual work in a shared RAF, batches reads before layout writes, and filters mutations before rescheduling scans. `enhancedload` navigation clears previous state and initializes inserted elements, avoiding accumulated callbacks/listeners. The wordmark retains its 30 fps limit and disposes removed instances; visibility/reduced motion control drawing.

Animations preserve server-rendered text nodes. The earlier JavaScript span splitting was removed: Blazor maintains a logical child tree, and replacing nodes could leave previous-route headings/actions after navigation. Headings use 560 ms CSS masks; counters/wordmark coordinates update only existing node values. The navigation bar is also server-rendered. This choice was checked against [official Blazor .NET 10 code](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web.JS/src/Rendering/LogicalElements.ts) and [SSR navigation/script documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/static-server-rendering?view=aspnetcore-10.0).

The no-JavaScript fallback uses independent `hts-script`, applied before first paint and restored after navigation. It is separate from motion-only `hts-js`, preventing controls from disappearing when users prefer reduced motion. The fallback stylesheet always loads, with selectors active only when scripts are disabled.

Visibility triggers entrances, with static fallback when JavaScript/motion is unavailable. Selected states, focus, links, and content remain readable. Storefront copy uses EN/PT-BR resources.

### Commercial data and authenticity

Prices, discounts, periods, memory, CPU, disk, and backup limits are projections of existing catalogs/billing policy. Review and payment remain server-calculated. CPU is shown as percentage allocation without turning it into a dedicated-core promise.

Photos come from HTS assets, and specifications from the infrastructure service. The frontend adds no unsupported SLA, latency, uptime, customer counts, or online-server numbers. Game availability follows catalog state; planned products remain identified as planned.

## Second iteration — historical validation and previews

Integrated review completed on October 5, 2026 in an isolated local preview. Release build: **zero errors and zero warnings**. Automated suite: **530 passing tests**, zero failures and zero skipped tests.

Chromium review includes **66 main checks** in [qa-v2.json](frontend/qa-v2.json) and **8 additional checks** in [qa-v2-navigation.json](frontend/qa-v2-navigation.json). Additional checks cover home → plans → home, restored content, header controls after navigation, static-text proportions, and reduced-motion preference. Final runs found no script errors or missing assets.

The six routes reviewed at **320, 375, 768, 1024, and 1440 px** were homepage, catalog, Project Zomboid product, infrastructure, login, and purchase review. Coverage includes:

- Dark/light themes and PT-BR/EN, including preference persistence.
- Real WebGL through browser SwiftShader: drawing, interaction, idle/offscreen stopping, context loss/restoration, and disposal/remounting after navigation.
- Static wordmark with reduced motion, unavailable WebGL, or disabled scripts; proportional fallback typography.
- Hero memory selection, eight-plan comparison, and monthly/quarterly/annual native billing radios, including without JavaScript.
- Mobile menu, Escape focus return, FAQ, and interactive panel demonstration.
- Keyboard custom configuration and preserved contact-draft text.
- Review POST, antiforgery, plan, and period; no purchase was submitted.

The scope audit found no backend changes and matching EN/PT-BR keys: **80** for `CommonText`, **178** for `CheckoutText`, and **87** for `StorefrontText` per language, without formatting-parameter differences.

The preview uses `HTS_SKIP_DOTENV=true`, temporary settings, and no operational payment/database/panel credentials. Visual tests do not execute payments, provisioning, or production SQL Server. WebGL validation covers functionality/lifecycle without claiming a hardware benchmark or Lighthouse score.

### Second-iteration screenshots

| Second-iteration preview (historical) | File |
| --- | --- |
| Entrance, dark theme | [hero-v2-dark.png](frontend/hero-v2-dark.png) |
| Entrance, light theme | [hero-v2-light.png](frontend/hero-v2-light.png) |
| Wordmark during interaction | [wordmark-v2-active.png](frontend/wordmark-v2-active.png) |
| Mobile entrance | [hero-v2-mobile.png](frontend/hero-v2-mobile.png) |
| Mobile with reduced motion | [hero-v2-reduced.png](frontend/hero-v2-reduced.png) |
| Full homepage, dark | [home-v2-dark.png](frontend/home-v2-dark.png) |
| Full homepage, light | [home-v2-light.png](frontend/home-v2-light.png) |
| Full mobile homepage | [home-v2-mobile.png](frontend/home-v2-mobile.png) |
| Catalog | [catalog-v2.png](frontend/catalog-v2.png) |
| Plan comparison | [plans-v2-comparison.png](frontend/plans-v2-comparison.png) |
| Purchase review, desktop | [review-v2-desktop.png](frontend/review-v2-desktop.png) |
| Purchase review, mobile | [review-v2-mobile.png](frontend/review-v2-mobile.png) |
| Infrastructure entrance | [infrastructure-v2.png](frontend/infrastructure-v2.png) |

First-iteration records remain historical: [qa-initial.json](frontend/qa-initial.json), with 51 checks, and [qa-final.json](frontend/qa-final.json), with 22. Screenshots without `v2` show the earlier composition; this table records the second iteration.
