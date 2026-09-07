# HowToSoftware — Project Zomboid Hosting

The HowToSoftware **Project Zomboid server hosting** platform, built with
**C# / .NET 10 / Blazor**, with a Stripe, SQL Server and Pterodactyl backend foundation.

> 🇧🇷 **A versão em português está mais abaixo** — veja [Português (BR)](#português-br).

---

> ### ⚠️ This is a prototype, not a product
>
> - **All pricing is placeholder test data.** The plans, prices, discounts and promotional
>   codes exist so the interface can be reviewed. **Nothing here is a commercial offer.**
> - **Checkout is implemented but credentials are placeholders.** Nothing is charged until a
>   developer deliberately supplies Stripe test/live credentials.
> - **The control panel is a mock.** It looks and behaves like the real thing, but no server
>   is ever created, started or stopped.
> - **Provisioning is real, and it is gated.** The Pterodactyl integration creates and deletes
>   actual servers. It is reachable only from a development-only lab page, only after an explicit
>   confirmation, and only with an API key supplied from outside the repository.
> - **Sign-in authenticates nobody.** There is no identity provider, no user list, no demo
>   credential and no password storage. Every attempt reports that authentication is not
>   connected, and the screen says so before you type anything.
> - **The infrastructure page has no specifications in it.** CPU, memory, storage, network,
>   allocation and region are all still to be supplied, so the page shows a visible
>   `TEXT ABOUT HERE` placeholder wherever a figure or a sentence will go.

---

## Screenshots

### Dark theme (default)
![Hero, dark theme](docs/hero-dark.png)

### Light theme
![Hero, light theme](docs/hero-light.png)

### Sign in
![Sign in](docs/login.png)

### Sign in, in Brazilian Portuguese
![Sign in in Portuguese](docs/login-pt-br.png)

### Infrastructure
![Infrastructure](docs/infrastructure.png)

### Control panel section
![Control panel](docs/control-panel.png)

### Plan catalogue (test data)
![Plans](docs/plans.png)

---

## Quick start

**Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).**
Check with `dotnet --list-sdks` — you need a `10.x` entry.

```bash
git clone https://github.com/gamerplay20p5-dotcom/howtoosoftware-hosting-prototype.git
cd howtoosoftware-hosting-prototype

dotnet run --project src/HowToSoftware.Hosting
```

Then open **http://localhost:5147**.

```bash
dotnet build          # build everything
dotnet test           # run the automated test suite
```

Important routes include:

| Route | What it is |
|---|---|
| `/` | the marketing homepage |
| `/infrastructure` (also `/hardware`) | the hardware and infrastructure page |
| `/game-hosting/project-zomboid` | the plan ladder, build-to-order and the plan questions |
| `/login` | the control-panel sign-in screen |
| `/dev/provisioning` | the explicitly enabled real provisioning lab |

No npm is required. The site starts without external credentials; Stripe, SQL Server and
Pterodactyl features report “not configured” until `.env` is filled. See
[`docs/COMMERCE-ARCHITECTURE.md`](docs/COMMERCE-ARCHITECTURE.md),
[`docs/SQLSERVER-SETUP.md`](docs/SQLSERVER-SETUP.md), and
[`docs/stripe-testing.md`](docs/stripe-testing.md). For a Portuguese map of every major folder,
page and integration, see [`docs/PROJECT-MAP.md`](docs/PROJECT-MAP.md).

---

## Plans

Eight tiers, from 4 GB to 16 GB of memory. The figures below are the product **and** the
provisioning payload — [`StaticPlanCatalogService`](src/HowToSoftware.Hosting/Services/StaticPlanCatalogService.cs)
is the only place they exist, and both the plan cards and the Pterodactyl request read from it.

| Plan | RAM | CPU | Storage | Backups | Computed | Charged | Renews at |
|---|---|---|---|---|---|---|---|
| Outpost | 4 GB | 300% | 25 GB | 1 | 7.88 | **7.99** | 7.59 |
| Settlement | 5 GB | 400% | 25 GB | 2 | 9.98 | **9.99** | 9.49 |
| Stronghold | 6 GB | 400% | 25 GB | 3 | 11.18 | **10.99** | 10.44 |
| Knox Cell | 8 GB | 500% | 40 GB | 5 | 14.70 | **14.99** | 14.24 |
| Rosewood | 10 GB | 500% | 40 GB | 6 | 17.10 | **16.99** | 16.14 |
| West Point | 12 GB | 600% | 40 GB | 7 | 20.40 | **19.99** | 18.99 |
| Louisville | 14 GB | 600% | 40 GB | 8 | 22.80 | **22.99** | 21.84 |
| Knox County | 16 GB | 700% | 40 GB | 10 | 26.10 | **25.99** | 24.69 |

**CPU is a share, not a core count.** Pterodactyl's `cpu` limit is a percentage of one logical
thread: 100 is one thread, 300 lets a container burst across three. It does not pin cores, so the
site says "300% CPU allocation" and never "3 dedicated cores".

Memory and disk reach Pterodactyl in **MiB**, not MB — 4 GB is 4096, 25 GB is 25600, 40 GB is
40960. The conversion happens once, in the catalogue. Storage steps up once, at the 8 GB tier.

### Prices are a rate card, not a price list

There is no list of eight agreed prices anywhere. There are three rates, and every price is those
rates applied to what the tier actually ships:

```jsonc
"HostingPlans": {
  "CurrencySymbol": "$",
  "Rates": {
    "CpuPer100Percent": "0.90",   // per 100% CPU, the equivalent of one thread
    "MemoryPerGb": "1.20",
    "DiskPerBlock": "0.30",       // per DiskBlockGb, prorated within a block
    "DiskBlockGb": 20
  },
  "Prices": { "zomboid-4gb": "" } // optional per-slug override
}
```

So the 4 GB tier computes to `(300 ÷ 100 × 0.90) + (4 × 1.20) + (25 ÷ 20 × 0.30)` = **7.88**, and
a rate change moves every tier at once rather than leaving one card quietly inconsistent with the
rest. Storage is prorated inside a block, so 25 GB costs a block and a quarter rather than being
rounded up to two.

### Charm pricing, and the renewal rate

`"CharmPricing": true` snaps every computed price to the nearest `x.99`:

- **at or above the half unit** the price keeps its unit and takes 99 cents — 14.70 → **14.99**
- **below it** the price drops a whole unit first — 17.10 → **16.99**

Dropping the unit is the point; it is what makes 17.10 read as sixteen rather than seventeen.
It applies to *computed* prices only. A figure typed into `Prices` is a decision somebody already
made and is charged exactly as written.

`"RenewalDiscountPercent": 5` prints the second-month-onward rate on every card. It is a plain
percentage off the first month and is **not** charm-rounded — a figure nudged to `x.99` afterwards
would not be the percentage the site just claimed. Set it to `0` to remove the line entirely
rather than print "0% off".

An entry in `Prices` overrides the rate card for that slug alone — that is how a promotional price
is set without disturbing the ladder. With no rate card and no override a plan renders as
**PRICE PENDING**: there is deliberately no fallback number, because a default price is a price
somebody reads as real.

Rates and overrides are held as **strings** and parsed with the invariant culture. Bound as
decimals, a configuration provider would use the host's culture and read `0.90` as ninety wherever
the decimal separator is a comma.

### Build to order

Above the top tier, `/project-zomboid` carries a
[build-to-order panel](src/HowToSoftware.Hosting/Components/Home/CustomBuildSection.razor):
memory, CPU and storage sliders, a live estimate priced from the same rate card, and the
breakdown that produced it.

The panel is careful about two things. The estimate says it is an estimate, and the total is the
three visible lines added up rather than a fourth calculation — a breakdown that does not sum to
the figure above it is the kind of detail a reader checks once and then stops trusting. And
**nothing is submitted**: there is no request table behind it yet, so the button hands the
specification to the visitor's own mail client instead of pretending to file it.

The quote carries its rounding as a **line of the breakdown**, signed, so the lines still sum to
the total. Applying charm pricing silently after printing the lines would leave a total that
disagreed with its own arithmetic — the one thing the breakdown exists to prevent. The renewal
rate is quoted too, and both figures go into the mail draft.

Slider limits are configuration (`HostingPlans:CustomBuild`):

| Limit | Value | Where it comes from |
|---|---|---|
| Memory | 64 GB | agreed ceiling |
| CPU | 3600% | the 36 threads of the processor the infrastructure page names (Pterodactyl counts 100% as one thread, so 36 threads is 3600, not 36000) |
| Storage | 500 GB | agreed ceiling |

The **floor** is read from the largest plan rather than restated, so adding a tier moves it
automatically. The ceilings bound the *form*, not the hardware, and the panel says so — past them
it prints the contact address instead of a slider.

---

---

## Provisioning

Real integration with the Pterodactyl **Application API**, verified against Panel source at
v1.15.1. Setup, the exact request shape and the pitfalls are in
**[docs/PTERODACTYL-SETUP.md](docs/PTERODACTYL-SETUP.md)**.

```
order → resolve plan → read egg → find-or-create panel user → create server
```

| Piece | What it does |
|---|---|
| [`IPterodactylClient`](src/HowToSoftware.Hosting/Infrastructure/Pterodactyl/IPterodactylClient.cs) | the panel API, narrowed to what provisioning needs |
| [`IProvisioningService`](src/HowToSoftware.Hosting/Services/Provisioning/IProvisioningService.cs) | the pipeline the payment webhook will call |
| [`GameTemplateCatalog`](src/HowToSoftware.Hosting/Services/GameTemplateCatalog.cs) | egg, image, startup and variables, per game |
| `/dev/provisioning` | a development-only lab that drives it by hand |

**Idempotent on the request id.** Every provisioning request writes an `external_id`, and the
pipeline looks it up before creating anything. A retried payment webhook, a double-clicked button
and a re-run test all reuse the existing server. Payment providers retry webhooks; this is not a
nicety.

**Node placement is the panel's job.** The request carries a `deploy` block naming the location
and Pterodactyl picks a public node with headroom and a free allocation. There is no load
balancer in this codebase, on purpose.

### The API key

Never in `appsettings.json`, never in the repository, never sent to a browser, never logged.
Supplied from the environment or user-secrets:

```bash
dotnet user-secrets --project src/HowToSoftware.Hosting set "Pterodactyl:ApiKey" "ptla_..."
export Pterodactyl__ApiKey="ptla_..."
```

Diagnostics show the key's prefix and length and nothing else. With no key configured the site
still runs — only provisioning reports `NotConfigured`.

### The lab

`/dev/provisioning` is guarded three ways: the page renders a closed notice outside Development
unless `ProvisioningTest:Enabled` is set, **every action re-checks that on the server** rather than
trusting a hidden button, and both creating and deleting require an explicit confirmation.

Deletion is guarded again: the button passes a provisioning request id, not a server id, and the
service re-reads the server and refuses unless its `external_id` begins with `hts-test-server:`.
A customer's server cannot be reached from that page.

**Payments are deliberately not implemented.** The manual button and a future webhook call the
same `ProvisionAsync`, so the pipeline exists before the checkout does.

---

## Language

The site ships in **English** and **Brazilian Portuguese**, and picks one for you on your
first visit from the browser's `Accept-Language` header. Anything Portuguese - `pt-BR`, `pt`,
even `pt-PT` - gets Brazilian Portuguese; anything the site does not speak falls back to
English.

The **EN / PT-BR** control in the header overrides that. It is two ordinary links pointing at
`/culture/select`, which writes the choice to a cookie and sends you back to the page you were
on, so the whole response comes back in the new language and stays there on your next visit.
No JavaScript is involved, and an explicit choice always beats the browser's preference.

Labels are language names, never flags - a language is not a country.

Everything is [.NET localisation](src/HowToSoftware.Hosting/Localization): `IStringLocalizer`
over four `.resx` resource sets, wired through `RequestLocalizationOptions` in
[`Program.cs`](src/HowToSoftware.Hosting/Program.cs). No translation widget, no runtime machine
translation, and no `if (language == "pt")` anywhere - components ask for a string and the
culture chosen for the request decides which resource answers.

| Resource set | Covers |
|---|---|
| `CommonText` | navigation, shared actions, footer, status labels, theme and language controls |
| `HomeText` | the homepage, plus the plan catalogue and marketing content behind the services |
| `LoginText` | the sign-in screen |
| `HardwareText` | the infrastructure page, including the `TEXT ABOUT HERE` placeholder |

---

## Switching between dark and light

The site ships **dark by default**. Use the **DARK / LIGHT toggle in the top-right of the
header** to switch. Your choice is remembered in `localStorage` and is applied before the
page paints, so there is no flash of the wrong theme on reload.

Both themes are driven from one file — [`wwwroot/css/theme.css`](src/HowToSoftware.Hosting/wwwroot/css/theme.css).
The light theme redefines design tokens only; no component CSS is duplicated.

Two deliberate decisions in the light theme:

1. **The brand ramp inverts its role.** On navy, the pale blues and purples carry text. On
   white those would be unreadable, so the same token names point at the deep steps instead.
   Components keep asking for "the blue that carries data" and get the right one per theme.
2. **The console stays dark in both themes.** A terminal is dark. Inverting it would read as
   a bug, not a theme.

Every visible text element was measured against its real rendered background in **both**
themes and passes **WCAG 2.1 AA** contrast.

---

## What is interactive vs. what is mock

Everything interactive is **Blazor / C# state** — there is no JavaScript framework here.

| Works for real (Blazor state) | Mock / placeholder data |
|---|---|
| Billing period switch (monthly / quarterly / annual) | The prices themselves |
| Promotional code field (try `SURVIVOR10`, `KNOX25`, `HTS2026`) | Discounts are display-only, nothing is charged |
| Control panel sidebar (Overview / Console / Mods) | Server metrics, console log, mod list |
| Start / Restart / Stop power buttons and their transitions | No real server is touched |
| Scroll-driven provisioning stages and the diagram that follows them | The deployment flow is illustrative |
| FAQ accordion, mobile navigation, theme toggle | — |
| Sign-in validation, password show/hide, remember me, busy state | No credentials are checked or stored |
| The auth gateway diagram, which follows the form's state | The hops it draws |
| Language switching and the culture cookie | — |

### About the JavaScript

There is exactly **one dependency-free JS file**
([`wwwroot/js/site.js`](src/HowToSoftware.Hosting/wwwroot/js/site.js)). It only reports browser
facts that neither C# nor CSS can observe: scroll position, element visibility, pointer
position, the saved theme, and when enhanced navigation has swapped the page.
**It holds no application state.**

The provisioning section is the clearest example of the split: JavaScript notices which
stage scrolled into the middle of the screen and calls a `[JSInvokable]` C# method. Blazor
then decides everything visible — which stage is active, which diagram nodes light up, the
step counter. **JavaScript observes; Blazor decides.**

The pointer spotlight on the panels is the same split again: JavaScript publishes where the
cursor is as two CSS custom properties, and the stylesheet decides what that looks like.

Without JavaScript the site still works: all content is visible, language switching still
works, and only the entrance animations are skipped. The site fully respects
`prefers-reduced-motion` - including the route entrance, which is switched off rather than
shortened.

---

## Project structure

```
src/HowToSoftware.Hosting/
├── Components/
│   ├── Layout/       SiteHeader, SiteFooter, MainLayout
│   ├── Shared/       BrandMark, CtaButton, Icon, ThemeToggle, LanguageSwitcher, CopySlot
│   ├── Home/         one component per homepage section (16 of them)
│   ├── Account/      the sign-in experience and its gateway instrument
│   ├── Hardware/     the infrastructure page's sections
│   └── Pages/        Home, Infrastructure, Login, Error, NotFound
├── Localization/     marker types + the .resx sets, and the culture policy
├── Models/           strongly-typed records (plans, telemetry, server state, …)
├── Services/         interfaces + the static/mock implementations behind them
└── wwwroot/
    ├── css/theme.css design tokens — the single source of truth for both themes
    ├── css/app.css    reset, typography, layout primitives, spotlight, route entrance
    └── js/site.js     the only JavaScript

tests/HowToSoftware.Hosting.Tests/   153 unit tests
```

Each component has its own scoped `.razor.css`, so styles cannot leak between sections.

### Page sections, in order

1. **Hero** — three layers: an animated infrastructure field, oversized type, and a cluster
   of connected UI fragments
2. **Telemetry strip** — live-looking platform readouts
3. **Persistent worlds** — editorial copy beside a Knox County coordinate grid
4. **Workshop** — a mod-synchronisation interface
5. **Provisioning** — sticky-scroll storytelling with a diagram that builds itself
6. **Control panel** — the product reveal, at near-full page width
7. **Nodes** — abstract hardware panels
8. **Capabilities** — numbered editorial blocks
9. **Plans** — the catalogue with billing switch and promo code
10. **FAQ** and **closing call to action**

### The infrastructure page

`/infrastructure` (and `/hardware`, which answers to the same component) explains the platform
the worlds run on. **Its copy has not been written yet**, so every descriptive slot renders
`TEXT ABOUT HERE` in a treatment that is unmistakably a placeholder — see
[`CopySlot.razor`](src/HowToSoftware.Hosting/Components/Shared/CopySlot.razor).

No two sections share a composition, and none of them states a specification:

1. **Hero** — a drawn rack elevation behind oversized type
2. **Physical infrastructure** — a specification ledger beside a twelve-unit rack drawing
3. **Node topology** — platform, provisioning bus, nodes and the servers they place worlds on,
   drawn as one continuous diagram that collapses to a single vertical run on a phone
4. **Compute** — a die plate with its active cores lit
5. **Memory** — three bands, distinguished by treatment rather than by width
6. **Storage** — a volume stack stepped in depth
7. **Network** — four hops on one rail, with a packet crossing it
8. **Server allocation** — a capacity schematic carved into instances
9. **Reliability and operations** — a ticked timeline of operational signals

Which composition each chapter gets is data, in
[`StaticInfrastructureContentService`](src/HowToSoftware.Hosting/Services/StaticInfrastructureContentService.cs),
and a test asserts no two consecutive chapters repeat one.

### The sign-in page

`/login` is a complete sign-in screen with nothing behind it. The form is a Blazor island: the
validation, the password reveal, the busy state and every notice are C# state, and the gateway
instrument beside it draws where the current attempt actually got to — client, edge, then a
stop at identity, because there is nothing there to answer.

`IAuthenticationGateway` is the seam a real provider plugs into. The prototype implementation
behind it reads nothing, compares nothing and stores nothing; see
[`PrototypeAuthenticationGateway`](src/HowToSoftware.Hosting/Services/PrototypeAuthenticationGateway.cs)
for why that is the only honest stand-in.

---

## Text effects

Headlines and mono kickers carry `data-text-effect`. [`site.js`](src/HowToSoftware.Hosting/wwwroot/js/site.js)
splits their text into per-character spans — walking the text nodes, so a heading keeps its `<br>`
and its accent span — and [`app.css`](src/HowToSoftware.Hosting/wwwroot/css/app.css) decides what
that is worth: `rise` lifts characters into focus, `flicker` brings them up like a display
warming, `sweep` runs a light along the line.

The stagger runs off the character index, so a long headline takes longer to settle than a short
one. Characters are wrapped a word at a time, because a line will otherwise break between any two
inline-blocks and split a word down the middle.

All of it is additive: with scripting off there are no spans and the selectors match nothing, and
with `prefers-reduced-motion` set every effect collapses to "the text is there".

---

## Infrastructure photography

The infrastructure page shows photographs of the actual machines. It never substitutes stock
imagery: a slot with no file renders a labelled placeholder naming the exact path to drop it at,
and the next render picks the file up with no code change.

Files go in
[`wwwroot/images/infrastructure/`](src/HowToSoftware.Hosting/wwwroot/images/infrastructure/) —
`rack-front.webp`, `rack-elevation.webp`, `rack-open.webp`, `rack-interior.webp`,
`rack-detail-top.webp`, `rack-detail-bottom.webp`, each with a `.jpg` beside it for browsers
without WebP. That folder's README covers format, sizing and what to check for before publishing
a photo of a rack.

Captions describe the photograph, not the hardware: both nodes live in the same cabinet and no
supplied frame tells them apart, so the node specifications sit beside the images rather than
labelling one of them "node 01".

Every frame reserves its aspect ratio before the image decodes, so nothing reflows; everything
below the fold loads lazily.

---

## Ready for the real backend

The mock data sits behind interfaces, so replacing it does not touch a single component:

| Interface | Prototype implementation | Future implementation |
|---|---|---|
| `IServerPreviewService` | `MockServerPreviewService` | Pterodactyl API client |
| `IPlanCatalogService` | `StaticPlanCatalogService` | Real pricing + billing provider |
| `IMarketingContentService` | `StaticMarketingContentService` | CMS or database |
| `IInfrastructureContentService` | `StaticInfrastructureContentService` | Real node specifications and status |
| `IAuthenticationGateway` | `PrototypeAuthenticationGateway` | ASP.NET Core Identity, or an OIDC provider |

Swap the registration in [`Program.cs`](src/HowToSoftware.Hosting/Program.cs) and the UI
keeps working. `IServerPreviewService` is already **async and command-based**, matching how
a real game-panel API behaves: a power action returns immediately with a transitional state,
and the caller polls until the instance settles.

---

## Brand and design system

Palette: **Light Blue + Purple + White**, on a deep navy foundation.

Colours were sampled from the HTS logo itself:

| Role | Colour |
|---|---|
| Logo purple — brand accent, CTAs, active states | `#B069FF` |
| Logo light blue — technical data, metrics, status | `#83A8F3` |
| White — typography and hierarchy | `#FFFFFF` |
| Deep navy — background foundation only | `#05060E` |

Status indicators deliberately use **blue and purple instead of the usual green and amber**,
so nothing on the page falls outside the brand.

---

## Not implemented yet

- Production Stripe credentials and Dashboard configuration
- Customer accounts and authentication (the sign-in screen exists; nothing is behind it)
- Account creation and password recovery
- The infrastructure page's copy and specifications
- Authenticated customer billing/Customer Portal pages
- Authorized admin retry and diagnostics (the real deploy lab is explicitly gated)
- Legal pages (Terms, Privacy, Acceptable Use) — shown as "Soon" placeholders
- A real contact address (currently a placeholder in `appsettings.json`)

---

## Licence

See [LICENSE](LICENSE). This is **proprietary software** —
© 2026 Henry Lawrence Cahill. All rights reserved.

---
---

# Português (BR)

Plataforma de **hospedagem de servidores de Project Zomboid** da HowToSoftware, feita em
**C# / .NET 10 / Blazor**, com base de backend para Stripe, SQL Server e Pterodactyl.

---

> ### ⚠️ Isto é um protótipo, não um produto
>
> - **Todos os preços são dados de teste.** Os planos, preços, descontos e cupons existem
>   apenas para avaliar a interface. **Nada aqui é uma oferta comercial.**
> - **O checkout está implementado, mas as credenciais são placeholders.** Nada é cobrado até
>   que um desenvolvedor forneça deliberadamente credenciais de teste/produção do Stripe.
> - **O painel de controle é simulado.** Ele se parece e se comporta como o real, mas nenhum
>   servidor é criado, iniciado ou parado.
> - **O provisionamento Pterodactyl é real e protegido.** O laboratório só abre após ativação
>   explícita e confirmação; sem credenciais a integração informa “não configurado”.
> - **O login não autentica ninguém.** Não existe provedor de identidade, lista de usuários,
>   credencial de demonstração nem armazenamento de senha. Toda tentativa informa que a
>   autenticação não está conectada, e a tela avisa isso antes de você digitar qualquer coisa.
> - **A página de infraestrutura não tem nenhuma especificação.** CPU, memória, armazenamento,
>   rede, alocação e região ainda serão fornecidos, então a página mostra o marcador visível
>   `TEXTO SOBRE AQUI` onde entrarão os números e os textos.

---

## Como rodar

**Requer o [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).**
Confira com `dotnet --list-sdks` — precisa aparecer uma versão `10.x`.

```bash
git clone https://github.com/gamerplay20p5-dotcom/howtoosoftware-hosting-prototype.git
cd howtoosoftware-hosting-prototype

dotnet run --project src/HowToSoftware.Hosting
```

Depois abra **http://localhost:5147**.

```bash
dotnet build          # compila tudo
dotnet test           # roda a suíte automatizada
```

Rotas importantes:

| Rota | O que é |
|---|---|
| `/` | a página inicial |
| `/infrastructure` (e também `/hardware`) | a página de hardware e infraestrutura |
| `/game-hosting/project-zomboid` | planos, revisão e início do checkout |
| `/login` | a tela de acesso ao painel de controle |
| `/dev/provisioning` | laboratório real, somente após ativação explícita |

Não precisa de npm. O site inicia sem credenciais externas; Stripe, SQL Server e Pterodactyl
informam “não configurado” até o `.env` ser preenchido. Consulte
[`docs/COMMERCE-ARCHITECTURE.md`](docs/COMMERCE-ARCHITECTURE.md),
[`docs/SQLSERVER-SETUP.md`](docs/SQLSERVER-SETUP.md) e
[`docs/stripe-testing.md`](docs/stripe-testing.md).

---

## Planos e provisionamento

Quatro planos, diferindo em memória e alocação de CPU. Os números são o produto **e** o payload de
provisionamento ao mesmo tempo — existem em um lugar só, e tanto os cards quanto a requisição ao
Pterodactyl leem de lá.

| Plano | RAM | Alocação de CPU | Armazenamento | Backups | `limits` do Pterodactyl |
|---|---|---|---|---|---|
| Outpost | 4 GB | 300% | 25 GB | 1 | `memory 4096 · cpu 300 · disk 25600` |
| Settlement | 5 GB | 400% | 25 GB | 2 | `memory 5120 · cpu 400 · disk 25600` |
| Stronghold | 6 GB | 400% | 25 GB | 3 | `memory 6144 · cpu 400 · disk 25600` |
| Knox Cell | 8 GB | 500% | 25 GB | 5 | `memory 8192 · cpu 500 · disk 25600` |

**CPU é uma fatia, não uma contagem de núcleos.** O limite `cpu` do Pterodactyl é uma porcentagem
de uma thread: 100 é uma thread, 300 permite estourar em três. Não fixa núcleos, por isso o site
diz "300% de alocação de CPU" e nunca "3 núcleos dedicados".

Memória e disco são **MiB**, não MB — 4 GB é 4096, 25 GB é 25600.

Ainda não há preços, e os cards dizem isso. Os preços vêm de configuração (`HostingPlans:Prices`),
e um plano sem preço configurado aparece como **PREÇO PENDENTE**.

A integração com a Application API do Pterodactyl é real e está verificada contra o código-fonte
do painel v1.15.1. A configuração está em
**[docs/PTERODACTYL-SETUP.md](docs/PTERODACTYL-SETUP.md)**. A chave de API nunca vai para o
`appsettings.json`, nunca vai para o repositório e nunca chega ao navegador.

---

## Idioma

O site vem em **inglês** e **português do Brasil**, e escolhe um para você na primeira visita
a partir do cabeçalho `Accept-Language` do navegador. Qualquer variante de português -
`pt-BR`, `pt` e até `pt-PT` - recebe português do Brasil; qualquer idioma que o site não fale
cai para o inglês.

O controle **EN / PT-BR** no cabeçalho passa por cima disso. São dois links comuns apontando
para `/culture/select`, que grava a escolha em um cookie e devolve você para a página em que
estava, então a resposta inteira volta no novo idioma e continua assim na próxima visita. Não
tem JavaScript envolvido, e a escolha explícita sempre vence a preferência do navegador.

Os rótulos são nomes de idioma, nunca bandeiras - idioma não é país.

Tudo é [localização do .NET](src/HowToSoftware.Hosting/Localization): `IStringLocalizer` sobre
quatro conjuntos `.resx`, ligados por `RequestLocalizationOptions` no
[`Program.cs`](src/HowToSoftware.Hosting/Program.cs). Sem widget de tradução, sem tradução
automática em tempo de execução e sem nenhum `if (language == "pt")` espalhado - os componentes
pedem uma string e a cultura escolhida para a requisição decide qual recurso responde.

| Conjunto de recursos | Cobre |
|---|---|
| `CommonText` | navegação, ações compartilhadas, rodapé, rótulos de status, tema e idioma |
| `HomeText` | a página inicial, mais o catálogo de planos e o conteúdo por trás dos serviços |
| `LoginText` | a tela de acesso |
| `HardwareText` | a página de infraestrutura, incluindo o marcador `TEXTO SOBRE AQUI` |

---

## Alternando entre tema escuro e claro

O site vem **escuro por padrão**. Use o **botão DARK / LIGHT no canto superior direito do
cabeçalho** para trocar. A escolha fica salva no `localStorage` e é aplicada antes da página
renderizar, então não existe aquele "flash" de tema errado ao recarregar.

Os dois temas saem de um único arquivo:
[`wwwroot/css/theme.css`](src/HowToSoftware.Hosting/wwwroot/css/theme.css). O tema claro
apenas redefine os tokens de design; nenhum CSS de componente é duplicado.

Duas decisões propositais no tema claro:

1. **A escala da marca inverte de papel.** No fundo escuro, os tons claros de azul e roxo
   carregam o texto. No branco eles seriam ilegíveis, então os mesmos nomes de token apontam
   para os tons profundos. Os componentes continuam pedindo "o azul que carrega dado" e
   recebem o certo em cada tema.
2. **O console continua escuro nos dois temas.** Terminal é escuro. Inverter isso pareceria
   um bug, não um tema.

Todo texto visível foi medido contra o fundo real renderizado nos **dois** temas e passa no
contraste **WCAG 2.1 AA**.

---

## O que é interativo de verdade vs. o que é simulado

Tudo que é interativo é **estado Blazor / C#** — não existe framework JavaScript aqui.

| Funciona de verdade (estado Blazor) | Dados simulados |
|---|---|
| Troca de período (mensal / trimestral / anual) | Os preços em si |
| Campo de cupom (teste `SURVIVOR10`, `KNOX25`, `HTS2026`) | Descontos são só visuais, nada é cobrado |
| Menu lateral do painel (Overview / Console / Mods) | Métricas, log do console, lista de mods |
| Botões Start / Restart / Stop e suas transições | Nenhum servidor real é tocado |
| Estágios de provisionamento guiados por scroll e o diagrama que os acompanha | O fluxo é ilustrativo |
| Acordeão do FAQ, menu mobile, troca de tema | — |
| Validação do login, mostrar/ocultar senha, lembrar de mim, estado de carregamento | Nenhuma credencial é verificada ou guardada |
| O diagrama do gateway de autenticação, que segue o estado do formulário | Os saltos que ele desenha |
| Troca de idioma e o cookie de cultura | — |

### Sobre o JavaScript

Existe exatamente **um arquivo JS sem dependências**
([`wwwroot/js/site.js`](src/HowToSoftware.Hosting/wwwroot/js/site.js)). Ele só reporta fatos do
navegador que nem o C# nem o CSS conseguem observar: posição de scroll, visibilidade de
elementos, posição do ponteiro, o tema salvo e quando a navegação aprimorada trocou a página.
**Ele não guarda nenhum estado da aplicação.**

A seção de provisionamento é o exemplo mais claro dessa divisão: o JavaScript percebe qual
estágio entrou no meio da tela e chama um método C# marcado com `[JSInvokable]`. O Blazor
então decide tudo que aparece — qual estágio está ativo, quais nós do diagrama acendem, o
contador de etapa. **O JavaScript observa; o Blazor decide.**

O spotlight do cursor nos painéis é a mesma divisão de novo: o JavaScript publica onde o
cursor está como duas custom properties de CSS, e a folha de estilo decide como isso aparece.

Sem JavaScript o site continua funcionando: todo o conteúdo aparece, a troca de idioma continua
funcionando e só as animações de entrada são puladas. O site respeita `prefers-reduced-motion`
por completo - inclusive a entrada de rota, que é desligada em vez de encurtada.

---

## Estrutura do projeto

```
src/HowToSoftware.Hosting/
├── Components/
│   ├── Layout/       SiteHeader, SiteFooter, MainLayout
│   ├── Shared/       BrandMark, CtaButton, Icon, ThemeToggle, LanguageSwitcher, CopySlot
│   ├── Home/         um componente por seção da página inicial (16 no total)
│   ├── Account/      a tela de acesso e seu instrumento de gateway
│   ├── Hardware/     as seções da página de infraestrutura
│   └── Pages/        Home, Infrastructure, Login, Error, NotFound
├── Localization/     tipos marcadores + os conjuntos .resx, e a política de cultura
├── Models/           records fortemente tipados (planos, telemetria, estado do servidor…)
├── Services/         interfaces + as implementações estáticas/simuladas
└── wwwroot/
    ├── css/theme.css tokens de design — fonte única de verdade dos dois temas
    ├── css/app.css    reset, tipografia, primitivos, spotlight, entrada de rota
    └── js/site.js     o único JavaScript

tests/HowToSoftware.Hosting.Tests/   153 testes unitários
```

Cada componente tem seu próprio `.razor.css` isolado, então estilo não vaza entre seções.

### Seções da página, em ordem

1. **Hero** — três camadas: campo de infraestrutura animado, tipografia grande e um conjunto
   de fragmentos de UI conectados
2. **Faixa de telemetria** — leituras da plataforma
3. **Mundos persistentes** — texto editorial ao lado de uma grade de coordenadas de Knox County
4. **Workshop** — uma interface de sincronização de mods
5. **Provisionamento** — narrativa com scroll fixo e um diagrama que se constrói sozinho
6. **Painel de controle** — a revelação do produto, quase na largura toda da página
7. **Nós** — painéis abstratos de hardware
8. **Capacidades** — blocos editoriais numerados
9. **Planos** — o catálogo com troca de período e cupom
10. **FAQ** e **chamada final**

### A página de infraestrutura

`/infrastructure` (e `/hardware`, que responde pelo mesmo componente) explica a plataforma em
que os mundos rodam. **O texto dela ainda não foi escrito**, então todo espaço descritivo
mostra `TEXTO SOBRE AQUI` num tratamento que é inconfundivelmente um marcador — veja
[`CopySlot.razor`](src/HowToSoftware.Hosting/Components/Shared/CopySlot.razor).

Nenhuma seção repete a composição de outra, e nenhuma afirma uma especificação:

1. **Hero** — uma elevação de rack desenhada atrás de tipografia grande
2. **Infraestrutura física** — uma ficha técnica ao lado de um rack de doze unidades
3. **Topologia de nós** — plataforma, barramento de provisionamento, nós e os servidores em que
   os mundos são colocados, desenhados como um diagrama contínuo que vira uma coluna vertical
   no celular
4. **Computação** — uma pastilha com os núcleos ativos acesos
5. **Memória** — três faixas, diferenciadas pelo tratamento e não pela largura
6. **Armazenamento** — uma pilha de volumes escalonada em profundidade
7. **Rede** — quatro saltos em um trilho, com um pacote atravessando
8. **Alocação de servidor** — um esquema de capacidade dividido em instâncias
9. **Confiabilidade e operações** — uma linha do tempo marcada com os sinais operacionais

Qual composição cada capítulo recebe é dado, no
[`StaticInfrastructureContentService`](src/HowToSoftware.Hosting/Services/StaticInfrastructureContentService.cs),
e um teste garante que dois capítulos seguidos nunca repitam a mesma.

### A página de login

`/login` é uma tela de acesso completa sem nada por trás. O formulário é uma ilha Blazor: a
validação, o botão de mostrar a senha, o estado de carregamento e todos os avisos são estado em
C#, e o instrumento de gateway ao lado desenha até onde a tentativa atual realmente chegou —
cliente, edge e então uma parada na identidade, porque não há nada ali para responder.

O `IAuthenticationGateway` é a costura em que um provedor real vai se encaixar. A implementação
de protótipo por trás dele não lê nada, não compara nada e não guarda nada; veja o
[`PrototypeAuthenticationGateway`](src/HowToSoftware.Hosting/Services/PrototypeAuthenticationGateway.cs)
para o porquê de esse ser o único comportamento honesto.

---

## Pronto para o backend real

Os dados simulados ficam atrás de interfaces, então trocá-los não mexe em nenhum componente:

| Interface | Implementação do protótipo | Implementação futura |
|---|---|---|
| `IServerPreviewService` | `MockServerPreviewService` | Cliente da API do Pterodactyl |
| `IPlanCatalogService` | `StaticPlanCatalogService` | Preços reais + gateway de pagamento |
| `IMarketingContentService` | `StaticMarketingContentService` | CMS ou banco de dados |
| `IInfrastructureContentService` | `StaticInfrastructureContentService` | Especificações e status reais dos nós |
| `IAuthenticationGateway` | `PrototypeAuthenticationGateway` | ASP.NET Core Identity, ou um provedor OIDC |

Basta trocar o registro no [`Program.cs`](src/HowToSoftware.Hosting/Program.cs) que a UI
continua funcionando. O `IServerPreviewService` já é **assíncrono e baseado em comandos**,
igual ao comportamento de uma API de painel real: a ação de energia retorna na hora com um
estado transitório e quem chamou fica consultando até a instância estabilizar.

---

## Marca e design system

Paleta: **Azul Claro + Roxo + Branco**, sobre uma base azul-marinho profunda.

As cores foram extraídas do próprio logo da HTS:

| Papel | Cor |
|---|---|
| Roxo do logo — destaque da marca, CTAs, estados ativos | `#B069FF` |
| Azul claro do logo — dados técnicos, métricas, status | `#83A8F3` |
| Branco — tipografia e hierarquia | `#FFFFFF` |
| Azul-marinho profundo — apenas o fundo | `#05060E` |

Os indicadores de status usam propositalmente **azul e roxo no lugar do verde e âmbar
habituais**, para que nada na página saia da identidade da marca.

---

## Ainda não implementado

- Credenciais de produção e configuração do Stripe Dashboard
- Contas de cliente e autenticação (a tela de acesso existe; não há nada por trás dela)
- Criação de conta e recuperação de senha
- O texto e as especificações da página de infraestrutura
- Páginas autenticadas de cobrança/Customer Portal
- Retry e diagnósticos administrativos autorizados (o laboratório real é explicitamente protegido)
- Páginas legais (Termos, Privacidade, Uso Aceitável) — aparecem como "Soon"
- Um endereço de contato real (hoje é um placeholder no `appsettings.json`)

---

## Licença

Veja [LICENSE](LICENSE). Este é um **software proprietário** —
© 2026 Henry Lawrence Cahill. Todos os direitos reservados.
