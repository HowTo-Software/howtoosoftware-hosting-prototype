# Política de segurança

## Como reportar

Não abra uma issue pública com chaves, dados pessoais, URLs internas ou passos de exploração.
Envie o relato para `henry.cahill@howtoosoftware.com` com o assunto começando por `[SECURITY]` e
inclua apenas o necessário para reproduzir o problema. Credenciais encontradas devem ser
revogadas; não as use para validar o impacto.

## Escopo atual

A versão mantida é a versão mais recente da branch principal. A tela de login ainda é apenas uma
interface fechada: ela não autentica ninguém e não persiste senhas. Contas, painel do cliente e
qualquer rota administrativa só podem ser publicados depois de existir identidade real,
autorização por recurso e auditoria.

## Regras do repositório

- `.env`, bancos locais, certificados, dumps e arquivos de publicação são ignorados pelo Git.
- Chaves Stripe, Supabase e Pterodactyl são exclusivas do servidor e devem vir do cofre de
  segredos/ambiente da hospedagem.
- Nunca publique logs, capturas ou exports que contenham dados de clientes.
- Ao suspeitar de vazamento, revogue e substitua a credencial antes de investigar a causa.

O checklist completo de implantação está em [`docs/SECURITY-HARDENING.md`](docs/SECURITY-HARDENING.md).

