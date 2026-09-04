# README do projeto

Este guia é o mapa de navegação do repositório que serve o site local em
`http://localhost:5147`: **`howtoosoftware-hosting-prototype`**.  Use-o como ponto de
partida antes de alterar páginas, preços, pagamentos ou infraestrutura.

## Começar e validar

Na raiz do repositório:

```powershell
Copy-Item .env.example .env
dotnet run --project src/HowToSoftware.Hosting --launch-profile http
```

O site abre em `http://localhost:5147`. O modo local funciona sem Stripe, Supabase e
Pterodactyl preenchidos; nesses casos, pagamento e provisionamento continuam desativados de
forma explícita. Para validar alterações, rode `dotnet build` e `dotnet test` na raiz.

> `.env` é secreto e está ignorado pelo Git. Versione apenas o `.env.example`, com valores de
> exemplo, nunca chaves reais.

## Mapa rápido

```text
.
|- .env.example                         configuração documentada, sem segredos
|- README.md                             visão geral e início rápido
|- docs/                                 guias operacionais e de arquitetura
|- src/HowToSoftware.Hosting/           aplicação web .NET/Blazor
|  |- Components/                       páginas, layout e componentes de interface
|  |- Data/                             SQLite local, PostgreSQL/Supabase e migrations EF
|  |- Endpoints/                        endpoints HTTP, inclusive webhook Stripe
|  |- Infrastructure/                   integrações e configuração externa
|  |- Localization/                     textos em inglês e português
|  |- Models/                           contratos e regras de domínio
|  |- Services/                         catálogo, pedidos, cobrança e provisionamento
|  `- wwwroot/                          CSS, JavaScript, fontes e imagens públicas
`- tests/HowToSoftware.Hosting.Tests/   testes automatizados
```

## Interface e páginas

- `Components/Pages/Home.razor`: página inicial e seções de marketing.
- `Components/Pages/GameHosting.razor`: catálogo de jogos.
- `Components/Pages/ProjectZomboid.razor` e `.razor.css`: página comercial do Project
  Zomboid, plano, arte e animações visuais.
- `Components/Pages/PlanReview.razor`: revisão de jogo, plano e período antes do Checkout.
- `Components/Pages/PaymentSuccess.razor` e `PaymentCancel.razor`: retornos visuais do Stripe.
- `Components/Layout/`: navegação, layout principal e rodapé. O ticker contínuo do rodapé está
  em `SiteFooter.razor`/`.css` e respeita `prefers-reduced-motion`.
- `Components/Shop/`: cards e capas usados pelo catálogo.
- `wwwroot/images/games/`: banners dos jogos do catálogo. A arte específica do Zomboid também
  está documentada em `wwwroot/images/zomboid/README.md`.

Os estilos isolados (`Arquivo.razor.css`) pertencem ao componente de mesmo nome. Prefira-os para
ajustes de uma página; use `wwwroot` apenas para estilos/recursos realmente compartilhados.

### Sistema de movimento

Para manter o efeito visual sem pesar no navegador, o scroll mede apenas cenas próximas ao
viewport, mutações de DOM são agrupadas em um único frame e o efeito `rise` cria um nó por
palavra. Somente `flicker`/`sweep`, que dependem disso visualmente, criam um nó por letra.

O sistema global está em `wwwroot/js/site.js` e `wwwroot/css/app.css`. Ele é declarativo e não
depende de uma biblioteca pesada:

- `data-motion="section"` monta uma seção na ordem label → título → texto → visual → detalhes.
- `data-motion="scene" data-scene` também permite que marcadores técnicos cedam espaço quando a
  próxima cena se aproxima.
- `data-motion-step`, `data-motion-draw`, `data-motion-stagger` e `data-text-effect` descrevem
  os papéis de cada elemento; o JavaScript só observa e aplica estados.
- `data-parallax` desloca seletivamente mídia/diagramas próximos ao viewport; `data-count`
  anima métricas não monetárias.

O ticker da Home, do Project Zomboid e do rodapé usa trilhas duplicadas e `transform` linear para
um loop contínuo sem salto. Toda essa camada é progressiva: sem JavaScript o conteúdo continua
visível; com `prefers-reduced-motion`, revelações, scroll-linked motion e loops decorativos são
reduzidos ou desligados.

## Catálogo, planos e preços

- `Services/StaticGameCatalogService.cs`: jogos disponíveis e seus caminhos de compra.
- `Services/StaticPlanCatalogService.cs`: tiers do Project Zomboid e recursos de cada plano.
- `Models/HostingPlanPricingOptions.cs`: regra de preço, descontos por período e validação.
- `Services/OrderPricingService.cs`: recalcula o valor no servidor; o navegador nunca decide o
  preço final.
- `appsettings.json` → seção `HostingPlans`: rate card e substituições temporárias de preço.

O desconto anual atual é **10%**. Enquanto o painel administrativo/Discord ainda não usa o banco
como fonte dos preços, alterações comerciais controladas ficam no `HostingPlans` ou em variáveis
de ambiente equivalentes. A futura migração para SQL deve manter `OrderPricingService` como o
único lugar que autoriza o total cobrado.

## Pagamento, pedido e provisionamento

O fluxo é deliberadamente separado para evitar que uma página do navegador possa marcar algo
como pago:

```text
Plano escolhido -> OrderPricingService -> pedido interno -> Stripe Checkout
      -> webhook Stripe validado -> fila de fulfillment -> Pterodactyl -> servidor ativo
```

- `Endpoints/PaymentEndpoints.cs`: início do Checkout e recepção do webhook.
- `Services/Payments/`: criação de sessão Stripe, validação de eventos e registros de cobrança.
- `Services/Orders/`: criação, leitura e persistência de pedidos.
- `Services/Provisioning/`: fila, worker e integração de criação no Pterodactyl.
- `Infrastructure/Stripe/` e `Infrastructure/Pterodactyl/`: opções e clientes HTTP seguros.

O redirecionamento de sucesso do Stripe apenas informa o resultado ao visitante. Só um webhook
assinado, com valor conferido, pode enviar o pedido à fila de provisionamento.

## Banco de dados

Sem Supabase configurado, `Data/HostingDbContext.cs` usa SQLite local (`hosting.db`) para o modo
de desenvolvimento. Com `SUPABASE_DB_CONNECTION_STRING`, o comércio usa PostgreSQL/Supabase:

- `Data/CommerceDbContext.cs`: entidades de clientes, pedidos, eventos, cobranças e estado de
  provisionamento.
- `Data/CommerceMigrations/`: migrations exclusivas do schema de comércio no Supabase.
- `Data/Migrations/`: migrations do fallback SQLite.
- `Services/Orders/PostgresOrderStore.cs`: persistência de pedidos no PostgreSQL.
- `Services/Payments/IBillingStore.cs` e `Services/Provisioning/IProvisioningStateStore.cs`:
  contratos e implementações persistentes para cobrança e provisionamento.

Para aplicar migrations reais, após preencher o `.env`:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-commerce --seed-commerce
```

Leia [`SUPABASE-SETUP.md`](SUPABASE-SETUP.md) antes: ele cobre acesso, RLS e a diferença entre
chaves publicáveis e segredos de servidor.

## Configuração e segredos

`.env.example` contém todas as variáveis atualmente lidas pelo aplicativo ou necessárias para
operá-lo:

| Grupo | Para que serve | Onde é usado |
|---|---|---|
| `STRIPE_*` | Checkout, webhook, URLs de retorno e moeda | `Infrastructure/Stripe/`, `Services/Payments/` |
| `SUPABASE_*` | banco PostgreSQL e futuras chamadas Data API | `Infrastructure/Supabase/`, `Data/CommerceDbContext.cs` |
| `PTERODACTYL_*` | painel, chave Application e topologia do servidor | `Infrastructure/Pterodactyl/`, `Services/Provisioning/` |
| `APP_*` | URL pública e ambiente ASP.NET | `Models/SiteOptions.cs`, inicialização em `Program.cs` |
| `ConnectionStrings__Hosting` | SQLite local quando Supabase não está ativo | `Data/HostingDbContext.cs` |
| `HostingPlans__*` | override opcional de rate card/preço | `Models/HostingPlanPricingOptions.cs` |

`Infrastructure/Configuration/EnvironmentFile.cs` carrega o `.env` somente em
desenvolvimento/local e traduz os nomes portáteis para a configuração padrão do ASP.NET Core.
Em produção, prefira variáveis do host ou um cofre de segredos. Não use `SUPABASE_SECRET_KEY`,
`STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` nem `PTERODACTYL_APPLICATION_API_KEY` no browser.

## Documentação por tarefa

- [`COMMERCE-ARCHITECTURE.md`](COMMERCE-ARCHITECTURE.md): limites de segurança e fluxo completo.
- [`SUPABASE-SETUP.md`](SUPABASE-SETUP.md): conexão do PostgreSQL, migrations e segurança.
- [`stripe-testing.md`](stripe-testing.md): Checkout, Stripe CLI e teste de webhooks.
- [`PTERODACTYL-SETUP.md`](PTERODACTYL-SETUP.md): painel, egg e laboratório de provisionamento.
- [`SECURITY-HARDENING.md`](SECURITY-HARDENING.md): fronteiras do backend, headers, TLS, WAF,
  DDoS, banco, segredos e checklist de produção.

## Ordem segura para ligar produção

1. Configure domínio/HTTPS e `APP_BASE_URL`.
2. Conecte Supabase, aplique as migrations e confirme o health check.
3. Cadastre produtos/Price IDs Stripe (ou mantenha preços recorrentes calculados) e teste em modo
   teste com Stripe CLI.
4. Configure Pterodactyl primeiro no laboratório de desenvolvimento, com uma chave `ptla_` de
   Application e um egg validado.
5. Só depois habilite credenciais de produção e implemente identidade/autorização antes de expor
   páginas de conta, faturas ou controles de servidor ao cliente.

O projeto não contém uma conta demo, senha, nem chave real. Essa ausência é intencional.
