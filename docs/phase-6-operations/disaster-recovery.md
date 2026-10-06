# Service and data recovery

> **Status:** Proposed procedure; restoration and targets not verified in production
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

The repository includes image rollback but no complete backup/restore automation or approved RPO/RTO values. Adapt and rehearse this procedure in an isolated environment.

## What must be recoverable

| Item | Why it matters |
| --- | --- |
| Commerce SQL database or orders context in use | Financial snapshots, events, state, and external references |
| Data Protection volume | Compatible tokens/cookies across restarts |
| Private runtime and migration configuration | Connections and provider integration |
| Compatible image tag/SHA | Code that understands the restored schema |
| Pterodactyl servers/data | Game worlds/files are not in the storefront database |
| Stripe/panel references | Payment and fulfillment reconciliation after restore |

A storefront SQL backup is not a game-world backup. A Docker image backup is not a SQL or persistent-volume backup.

## Preparation

Define owners, frequency, retention, encryption, and private backup location. Keep secrets separate from public files. Rehearse restoration to an isolated target, measure possible data loss and recovery time, and only then approve targets.

The backup/restore identity needs its own permissions. Do not grant the application login administrative privileges to simplify recovery.

## Controlled recovery

1. Identify incident, affected target, and last usable backup.
2. Suspend fulfillment operations that could cause external effects while reconciling state.
3. Restore database and keys in isolation; check schema, compatible image, and references.
4. Compare Stripe payments/events after the backup and existing panel servers.
5. Resolve discrepancies through stable order, event, and server IDs; do not recreate all fulfillment indiscriminately.
6. Plan runtime/worker recovery and safe event replay after reconciliation.
7. Verify health, an appropriate test purchase, order state, and panel access.
8. Record actual loss, duration, actions, and procedure improvements.

No generic SQL command can safely restore arbitrary production: the operator must choose the actual backup, target, interruption, and permissions.

## Image rollback

Deployment attempts to restore the previous image on failed health. This does not undo migrations or recover deleted data. An incompatible migration can make image rollback worsen the incident; assess schema and recovery plans first.

Details: [DEPLOYMENT.md](../DEPLOYMENT.md), [data model](../phase-2-design/data-model.md), [runbook](runbook.md).
