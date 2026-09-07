# Roadmap do AgroControl

## Sprint 0 — Fundação ✅

- [x] arquitetura e stack;
- [x] monorepo e solução .NET;
- [x] catálogo de módulos;
- [x] health endpoint;
- [x] Docker/PostgreSQL;
- [x] documentação e CI inicial.

## Sprint 1 — Identity + Organizations ✅

- [x] Entity Framework Core e PostgreSQL;
- [x] `AgroControlDbContext`, migrations e design-time factory;
- [x] Organization, User e membership;
- [x] papéis e autenticação JWT;
- [x] PBKDF2-HMAC-SHA512;
- [x] planos e entitlements;
- [x] bloqueio backend por módulo;
- [x] testes e CI.

## Sprint 2 — Produção Rural ✅

- [x] Farm, Field, Crop e Season;
- [x] status de safra;
- [x] CRUD e soft delete;
- [x] validações de área e datas;
- [x] isolamento multi-tenant;
- [x] filtros, busca e paginação;
- [x] migration `ProductionCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 3 — Estoque ✅

- [x] itens com SKU;
- [x] categorias de insumo;
- [x] unidades de medida;
- [x] depósitos/localizações;
- [x] entradas e saídas;
- [x] ajustes positivos e negativos;
- [x] ledger append-only;
- [x] bloqueio de saída com saldo insuficiente;
- [x] lote e validade;
- [x] vínculo de consumo com propriedade/talhão/safra;
- [x] saldo por item e depósito;
- [x] histórico paginado e filtrável;
- [x] alertas de estoque baixo;
- [x] migration `InventoryCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 4 — Financeiro ⏭️

- [ ] centros de custo;
- [ ] despesas;
- [ ] receitas;
- [ ] contas a pagar e receber;
- [ ] vínculo com propriedade/talhão/safra;
- [ ] categorias financeiras;
- [ ] custo por hectare;
- [ ] custo por unidade produzida;
- [ ] ponto de equilíbrio;
- [ ] margem estimada e realizada;
- [ ] integração futura com consumo de estoque.

## Sprint 5 — Machinery + Market

- [ ] máquinas e implementos;
- [ ] horímetro e combustível;
- [ ] manutenção;
- [ ] preços de commodities;
- [ ] histórico e alertas.

## Sprint 6 — AgroControl Intelligence

- [ ] FastAPI;
- [ ] contrato API↔Intelligence;
- [ ] análise exploratória;
- [ ] previsão inicial de produtividade;
- [ ] processamento assíncrono quando necessário;
- [ ] visão computacional em fase posterior.

## Sprint 7 — AgroControl Telemetry

- [ ] Spring Boot;
- [ ] modelo de eventos;
- [ ] sensores e MQTT;
- [ ] normalização;
- [ ] integração com máquinas e talhões.

## Sprint 8 — Plataforma / DevOps

- [ ] pipelines completos e imagens versionadas;
- [ ] ambientes e secrets management;
- [ ] OpenTelemetry, métricas e Grafana;
- [ ] Kubernetes quando o projeto justificar a orquestração.
