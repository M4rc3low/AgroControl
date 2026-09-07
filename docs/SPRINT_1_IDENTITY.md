# Sprint 1 — Identity, Organizations e acesso por módulos

## Objetivo

Criar a primeira base persistente e segura do AgroControl, com organização, usuário, membership, autenticação JWT e entitlements de módulos por plano.

## Modelo persistido

- `organizations`: tenant lógico principal;
- `users`: identidade do usuário;
- `organization_memberships`: vínculo N:N entre usuário e organização com papel;
- `subscriptions`: plano ativo da organização;
- `organization_module_entitlements`: override explícito de módulo por organização.

## Papéis iniciais

- Owner
- Admin
- Manager
- Viewer

## Planos

### Basic

Identity, Organizations, Farms, Fields, Crops, Seasons, Inventory e Finance.

### Pro

Tudo do Basic + Machinery, Market, Precision Agriculture, Irrigation e Sustainability.

### Intelligence

Tudo do Pro + Intelligence e Telemetry.

### Enterprise

Todos os módulos, incluindo Export.

## Autenticação

O cadastro inicial cria, na mesma unidade de trabalho:

1. organização;
2. usuário;
3. membership do usuário como `Owner`;
4. assinatura `Basic` ativa.

Senhas são armazenadas usando PBKDF2-HMAC-SHA512 com salt aleatório e comparação em tempo constante. A API emite JWT com `sub`, `email`, `org_id` e `role`.

## Endpoints

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `GET /api/v1/me`
- `GET /api/v1/organizations/current`
- `GET /api/v1/platform/entitlements`
- `GET /api/v1/platform/modules/{moduleKey}/access`

O último endpoint retorna `403 Forbidden` quando o módulo não está habilitado para a organização.

## Banco e migrations

A migration inicial é `20260907002000_InitialIdentity`.

No Docker Compose, `Database__ApplyMigrations=true`, portanto a API aplica migrations na inicialização após o PostgreSQL ficar saudável.

## Segurança

A chave JWT de produção deve ser fornecida via configuração segura/variável de ambiente. A chave presente em `appsettings.Development.json` é exclusivamente para desenvolvimento local e nunca deve ser usada em produção.

## Testes

A suíte inicial cobre:

- matriz de entitlements por plano;
- round-trip e rejeição de senha incorreta no password hasher.

O GitHub Actions executa restore, build e testes em cada mudança relevante do backend.
