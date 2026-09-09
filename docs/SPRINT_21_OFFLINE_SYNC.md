# Sprint 21 — Offline real por fazenda + sincronização controlada

**Status:** concluída tecnicamente — aguardando apenas gates finais e squash merge.  
**API:** `0.21.0`

## Objetivo

Permitir que um usuário autorizado prepare explicitamente uma fazenda para uso offline, continue trabalhando com dados de produção rural sem conexão e sincronize as alterações com segurança ao voltar online.

O cliente offline nunca se torna autoridade. Tenant, `FarmAccessScope`, entitlement, concorrência e validações continuam server-side.

## Invariantes

1. `OrganizationId` é a fronteira máxima do tenant.
2. `FarmAccessScope` é a fronteira operacional.
3. Dados locais são isolados por `UserId + OrganizationId + FarmId`.
4. JWT, senha, API key e secrets não entram no OfflineStore.
5. A operação só sai da outbox após ACK válido.
6. Reenvio após timeout/ACK perdido não duplica efeito.
7. Não existe last-write-wins silencioso para Field/Season.
8. Cursor de pull nunca amplia escopo.
9. Revogação de acesso é respeitada na reconexão e no meio do lote.
10. Novo domínio só entra na allowlist após definir autorização, idempotência e conflito.

## Arquitetura

```text
React / PWA / Tauri
        │
        ├── IndexedDB OfflineStore
        │     ├── records
        │     ├── outbox
        │     └── syncMetadata
        │
        └── Sync Engine
              ├── push idempotente
              ├── retry/backoff
              ├── pull incremental
              └── conflito explícito
                      │
                      ▼
               AgroControl API
                      │
                      ├── change-log
                      ├── idempotency store
                      └── FOR UPDATE
                      │
                      ▼
               PostgreSQL/PostGIS
```

Não há PostgreSQL local, backend C# embutido, sidecar Python/Java ou replicação direta do banco principal.

## Namespace local

```text
UserId + OrganizationId + FarmId
```

A chave lógica completa participa dos registros e da metadata local. Troca de contexto não mistura stores.

## IndexedDB

Banco:

```text
agrocontrol.offline
```

Schema local inicial: `1`.

Stores:

- `records`;
- `outbox`;
- `syncMetadata`.

### records

Metadata principal:

- `namespaceKey`;
- `entityKind`;
- `entityId`;
- `data`;
- `serverVersion`;
- `updatedAtUtc`;
- `syncState`;
- `lastSyncedAtUtc`;
- snapshot de conflito quando necessário.

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

Cada intenção local contém:

- `operationId` UUID;
- namespace;
- entidade/ID;
- `create | update | delete`;
- payload mínimo;
- `baseServerVersion`;
- instante de criação;
- tentativas;
- estado;
- último erro.

Estados:

```text
Pending
Retryable
Conflict
Failed
```

O staging de `record + mutation` ocorre na mesma transação IndexedDB. Uma entidade não aceita uma segunda mutação enquanto a anterior não for sincronizada/resolvida.

### syncMetadata

Por namespace:

- cursor;
- última sincronização;
- estado de preparação;
- schema local;
- último erro.

Estados de preparação:

```text
NotPrepared
Preparing
Ready
Blocked
Error
```

## Allowlist inicial

Snapshot offline:

- Farm selecionada;
- Fields da farm;
- Crops referenciados pelas Seasons;
- Seasons da farm.

Mutações:

- Field create/update/delete lógico;
- Season create/update/delete lógico.

Fora da allowlist automática:

- financeiro completo;
- telemetria histórica em massa;
- raster/GeoTIFF/COG;
- tiles/mapas em massa;
- documentos pesados;
- secrets;
- banco PostgreSQL local.

## API

```text
GET  /api/v1/sync/status?farmId=...
GET  /api/v1/sync/bootstrap?farmId=...
GET  /api/v1/sync/pull?farmId=...&cursor=...
POST /api/v1/sync/push
```

Todos os endpoints exigem autenticação e entitlement de Farms.

## Bootstrap

Fluxo:

1. revalida `FarmAccessScope`;
2. valida farm ativa dentro do tenant;
3. captura watermark atual do change-log;
4. emite cursor protegido;
5. carrega somente dados da fazenda escolhida;
6. crops são incluídos apenas quando referenciados;
7. frontend revalida relações e namespace;
8. snapshot limpo substitui o anterior atomicamente somente se não houver outbox pendente.

Limites:

- 2.000 Fields;
- 10.000 Seasons;
- 1.000 Crops referenciados.

## Change-log e pull incremental

O servidor mantém sequência monotônica de alterações farm-scoped.

O cursor é opaco/protegido e carrega contexto suficiente para impedir reutilização em outra organização/fazenda.

Pull:

- máximo de 500 mudanças por página;
- `hasMore` explícito;
- cursor avança pela sequência efetivamente varrida;
- entidade que saiu da farm é materializada como delete no escopo antigo, sem vazar o payload atual;
- mudanças desconhecidas não são promovidas automaticamente à allowlist.

## Push

Máximo: **100 operações por lote**.

Validações do lote:

- FarmId obrigatório;
- 1–100 operações;
- operationId/entityId não vazios;
- operationId não duplicado no mesmo lote.

Para cada operação:

1. revalida acesso à farm;
2. valida entity kind/operation allowlisted;
3. calcula hash canônico da intenção;
4. tenta claim idempotente;
5. replaya resultado anterior quando hash coincide;
6. update/delete obtêm row lock `FOR UPDATE`;
7. compara versão-base;
8. executa regra de domínio;
9. armazena resultado;
10. efeito + resultado idempotente commitam juntos.

Resultados possíveis:

```text
Applied
RetryableError
Forbidden
ValidationError
Conflict
NotFound
```

## Idempotência

Chave persistida:

```text
OrganizationId + UserId + operationId
```

Retenção: **30 dias**.

O resultado é armazenado em JSONB. Repetir a mesma operação retorna o resultado anterior com `Replayed=true`.

Reusar o mesmo `operationId` com conteúdo diferente retorna `OperationIdReuse`; isso não é tratado como conflito de versão de negócio.

## Concorrência

`baseServerVersion` é comparada ao `UpdatedAtUtc` bloqueado no banco.

Field/Season geram timestamps UTC normalizados à precisão de microssegundos do PostgreSQL para impedir falsos conflitos após ACK.

Conflito de versão retorna versão/snapshot atuais quando seguro.

## Resolução de conflito no cliente

O frontend mantém:

- proposta local;
- versão atual do servidor;
- snapshot atual do servidor;
- motivo.

Ações:

### Usar servidor

Descarta a intenção local e substitui o registro pelo snapshot confirmado.

### Reaplicar minha alteração

- preserva a proposta;
- usa a versão atual do servidor como nova base;
- gera **novo `operationId`**;
- volta para `Pending`.

Create conflict não é reaplicado automaticamente sem revisão.

## Retry / ACK perdido

- exceção de transporte mantém mutação em `Retryable`;
- ausência de ACK mantém mutação em `Retryable`;
- backend retorna `RetryableError` quando a transação não foi commitada;
- a engine usa backoff exponencial limitado dentro de uma execução;
- reconexão dispara sync automático com debounce;
- um `operationId` reapresentado após ACK perdido não duplica efeito.

Pull só roda quando não existem mutações enviáveis, conflicts ou failed pendentes.

## Lote parcialmente rejeitado

Cada operação é transacionada/idempotente de forma independente dentro do batch. Uma operação válida pode receber `Applied` mesmo quando outra retorna Validation/Conflict/Forbidden.

ACK confirmado não é perdido por erro posterior do lote.

## Revogação de acesso

Cenário obrigatório implementado/testado:

1. usuário tem acesso à Farm A;
2. prepara/trabalha offline;
3. acesso é removido;
4. sincronização começa;
5. backend pode aceitar uma operação que passou antes da revogação;
6. revalidação anterior à operação seguinte detecta a remoção;
7. operação posterior recebe `FarmAccessRevoked`;
8. cliente purga records/outbox da farm e mantém metadata `Blocked`.

`401`, `403` e `404` durante sync também acionam política conservadora de bloqueio/purge da cópia local daquele namespace.

## Logout e troca de usuário

- logout explícito limpa material offline;
- login de usuário/organização diferente limpa o contexto anterior;
- reiniciar e entrar novamente com o mesmo usuário preserva a cópia offline válida;
- JWT continua somente na camada de sessão, não no IndexedDB offline.

## Segurança horizontal

Cobertura inclui:

- cross-farm na mesma organização;
- tentativa de payload mover Field para outra farm;
- Season apontando para Field de outra farm;
- Organização A tentando usar Farm da Organização B;
- cursor de outra farm;
- revalidação por operação durante o lote.

## UX

Rota:

```text
/offline
```

A tela oferece:

- escolha explícita de farm;
- `Disponibilizar offline`;
- remover cópia;
- sync manual;
- sync ao reconectar;
- indicador Online/Offline;
- última sincronização;
- contagem de pendências;
- conflitos;
- rejeições;
- CRUD local allowlisted de Fields/Seasons;
- resolução explícita de conflitos.

O sistema não comunica `Synced` quando existe outbox pendente, conflito ou rejeição.

## Observabilidade

Meter:

```text
AgroControl.OfflineSync
```

Métricas:

```text
agrocontrol.sync.batches
agrocontrol.sync.operations
agrocontrol.sync.retries
agrocontrol.sync.invalid_cursors
agrocontrol.sync.access_revocations
agrocontrol.sync.duration
agrocontrol.sync.batch_size
```

Labels permitidas são de baixa cardinalidade (`direction`, `outcome`, `entity`, `replayed`).

Nunca usar `UserId`, `FarmId` ou `operationId` como labels.

## Testes

Backend:

- bootstrap por farm;
- farm fora de escopo;
- crop referenciado ausente;
- cursor válido/adulterado/expirado/outra farm;
- change-log farm-scoped;
- Field movido entre farms;
- idempotency claim/replay/rollback;
- create + replay sem duplicação;
- stale version → Conflict;
- lote parcialmente rejeitado;
- cross-farm;
- revogação no meio do lote;
- cross-organization.

Frontend:

- namespace/chave composta;
- validação de persistência;
- bootstrap e relações;
- pull incremental/ordenação/cross-farm;
- ACK Applied;
- falha de rede → Retryable;
- conflito com snapshot;
- revogação → purge/Blocked;
- cálculo do backoff.

## Migrations

```text
20260909010000_OfflineSyncChangeLog
20260909013000_OfflineSyncIdempotency
```

## Gate final

A Sprint 21 só é considerada encerrada depois de, no mesmo head final:

- Backend CI verde;
- Frontend CI verde;
- Platform CI verde;
- Desktop CI verde;
- CodeQL verde;
- PR #60 fora de draft e squash merged;
- Issue #59 encerrada como `completed`.
