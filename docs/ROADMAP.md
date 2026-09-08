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

## Sprint 9 — AgroControl Web ✅

- [x] React 19 + TypeScript 7 + Vite 8;
- [x] React Router e rotas protegidas;
- [x] cliente HTTP tipado e ProblemDetails;
- [x] registro, login, logout e sessão JWT;
- [x] organização, papel, plano e entitlements no contexto da UI;
- [x] app shell e design system responsivos;
- [x] dashboard com dados reais de produção, estoque e financeiro;
- [x] CRUD web de Farm, Field, Crop e Season;
- [x] `package-lock.json` e `npm ci` para builds reproduzíveis;
- [x] Frontend CI, CodeQL JS/TS e Dependabot npm;
- [x] Nginx unprivileged, Docker Compose, GHCR e Kubernetes para a web;
- [x] documentação da Sprint 9.

## Sprint 10 — Agricultura de Precisão ✅

- [x] PostGIS no banco principal de desenvolvimento e CI;
- [x] `geography(Polygon,4326)` para limites de talhões;
- [x] índice espacial GiST;
- [x] contrato GeoJSON e validação WGS84;
- [x] validação topológica com PostGIS;
- [x] área geodésica em hectares;
- [x] diferença entre área espacial e cadastral sem alteração automática;
- [x] endpoints protegidos por `PrecisionAgriculture`;
- [x] isolamento multi-tenant;
- [x] mapa MapLibre no AgroControl Web;
- [x] desenho, redesenho e remoção de limites;
- [x] configuração de assets/style de mapa por ambiente;
- [x] testes unitários frontend/backend e integração PostGIS;
- [x] documentação da fundação espacial.

## Sprint 11 — Irrigação ✅

- [x] zonas de irrigação vinculadas a talhões da mesma organização;
- [x] área, método, status e limites mínimo/alvo/máximo de umidade;
- [x] vínculo opcional com dispositivo de telemetria;
- [x] validação de área contra o talhão;
- [x] aplicações de água append-only;
- [x] cálculo determinístico de volume em m³;
- [x] correções por registro compensatório;
- [x] métrica canônica `soil_moisture_percent` consultada no Telemetry;
- [x] classificação `Critical`, `Dry`, `Target` e `Wet`;
- [x] recomendações sem atuação autônoma em equipamentos;
- [x] API protegida por `Irrigation` e isolamento multi-tenant;
- [x] interface web responsiva para zonas, leituras e aplicações;
- [x] migration `IrrigationCore` e índices;
- [x] testes unitários e integração PostgreSQL;
- [x] documentação da Sprint 11.

## Próximos aprimoramentos

O hardening operacional que depende de políticas, ambiente real ou testes de carga continua no issue #18. As próximas evoluções funcionais podem avançar para sustentabilidade e indicadores ambientais, importação de geometrias, imagens de satélite/drone, índices vegetativos, zonas de manejo e aprofundamento agronômico do balanço hídrico, mantendo cada responsabilidade em uma sprint separada.
