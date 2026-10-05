# Operations runbook

> **Status:** Operational procedure; execution depends on the real environment
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

This procedure assumes the layout defined by deployment. Verify machine, directory, and image before acting; the documentation review did not access the host.

## Initial inspection

On the server, with the appropriate operational account:

```bash
cd /opt/howtoosoftware-hosting-prototype
docker compose --env-file image.env -f docker-compose.yml ps
docker inspect --format '{{.Config.Image}}' hts-hosting-site
curl --fail --silent http://127.0.0.1:5147/health
docker logs --tail 80 hts-hosting-site
```

Review logs privately before sharing. Do not use full `docker inspect` output: it contains environment variables and secrets.

## Diagnosis by symptom

| Symptom | Check | Next action |
| --- | --- | --- |
| Container does not start | Image, valid configuration, sanitized logs | Correct settings or return to a compatible image |
| Health Unhealthy | SQL network, certificate, login, and commerce target | Restore database access; do not disable settings to hide failure |
| HTTPS redirect loop | X-Forwarded-Proto and KnownProxies | Correct known proxy/origin |
| Host rejected | Effective proxy Host and AllowedHosts | Align application domain |
| Homepage works, checkout fails | Stripe keys, origin, orders, and migrations | Diagnose backend; health does not cover all checkout |
| Order Pending after payment | Webhook delivery, signature, and HTTP response | Check Stripe event and idempotency before resending |
| Order stuck Paid/Provisioning | Worker logs, state, panel/egg/capacity | Investigate fulfillment; do not manually create a duplicate server |
| Suspected duplicate server | Order external ID and panel records | Reconcile before recreating anything |
| Worker logs empty connection in database-free preview | Known public-mode limitation without orders | Expected in isolated preview; configure a store to exercise purchases |
| Interactive controls disconnect | Circuit, proxy, and WebSocket | Check connection infrastructure |
| Lab inaccessible in production | Expected behavior | Use a dedicated Development environment; do not open production |

## Payment and delivery

A success redirect does not prove webhook arrival. Check the Stripe event and correlate order/state privately. The public endpoint exposes only progress, not account-management data.

The worker queries orders awaiting delivery at startup and every 60 seconds. Queue deduplication prevents repeated scans from scheduling an order already queued or processing. A failed scan is logged without connection details and retried on the next interval. Resolve the underlying database/panel issue and reconcile the order with Stripe and Pterodactyl; a restart is not required just to schedule another scan. No public administrative retry endpoint exists.

The provisioner checks the stable external ID before creation. Preserving these references is essential to prevent repeated delivery after restoring a database.

## Rollback and incidents

Use a manual main workflow with `image_tag` identifying an existing image from the same repository, as described in [DEPLOYMENT.md](../DEPLOYMENT.md). The migrated database remains; verify compatibility first.

For a suspected leak, follow [SECURITY.md](../../SECURITY.md): revoke credentials, replace environment configuration, and investigate without publishing secrets. For data loss, follow [recovery](disaster-recovery.md).

The repository defines no formal on-call rotation or incident SLA. The team must establish owners and internal channels; this runbook does not create them.
