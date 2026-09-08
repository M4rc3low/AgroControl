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
- [x] planos e entitlements;
- [x] bloqueio backend por módulo;
- [x] testes e CI.

## Sprint 2 — Produção Rural ✅

- [x] Farm, Field, Crop e Season;
- [x] CRUD, soft delete, filtros e paginação;
- [x] validações de área, datas e produtividade;
- [x] isolamento multi-tenant;
- [x] migration `ProductionCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 3 — Estoque ✅

- [x] itens, categorias, depósitos, SKU e unidades;
- [x] entradas, saídas e ajustes em ledger append-only;
- [x] bloqueio de saldo negativo;
- [x] lote, validade e vínculo com produção;
- [x] saldo e alertas de estoque baixo;
- [x] migration `InventoryCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 4 — Financeiro ✅

- [x] categorias e centros de custo;
- [x] despesas, receitas, contas a pagar e receber;
- [x] competência, vencimento e liquidação;
- [x] vínculo com propriedade/talhão/safra;
- [x] fluxo de caixa, resultado, margem e rentabilidade;
- [x] custo por hectare/unidade e ponto de equilíbrio;
- [x] migration `FinanceCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 5 — Machinery + Market ✅

- [x] máquinas, status, horímetro e combustível;
- [x] manutenção preventiva/corretiva e custos;
- [x] commodities e histórico de cotações;
- [x] variação e alertas por preço-alvo;
- [x] contrato para provedores externos;
- [x] migration `MachineryMarketCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 6 — AgroControl Intelligence ✅

- [x] FastAPI e contrato HTTP versionado;
- [x] baseline e regressão Ridge;
- [x] MAE/RMSE e comportamento `insufficient_data`;
- [x] cliente HTTP C# e isolamento multi-tenant;
- [x] timeout e tratamento de indisponibilidade;
- [x] Docker, CI Python e documentação.

## Sprint 7 — AgroControl Telemetry ✅

- [x] Java 21 + Spring Boot e PostgreSQL próprio;
- [x] dispositivos por organização;
- [x] eventos append-only e idempotência;
- [x] MQTT QoS 1 e tópico versionado;
- [x] última leitura e histórico;
- [x] integração C# ↔ Java e credencial interna;
- [x] entitlement, Docker, OpenAPI e CI MQTT ponta a ponta.

## Sprint 8 — Plataforma / DevOps ✅

- [x] OpenTelemetry nos três serviços;
- [x] Prometheus, Tempo e Grafana;
- [x] liveness/readiness;
- [x] Platform CI integrado;
- [x] Dependabot e CodeQL;
- [x] GHCR com SHA/SemVer, provenance e SBOM;
- [x] Kubernetes + Kustomize, probes, resources, security context e PDB;
- [x] runbook, ADR e documentação operacional.

## Sprint 9 — AgroControl Web 🚀

- [x] React 19 + TypeScript 7 + Vite 8;
- [x] React Router e rotas protegidas;
- [x] cliente HTTP tipado e ProblemDetails;
- [x] registro, login, logout e sessão JWT;
- [x] organização, papel, plano e entitlements no contexto da UI;
- [x] app shell e design system responsivos;
- [x] dashboard com dados reais de produção, estoque e financeiro;
- [x] CRUD web de Farm, Field, Crop e Season;
- [x] páginas de entrada e estados bloqueados dos demais módulos;
- [x] `package-lock.json` e `npm ci` para builds reproduzíveis;
- [x] Frontend CI com type-check, testes, build, imagem e smoke test;
- [x] CodeQL para JavaScript/TypeScript e Dependabot npm;
- [x] Nginx unprivileged e proxy same-origin para a API;
- [x] Docker Compose com serviço web;
- [x] Platform CI validando web e proxy `/api`;
- [x] imagem `agrocontrol-web` incluída no pipeline GHCR;
- [x] Kubernetes com Deployment, Service, probes, resources e PDB para web;
- [x] documentação da Sprint 9.

## Próximos aprimoramentos

O hardening operacional que depende de políticas, ambiente real ou testes de carga continua no issue #18. Evoluções de produto podem aprofundar Estoque/Financeiro/Machinery/Market/Intelligence/Telemetry no frontend, adicionar agricultura de precisão e, quando houver justificativa real, avançar para PWA/offline ou aplicações móveis.
