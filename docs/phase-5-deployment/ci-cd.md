# CI/CD pipeline

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

Source of truth: [.github/workflows/ci-cd.yml](../../.github/workflows/ci-cd.yml).

| Event | Tests | Image | Production |
| --- | --- | --- | --- |
| Pull request to main | Restore, Release build, and tests | Build without login/push | Not run |
| Push to main | Same cycle | GHCR push sha-commit and main | Production runner job |
| Manual on main, empty tag | Same cycle | New image | Deploy built image |
| Manual on main, image_tag populated | test/image jobs skipped | Existing tag | Deploy/rollback existing artifact |

## Jobs and permissions

`test` and `image` run on ubuntu-latest. Release builds treat warnings as errors. TRX is retained for 14 days. The image job uses build cache and packages:write; PRs do not push.

`deploy` accepts only push/manual events on main, uses `self-hosted, linux, hts-production`, Environment `production`, and packages:read. Checkout is limited to `deploy`, without persisted credentials. GHCR login uses the job token and isolated Docker configuration, preserving other host applications' logins.

CI concurrency cancels earlier PR runs; deployment concurrency does not interrupt an active deployment. Job timeouts are in the YAML and do not promise deployment duration.

## What the YAML does not prove

Required reviewers, Environment branch restrictions, main protection, package permissions, and runner registration are external settings. They must be checked; this review did not inspect GitHub Settings.

A manual run with an existing tag skips current-tree tests because it reuses a built image. This does not validate reverse migrations or prove database compatibility.

## Pipeline changes

Deployment/script changes must retain event/branch gates and keep PR jobs away from the production runner. Also review paths, permissions, tokens, and migration compatibility.

Details: [DEPLOYMENT.md](../DEPLOYMENT.md), [diagrams](../phase-2-design/diagrams.md), [security](../SECURITY-HARDENING.md).
