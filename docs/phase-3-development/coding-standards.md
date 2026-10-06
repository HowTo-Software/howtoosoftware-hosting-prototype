# Development standards

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## C# and domain

The project centralizes framework/options in `Directory.Build.props`, versions in `Directory.Packages.props`, and namespaces by responsibility. Keep nullable enabled and Release builds warning-free.

Commercial rules belong in domain/services: do not duplicate discount percentages in components, calculate prices in JavaScript, or infer resources from marketing text. Use decimal for calculations and explicit conversion to cents for integrations.

Stores use business operations and short units of work; do not keep a DbContext alive indefinitely in a circuit or worker. Pass CancellationToken through external calls and do not classify cancellation as payment failure.

## Razor, CSS, and JavaScript

- Use small components with semantic HTML, labels, visible focus, and error/busy states.
- Keep visible copy in EN/PT-BR resources; maintain matching keys and formatting placeholders.
- Use isolated CSS for component styling and theme tokens for color, spacing, and surfaces.
- Do not require hover to see prices or purchase; test keyboard and touch.
- Preserve Blazor-rendered nodes. Effects require per-page initialization/teardown, no-JS fallback, and reduced-motion support.
- Retain WebGL fps/DPR limits and pausing; do not introduce an idle continuous loop.
- Avoid extra dependencies when existing Razor/CSS/JS can implement the interaction.

## Integrations and security

Keys stay on the server. Do not expose webhook bodies, SQL strings, or raw provider errors in public responses or logs. Use parameterized APIs and resource-level authorization before adding private interfaces.

Do not disable antiforgery to simplify a form. Webhooks are the signature-authenticated exception. Do not accept an external Host to construct Stripe returns. Do not enable the production lab.

## Documentation and verification

Changes to catalogs, routes, settings, or billing lifecycle require the corresponding guide update. Keep documentation in English. A decision changing an architectural boundary needs an [ADR](../phase-2-design/adr/README.md).

Use the existing suite and tests covering the new risk; editorial corrections do not require C# tests mirroring prose. Check links and filenames when changing documentation. See [testing](../phase-4-testing/test-plan.md).
