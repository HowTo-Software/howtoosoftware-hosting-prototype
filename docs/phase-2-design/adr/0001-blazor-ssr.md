# ADR-0001 — Blazor SSR and localized interactivity

> **Status:** Retrospective record of an implemented decision; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Context

The project uses ASP.NET Core and needs to deliver indexable content, a catalog, and commercial journeys without shipping an additional frontend framework. Some areas need interactive state.

## Observed decision

Use SSR for public pages and InteractiveServer where components need interaction. Port OriginKit/Skiper examples to Razor, CSS, and JavaScript without adding React, Tailwind, or Framer Motion to the runtime. The centered hero needs no circuit; the plan picker uses native controls. Trial forms use static SSR and protected native POST endpoints.

## Consequences

Initial content and commercial actions remain available with less script dependency. InteractiveServer islands need a connection and suitable proxy. React examples require adaptation of lifecycle, styling, and accessibility.

Converting everything into a SPA or making the entire page interactive would add dependencies and cost; these alternatives were not adopted in this redesign.

Sources: [Program.cs](../../../src/HowToSoftware.Hosting/Program.cs), [frontend](../../FRONTEND-REDESIGN.md).
