# ADR-0004 — Animations that preserve the DOM

> **Status:** Retrospective record of an implemented decision; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Context

Blazor maintains a logical tree of rendered elements. JavaScript text splitting into spans replaced managed children and could retain another route's titles/actions after enhanced navigation.

## Observed decision

Render text and decorative layers in Razor, animate CSS masks, and update only existing node values when needed. Render the progress bar on the server. Separate script availability (`hts-script`) from motion (`hts-js`).

The wordmark uses its own canvas, limited in fps/DPR and paused offscreen, with reduced-motion handling, context-loss support, and static fallback. The script disposes its resources on page exit.

## Consequences

Navigation preserves semantics, controls, and text. New effects must be tested through route sequences, without JS, and with reduced motion; a single screenshot cannot detect DOM regressions.

Reintroducing structural changes to Blazor children without coordination is unacceptable. The review recorded Chromium navigation and restoration scenarios.

Sources: [guide and technical research](../../FRONTEND-REDESIGN.md), [site.js](../../../src/HowToSoftware.Hosting/wwwroot/js/site.js), [wordmark](../../../src/HowToSoftware.Hosting/wwwroot/js/vector-wordmark.js), [evidence](../../frontend/qa-v2-navigation.json).
