# AgroControl

**AgroControl** é uma plataforma modular para gestão e inteligência no agronegócio. O projeto começa como um **monólito modular em C# / ASP.NET Core**, mantendo pontos claros de integração com serviços especializados em **Python** (dados, IA e visão computacional) e **Java** (telemetria e IoT).

> Status atual: **Sprint 3 concluída — estoque e movimentações de insumos**

## Objetivo

Centralizar, em uma única plataforma, os principais fluxos de uma operação rural:

- gestão de propriedades e talhões;
- culturas e safras;
- estoque de insumos;
- custos e resultado financeiro;
- máquinas e manutenção;
- mercado e commodities;
- agricultura de precisão;
- análise de dados e IA;
- irrigação e sensores;
- sustentabilidade e carbono;
- exportação.

Os módulos podem ser liberados por plano. Recursos indisponíveis continuam visíveis na interface como **bloqueados** ou **em breve**, mas o bloqueio também é validado no backend.

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | React + TypeScript (planejado) |
| API principal | C# + ASP.NET Core / .NET 10 |
| Banco de dados | PostgreSQL |
| ORM | Entity Framework Core |
| Inteligência / Dados | Python + FastAPI (planejado) |
| Telemetria / IoT | Java + Spring Boot (planejado) |
| Autenticação | JWT |
| Containers | Docker / Docker Compose |
| Orquestração futura | Kubernetes |
| CI/CD | GitHub Actions |
| Observabilidade futura | OpenTelemetry + Grafana |

## Arquitetura

```mermaid
flowchart TB
    UI[React + TypeScript] --> API[AgroControl API\nC# / ASP.NET Core]
    API --> DB[(PostgreSQL)]
    API --> AI[AgroControl Intelligence\nPython / FastAPI]
    TEL[AgroControl Telemetry\nJava / Spring Boot] --> API
    SENSORS[Sensores / GPS / Estações] --> TEL
```

A API principal começa como um **monólito modular**. Python e Java só entram como serviços independentes quando houver uma justificativa técnica real.

## O que já funciona

### Plataforma e segurança

- solução .NET 10 organizada em Domain, Application, Infrastructure e API;
- PostgreSQL com Entity Framework Core e migrations;
- Organization, User e membership usuário-organização;
- papéis `Owner`, `Admin`, `Manager` e `Viewer`;
- cadastro e login com JWT;
- senha protegida com PBKDF2-HMAC-SHA512 e salt aleatório;
- planos `Basic`, `Pro`, `Intelligence` e `Enterprise`;
- entitlements e overrides por organização;
- filtro reutilizável de acesso aos módulos;
- retorno `403 Forbidden` para módulo não habilitado;
- Docker Compose com aplicação automática das migrations.

### Produção Rural

- propriedades (`Farm`);
- talhões (`Field`);
- culturas e variedades (`Crop`);
- safras (`Season`);
- CRUD com soft delete, paginação, busca e filtros;
- isolamento por `OrganizationId`;
- validação para a soma dos talhões ativos não ultrapassar a área da propriedade;
- produtividade esperada e realizada por hectare.

### Estoque

- categorias de insumo;
- itens com SKU, unidade de medida e estoque mínimo;
- depósitos, opcionalmente vinculados a propriedades;
- entradas, saídas e ajustes registrados em ledger append-only;
- saldo calculado pelo histórico de movimentações;
- bloqueio de saída quando o saldo é insuficiente;
- lote e validade opcionais com regra de rastreabilidade;
- vínculo opcional do consumo com propriedade, talhão e safra;
- consulta de estoque baixo;
- paginação e filtros de movimentações.

### Qualidade

- testes unitários de domínio e segurança;
- testes de integração com PostgreSQL real;
- CI provisionando PostgreSQL 17 e executando restore, build e test.

## Módulos e planos

### Basic

Identity, Organizations, Farms, Fields, Crops, Seasons, Inventory e Finance.

### Pro

Tudo do Basic + Machinery, Market, Precision Agriculture, Irrigation e Sustainability.

### Intelligence

Tudo do Pro + Intelligence e Telemetry.

### Enterprise

Todos os módulos, incluindo Export.

Veja [`docs/MODULES.md`](docs/MODULES.md), [`docs/SPRINT_1_IDENTITY.md`](docs/SPRINT_1_IDENTITY.md), [`docs/SPRINT_2_PRODUCTION.md`](docs/SPRINT_2_PRODUCTION.md) e [`docs/SPRINT_3_INVENTORY.md`](docs/SPRINT_3_INVENTORY.md).

## Executando com Docker

1. Copie `.env.example` para `.env`.
2. Troque `JWT_KEY` e, se desejar, as credenciais locais do PostgreSQL.
3. Execute:

```bash
docker compose up --build
```

A API ficará em `http://localhost:8080`.

## Endpoints atuais

### Plataforma e autenticação

```text
GET  /health
GET  /api/v1/platform/modules
POST /api/v1/auth/register
POST /api/v1/auth/login
GET  /api/v1/me
GET  /api/v1/organizations/current
GET  /api/v1/platform/entitlements
GET  /api/v1/platform/modules/{moduleKey}/access
```

### Produção rural

```text
/api/v1/farms
/api/v1/fields
/api/v1/crops
/api/v1/seasons
```

### Estoque

```text
/api/v1/inventory/categories
/api/v1/inventory/items
/api/v1/inventory/warehouses
/api/v1/inventory/movements
/api/v1/inventory/low-stock
```

Consulte [`docs/SPRINT_3_INVENTORY.md`](docs/SPRINT_3_INVENTORY.md) para os contratos e regras do estoque.

## Migrations

- `20260907002000_InitialIdentity`
- `20260907010000_ProductionCore`
- `20260907134514_InventoryCore`

No Docker Compose, as migrations são aplicadas automaticamente porque `Database__ApplyMigrations=true`.

## Segurança

A chave de `appsettings.Development.json` é apenas uma chave conhecida de desenvolvimento local. **Nunca use essa chave em produção.** Em ambientes reais, forneça `Jwt__Key` por secret/variável de ambiente segura.

## Roadmap resumido

1. ✅ **Sprint 0** — fundação, documentação, arquitetura, CI e containers.
2. ✅ **Sprint 1** — identidade, organizações, PostgreSQL e autorização por módulo.
3. ✅ **Sprint 2** — propriedades, talhões, culturas e safras.
4. ✅ **Sprint 3** — estoque e movimentações de insumos.
5. ⏭️ **Sprint 4** — financeiro.
6. **Sprint 5** — máquinas e mercado.
7. **Sprint 6** — serviço Python de inteligência.
8. **Sprint 7** — serviço Java de telemetria.
9. **Sprint 8** — observabilidade, CI/CD avançado e Kubernetes.

Detalhes em [`docs/ROADMAP.md`](docs/ROADMAP.md).

## Licença

A licença ainda não foi definida. Não adicione uma licença pública ao projeto sem decidir antes o modelo de distribuição do AgroControl.
