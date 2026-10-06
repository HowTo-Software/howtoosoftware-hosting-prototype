# ADR-0003 — Persistence, migrations, and recovery

> **Status:** Retrospective record of an implemented decision; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Context

An entirely in-memory queue loses work on restart. A deployment that silently changes the database complicates rollback and permission separation.

## Observed decision

Persist orders/state in SQL Server, use a local channel for immediate work, and query pending fulfillment at startup. Use stable external Pterodactyl IDs. Run migrations only with explicit flags; deployment migrates before replacing the image.

## Consequences

Runtime and migrations can use separate principals. The previous image must understand the migrated schema. The channel is not a distributed broker; multiple instances require coordination analysis.

Public mode without a database remains supported. Invalid commerce configuration must not silently switch to another target.

Sources: [SQL Server](../../SQLSERVER-SETUP.md), [OrderFulfillment](../../../src/HowToSoftware.Hosting/Services/Orders/OrderFulfillment.cs), [deploy.sh](../../../deploy/deploy.sh).
