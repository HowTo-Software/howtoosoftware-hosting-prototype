# Frequently asked questions

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

**Which games can I purchase?**

Project Zomboid has eight paid plans. Minecraft supports trials with approved profiles; paid upgrade tiers appear when HTS configures their prices.

**How many plans are available?**

Eight, from 4 to 16 GiB of memory. Current resources and prices appear on the product page.

**Does 300% CPU mean three dedicated cores?**

It is a usage limit on the panel's shared pool. No exclusive cores are promised.

**Does changing language change currency?**

No. EN/PT-BR changes presentation; currency follows commercial configuration.

**Is payment handled on the site?**

The site creates an order and redirects to Stripe Checkout. Card details are provided to Stripe.

**Does the success page mean my server is active?**

No. Webhook confirmation and installation precede Active. The page displays progress.

**Can I manage subscriptions and download invoices on the site?**

There is no public authenticated billing/invoice portal yet.

**Does local login create my account?**

No. Local login/registration are demonstrations. The external panel has its own authentication.

**Where do I manage my server?**

Use the external panel link. The homepage no longer displays simulated server charts.

**Is a custom estimate a purchase?**

It is an estimate with an email draft for contact, without automatic checkout.

**Is the 24-hour test activated automatically?**

On a configured host, email confirmation starts automatic account/server provisioning. The trial runs for 24 hours after installation, then suspends. You have 3 more days to purchase a plan and preserve the same save. It is allowed once per account across Project Zomboid and Minecraft.

**What resources does the trial include?**

Both games default to 6 GiB RAM and 25 GiB disk. CPU 0 is an unlimited shared CPU setting, not a dedicated core guarantee. HTS controls these values through configuration.

**Is the site available in Spanish?**

This repository currently has EN and PT-BR. Spanish is proposed future work.

More details: [user guide](user-guide.md). Operators should consult the [runbook](../phase-6-operations/runbook.md).
