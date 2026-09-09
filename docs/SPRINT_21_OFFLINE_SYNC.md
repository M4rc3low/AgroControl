# Sprint 21 — Offline real por fazenda + sincronização controlada

**Status:** concluída e mesclada.  
**API:** `0.21.0`  
**Desktop:** `0.21.0`  
**PR:** #60 — squash merged  
**Issue:** #59 — completed

## Objetivo

Permitir que um usuário autorizado prepare explicitamente uma fazenda para uso offline, continue trabalhando com dados de produção rural sem conexão e sincronize as alterações com segurança quando a rede voltar.

O cliente offline nunca se torna autoridade. Tenant, `FarmAccessScope`, entitlement, concorrência, validações e estado final continuam sendo definidos pelo backend.

## Invariantes

1. `OrganizationId` é a fronteira máxima do tenant.
2. `FarmAccessScope` é a fronteira operacional.
3. Dados locais são isolados por `UserId + OrganizationId + FarmId`.
4. JWT, senha, API key e secrets não entram no OfflineStore.
5. Operações só saem da outbox após ACK válido.
6. Retry após timeout/ACK perdido não duplica efeito.
7. Field/Season não usam last-write-wins silencioso.
8. Revogação de acesso é respeitada na próxima sincronização.
9. O desktop continua sendo cliente da API central; PostgreSQL, C#, Python e Java não são embutidos no instalador.

## Armazenamento local

A SPA usa IndexedDB `agrocontrol.offline` com três stores:

- `records` — cópia allowlisted da fazenda;
- `outbox` — mutações locais pendentes;
- `syncMetadata` — cursor, estado, última sincronização e bloqueios.

O namespace lógico obrigatório é:

```text
UserId + OrganizationId + FarmId
```

O store é versionado e valida chaves antes de persistir. Bootstrap e pull não sobrescrevem trabalho local pendente.

## Allowlist da Sprint 21

### Leitura offline

- Farm selecionada;
- Fields da fazenda;
- Crops efetivamente referenciados;
- Seasons da fazenda.

### Mutação offline

- Field: create/update/delete lógico;
- Season: create/update/delete lógico.

Fora da allowlist inicial:

- financeiro completo;
- telemetria histórica em massa;
- raster/GeoTIFF/COG;
- tiles/mapas em massa;
- documentos pesados;
- modelos de Intelligence;
- PostgreSQL local;
- sidecars C#/Python/Java.

## Bootstrap

Endpoints:

```text
GET /api/v1/sync/status?farmId=...
GET /api/v1/sync/bootstrap?farmId=...
```

O bootstrap é opt-in e limitado a uma única fazenda acessível.

Limites:

- até 2.000 Fields;
- até 10.000 Seasons;
- até 1.000 Crops referenciados.

Usuário com `AllFarms` não recebe download global implícito.

O snapshot é gravado atomicamente no IndexedDB. Se houver outbox pendente, o bootstrap é bloqueado para evitar perda de trabalho offline.

## Pull incremental

Endpoint:

```text
GET /api/v1/sync/pull?farmId=...&cursor=...
```

Características:

- change-log PostgreSQL append-only e monotônico;
- triggers na mesma transação das alterações de negócio;
- cursor opaco e autenticado server-side;
- cursor vinculado à organização e fazenda;
- validade limitada;
- até 500 mudanças por página;
- segunda validação de pertencimento ao materializar entidades;
- Field movido de Farm A para Farm B gera remoção no escopo antigo sem vazar payload da nova fazenda.

O watermark do bootstrap é capturado antes da leitura do snapshot; mudanças concorrentes durante o bootstrap aparecem no pull seguinte.

## Push idempotente

Endpoint:

```text
POST /api/v1/sync/push
```

Características:

- até 100 operações por lote;
- `operationId` UUID gerado no cliente;
- IDs estáveis de entidades criadas offline;
- idempotência persistida por `OrganizationId + UserId + operationId`;
- hash canônico da operação;
- retenção de 30 dias;
- replay do mesmo conteúdo retorna o resultado persistido;
- reutilização do mesmo `operationId` com conteúdo diferente é rejeitada;
- efeito de negócio e resultado idempotente usam a mesma transação PostgreSQL;
- update/delete usam `FOR UPDATE`;
- `serverVersion` usa UTC com precisão compatível com PostgreSQL.

## Outbox e staging

Toda mutação offline é gravada atomicamente como:

```text
registro local + operação de outbox
```

Estados de mutação:

```text
Pending
Retryable
Conflict
Failed
```

Estados de registro:

```text
Clean
PendingCreate
PendingUpdate
PendingDelete
Conflict
Failed
```

O cliente não permite duas mutações pendentes concorrentes sobre a mesma entidade.

## Conflitos

Não existe last-write-wins silencioso.

Quando `BaseServerVersion` não corresponde ao estado atual:

- backend retorna `Conflict`;
- cliente mantém a proposta local;
- snapshot atual do servidor é preservado;
- usuário pode escolher `Usar servidor`;
- ou `Reaplicar minha alteração` com a versão atual e um novo `operationId`.

## Retry e reconexão

Fluxo do sync engine:

```text
push -> resolver pendências -> pull incremental
```

O pull só ocorre quando a outbox não possui operação enviável, conflito ou rejeição pendente.

Falhas transitórias permanecem em `Retryable`. O motor aplica backoff exponencial limitado e também tenta sincronizar após a recuperação da conexão.

## Revogação de acesso

`FarmAccessScope` é revalidado no início da sincronização e antes de cada operação do lote.

Se o acesso for removido durante o período offline ou no meio de um lote:

- ACKs já confirmados permanecem válidos;
- operações posteriores são bloqueadas;
- records/outbox da fazenda são removidos do cliente;
- fica apenas metadata `Blocked` com o motivo;
- nova sincronização exige autorização/preparação válida.

`401`, `403` e `404` durante o fluxo usam política conservadora de bloqueio/purge.

Logout explícito e troca de usuário/organização também limpam material local do contexto anterior. Novo login do mesmo usuário pode preservar uma cópia ainda válida conforme a política implementada.

## UX

Rota:

```text
/offline
```

A interface permite:

- selecionar explicitamente a fazenda;
- `Disponibilizar offline`;
- remover a cópia local;
- sincronizar manualmente;
- sincronizar ao reconectar;
- visualizar estado online/offline;
- visualizar última sincronização;
- ver pendências, conflitos e rejeições;
- criar/editar/desativar Fields e Seasons offline;
- resolver conflitos explicitamente.

## Observabilidade

Meter:

```text
AgroControl.OfflineSync
```

Métricas cobrem:

- batches push/pull;
- latência;
- tamanho de lote;
- operações por resultado;
- retries;
- cursores inválidos/expirados;
- revogação de acesso.

`UserId`, `FarmId` e `operationId` não são usados como labels.

## Segurança

- JWT continua em sessão, não no OfflineStore;
- nenhuma autorização depende do frontend;
- push e pull revalidam escopo no servidor;
- operação não pode escolher arbitrariamente outro tenant;
- payload cross-farm é rejeitado;
- cursor não amplia escopo;
- lote e bootstrap possuem limites explícitos;
- service worker não cacheia dados autenticados;
- Desktop Tauri mantém superfície nativa mínima.

## Testes de fechamento

O head final do PR #60 passou pelos cinco gates no mesmo SHA:

- Backend CI ✅
- Frontend CI ✅
- Platform CI ✅
- Desktop CI ✅
- CodeQL ✅

Backend no gate final:

- 88 testes unitários passando;
- 29 testes de integração passando;
- 0 warnings;
- 0 errors.

Frontend no gate final:

- 45 testes passando;
- typecheck;
- build de produção;
- verificação PWA;
- imagem Docker;
- smoke test.

Cobertura relevante inclui:

- bootstrap e isolamento por fazenda;
- cursor inválido/expirado;
- Field movido entre fazendas;
- idempotência e rollback;
- create + replay sem duplicação;
- stale version/conflict;
- lote parcialmente rejeitado;
- cross-farm;
- cross-organization;
- revogação no meio do lote;
- ACK/retry/conflict/purge no cliente.

## Fechamento

- API `0.21.0`;
- Desktop `0.21.0` após o ajuste final de consistência de release;
- PR #60 squash merged;
- Issue #59 completed;
- Sprint 21 concluída.
