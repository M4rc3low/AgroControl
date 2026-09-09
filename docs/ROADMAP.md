# Roadmap do AgroControl

Este roadmap registra o estado funcional consolidado do projeto. **Sprints 0–21 estão concluídas.** Os detalhes técnicos das sprints recentes ficam nos documentos específicos em `docs/`.

## Sprints concluídas

### Sprint 0 — Fundação ✅

- solução e estrutura inicial;
- padrões de projeto;
- base de domínio e persistência.

### Sprint 1–7 — Núcleo operacional ✅

- identidade e organização;
- propriedades, talhões, culturas e safras;
- estoque;
- financeiro;
- máquinas;
- mercado;
- intelligence e telemetria.

### Sprint 8 — Plataforma, DevOps e observabilidade ✅

- Docker Compose integrado;
- OpenTelemetry;
- Prometheus/Tempo/Grafana;
- Kubernetes/Kustomize;
- CI/CD, GHCR, SBOM e CodeQL.

### Sprint 9–15 — Expansão funcional ✅

- agricultura de precisão;
- irrigação;
- sustentabilidade;
- exportação;
- comercial;
- segurança horizontal e hardening dos módulos.

### Sprint 16 — Sensoriamento remoto e raster ✅

- cenas e índices vegetativos;
- Rasterio/NumPy;
- processamento zonal;
- PostGIS e lifecycle de processamento.

### Sprint 17 — Hardening e produto ✅

- segurança e observabilidade adicionais;
- integração de módulos;
- refinamentos de produto e CI.

### Sprint 18 — Operação multi-fazenda ✅

- regiões operacionais;
- `AllFarms`, Region e Farm;
- `OperationalScopeContext`;
- proteção EF + SQL/PostGIS + Telemetry;
- testes Fazenda A × Fazenda B;
- dashboard/mapa multi-fazenda;
- timezone por propriedade;
- API 0.18.0.

### Sprint 19 — STAC e descoberta de cenas ✅

- providers STAC configurados server-side;
- busca pelo boundary do Field;
- paginação protegida;
- filtros de coleção/período/nuvens;
- importação explícita e idempotente;
- asset selecionado por chave, não URL arbitrária;
- métricas de baixa cardinalidade;
- API 0.19.0.

### Sprint 20 — PWA + Desktop Tauri ✅

- app shell instalável como PWA;
- service worker sem cache de dados autenticados;
- mesma SPA no Tauri v2;
- bundles NSIS/MSI;
- CSP/CORS endurecidos;
- Desktop CI.

### Sprint 21 — Offline real por fazenda + sincronização controlada ✅

- [x] namespace local `UserId + OrganizationId + FarmId`;
- [x] IndexedDB com `records`, `outbox` e `syncMetadata`;
- [x] nenhuma persistência de JWT/secret no OfflineStore;
- [x] preparação offline opt-in por fazenda;
- [x] bootstrap de Farm/Field/Crop/Season;
- [x] limites de bootstrap e schema local;
- [x] change-log server-side monotônico e farm-scoped;
- [x] cursor opaco/protegido;
- [x] pull incremental paginado;
- [x] push em lote de até 100 operações;
- [x] Field e Season create/update/delete lógico offline;
- [x] staging atômico record + outbox;
- [x] idempotência persistida por organização/usuário/operação;
- [x] retenção idempotente de 30 dias;
- [x] replay de ACK perdido sem duplicação;
- [x] lock `FOR UPDATE` para update/delete;
- [x] `serverVersion` UTC compatível com precisão PostgreSQL;
- [x] conflito explícito, sem last-write-wins silencioso;
- [x] usar versão do servidor;
- [x] reaplicar alteração com novo `operationId`;
- [x] retry transitório com backoff exponencial limitado;
- [x] pull bloqueado enquanto existem alterações locais não resolvidas;
- [x] revalidação de `FarmAccessScope` no início e por operação;
- [x] revogação durante lote sem desfazer ACK já confirmado;
- [x] purge + metadata `Blocked` quando acesso é revogado;
- [x] isolamento Fazenda A × Fazenda B;
- [x] isolamento Organização A × Organização B;
- [x] política de logout/troca de usuário no dispositivo;
- [x] rota UI `Modo offline`;
- [x] status online/offline, última sincronização, pendências, conflitos e rejeições;
- [x] sync manual e automático ao reconectar;
- [x] métricas OpenTelemetry de sync sem IDs de alta cardinalidade;
- [x] testes de bootstrap, cursor, pull, change-log, idempotência, push, ACK, retry, conflito e revogação;
- [x] API 0.21.0;
- [x] Desktop 0.21.0;
- [x] README, ARCHITECTURE, ROADMAP e documentação da sprint atualizados;
- [x] Backend CI + Frontend CI + Platform CI + Desktop CI + CodeQL verdes no mesmo head final;
- [x] PR #60 squash merged;
- [x] Issue #59 encerrada como completed.

## Estado de release

A release funcional consolidada após a Sprint 21 é **0.21.0** para API e Desktop. A aplicação Web/PWA compartilha a mesma base React e o mesmo contrato da API; o número `0.9.0` do pacote npm privado `@agrocontrol/web` é versionamento interno de pacote e não representa a versão pública da release.

## Próximas evoluções candidatas

O roadmap após a Sprint 21 fica aberto a priorização por valor. Candidatos naturais:

- ampliar a allowlist offline para estoque farm-scoped com regra explícita de conflito;
- migrations evolutivas do schema IndexedDB além da versão 1;
- política de quota/estimativa de armazenamento offline;
- sincronização seletiva de mapas/tiles leves;
- ingestão assíncrona de assets remotos;
- máscaras de qualidade/nuvens e produtos derivados de bandas;
- importação KML/Shapefile;
- integrações CAR/SIGEF/IBGE;
- funções agronômicas mais profundas;
- produção/distribuição assinada do Desktop;
- hardening operacional para ambientes de produção.

Qualquer novo domínio offline deve entrar apenas depois de definir autorização, idempotência, concorrência, purge e testes de isolamento.
