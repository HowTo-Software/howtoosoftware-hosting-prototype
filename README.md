# HowToSoftware — Project Zomboid Hosting (Frontend Prototype)

A frontend prototype for the HowToSoftware **Project Zomboid server hosting** platform,
built with **C# / .NET 10 / Blazor**.

> 🇧🇷 **A versão em português está mais abaixo** — veja [Português (BR)](#português-br).

---

> ### ⚠️ This is a prototype, not a product
>
> - **All pricing is placeholder test data.** The plans, prices, discounts and promotional
>   codes exist so the interface can be reviewed. **Nothing here is a commercial offer.**
> - **No checkout, no payments, no accounts, no database.** There is no billing provider
>   connected and nothing is charged.
> - **The control panel is a mock.** It looks and behaves like the real thing, but no server
>   is ever created, started or stopped.
> - **No game panel integration yet.** Pterodactyl is the intended target and the code is
>   structured for it, but it is not wired up.

---

## Screenshots

### Dark theme (default)
![Hero, dark theme](docs/hero-dark.png)

### Light theme
![Hero, light theme](docs/hero-light.png)

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
dotnet test           # run the 42 unit tests
```

There is nothing else to install. No npm, no database, no configuration, no API keys.

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

### About the JavaScript

There is exactly **one 177-line dependency-free JS file**
([`wwwroot/js/site.js`](src/HowToSoftware.Hosting/wwwroot/js/site.js)). It only reports
browser facts that C# cannot observe: scroll position, element visibility, and the saved
theme. **It holds no application state.**

The provisioning section is the clearest example of the split: JavaScript notices which
stage scrolled into the middle of the screen and calls a `[JSInvokable]` C# method. Blazor
then decides everything visible — which stage is active, which diagram nodes light up, the
step counter. **JavaScript observes; Blazor decides.**

Without JavaScript the site still works: all content is visible, only the entrance
animations are skipped. The site also fully respects `prefers-reduced-motion`.

---

## Project structure

```
src/HowToSoftware.Hosting/
├── Components/
│   ├── Layout/       SiteHeader, SiteFooter, MainLayout
│   ├── Shared/       BrandMark, CtaButton, Icon, ThemeToggle, SectionHeading
│   ├── Home/         one component per page section (16 of them)
│   └── Pages/        Home, Error, NotFound
├── Models/           strongly-typed records (plans, telemetry, server state, …)
├── Services/         interfaces + the static/mock implementations behind them
└── wwwroot/
    ├── css/theme.css design tokens — the single source of truth for both themes
    ├── css/app.css    reset, typography, layout primitives
    └── js/site.js     the only JavaScript

tests/HowToSoftware.Hosting.Tests/   42 unit tests
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

---

## Ready for the real backend

The mock data sits behind interfaces, so replacing it does not touch a single component:

| Interface | Prototype implementation | Future implementation |
|---|---|---|
| `IServerPreviewService` | `MockServerPreviewService` | Pterodactyl API client |
| `IPlanCatalogService` | `StaticPlanCatalogService` | Real pricing + billing provider |
| `IMarketingContentService` | `StaticMarketingContentService` | CMS or database |

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

- Payments, checkout and real pricing
- Customer accounts and authentication
- Pterodactyl / game panel integration
- Database and server provisioning
- Legal pages (Terms, Privacy, Acceptable Use) — shown as "Soon" placeholders
- A real contact address (currently a placeholder in `appsettings.json`)

---

## Licence

See [LICENSE](LICENSE). This is **proprietary software** —
© 2026 Henry Lawrence Cahill. All rights reserved.

---
---

# Português (BR)

Protótipo de frontend para a plataforma de **hospedagem de servidores de Project Zomboid**
da HowToSoftware, feito em **C# / .NET 10 / Blazor**.

---

> ### ⚠️ Isto é um protótipo, não um produto
>
> - **Todos os preços são dados de teste.** Os planos, preços, descontos e cupons existem
>   apenas para avaliar a interface. **Nada aqui é uma oferta comercial.**
> - **Sem checkout, sem pagamentos, sem contas, sem banco de dados.** Não há nenhum gateway
>   conectado e nada é cobrado.
> - **O painel de controle é simulado.** Ele se parece e se comporta como o real, mas nenhum
>   servidor é criado, iniciado ou parado.
> - **Ainda não há integração com painel de jogo.** O Pterodactyl é o alvo pretendido e o
>   código está estruturado para isso, mas não está conectado.

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
dotnet test           # roda os 42 testes unitários
```

Não precisa instalar mais nada. Sem npm, sem banco de dados, sem configuração, sem chaves.

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

### Sobre o JavaScript

Existe exatamente **um arquivo JS de 177 linhas, sem dependências**
([`wwwroot/js/site.js`](src/HowToSoftware.Hosting/wwwroot/js/site.js)). Ele só reporta fatos
do navegador que o C# não consegue observar: posição de scroll, visibilidade de elementos e
o tema salvo. **Ele não guarda nenhum estado da aplicação.**

A seção de provisionamento é o exemplo mais claro dessa divisão: o JavaScript percebe qual
estágio entrou no meio da tela e chama um método C# marcado com `[JSInvokable]`. O Blazor
então decide tudo que aparece — qual estágio está ativo, quais nós do diagrama acendem, o
contador de etapa. **O JavaScript observa; o Blazor decide.**

Sem JavaScript o site continua funcionando: todo o conteúdo aparece, só as animações de
entrada são puladas. O site também respeita `prefers-reduced-motion`.

---

## Estrutura do projeto

```
src/HowToSoftware.Hosting/
├── Components/
│   ├── Layout/       SiteHeader, SiteFooter, MainLayout
│   ├── Shared/       BrandMark, CtaButton, Icon, ThemeToggle, SectionHeading
│   ├── Home/         um componente por seção da página (16 no total)
│   └── Pages/        Home, Error, NotFound
├── Models/           records fortemente tipados (planos, telemetria, estado do servidor…)
├── Services/         interfaces + as implementações estáticas/simuladas
└── wwwroot/
    ├── css/theme.css tokens de design — fonte única de verdade dos dois temas
    ├── css/app.css    reset, tipografia, primitivos de layout
    └── js/site.js     o único JavaScript

tests/HowToSoftware.Hosting.Tests/   42 testes unitários
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

---

## Pronto para o backend real

Os dados simulados ficam atrás de interfaces, então trocá-los não mexe em nenhum componente:

| Interface | Implementação do protótipo | Implementação futura |
|---|---|---|
| `IServerPreviewService` | `MockServerPreviewService` | Cliente da API do Pterodactyl |
| `IPlanCatalogService` | `StaticPlanCatalogService` | Preços reais + gateway de pagamento |
| `IMarketingContentService` | `StaticMarketingContentService` | CMS ou banco de dados |

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

- Pagamentos, checkout e preços reais
- Contas de cliente e autenticação
- Integração com Pterodactyl / painel de jogo
- Banco de dados e provisionamento de servidores
- Páginas legais (Termos, Privacidade, Uso Aceitável) — aparecem como "Soon"
- Um endereço de contato real (hoje é um placeholder no `appsettings.json`)

---

## Licença

Veja [LICENSE](LICENSE). Este é um **software proprietário** —
© 2026 Henry Lawrence Cahill. Todos os direitos reservados.
