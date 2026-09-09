# Sprint 21 — Offline real por fazenda + sincronização controlada

## Objetivo

A Sprint 21 adiciona uma camada offline controlada ao AgroControl sem transformar o cliente em autoridade de negócio e sem replicar o backend localmente.

A unidade de preparação offline é uma **fazenda explicitamente escolhida pelo usuário**. A autorização continua sendo server-side e é revalidada em toda sincronização.

## Invariantes

1. `OrganizationId` continua sendo a fronteira máxima do tenant.
2. `FarmAccessScope` continua sendo a fronteira operacional.
3. Dados locais são isolados por `UserId + OrganizationId + FarmId`.
4. JWT e secrets não entram no banco offline.
5. Nenhuma mutação offline é considerada aplicada no servidor sem ACK.
6. Reenvio da mesma operação não pode duplicar efeito.
7. Conflitos de negócio não usam last-write-wins silencioso.
8. Um cursor de pull nunca amplia o escopo autorizado.
9. Revogação de acesso enquanto offline precisa ser respeitada na reconexão.
10. Domínios só entram na allowlist offline quando possuem estratégia explícita de conflito e idempotência.

## Arquitetura

```text
React / PWA / Tauri
        │
        ├── OfflineStore
        │     ├── records
        │     ├── outbox
        │     └── syncMetadata
        │
        └── SyncEngine
              ├── push idempotente
              ├── pull incremental
              ├── retry/backoff
              └── conflito explícito
                      │
                      ▼
               AgroControl API
                      │
                      ▼
               PostgreSQL/PostGIS
```

O Desktop Tauri reutiliza a mesma SPA e, no primeiro estágio, pode reutilizar IndexedDB da WebView. Um plugin nativo de banco só será introduzido se existir necessidade técnica comprovada.

## Namespace offline

Formato lógico:

```text
UserId + OrganizationId + FarmId
```

Nenhum registro offline é armazenado sem o namespace completo. A chave composta impede mistura acidental entre usuários, organizações ou fazendas na mesma instalação.

## Stores locais

### records

Snapshots locais de entidades permitidas para offline.

Metadata mínima:

- `namespaceKey`;
- `entityKind`;
- `entityId`;
- `data`;
- `serverVersion`;
- `updatedAtUtc`;
- `syncState`;
- `lastSyncedAtUtc`.

Estados:

```text
Clean
PendingCreate
PendingUpdate
PendingDelete
Conflict
Failed
```

### outbox

Fila append-only de intenção local.

Cada entrada contém:

- `operationId` UUID;
- namespace completo;
- entidade e identificador;
- operação `create | update | delete`;
- payload mínimo;
- versão-base conhecida do servidor;
- instante de criação;
- tentativas;
- último erro.

Uma entrada só é removida/confirmada após resposta idempotente do backend.

### syncMetadata

Por namespace de fazenda:

- cursor de pull;
- última sincronização concluída;
- status da preparação offline;
- schema version local.

## Banco local

Nome inicial:

```text
agrocontrol.offline
```

Schema local começa em `1` e deve evoluir somente com migrations explícitas do IndexedDB.

Object stores iniciais:

```text
records
outbox
syncMetadata
```

A implementação não armazena JWT, senha, API key ou credencial de provider.

## Primeira allowlist

O primeiro vertical slice deve começar com dados leves e farm-scoped:

- Farm selecionada;
- Fields;
- Crops necessários ao contexto;
- Seasons necessários ao contexto.

Mutações de negócio entram somente depois que o protocolo de push/idempotência estiver implementado.

## Protocolo de sincronização planejado

```text
POST /api/v1/sync/push
GET  /api/v1/sync/pull?farmId=...&cursor=...
GET  /api/v1/sync/status?farmId=...
```

O backend deve revalidar organização, entitlement e `FarmAccessScope` em cada batch.

## Concorrência

Não usar relógio do dispositivo como autoridade de merge.

O cliente envia a versão-base conhecida. O backend decide:

- `Accepted`;
- `Conflict`;
- `Forbidden`;
- `ValidationFailed`;
- `RetryableFailure`.

Conflitos ficam persistidos localmente até resolução explícita.

## Revogação de acesso

Cenário obrigatório de teste:

1. usuário prepara Farm A para offline;
2. fica sem rede;
3. administrador remove seu acesso à Farm A;
4. cliente tenta sincronizar;
5. backend rejeita o push/pull;
6. namespace local entra em estado bloqueado e segue a política de purge.

## Fases da Sprint 21

### Fase A — Fundação local

- namespace;
- tipos de sync;
- IndexedDB versionado;
- records/outbox/syncMetadata;
- testes unitários dos invariantes de chave;
- documentação.

### Fase B — API de sync

- contratos `push/pull/status`;
- idempotência server-side;
- cursor opaco;
- autorização por farm;
- testes Fazenda A × Fazenda B.

### Fase C — Primeiro vertical slice

- preparar uma Farm para offline;
- download inicial de Farm/Field/Crop/Season;
- leitura desconectada;
- status de última sincronização.

### Fase D — Mutações

- outbox;
- push em lote;
- retry/backoff;
- ACK perdido sem duplicação;
- conflito explícito.

### Fase E — UX e hardening

- estados Online/Offline/Sincronizando/Conflito/Erro;
- contagem de pendências;
- sync manual e automático;
- revogação de acesso;
- purge;
- observabilidade;
- documentação final.

## Fora do escopo

- PostgreSQL local;
- backend C# local;
- sidecars Python/Java;
- raster offline em massa;
- tiles de mapa offline em massa;
- telemetria histórica completa;
- sincronização financeira genérica sem regra de conflito;
- secrets no cliente;
- auto-update/assinatura pública do desktop.

## Gate final

Sprint 21 só encerra com Frontend CI, Desktop CI, Backend CI, Platform CI e CodeQL verdes no mesmo head, documentação atualizada e squash merge do PR associado à Issue #59.
