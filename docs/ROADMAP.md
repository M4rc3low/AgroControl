# Roadmap do AgroControl

## Sprint 0 — Fundação

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

## Sprint 1 — Identity + Organizations

- [ ] configurar Entity Framework Core;
- [ ] criar `AgroControlDbContext`;
- [ ] configurar PostgreSQL;
- [ ] migration inicial;
- [ ] Organization;
- [ ] User;
- [ ] membership usuário-organização;
- [ ] papéis e permissões;
- [ ] autenticação;
- [ ] entitlement de módulos;
- [ ] auditoria básica.

## Sprint 2 — Produção Rural

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
