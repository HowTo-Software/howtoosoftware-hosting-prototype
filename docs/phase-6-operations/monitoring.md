# Health, logs, and monitoring

> **Status:** Existing signals checked; alert configuration proposed
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Available signals

- `/health`: minimal uncached JSON; checks the configured commerce SQL connection.
- Structured order, event, worker, and sanitized error logs.
- Persisted order/job states and deployment events in the commerce model.
- Public progress status by session_id, without personal details.
- Deployment result and health polling in GitHub Actions.

Healthy without a database is an allowed public mode. It does not mean billing or fulfillment is ready. Health does not verify Stripe, Pterodactyl, full schema, interactive circuits, or game-server networking.

## Alerts to configure

| Signal | Detected risk | Note |
| --- | --- | --- |
| HTTP/health unavailable | Application or SQL unavailable | Window and threshold depend on operations |
| Webhook 5xx | Event unprocessed | Check Stripe retries and idempotent effects |
| Accumulating old Paid/Provisioning orders | Interrupted fulfillment | Correlate worker, database, and panel |
| Provisioning Failed | Egg, capacity, installation, or permissions | Distinguish from pending payment |
| Startup scan error | Recovery not queued | Diagnose before restarting |
| TLS/DNS/redirect failure | Domain access impaired | Monitor final domain and origin |
| Backup/restore failure | Unreliable recovery | Restore evidence is more useful than “backup created” |
| 429 and unusual traffic | Limits/bots/traffic surge | Application limits do not replace edge controls |

These alerts are guidance, not installed Prometheus/Grafana rules. The repository has no configured observability dashboard, metrics exporter, or formal SLO.

## Logs and privacy

Collect only necessary signals and control access/retention. Do not send webhook bodies, passwords, keys, emails, or SQL strings to public tickets. Do not enable SensitiveDataLogging in production.

The visual preview uses `MockServerPreviewService`. Demonstration charts must not be used as operational monitoring data.

See [runbook](runbook.md), [data and compliance](../security/compliance.md), and [HTTP contracts](../phase-2-design/api-specification.md).
