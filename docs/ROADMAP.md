# Roadmap do AgroControl

Este roadmap registra o estado funcional consolidado do projeto. Detalhes técnicos das sprints mais recentes ficam nos documentos específicos em `docs/`.

## Sprint 0 — Fundação ✅

- [x] arquitetura e stack;
- [x] monorepo e solução .NET;
- [x] health endpoint;
- [x] Docker/PostgreSQL;
- [x] documentação e CI inicial.

## Sprint 1 — Identity + Organizations ✅

- [x] autenticação JWT;
- [x] Organization, User e membership;
- [x] papéis organizacionais;
- [x] planos, módulos e entitlements;
- [x] isolamento por `OrganizationId`;
- [x] testes e CI.

## Sprint 2 — Produção Rural ✅

- [x] Farm, Field, Crop e Season;
- [x] CRUD, soft delete, filtros e paginação;
- [x] validações de área, período e produtividade;
- [x] persistência PostgreSQL e testes de integração.

## Sprint 3 — Estoque ✅

- [x] categorias, itens, depósitos e unidades;
- [x] entradas, saídas e ajustes em ledger;
- [x] lote, validade e vínculo com produção;
- [x] saldo e alertas de estoque baixo;
- [x] testes e migration.

## Sprint 4 — Financeiro ✅

- [x] categorias e centros de custo;
- [x] despesas, receitas, contas a pagar e receber;
- [x] competência, vencimento e liquidação;
- [x] fluxo de caixa, resultado, margem e rentabilidade;
- [x] custo por hectare/unidade e ponto de equilíbrio.

## Sprint 5 — Máquinas + Mercado ✅

- [x] máquinas, horímetro, combustível e manutenção;
- [x] manutenção preventiva/corretiva e custos;
- [x] commodities e histórico de cotações;
- [x] variação e alertas de preço.

## Sprint 6 — AgroControl Intelligence ✅

- [x] FastAPI e contrato HTTP versionado;
- [x] baseline e regressão Ridge;
- [x] MAE/RMSE;
- [x] `insufficient_data` para histórico insuficiente;
- [x] cliente HTTP C# e tratamento de indisponibilidade;
- [x] Docker, testes Python e CI.

## Sprint 7 — AgroControl Telemetry ✅

- [x] Java 21 + Spring Boot;
- [x] PostgreSQL próprio;
- [x] dispositivos por organização;
- [x] eventos append-only e idempotência;
- [x] MQTT QoS 1;
- [x] última leitura e histórico;
- [x] integração C# ↔ Java e CI MQTT.

## Sprint 8 — Plataforma / DevOps ✅

- [x] OpenTelemetry nos serviços;
- [x] Prometheus, Tempo e Grafana;
- [x] liveness/readiness;
- [x] Platform CI integrado;
- [x] Dependabot e CodeQL;
- [x] GHCR, provenance e SBOM;
- [x] Kubernetes + Kustomize.

## Sprint 9 — AgroControl Web ✅

- [x] React + TypeScript + Vite;
- [x] autenticação e rotas protegidas;
- [x] cliente HTTP tipado;
- [x] app shell responsivo;
- [x] dashboard operacional;
- [x] CRUD de produção rural;
- [x] módulos/entitlements no frontend;
- [x] Frontend CI e container Nginx.

## Sprint 10 — Agricultura de Precisão ✅

- [x] PostGIS;
- [x] limites de talhão WGS84;
- [x] GeoJSON e validação topológica;
- [x] índice espacial GiST;
- [x] cálculo geodésico de área;
- [x] MapLibre no AgroControl Web;
- [x] testes frontend/backend e integração PostGIS.

## Sprint 11 — Irrigação ✅

- [x] zonas de irrigação vinculadas a talhões;
- [x] métodos, status e limites de umidade;
- [x] integração com Telemetry;
- [x] aplicações de água append-only;
- [x] classificação hídrica e recomendações sem atuação autônoma;
- [x] interface web e testes.

## Sprint 12 — Sustentabilidade ✅

- [x] fatores e atividades de emissão;
- [x] snapshot do fator utilizado;
- [x] `kgCO2e` e `tCO2e`;
- [x] qualidade do dado;
- [x] vínculos com Farm/Field/Season;
- [x] indicadores e resumos gerenciais;
- [x] interface web e testes.

## Sprint 13 — Exportação ✅

- [x] pedidos de exportação;
- [x] moeda e snapshot cambial;
- [x] Incoterms e lifecycle;
- [x] timeline, documentos e custos;
- [x] vínculos com produção;
- [x] resumo por status/país/moeda;
- [x] interface web e testes.

## Sprint 14 — Comercial / CRM ✅

- [x] clientes e contatos;
- [x] oportunidades e pipeline;
- [x] transições explícitas de estágio;
- [x] timeline append-only;
- [x] vínculos com Farm/Crop/Season/ExportOrder;
- [x] resumo por moeda e conversão;
- [x] interface web e testes.

## Sprint 15 — Importação geoespacial e zonas de manejo ✅

- [x] `ManagementZone` com PostGIS;
- [x] tipos Soil, Yield, Vegetation, Prescription e Custom;
- [x] importação GeoJSON Feature/FeatureCollection;
- [x] validação de contenção no talhão;
- [x] importação em lote transacional;
- [x] exportação e camadas MapLibre;
- [x] testes e documentação.

## Sprint 16 — Sensoriamento remoto e índices vegetativos ✅

- [x] cenas Satellite/Drone/Other;
- [x] footprints PostGIS;
- [x] NDVI, NDRE, EVI e Custom;
- [x] observações append-only;
- [x] séries temporais e latest metrics;
- [x] workspace web com filtros, gráfico e mapa;
- [x] testes e API `0.16.0`.

## Sprint 17 — Processamento raster e estatísticas zonais ✅

- [x] Rasterio + NumPy;
- [x] GeoTIFF/COG;
- [x] reprojeção de CRS, máscara e NoData;
- [x] estatísticas zonais de talhão e zonas de manejo;
- [x] lifecycle de processamento e idempotência;
- [x] integração HTTP C# ↔ Python;
- [x] painel web de processamento;
- [x] testes Python/C# e API `0.17.0`.

## Sprint 18 — Operação multi-fazenda ✅

- [x] `Farm` como fronteira operacional abaixo de `Organization`;
- [x] `OperationalRegion` e localização estruturada;
- [x] escopo explícito `AllFarms`, `Region` e `Farm`;
- [x] `OperationalScopeContext` por request;
- [x] filtros horizontais EF Core;
- [x] proteção explícita de SQL/PostGIS;
- [x] proteção de Telemetry por Farm/Field/Machine;
- [x] matriz Fazenda A × Fazenda B na mesma organização;
- [x] seletor global Todas / Região / UF / Fazenda;
- [x] contexto persistido durante a sessão;
- [x] dashboard e mapa multi-fazenda;
- [x] consolidação financeira sem média indevida de percentuais;
- [x] prevenção de agregações com unidades incompatíveis;
- [x] timezone operacional IANA e testes SP × MT × AM × AC;
- [x] migrations e backfills de hardening;
- [x] documentação e API `0.18.0`;
- [x] Backend CI, Frontend CI, Platform CI e CodeQL verdes no mesmo head;
- [x] PR #51 mesclado por squash;
- [x] Issue #50 encerrada como `completed`.

## Estado atual

Todas as Sprints 0–18 acima estão **concluídas**.

A base atual suporta uma organização com múltiplas propriedades em diferentes regiões, UFs e fusos horários sem criar tenants separados, preservando autorização horizontal e contexto operacional.

## Próximas evoluções candidatas

As próximas sprints devem ser definidas separadamente, sem reabrir o escopo da Sprint 18. Candidatos já identificados:

- catálogo STAC e descoberta de cenas;
- ingestão assíncrona de assets;
- máscaras de qualidade/nuvem;
- geração de produtos a partir de bandas brutas;
- importação KML/Shapefile;
- aprofundamento agronômico;
- sincronização offline;
- integrações oficiais CAR/SIGEF/IBGE;
- hardening de produção dependente de política/infraestrutura real.

O issue #18 continua concentrando hardening dependente de ambiente real, política operacional ou testes de carga.