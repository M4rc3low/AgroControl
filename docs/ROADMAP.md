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

- [x] itens com SKU e unidades de medida;
- [x] categorias e depósitos;
- [x] entradas, saídas e ajustes;
- [x] ledger append-only;
- [x] bloqueio de saída com saldo insuficiente;
- [x] lote e validade;
- [x] vínculo de consumo com propriedade/talhão/safra;
- [x] saldo por item e depósito;
- [x] histórico e alertas de estoque baixo;
- [x] migration `InventoryCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 4 — Financeiro ✅

- [x] categorias financeiras;
- [x] centros de custo;
- [x] despesas e receitas;
- [x] contas a pagar e receber por status;
- [x] competência, vencimento e liquidação;
- [x] vínculo com propriedade/talhão/safra;
- [x] visão por competência e fluxo de caixa;
- [x] resumo de receitas, despesas, resultado e margem;
- [x] custo por hectare;
- [x] custo por unidade produzida;
- [x] ponto de equilíbrio por unidade;
- [x] isolamento multi-tenant e bloqueio pelo módulo `Finance`;
- [x] migration `FinanceCore`;
- [x] testes unitários e integração PostgreSQL;
- [x] ponto de integração futura com consumo de estoque documentado.

## Sprint 5 — Machinery + Market ⏭️

- [ ] máquinas e implementos;
- [ ] horímetro e combustível;
- [ ] manutenção preventiva e corretiva;
- [ ] custos operacionais de máquinas;
- [ ] preços de commodities;
- [ ] histórico de preços;
- [ ] alertas de mercado.

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
