# Sprint 2 — Produção Rural

## Objetivo

Implementar o núcleo operacional agrícola do AgroControl: propriedades, talhões, culturas e safras, sempre com isolamento por organização.

## Modelo de domínio

```text
Organization
  ├── Farm
  │    └── Field
  │          └── Season
  └── Crop ───────┘
```

### Farm

Representa uma propriedade rural. Armazena nome, área total em hectares, cidade, estado e estado ativo/inativo.

### Field

Representa um talhão pertencente a uma propriedade. A soma das áreas dos talhões ativos não pode superar a área total da propriedade.

### Crop

Representa uma cultura/variedade administrada pela organização, por exemplo soja — M6410 IPRO ou milho — AG8700 PRO3.

### Season

Representa a safra de um talhão para determinada cultura, contendo período, produtividade esperada/realizada e status `Planned`, `Active`, `Harvested` ou `Cancelled`.

## Multi-tenancy

Todas as entidades de produção armazenam `OrganizationId`. Todas as consultas do repositório exigem o identificador da organização vindo do token JWT. Assim, um usuário não consulta entidades pertencentes a outro tenant por simples manipulação de IDs.

## Soft delete

`DELETE` não remove fisicamente registros de produção. O registro recebe `IsActive=false`, preservando histórico. Listagens ocultam inativos por padrão e aceitam `includeInactive=true` quando necessário.

## Endpoints

Cada recurso suporta listagem, consulta por ID, criação, atualização e desativação:

- `/api/v1/farms`
- `/api/v1/fields`
- `/api/v1/crops`
- `/api/v1/seasons`

Listagens usam `page`, `pageSize` (máximo 100) e `search`. Fields também permitem `farmId`; Seasons permitem `fieldId` e `status`.

Cada grupo usa o `ModuleAccessEndpointFilter`, que valida autenticação, organização e entitlement do módulo antes da execução do endpoint.

## Persistência

A migration `20260907010000_ProductionCore` cria as tabelas:

- `farms`
- `fields`
- `crops`
- `seasons`

As tabelas possuem chaves estrangeiras, índices por organização e colunas temporais básicas.

## Testes

A Sprint 2 adiciona:

- testes de domínio para regras de área, soft delete e datas;
- teste de integração contra PostgreSQL real no GitHub Actions;
- verificação de isolamento de Farms por `OrganizationId` no repositório.

O pipeline provisiona PostgreSQL 17, aplica as migrations e executa build + testes.
