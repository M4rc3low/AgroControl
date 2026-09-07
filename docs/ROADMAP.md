# Roadmap do AgroControl

## Sprint 0 — Fundação ✅

Objetivo: deixar o projeto pronto para evolução controlada.

- [x] definir arquitetura;
- [x] definir stack;
- [x] criar estrutura do monorepo;
- [x] criar solução C#;
- [x] criar catálogo inicial de módulos;
- [x] health endpoint;
- [x] Dockerfile da API;
- [x] PostgreSQL no Docker Compose;
- [x] documentação inicial;
- [x] CI inicial.

## Sprint 1 — Identity + Organizations ✅

- [x] configurar Entity Framework Core;
- [x] criar `AgroControlDbContext`;
- [x] configurar PostgreSQL;
- [x] migration inicial e model snapshot;
- [x] design-time DbContext factory;
- [x] Organization;
- [x] User;
- [x] membership usuário-organização;
- [x] papéis iniciais;
- [x] autenticação JWT;
- [x] hash de senha com PBKDF2-HMAC-SHA512;
- [x] planos Basic/Pro/Intelligence/Enterprise;
- [x] entitlement de módulos;
- [x] overrides de módulo por organização;
- [x] bloqueio backend com `403 Forbidden`;
- [x] auditoria temporal básica (`CreatedAtUtc` / `UpdatedAtUtc`);
- [x] testes unitários e CI com `dotnet test`.

## Sprint 2 — Produção Rural ✅

- [x] Farm;
- [x] Field;
- [x] Crop;
- [x] Season;
- [x] status de safra;
- [x] operações CRUD;
- [x] soft delete;
- [x] validações de área e datas;
- [x] validação da área acumulada dos talhões;
- [x] isolamento multi-tenant por `OrganizationId`;
- [x] filtro reutilizável de acesso por módulo;
- [x] filtros, busca e paginação;
- [x] migration `ProductionCore`;
- [x] testes unitários;
- [x] teste de integração com PostgreSQL real no CI.

## Sprint 3 — Estoque ⏭️

- [ ] Item de estoque;
- [ ] categorias de insumo;
- [ ] unidades de medida;
- [ ] depósitos/localizações;
- [ ] entrada;
- [ ] saída;
- [ ] ajuste;
- [ ] lote e validade quando aplicável;
- [ ] vínculo de consumo com propriedade/talhão/safra;
- [ ] saldo por propriedade e depósito;
- [ ] histórico de movimentações;
- [ ] alertas de estoque baixo;
- [ ] testes e migration.

## Sprint 4 — Financeiro

- [ ] centros de custo;
- [ ] despesas;
- [ ] receitas;
- [ ] vínculo com safra;
- [ ] custo por hectare;
- [ ] custo por unidade produzida;
- [ ] ponto de equilíbrio;
- [ ] margem estimada e realizada.

## Sprint 5 — Machinery + Market

- [ ] máquinas;
- [ ] implementos;
- [ ] horímetro;
- [ ] combustível;
- [ ] manutenção;
- [ ] preços de commodities;
- [ ] histórico;
- [ ] alertas.

## Sprint 6 — AgroControl Intelligence

- [ ] criar serviço FastAPI;
- [ ] contrato API↔Intelligence;
- [ ] análise exploratória;
- [ ] previsão inicial de produtividade;
- [ ] filas para processamento pesado, se necessário;
- [ ] visão computacional em fase posterior.

## Sprint 7 — AgroControl Telemetry

- [ ] Spring Boot;
- [ ] modelo de eventos;
- [ ] ingestão de sensores;
- [ ] MQTT;
- [ ] normalização;
- [ ] integração com máquinas e talhões.

## Sprint 8 — Plataforma / DevOps

- [ ] pipelines completos;
- [ ] imagens Docker versionadas;
- [ ] ambientes;
- [ ] secrets management;
- [ ] OpenTelemetry;
- [ ] métricas e Grafana;
- [ ] Kubernetes quando o projeto justificar a orquestração.
