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

## Sprint 12 — Sustentabilidade ✅

- [x] fatores de emissão por organização, categoria e unidade;
- [x] valor em `kgCO2e/unidade`, referência metodológica e vigência;
- [x] atividades de emissão append-only;
- [x] snapshot do fator utilizado no lançamento;
- [x] cálculo determinístico de `kgCO2e` e `tCO2e`;
- [x] qualidade de dado `Measured`, `Recorded` e `Estimated`;
- [x] vínculos opcionais com Farm, Field e Season;
- [x] referência externa desacoplada e idempotente por organização;
- [x] correções por lançamento compensatório;
- [x] resumo por período, categoria, propriedade e safra;
- [x] comparação descritiva com período anterior;
- [x] `tCO2e/ha` e `kgCO2e` por unidade produzida quando aplicável;
- [x] API protegida pelo entitlement `Sustainability`;
- [x] interface web responsiva com disclaimer de estimativa gerencial;
- [x] migration `SustainabilityCore`, índices e unicidade da referência externa;
- [x] testes unitários e integração PostgreSQL;
- [x] documentação da Sprint 12.

## Sprint 13 — Exportação ✅

- [x] pedidos de exportação isolados por organização e número único por tenant;
- [x] comprador, país ISO alpha-2, produto, quantidade e unidade;
- [x] vínculo opcional e consistente com Farm, Field, Crop e Season;
- [x] moeda ISO 4217, preço unitário e snapshot de câmbio para BRL;
- [x] Incoterms e regras explícitas de transição de status;
- [x] timeline operacional append-only;
- [x] origem, destino, shipment, booking e container;
- [x] checklist documental e metadados de emissão;
- [x] custos logísticos com moeda e snapshot cambial;
- [x] resumo por período, status, país e moeda;
- [x] interface web responsiva para pedidos, documentos, timeline e custos;
- [x] API protegida pelo entitlement `Export` e isolamento multi-tenant;
- [x] migration `ExportCore` e índices;
- [x] testes unitários e integração PostgreSQL;
- [x] documentação da Sprint 13.

## Sprint 14 — Comercial e CRM ✅

- [x] módulo `Commercial` com entitlement próprio;
- [x] clientes por organização e status de relacionamento;
- [x] contatos vinculados, principal e desativação lógica;
- [x] oportunidades com valor, moeda, probabilidade, responsável e próximo passo;
- [x] pipeline com transições explícitas e `Won`/`Lost` terminais;
- [x] timeline append-only das mudanças de etapa;
- [x] vínculos opcionais com Farm, Crop, Season e ExportOrder;
- [x] validação de todos os vínculos no mesmo `OrganizationId`;
- [x] resumo de pipeline, ganhos, perdas e conversão;
- [x] valores resumidos por moeda, sem agregação incorreta entre moedas;
- [x] API protegida pelo entitlement `Commercial`;
- [x] interface web responsiva para clientes, contatos e oportunidades;
- [x] migration `CommercialCore`, índices e model snapshot;
- [x] testes unitários e integração PostgreSQL;
- [x] documentação da Sprint 14.

## Sprint 15 — Importação geoespacial e zonas de manejo ✅

- [x] módulo mantido sob o entitlement `PrecisionAgriculture`;
- [x] entidade `ManagementZone` isolada por organização e talhão;
- [x] tipos `Soil`, `Yield`, `Vegetation`, `Prescription` e `Custom`;
- [x] geometria PostGIS WGS84 (`SRID 4326`) e índice espacial GiST;
- [x] nome, descrição, classificação, valor numérico opcional e unidade;
- [x] cálculo geodésico da área em hectares;
- [x] validação WGS84, topológica e contenção no limite do talhão;
- [x] tolerância técnica documentada de 0,5 m na contenção;
- [x] importação de `GeoJSON Feature` e `FeatureCollection`;
- [x] mapeamento seguro de propriedades GeoJSON para metadados;
- [x] importação em lote transacional e limite de 250 features;
- [x] exportação `FeatureCollection`;
- [x] filtros por talhão, tipo e classificação;
- [x] CRUD e desativação controlada de zonas;
- [x] upload `.geojson/.json`, pré-visualização e camadas no MapLibre;
- [x] alternância de visibilidade e exportação pela interface web;
- [x] migration SQL `20260908134500_ManagementZonesCore` e estratégia espacial documentada;
- [x] testes unitários, integração PostGIS e testes frontend;
- [x] documentação `SPRINT_15_GEOSPATIAL_ZONES.md`.

## Sprint 16 — Sensoriamento remoto e índices vegetativos ✅

- [x] cenas de satélite, drone e outras plataformas isoladas por `OrganizationId`;
- [x] vínculo obrigatório com Field e opcional com Season da mesma organização;
- [x] provedor, identificador externo e idempotência por organização/provedor/id;
- [x] aquisição UTC, cobertura de nuvens, resolução espacial e referência de asset;
- [x] footprint opcional WGS84/PostGIS e índice espacial GiST;
- [x] `VegetationIndexObservation` append-only;
- [x] índices `NDVI`, `NDRE`, `EVI` e `Custom`;
- [x] mínimo, máximo, média, mediana, desvio-padrão, cobertura válida e amostras;
- [x] snapshot de fonte e data da cena em cada observação;
- [x] vínculo opcional com ManagementZone do mesmo talhão;
- [x] validação de faixa normalizada para índices padronizados;
- [x] séries temporais por talhão, safra, zona e índice;
- [x] resumo com latest metric por índice e contagem de cenas;
- [x] API `/api/v1/precision/remote-sensing/*` protegida por `PrecisionAgriculture`;
- [x] workspace web com filtros, cards, timeline, gráfico temporal e mapa;
- [x] raster pesado mantido fora do banco relacional principal;
- [x] migration SQL `20260908150000_RemoteSensingCore` e estratégia espacial documentada;
- [x] testes unitários, integração PostgreSQL/PostGIS e testes frontend;
- [x] documentação `SPRINT_16_REMOTE_SENSING.md` e versão API `0.16.0`.

## Sprint 17 — Processamento raster e estatísticas zonais 🚀

- [x] Rasterio + NumPy no AgroControl Intelligence;
- [x] leitura de GeoTIFF/COG e metadados de CRS, dimensão, resolução e NoData;
- [x] reprojeção das geometrias para o CRS do raster;
- [x] máscara espacial e exclusão de NoData/valores não finitos;
- [x] mínimo, máximo, média, mediana, desvio-padrão, cobertura válida e amostras;
- [x] processamento do talhão e das zonas de manejo ativas;
- [x] limites MVP de 20 milhões de pixels e 250 alvos;
- [x] assets remotos desabilitados por padrão e validação de referências inseguras;
- [x] produtos raster, execuções e resultados zonais isolados por `OrganizationId`;
- [x] lifecycle `Pending`, `Processing`, `Succeeded` e `Failed`;
- [x] idempotência por chave de processamento e tratamento de corrida concorrente;
- [x] integração HTTP C# ↔ Python com timeout e indisponibilidade explícitos;
- [x] endpoints de processamento, histórico de execuções e resultados;
- [x] painel web dentro do workspace de sensoriamento remoto;
- [x] migration `20260908154500_RasterProcessingCore`;
- [x] testes Python com raster sintético e testes C# de domínio/persistência/contrato;
- [x] documentação `SPRINT_17_RASTER_PROCESSING.md` e versão API `0.17.0`.

## Sprint 18 — Operação multi-fazenda ✅

- [x] `Farm` como fronteira operacional abaixo de `Organization`;
- [x] regiões operacionais e localização estruturada por propriedade;
- [x] escopo explícito `AllFarms`, `Region` e `Farm` por usuário;
- [x] `OperationalScopeContext` por request e filtros horizontais EF Core;
- [x] proteção explícita de SQL/PostGIS para Agricultura de Precisão, Sensoriamento Remoto e Raster;
- [x] proteção de Telemetry por Farm/Field/Machine;
- [x] matriz A × B na mesma organização para os módulos vinculados a fazenda;
- [x] seletor global Todas / Região / UF / Fazenda com persistência do contexto;
- [x] dashboard e mapa multi-fazenda;
- [x] consolidação financeira em BRL sem média indevida de percentuais;
- [x] comparações sem agregar produtividade/unidades incompatíveis;
- [x] timezone IANA por propriedade e apresentação operacional SP × MT × AM × AC;
- [x] migration `MultiFarmRegionalOperations` e hardening de backfill por UF;
- [x] documentação `SPRINT_18_MULTI_FARM.md` e versão API `0.18.0`;
- [x] Backend CI, Frontend CI, Platform CI e CodeQL verdes no head final;
- [x] squash merge do PR da Sprint 18 e encerramento da Issue #50.

## Próximos aprimoramentos

O hardening operacional que depende de políticas, ambiente real ou testes de carga continua no issue #18. As próximas evoluções funcionais podem avançar para catálogo STAC e descoberta de cenas, ingestão assíncrona de assets, máscaras de qualidade/nuvem, geração de produtos a partir de bandas brutas, importação KML/Shapefile e aprofundamento agronômico, mantendo essas responsabilidades separadas do monólito transacional.