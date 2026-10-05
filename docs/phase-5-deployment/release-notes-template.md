# Preparing release notes

> **Status:** Completion template; not a published release
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

Release notes should let operators and reviewers understand a change without reading the development conversation. The [CHANGELOG](../../CHANGELOG.md) retains history; this guide describes a new entry.

## Note contents

- Identification: tag, exact commit, and actual publication date.
- Behavior: problem solved and resulting user experience.
- Changes: frontend, domain, integration, and operations only when relevant.
- Data/configuration: migrations, new variables, and nonsecret values to verify.
- Validation: commands, relevant counts, environment, and evidence links.
- Limitations: demonstrations, external tests not performed, and outstanding work.
- Recovery: previous image and schema compatibility, without credentials.

## Application to current work

The visual and documentation redesign remains **Unreleased** until a release is identified. Its note should link to [FRONTEND-REDESIGN.md](../FRONTEND-REDESIGN.md), record local evidence, and explain that the existing commerce backend was preserved.

Do not assign a fictional tag, announce ES as available, or say production passed tests that ran on localhost. Replace “Unreleased” only after verifying artifact and deployment.
