# Deploying a release

> **Status:** Operational procedure; server state must be verified
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

The detailed procedure is in [DEPLOYMENT.md](../DEPLOYMENT.md). This guide connects preparation, validation, and deployment acceptance.

## Preparation

1. Review diff, migrations, and configuration; preserve local changes in a branch.
2. Run the [test plan](../phase-4-testing/test-plan.md).
3. Check BaseUrl, AllowedHosts, and proxy domain; code defaults do not prove machine configuration.
4. Verify webhook, correct-mode keys, commerce SQL, and Pterodactyl profile.
5. Confirm a recoverable backup, Data Protection keys, and migration compatibility with the previous image.
6. Record notes with commit/tag and operational impact.

## Deployment

A PR receives build/tests/image build on a GitHub runner. A push to main publishes a SHA-identified GHCR image and requests the production job. Reviewers and branch rules belong to the configured GitHub Environment.

The production runner executes `deploy.sh`: validate image/files, pull image, migrate commerce in a temporary container, replace the service, and check health. A failed health check attempts to restore the previous image and leaves the job failed.

## Post-deployment acceptance

Confirm effective image, health, homepage, catalog, review, language/theme, and panel access. Validate financial flows only with an authorized scenario and appropriate environment; opening the homepage does not verify commerce fulfillment.

On failure, consult the [runbook](../phase-6-operations/runbook.md). Do not automatically reverse migrations when returning to an earlier image. The local redesign branch is not presented as already deployed.
