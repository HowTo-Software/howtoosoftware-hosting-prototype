# Main flow diagrams

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

These diagrams describe the current code and are versioned as text. The [component architecture](architecture.md) and [data relationships](data-model.md) complement the flows below.

## Purchase and fulfillment

```mermaid
sequenceDiagram
    actor Customer
    participant Site as Blazor / services
    participant DB as SQL store
    participant Stripe
    participant Worker
    participant Panel as Pterodactyl
    Customer->>Site: Review game, plan, and period
    Customer->>Site: POST with antiforgery
    Site->>Site: Resolve quote and validate promotion
    Site->>DB: Create Pending order and snapshot
    Site->>Stripe: Create subscription session
    Site-->>Customer: Redirect to Checkout
    Customer->>Stripe: Complete payment
    Stripe->>Site: Signed webhook
    Site->>Site: Verify event, amount, and currency
    Site->>DB: Record event / Paid order
    Site->>Worker: Enqueue order ID
    Site-->>Stripe: 200 after handling
    Worker->>DB: Record Provisioning
    Worker->>Panel: Find/create by stable external ID
    Worker->>Panel: Poll installation
    Worker->>DB: Record Active or Failed
    Customer->>Site: Query minimal status
    Site-->>Customer: Status and stage
```

Write ordering and idempotency are implemented in stores and services, without a distributed transaction with Stripe/Pterodactyl. Browser return and webhook may arrive in different orders.

## Deployment

```mermaid
flowchart TD
    PR["Pull request to main"] --> Test["Restore / Release build / tests"]
    Push["Push to main"] --> Test
    Test --> Image["Image build"]
    Image --> Event{"Event"}
    Event -->|PR| End["Finish without push or deploy"]
    Event -->|main| GHCR["GHCR push sha-commit"]
    GHCR --> Env["Production Environment"]
    Env --> Runner["Self-hosted runner"]
    Runner --> Migration["Temporary container: migrate-commerce"]
    Migration -->|success| Replace["Replace container"]
    Migration -->|failure| Preserve["Keep current image"]
    Replace --> Health{"Health OK?"}
    Health -->|yes| Complete["Finish"]
    Health -->|no| Rollback["Restore previous image; job fails"]
```

Environment reviewers and restrictions must be verified in GitHub. Image rollback does not reverse migrations.

## Animation lifecycle

```mermaid
stateDiagram-v2
    [*] --> Fallback
    Fallback --> Intro: JS and WebGL available / motion allowed
    Intro --> Idle: 4.4 seconds completed
    Idle --> Reaction: Pointer / allowed replay
    Reaction --> Idle: Movement ended
    Intro --> Paused: Offscreen / hidden tab
    Reaction --> Paused: Offscreen / hidden tab
    Paused --> Idle: Visible again
    Idle --> Fallback: Reduced motion / context lost
    Fallback --> [*]: Page exit
```

This diagram summarizes the visual lifecycle; context recovery and teardown are implemented in `vector-wordmark.js`. See details and evidence in [FRONTEND-REDESIGN.md](../FRONTEND-REDESIGN.md).
