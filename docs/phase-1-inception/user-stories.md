# User stories and acceptance criteria

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

| Story | Acceptance criteria | State |
| --- | --- | --- |
| As a player, I want to choose a game to see the correct offer | Availability explicit; only sellable games have plans and review | Implemented |
| As a buyer, I want to compare plans before paying | Catalog resources, identified recommendation, visible periods and total | Implemented |
| As a buyer, I want to use a valid promotion code | Server validation; invalid code does not create discounted Checkout | Implemented with commerce database |
| As a buyer, I want to follow delivery | Minimal status and real stages; redirect does not activate server | Implemented with integrations |
| As a visitor, I want to read in my language | EN/PT-BR cookie selection retained during navigation | Implemented |
| As a visitor, I want to select a theme and limit motion | Light/dark themes and prefers-reduced-motion support | Implemented |
| As a community, I want to request a special configuration | Normalized values, consistent estimate, and email draft | Implemented as an estimate |
| As an operator, I want to resume interrupted delivery | Order persistence, startup recovery, and stable external ID | Implemented |
| As a customer, I want site invoices and subscriptions | Real identity, ownership authorization, and auditing | Pending |
| As a prospective customer, I want a free 24-hour test | Verified email; one SQL claim per account; 24-hour server; suspension; 72-hour save retention and same-server paid conversion | Implemented locally; requires database migration and integration configuration |

These stories describe the code and its gaps without assigning tasks to people or promising deadlines. Visual acceptance evidence is in [FRONTEND-REDESIGN.md](../FRONTEND-REDESIGN.md); financial acceptance must use integration test environments.
