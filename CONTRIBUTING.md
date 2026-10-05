# Contributing to HTS Hosting

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

Start with [onboarding](docs/phase-3-development/onboarding.md) and the [code map](docs/PROJECT-MAP.md). Changes must preserve the real catalog, backend financial authority, and migration compatibility with the previous image.

## Before submitting a change

1. Work in your own branch from the correct base, preserving local changes.
2. Follow the [development standards](docs/phase-3-development/coding-standards.md).
3. Run a Release build and tests appropriate to the impact; for frontend changes, check keyboard access, mobile, themes, languages, and navigation between pages.
4. Update the relevant guide and [CHANGELOG.md](CHANGELOG.md). Write documentation in English.
5. Review the diff for credentials, personal logs, temporary files, and screenshots containing sensitive data.

A PR should describe the problem, resulting behavior, validation performed, and any required configuration or migration. Appearance changes should include useful screenshots; commercial changes should explain their impact on orders and webhooks.

The [Git workflow](docs/phase-3-development/git-workflow.md) provides branch and publishing commands. Merging into `main` participates in the production pipeline; Environment approval requirements depend on actual GitHub settings.

Report security issues privately as described in [SECURITY.md](SECURITY.md). Contributions retain the proprietary license and the credits documented in [FRONTEND-REDESIGN.md](docs/FRONTEND-REDESIGN.md).
