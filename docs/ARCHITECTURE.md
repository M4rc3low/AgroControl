# Arquitetura do AgroControl

## 1. Visão geral

O AgroControl usa um **monólito modular em C# / ASP.NET Core** como núcleo transacional. Python/FastAPI concentra inteligência e processamento científico; Java/Spring Boot concentra telemetria/IoT; React/TypeScript é a interface canônica para navegador, PWA e Tauri Desktop.

Estado arquitetural: **Sprint 21 concluída — API 0.21.0**.

```text
Browser / PWA / Tauri
        │
        ▼
React + TypeScript
        │
  ┌─────┴───────────────┐
  │                     │
  │ HTTPS               │ OfflineStore por fazenda
  │                     │ ├── records
  │                     │ ├── outbox
  │                     │ └── syncMetadata
  │                     │
  └──────────┬──────────┘
             ▼
      ASP.NET Core API
             │
   ┌─────────┼──────────┐
   ▼         ▼          ▼
PostgreSQL  Python     Java
+ PostGIS   Intelligence Telemetry/MQTT
```

## 2. Fronteiras de autoridade

### Backend

É autoridade para:

- identidade e autenticação;
- `OrganizationId`;
- papéis organizacionais;
- `FarmAccessScope`;
- entitlements;
- validação de negócio;
- persistência transacional;
- idempotência;
- concorrência otimista;
- autorização de sync;
- integração com serviços especializados.

### Frontend

É responsável por:

- experiência de usuário;
- seleção de escopo operacional;
- cache estático do app shell;
- armazenamento offline controlado;
- outbox local;
- apresentação de conflito e estados de sincronização.

O frontend **não** decide autorização nem estado final de negócio.

## 3. Multi-tenancy e operação multi-fazenda

A fronteira máxima é:

```text
OrganizationId
```

Dentro dela, a autorização operacional é:

```text
FarmAccessScope
├── AllFarms
├── Region
└── Farm
```

`OperationalScopeContext` é inicializado por request autenticado. Módulos farm-linked usam filtros EF, SQL/PostGIS explícito ou fachadas de serviço para preservar o escopo.

Testes cobrem Fazenda A × Fazenda B dentro da mesma organização e Organização A × Organização B.

## 4. Módulos

O backend mantém módulos separados por domínio, incluindo:

- Production;
- Inventory;
- Finance;
- Machinery;
- Market;
- Precision Agriculture;
- Remote Sensing;
- Intelligence;
- Telemetry;
- Irrigation;
- Sustainability;
- Export;
- Commercial.

O catálogo de entitlement permanece server-side.

## 5. Persistência

### Banco principal

PostgreSQL 17 + PostGIS + EF Core.

Usado para:

- entidades transacionais;
- geometrias;
- change-log offline;
- idempotência de sync;
- dados operacionais consolidados.

### Telemetria

Mantém banco dedicado ao serviço Java quando necessário à fronteira de ingestão/histórico.

## 6. Geoespacial e sensoriamento remoto

- WGS84/SRID 4326;
- GeoJSON;
- PostGIS/GiST;
- zonas de manejo;
- cenas Satellite/Drone/Other;
- STAC discovery;
- NDVI/NDRE/EVI/custom;
- Rasterio/NumPy;
- processamento zonal;
- MapLibre no frontend.

URLs/assets externos nunca substituem a validação server-side do provider/item.

## 7. Offline real

### 7.1 Unidade de sincronização

A unidade offline é **uma fazenda escolhida explicitamente**.

Namespace local:

```text
UserId + OrganizationId + FarmId
```

Isso evita mistura entre usuários, tenants ou propriedades no mesmo dispositivo.

### 7.2 IndexedDB

Banco:

```text
agrocontrol.offline
```

Object stores:

```text
records
outbox
syncMetadata
```

JWT e secrets não são persistidos nele.

### 7.3 Allowlist

Snapshot inicial:

- Farm;
- Field;
- Crop referenciado;
- Season.

Mutações inicialmente permitidas:

- Field create/update/delete lógico;
- Season create/update/delete lógico.

Novos domínios só entram após definir autorização, idempotência e conflito.

### 7.4 Bootstrap

```text
GET /api/v1/sync/bootstrap?farmId=...
```

Fluxo:

1. revalida `FarmAccessScope`;
2. valida farm ativa;
3. captura watermark do change-log;
4. emite cursor protegido;
5. carrega somente entidades da fazenda;
6. cliente valida relações e namespace;
7. snapshot é substituído atomicamente somente quando não existem mutações pendentes.

### 7.5 Pull incremental

```text
GET /api/v1/sync/pull?farmId=...&cursor=...
```

O cursor é opaco e inclui escopo/fazenda/sequence protegidos. Cursor adulterado, expirado ou pertencente a outra fazenda é rejeitado.

O change-log é monotônico e farm-scoped. Mudança de Field entre fazendas gera materialização segura nos dois escopos relevantes, sem vazar payload ao escopo antigo.

### 7.6 Push

```text
POST /api/v1/sync/push
```

Máximo de 100 operações por lote.

Para cada operação:

1. revalida acesso à farm;
2. valida entity/operation allowlisted;
3. calcula hash canônico da intenção;
4. tenta claim idempotente;
5. para update/delete, bloqueia a linha com `FOR UPDATE`;
6. compara `baseServerVersion`;
7. aplica domínio;
8. persiste resultado idempotente;
9. commit ocorre na mesma transação.

### 7.7 Idempotência

Chave lógica:

```text
OrganizationId + UserId + operationId
```

Retenção: **30 dias**.

O resultado aplicado fica armazenado como JSONB para replay. Reenvio após ACK perdido retorna o mesmo efeito sem duplicar entidade. Reuso do mesmo `operationId` com conteúdo diferente é tratado como `OperationIdReuse`.

### 7.8 Concorrência

`serverVersion` usa `UpdatedAtUtc` canônico. Timestamps gerados para Field/Season são normalizados à precisão compatível com PostgreSQL.

Não há last-write-wins silencioso.

Conflito retorna:

- versão atual;
- snapshot atual quando seguro;
- código explícito.

O cliente preserva a proposta local e oferece uso do servidor ou reaplicação com novo `operationId`.

### 7.9 Outbox

Staging local grava `record + mutation` na mesma transação IndexedDB.

Uma operação só é removida depois de ACK válido.

Falhas transitórias permanecem como `Retryable`. A engine usa retry exponencial limitado e só executa pull depois que a fila enviável está limpa.

### 7.10 Revogação

O acesso é revalidado no início do lote e antes de cada operação. Se for revogado durante o período offline ou no meio do lote:

- operações posteriores são bloqueadas;
- ACKs já confirmados permanecem válidos;
- cliente purga records/outbox da farm revogada;
- metadata fica `Blocked`.

## 8. Segurança local

- OfflineStore não contém JWT/secret;
- logout explícito limpa material offline;
- troca de usuário/organização limpa o contexto anterior;
- retorno do mesmo usuário preserva a cópia válida;
- operações não podem escolher outro `OrganizationId`;
- payload Field não pode mover entidade para outra farm;
- Season só pode apontar para Field da mesma farm do namespace;
- cursor não amplia escopo.

## 9. PWA e Desktop

A mesma SPA é usada nas três superfícies.

PWA:

- service worker para shell/assets;
- `/api` e `/health` não são cacheados como dados de negócio.

Tauri:

- reutiliza a SPA;
- WebView2;
- CSP explícita;
- sem backend local embutido;
- IndexedDB da WebView é usado pela camada offline atual;
- sem plugin de banco nativo enquanto não houver necessidade comprovada.

## 10. Observabilidade

OpenTelemetry coleta traces/métricas da API e integra com Collector/Prometheus/Tempo/Grafana.

Sync publica métricas de baixa cardinalidade:

- `agrocontrol.sync.batches`;
- `agrocontrol.sync.operations`;
- `agrocontrol.sync.retries`;
- `agrocontrol.sync.invalid_cursors`;
- `agrocontrol.sync.access_revocations`;
- `agrocontrol.sync.duration`;
- `agrocontrol.sync.batch_size`.

Não são utilizados `UserId`, `FarmId` ou `operationId` como labels.

## 11. CI/CD

Gates principais:

- Backend CI;
- Frontend CI;
- Platform CI;
- Desktop CI;
- CodeQL.

Imagens podem ser publicadas no GHCR com provenance/SBOM conforme workflows existentes.

## 12. Estado atual

Arquitetura consolidada até a **Sprint 21**.

As próximas evoluções devem preservar as mesmas fronteiras: tenant, farm scope, entitlement, cliente não-autoritativo, contratos explícitos entre serviços e entrada gradual de novos domínios na allowlist offline.
