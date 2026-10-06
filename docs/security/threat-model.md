# Threat model

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Assets and boundaries

Assets: Stripe/Pterodactyl keys, SQL credentials, customer data, order snapshots, game servers, and operational capacity. Browser input and public parameters are untrusted. Known proxy, backend, database, and providers have separate boundaries.

| Threat | Code control | Remaining limitation / verification |
| --- | --- | --- |
| Browser price/resource tampering | Server-resolved catalog and quote | Keep rules in services when changing UI |
| Forged payment success | Signed webhook and amount/currency validation | Configure correct secrets/endpoints |
| Repeated event / duplicate server | Idempotent events/stores and stable external ID | Reconcile external effects after restore |
| Reading another customer's data by URL | Minimal status without email/session_id management | Identity and authorization still required |
| Form CSRF | Antiforgery | Signature-authenticated webhook exception |
| XSS / external script | Razor encoding, CSP nonce, and local scripts | JSON-LD uses MarkupString for serialized JSON, not user HTML |
| SQL injection | EF Core and parameters | Review any new raw SQL |
| Key leak through panel redirect | Automatic redirects disabled; HTTPS | External least privilege and rotation |
| Production lab exposure | Guard requires Development + flag | Keep environment/flag correct |
| Forged IP/protocol | Forwarded headers restricted to known proxies | Configure real network/proxy |
| Endpoint abuse | Rate limits, sizes, and timeouts | Edge, firewall, and provider mitigation |
| PR execution on production host | Deployment event/branch gates | Verify GitHub/runner protections |
| Incompatible rollback | Migration before replacement; visible failure | Team must review compatibility |

## Known limitations

The storefront has no customer authentication, public financial authorization, or automatic suspension. Keeping these routes absent reduces exposure but does not itself implement a secure customer area.

An email-derived provisioning ID is not a login or ownership proof. A positive health response is not a security audit. A local queue does not guarantee distributed exclusion across replicas.

See [hardening](../SECURITY-HARDENING.md), the [reporting policy](../../SECURITY.md), and [HTTP contracts](../phase-2-design/api-specification.md). Update this model when adding authentication, private endpoints, integrations, or multiple workers.
