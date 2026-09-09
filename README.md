# AgroControl

**AgroControl** é uma plataforma modular de gestão, inteligência e tecnologia para o agronegócio. O núcleo transacional é um **monólito modular em C# / ASP.NET Core**, complementado por **Python / FastAPI** para inteligência e processamento científico, **Java / Spring Boot** para telemetria e uma única aplicação **React + TypeScript + Vite** distribuída em navegador, PWA e Desktop Windows.

> Status: **Sprint 21 concluída — offline real por fazenda + sincronização controlada**  
> Release: **0.21.0**  
> API: **0.21.0**  
> Desktop: **0.21.0**

> Nota de versionamento: `@agrocontrol/web` é um pacote npm privado com versionamento interno próprio. O número do pacote não representa a versão pública da release do AgroControl.

## Objetivo

Centralizar a operação rural em uma plataforma única preservando:

- isolamento multi-tenant por `OrganizationId`;
- autorização horizontal por `FarmAccessScope`;
- módulos liberados por entitlement no backend;
- geoprocessamento e inteligência em serviços especializados;
- uma única interface para Web, PWA e Desktop;
- operação offline opt-in sem transformar o cliente em autoridade de negócio.

## Arquitetura

```text
Browser / PWA / Tauri Desktop
            │
            ▼
   React + TypeScript + Vite
            │
     ┌──────┴─────────┐
     │                │
     │ online         │ offline por fazenda
     │                ▼
     │          IndexedDB OfflineStore
     │          ├── records
     │          ├── outbox
     │          └── syncMetadata
     │                │
     └──────────┬─────┘
                ▼
       AgroControl API 0.21.0
                │
      ┌─────────┼──────────┐
      ▼         ▼          ▼
 PostgreSQL   Python      Java
 + PostGIS   Intelligence Telemetry/MQTT
```

A API continua sendo a autoridade para autenticação, tenant, entitlement, `FarmAccessScope`, validação de negócio, idempotência e resolução de concorrência.

## Stack

| Camada | Tecnologia |
|---|---|
| Interface | React 19 + TypeScript + Vite |
| PWA | Web App Manifest + Service Worker |
| Desktop | Tauri v2 + Rust + WebView2 |
| API | C# + ASP.NET Core / .NET 10 |
| Banco principal | PostgreSQL 17 + PostGIS + EF Core |
| Inteligência | Python 3.12 + FastAPI + scikit-learn + Rasterio + NumPy |
| Telemetria | Java 21 + Spring Boot + MQTT |
| Mapas | MapLibre GL + GeoJSON |
| Catálogo geoespacial | STAC API |
| Observabilidade | OpenTelemetry + Prometheus + Tempo + Grafana |
| Containers | Docker / Docker Compose |
| Orquestração | Kubernetes + Kustomize |
| CI/CD | GitHub Actions + GHCR |

## Cobertura funcional

O AgroControl inclui:

- propriedades, regiões, talhões, culturas e safras;
- operação multi-fazenda e fusos por propriedade;
- estoque e movimentações;
- financeiro e rentabilidade;
- máquinas, combustível, horímetro e manutenção;
- commodities e mercado;
- agricultura de precisão, PostGIS e zonas de manejo;
- sensoriamento remoto, STAC, índices vegetativos e raster;
- previsão de produtividade;
- telemetria MQTT;
- irrigação;
- sustentabilidade e CO₂e;
- exportação e logística;
- CRM/pipeline comercial;
- dashboard e mapa multi-fazenda;
- PWA e Desktop Tauri;
- **modo offline real para produção rural por fazenda**.

## Multi-tenancy e escopo operacional

```text
OrganizationId
      │
      └── FarmAccessScope
              ├── AllFarms
              ├── Region
              └── Farm
```

O papel organizacional e o escopo operacional são conceitos distintos. O backend resolve o escopo efetivo em cada request e aplica proteção horizontal em módulos ligados a Farm/Field.

Testes exercitam Fazenda A × Fazenda B na mesma organização e Organização A × Organização B.

## Offline real — Sprint 21

O modo offline é **opt-in por fazenda**. Um usuário autorizado escolhe explicitamente `Disponibilizar offline`; o sistema não baixa automaticamente todas as propriedades de um usuário `AllFarms`.

### Namespace local

Cada cópia é isolada por:

```text
UserId + OrganizationId + FarmId
```

JWT, senha, API key e secrets **não** são persistidos no banco offline.

### Allowlist inicial

Leitura offline:

- Farm selecionada;
- Fields;
- Crops referenciados;
- Seasons.

Mutações offline:

- Field: create/update/delete lógico;
- Season: create/update/delete lógico.

Financeiro, telemetria histórica, raster, tiles e documentos pesados não entram automaticamente nessa allowlist.

### Protocolo

```text
GET  /api/v1/sync/status?farmId=...
GET  /api/v1/sync/bootstrap?farmId=...
GET  /api/v1/sync/pull?farmId=...&cursor=...
POST /api/v1/sync/push
```

Características:

- bootstrap limitado a uma fazenda;
- cursor opaco, protegido e farm-scoped;
- change-log monotônico server-side;
- pull incremental paginado;
- push em lote de até 100 operações;
- idempotência persistida por organização/usuário/operação;
- retenção da chave idempotente por 30 dias;
- `FOR UPDATE` em update/delete;
- `serverVersion` baseado em timestamp UTC canônico com precisão compatível com PostgreSQL;
- ACK perdido pode ser reenviado sem duplicar efeito;
- operação só sai da outbox depois de ACK válido.

### Outbox e estados

```text
Record:
Clean
PendingCreate
PendingUpdate
PendingDelete
Conflict
Failed

Mutation:
Pending
Retryable
Conflict
Failed
```

`record + outbox` são gravados atomicamente no IndexedDB. O cliente não permite duas mutações pendentes concorrentes para a mesma entidade.

### Conflitos

Não existe last-write-wins silencioso para Field/Season.

Quando a versão-base não coincide com o servidor:

- backend retorna `Conflict`;
- cliente mantém a proposta local;
- snapshot atual do servidor é preservado;
- usuário pode usar a versão do servidor;
- ou reaplicar sua alteração usando a versão atual e **novo `operationId`**.

### Retry e reconexão

- falha transitória permanece na outbox como `Retryable`;
- resposta sem ACK também permanece para retry;
- sync automático é disparado ao recuperar conexão com debounce;
- retries dentro de uma execução usam backoff exponencial limitado;
- pull só ocorre quando a outbox não possui operação enviável, conflito ou rejeição pendente.

### Revogação de acesso

O backend revalida `FarmAccessScope` no início do push e antes de cada operação do lote.

Se o acesso for removido enquanto o dispositivo esteve offline:

- novos pushes/pulls são rejeitados;
- o cliente remove records e outbox da fazenda;
- mantém somente metadata `Blocked` com o motivo;
- o namespace não volta a sincronizar sem nova autorização/preparação.

Logout explícito e troca de usuário/organização também limpam material offline do contexto anterior. Reabrir o app e autenticar novamente como o mesmo usuário preserva a cópia offline válida conforme a política implementada.

## Limites de sync

Bootstrap inicial:

- até 2.000 Fields;
- até 10.000 Seasons;
- até 1.000 Crops referenciados.

Pull incremental:

- até 500 mudanças por página.

Push:

- até 100 operações por lote.

## Observabilidade

O backend emite métricas OpenTelemetry de sync com labels de baixa cardinalidade:

- batches push/pull;
- duração;
- tamanho de lote;
- resultado de operações;
- retries;
- cursores inválidos;
- revogações detectadas.

`UserId`, `FarmId` e `operationId` não são labels de métricas.

## Segurança

Princípios:

- autenticação JWT no backend;
- tenant por `OrganizationId`;
- autorização horizontal por `FarmAccessScope`;
- entitlement validado server-side;
- frontend nunca é fronteira de autorização;
- secrets fora do repositório;
- CORS por allowlist;
- CSP explícita no Tauri;
- service worker sem cache de dados autenticados;
- OfflineStore sem JWT/secrets;
- push reautoriza a fazenda e impede cross-farm/cross-tenant;
- cursor não amplia escopo;
- CodeQL e Dependabot.

## Execução local

Na raiz:

```bash
cp .env.example .env
docker compose up --build
```

Serviços padrão:

```text
Web          http://localhost:3001
API          http://localhost:8080
Intelligence http://localhost:8090
Telemetry    http://localhost:8100
PostgreSQL   localhost:5432
MQTT         localhost:1883
```

Observabilidade opcional:

```bash
OTEL_ENABLED=true docker compose --profile observability up --build
```

## Desenvolvimento frontend

```bash
cd src/frontend
npm ci
npm run dev
```

Build:

```bash
npm run build
```

Desktop:

```bash
npm run desktop:dev
npm run desktop:build
```

O Desktop CI gera bundles Windows NSIS e MSI. A assinatura de distribuição continua fora do repositório até existir certificado/identidade de publicação apropriados.

## CI e política de merge

O fechamento de sprint exige, no mesmo head final do PR:

- Backend CI;
- Frontend CI;
- Platform CI;
- Desktop CI;
- CodeQL.

O fluxo recomendado é PR + checks verdes + squash merge. A configuração administrativa de branch protection/ruleset da `main` está acompanhada na Issue #52.

## Documentação

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/MODULES.md`](docs/MODULES.md)
- [`docs/ROADMAP.md`](docs/ROADMAP.md)
- [`docs/SPRINT_19_STAC_DISCOVERY.md`](docs/SPRINT_19_STAC_DISCOVERY.md)
- [`docs/SPRINT_20_PWA_TAURI.md`](docs/SPRINT_20_PWA_TAURI.md)
- [`docs/SPRINT_21_OFFLINE_SYNC.md`](docs/SPRINT_21_OFFLINE_SYNC.md)
- [`CONTRIBUTING.md`](CONTRIBUTING.md)

## Licença

Consulte o arquivo `LICENSE` do repositório.
