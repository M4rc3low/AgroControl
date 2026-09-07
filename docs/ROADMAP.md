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

## Sprint 2 — Produção Rural ⏭️

- [ ] Farm;
- [ ] Field;
- [ ] Crop;
- [ ] Season;
- [ ] operações CRUD;
- [ ] validações;
- [ ] filtros e paginação;
- [ ] primeiros testes de integração.

## Sprint 3 — Estoque

- [ ] Item;
- [ ] categoria;
- [ ] unidade de medida;
- [ ] entrada;
- [ ] saída;
- [ ] ajuste;
- [ ] saldo por propriedade;
- [ ] alertas de estoque baixo.

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
