# Segurança e checklist de produção

Este documento separa o que o aplicativo já garante do que precisa ser configurado na
infraestrutura. Nenhuma aplicação isolada consegue prometer “segurança máxima” ou absorver um
DDoS volumétrico; a defesa é feita em camadas e precisa ser monitorada continuamente.

## Controles dentro do aplicativo

- **Frontend não é autoridade:** plano, período e preço são resolvidos novamente no servidor. O
  webhook assinado do Stripe confere moeda e valor antes do provisionamento.
- **SQL injection:** acesso ao banco é feito por EF Core/LINQ com parâmetros. Não há SQL montado a
  partir de campos do navegador; o SQL bruto existente é uma migration estática versionada.
- **XSS e clickjacking:** Razor codifica conteúdo por padrão; não há `MarkupString`, `Html.Raw`,
  `eval` ou `innerHTML`. A CSP usa nonce aleatório por resposta e bloqueia scripts remotos,
  objetos, `base` externo e enquadramento por outro site.
- **CSRF:** formulários mutáveis usam antiforgery. O webhook é a única exceção e usa a assinatura
  Stripe sobre o corpo original.
- **Abuso automatizado:** status de pedido, webhook e health check têm limites por IP e não
  enfileiram excesso. O corpo do webhook e os limites gerais do Kestrel também são limitados.
- **Dados e logs:** EF Core não registra valores sensíveis; respostas públicas de status não
  incluem preço, email, ID interno do pedido nem identificador do servidor.
- **Superfície de laboratório:** criação/remoção manual no Pterodactyl exige simultaneamente
  `Development` e a flag explícita. A flag não consegue abrir o laboratório em staging/produção.
- **Saídas externas:** redirects automáticos do cliente Pterodactyl estão desligados, a URL exige
  HTTPS e a chave é colocada somente no header `Authorization` do backend.
- **Transporte do banco:** a conexão SQL Server força `Encrypt=True` mesmo que a string informada
  desative, e a validação do certificado continua ativa a menos que o operador escreva
  `TrustServerCertificate=True` deliberadamente - o que permite man-in-the-middle.

O middleware em `Infrastructure/Security/SecurityHardening.cs` centraliza CSP, headers,
antiforgery e rate limits. Os testes em `SecurityBoundaryTests.cs` impedem que esses limites sejam
removidos silenciosamente por uma refatoração.

## HTTPS, TLS e HSTS

Em produção, o aplicativo redireciona HTTP com 308 e envia HSTS por 365 dias. Desenvolvimento
local continua em `http://localhost:5147` para não depender de certificado local. Configure a
borda pública assim:

1. TLS mínimo 1.2, preferencialmente TLS 1.3, com certificado válido e renovação automática.
2. Se houver Cloudflare/proxy, use validação estrita do certificado também entre proxy e origem;
   nunca use um modo que aceite HTTP até a origem.
3. Bloqueie acesso público direto à porta da origem; permita apenas o proxy/load balancer.
4. Só habilite HSTS `includeSubDomains` e preload depois de provar que **todos** os subdomínios
   atuais e futuros são HTTPS-only. O código mantém ambos desligados por segurança operacional.
5. Configure proxies conhecidos no ASP.NET antes de consumir forwarded headers. Nunca confie em
   `X-Forwarded-For` vindo diretamente da internet.

## E2E: o que pode e o que não pode ser prometido

O checkout de cartão fica hospedado no Stripe; o site não recebe nem armazena número completo do
cartão. Entre navegador, aplicação, Stripe, SQL Server e Pterodactyl existe TLS em trânsito.

Isso **não é criptografia end-to-end** no sentido de mensagens privadas: o backend precisa ler o
pedido para cobrar e provisionar. Chamar esse fluxo de E2E seria incorreto. Dados pessoais devem
ser minimizados, criptografados pelo provedor em repouso e acessíveis apenas a serviços/operadores
que realmente precisam deles. Campos excepcionalmente sensíveis podem ganhar criptografia de
aplicação com chaves em KMS, nunca no mesmo banco.

## WAF, DDoS e brute force

Antes de abrir produção, coloque o domínio atrás de CDN/WAF e configure:

- regras gerenciadas do OWASP Core Rule Set, inicialmente em modo de observação;
- rate limit na borda para login futuro, criação de checkout, status e rotas administrativas;
- desafio/bloqueio progressivo para bots e tentativas repetidas;
- mitigação DDoS do provedor e alertas de pico de tráfego/erros;
- webhook Stripe acessível, mas sempre sujeito à assinatura e ao limite conservador da aplicação;
- origem sem portas de banco, painel ou SSH expostas ao público.

Ainda não existe autenticação real, portanto não existe senha de cliente para sofrer brute force.
Quando contas forem implementadas, use um provedor de identidade revisado, hash de senha moderno
fornecido por ele, MFA obrigatório para administradores, respostas indistinguíveis para usuário
inexistente/senha errada, bloqueio progressivo e autorização por proprietário em **cada** leitura
ou ação de pedido/servidor. Não transforme o formulário visual atual em autenticação caseira.

## Banco e segredos

- Use um login SQL Server dedicado à aplicação, nunca `sa`, sem owner nem permissões de DDL no
  runtime. Aplique migrations com uma identidade separada.
- Dê à aplicação um banco próprio. Dois aplicativos no mesmo banco compartilham a tabela de
  histórico de migrations, e um deploy de um pode remodelar o outro.
- Guarde chaves em secret manager, habilite rotação e separe teste/staging/produção.
- Persista chaves do ASP.NET Data Protection em armazenamento privado e criptografado quando
  houver múltiplas instâncias; sem isso, reinícios invalidam tokens/cookies.
- Não envie `.env` ao Git, Discord, tickets ou capturas. O `.env.example` só contém formato.
- Configure retenção curta, acesso restrito e redaction no agregador de logs.

## Verificação antes de publicar

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet package list --project src/HowToSoftware.Hosting --vulnerable --include-transitive
```

Além disso, teste headers/TLS no domínio final, execute DAST autorizado em staging, restaure um
backup em ambiente isolado e confirme alertas para falhas de webhook, provisionamento e login.
Repita revisão de dependências e threat modeling a cada nova rota com dados de cliente.

