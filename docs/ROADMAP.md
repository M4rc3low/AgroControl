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

## Sprint 5 — Machinery + Market ✅

- [x] máquinas e implementos;
- [x] status operacional e vínculo opcional com propriedade;
- [x] histórico de horímetro sem regressão;
- [x] abastecimentos e custo de combustível;
- [x] manutenção preventiva e corretiva;
- [x] próxima manutenção por data e/ou horímetro;
- [x] custos acumulados e custo por hora rastreada;
- [x] commodities por organização;
- [x] histórico append-only de cotações;
- [x] última cotação e variação absoluta/percentual;
- [x] alertas por preço-alvo;
- [x] contrato para provedores externos de cotação;
- [x] isolamento multi-tenant e bloqueio pelos módulos `Machinery` e `Market`;
- [x] migration `MachineryMarketCore`;
- [x] testes unitários e integração PostgreSQL.

## Sprint 6 — AgroControl Intelligence ✅

- [x] serviço Python com FastAPI;
- [x] contrato HTTP versionado `v1` entre C# e Python;
- [x] health check e informações do modelo;
- [x] pipeline determinístico de preparação e inferência;
- [x] baseline por produtividade esperada e média histórica;
- [x] regressão Ridge comparada ao baseline com MAE/RMSE;
- [x] comportamento explícito para dados insuficientes;
- [x] cliente HTTP tipado no backend C#;
- [x] isolamento multi-tenant antes da chamada ao Python;
- [x] timeout e indisponibilidade tratados separadamente;
- [x] Dockerfile e Docker Compose;
- [x] CI Python com Ruff e pytest;
- [x] testes C# e integração PostgreSQL;
- [x] documentação de arquitetura, contrato e limites do modelo.

## Sprint 7 — AgroControl Telemetry ✅

- [x] Java 21 + Spring Boot;
- [x] serviço independente em `src/telemetry`;
- [x] PostgreSQL próprio do serviço;
- [x] registro de dispositivos por organização;
- [x] vínculos opcionais com máquina, propriedade e talhão;
- [x] modelo de eventos append-only;
- [x] idempotência por dispositivo + eventId;
- [x] normalização e validação de eventos;
- [x] Eclipse Mosquitto no ambiente de desenvolvimento;
- [x] consumidor MQTT com QoS 1 e reconexão;
- [x] convenção de tópicos versionada;
- [x] última leitura e histórico por métrica;
- [x] integração C# ↔ Java por cliente HTTP tipado;
- [x] autenticação interna serviço-a-serviço;
- [x] tenant definido pelo dispositivo/usuário, não pelo payload;
- [x] proteção pelo módulo `Telemetry`;
- [x] Dockerfile e Docker Compose;
- [x] OpenAPI / Swagger UI;
- [x] CI Java, PostgreSQL real e teste MQTT ponta a ponta;
- [x] documentação de arquitetura e segurança.

## Sprint 8 — Plataforma / DevOps ⏭️

- [ ] pipelines completos e imagens versionadas;
- [ ] ambientes e secrets management;
- [ ] OpenTelemetry, métricas e Grafana;
- [ ] Kubernetes quando o projeto justificar a orquestração.
