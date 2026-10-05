# Adopted documentation standard

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

The reference is the local directory `C:\Users\Malaio\Desktop\best-document`, requested by the project owner. Its lifecycle phase index, status/owner/date metadata, Mermaid diagrams, and decision records were adopted.

The original template contains generic text and fields to fill in. Here, those fields have been replaced with HTS Hosting facts. The structure is adapted: an existing guide remains the reference for its procedure, and the phase folder provides context and a link to it.

## Maintenance rules

1. Write and maintain project documentation in **English**. This applies to headings, descriptions, tables, metadata, diagrams, link labels, and examples. Preserve code identifiers, paths, commands, and literal runtime messages.
2. Update documentation with the code when a route, setting, integration, or visible behavior changes.
3. Use relative repository links and real filenames.
4. Document settings by variable name and purpose; never copy `.env` values, credentials, customer data, or dumps.
5. Distinguish implemented code, required configuration, demonstrations, proposals, and externally verified operations.
6. Record test results with a date, environment, command, or artifact. An old result does not validate a new change.
7. Avoid duplicating rates, resources, and commercial rules across guides. Identify the source when a table is useful.
8. Add an ADR for a lasting decision, explaining its reason and consequences. Retrospective records must not imply an earlier approval.

## Sources and coverage

Explanations were checked against `Program.cs`, Razor components, services, models, migrations, tests, sample configuration, and the pipeline. Visual review evidence is in [FRONTEND-REDESIGN.md](FRONTEND-REDESIGN.md).

Real credentials, DNS, proxies, certificates, GitHub Environment settings, runner permissions, backups, and SQL Server/Stripe/Pterodactyl state **were not inspected on the production machine** during this documentation review.

The documentation reference does not replace [LICENSE](../LICENSE), existing policies, or third-party component attribution. No generic license or policy was imported from the template.

## Review verification

On 2026-10-05, the documentation organization was checked across 47 Markdown files: 300 local links resolved, metadata was present, code fences were balanced, no generic template fields remained, and no trailing whitespace was reported. `git diff --check` passed.

Hashes of 291 files in src, tests, .github, and deploy stayed unchanged during that task: the update was documentation-only and preserved the earlier frontend changes.

The isolated preview was temporarily started in Development on port 5165, using the existing Release artifact. Health and five public/review pages returned 200. That verification instance was stopped. The worker query failure without a database was observed and documented in onboarding; no external database, payment, or panel was used.

The C# suite and browser QA described in the frontend guide are evidence from the earlier visual review; they were not repeated for the documentation edit. Production state and external settings remain unverified in this task.

The subsequent language correction applies English throughout this documentation set and records English as the ongoing documentation language. Runtime UI translations and technical identifiers remain unchanged.
