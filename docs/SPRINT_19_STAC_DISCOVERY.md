# Sprint 19 — STAC e descoberta de cenas

## Status

**Concluída — API 0.19.0**

A Sprint 19 adiciona descoberta e importação controlada de cenas geoespaciais via STAC ao workspace de Sensoriamento Remoto, preservando as fronteiras de segurança por `OrganizationId` e `FarmAccessScope` consolidadas na Sprint 18.

A descoberta é externa e transitória. O AgroControl só persiste uma cena quando o usuário escolhe explicitamente importar um item para o fluxo existente de `RemoteSensingScene` e processamento raster.

## Princípios

- `RemoteSensingScene` continua sendo o modelo persistido canônico;
- STAC é uma fonte externa de descoberta, não um novo domínio paralelo;
- providers são configurados no backend e identificados por uma chave estável;
- o frontend nunca fornece uma URL arbitrária de catálogo;
- busca não baixa nem processa raster automaticamente;
- importação e processamento permanecem ações separadas;
- `Provider + ExternalId` preserva idempotência por organização;
- URLs externas são tratadas como input não confiável até serem validadas.

## Contrato STAC adotado

A integração normaliza STAC Item/ItemCollection e usa o contrato de Item Search como baseline interoperável.

O cliente interno expõe somente os campos necessários ao AgroControl:

- provider;
- collection;
- item id;
- datetime de aquisição;
- geometry/bbox;
- `eo:cloud_cover` quando presente;
- `gsd` quando presente;
- platform/constellation quando presentes;
- assets candidatos.

Payloads STAC completos não são persistidos nem repassados integralmente ao frontend.

## Arquitetura

```text
AgroControl Web
      |
      v
Remote Sensing Discovery API
      |
      v
RemoteSceneDiscoveryService
      |--------------------------> FarmAccessScope / Field / Season
      |
      v
IRemoteSceneDiscoveryClient
      |
      v
STAC provider configurado

Import explícito
      |
      v
reconsulta Provider + Collection + ExternalId
      |
      v
validação do AssetKey
      |
      v
RemoteSensingScene
      |
      v
Raster Processing (ação separada)
```

## Endpoints

```text
GET  /api/v1/precision/remote-sensing/discovery/providers
POST /api/v1/precision/remote-sensing/discovery/search
POST /api/v1/precision/remote-sensing/discovery/import
```

## Provider inicial

O repositório possui configuração inicial para **Earth Search v1**, com coleções públicas suportadas pelo provider configurado. A arquitetura não acopla o domínio ao Earth Search: novos providers entram por configuração e pelo contrato `IRemoteSceneDiscoveryClient`.

## Segurança de rede

- base URLs de STAC vêm apenas da configuração do backend;
- HTTPS é exigido fora de loopback/local controlado;
- `href` de asset não é executado durante descoberta;
- assets processáveis passam por validação de scheme, media type e roles;
- query strings são removidas das referências normalizadas de assets;
- credenciais embutidas em URLs não são aceitas;
- paginação só aceita continuação no mesmo origin e mesmo path do endpoint de busca;
- respostas possuem limite de tamanho;
- `HttpClient` dedicado possui timeout e cancellation token;
- falha do provider não interfere no CRUD/listagem de cenas persistidas.

## Escopo multi-fazenda

A busca começa a partir de um `FieldId` interno, nunca de uma geometria arbitrária fornecida pelo browser.

Fluxo:

1. API recebe `FieldId`, `SeasonId` opcional e filtros;
2. backend carrega o talhão respeitando `OrganizationId` + `FarmAccessScope`;
3. backend obtém o boundary canônico do talhão;
4. cliente STAC recebe somente geometria, período e filtros externos necessários;
5. resultado é normalizado;
6. importação revalida Field/Season;
7. backend reconsulta o item no provider por `Provider + Collection + ExternalId`;
8. somente o `AssetKey` escolhido e validado como raster elegível pode virar `AssetReference`.

Um usuário limitado à Fazenda A não consegue usar descoberta/importação para consultar ou vincular dados da Fazenda B. A suíte de integração valida esse cenário dentro da mesma organização e exige que o provider nem seja chamado quando o talhão está fora do escopo.

## Web

A Web possui a rota:

```text
/precision/remote-sensing/discovery
```

Funcionalidades entregues:

- ação `Descobrir cenas` no workspace;
- provider e collection vindos do backend;
- filtros por período e cobertura máxima de nuvens;
- resultados paginados;
- metadados de aquisição, resolução, plataforma e quantidade de assets raster;
- preview de footprint em MapLibre comparado ao limite do talhão;
- importação explícita por asset;
- feedback de loading, vazio e erro;
- layout responsivo;
- preservação do contexto de talhão/safra e do escopo operacional global.

## Observabilidade

Meter dedicado:

```text
AgroControl.RemoteSceneDiscovery
```

Métricas:

- `agrocontrol.stac.searches`;
- `agrocontrol.stac.search.duration`;
- `agrocontrol.stac.search.results`;
- `agrocontrol.stac.imports`.

Labels são limitadas a provider configurado e outcome. `itemId`, URL de asset, `farmId`, `userId` e tokens não são usados como labels.

## Testes entregues

### Serviço de aplicação

- [x] busca usa o boundary canônico do Field;
- [x] Field fora do escopo impede chamada ao provider;
- [x] asset não-raster é rejeitado;
- [x] importação reconsulta o item no provider;
- [x] somente o asset selecionado é persistido;
- [x] idempotência continua delegada ao `RemoteSensingScene` por `Provider + ExternalId`.

### Cliente STAC

- [x] parsing e normalização de item;
- [x] cloud cover/GSD/platform/constellation;
- [x] seleção de GeoTIFF/COG candidatos;
- [x] remoção de query string de asset;
- [x] rejeição de paginação cross-origin;
- [x] continuação same-origin/same-path;
- [x] provider desconhecido/desabilitado é rejeitado antes da rede;
- [x] 404 de item é mapeado para NotFound;
- [x] timeout, indisponibilidade e payload inválido possuem resultados explícitos no contrato.

### Integração e escopo

- [x] PostgreSQL/PostGIS real;
- [x] Fazenda A × Fazenda B dentro da mesma organização;
- [x] Field da Fazenda B não pode ser usado para busca/importação pelo usuário restrito à A;
- [x] provider não é chamado quando o contexto está fora do escopo.

### Frontend

- [x] helpers de seleção de asset raster;
- [x] chave estável de importação;
- [x] parsing seguro do footprint para preview;
- [x] TypeScript, Vitest e build de produção no Frontend CI.

## Entregas

- [x] contratos internos de descoberta;
- [x] `IRemoteSceneDiscoveryClient`;
- [x] cliente STAC HTTP;
- [x] provider configurado no backend;
- [x] `RemoteSceneDiscoveryService`;
- [x] endpoints `/providers`, `/search` e `/import`;
- [x] importação controlada para `RemoteSensingScene`;
- [x] segurança A × B;
- [x] Web de descoberta e footprint;
- [x] métricas OpenTelemetry;
- [x] documentação;
- [x] API `0.19.0`.

## Fora do escopo

Ficam para sprints posteriores:

- download/cache assíncrono de grandes assets;
- autenticação específica de marketplaces/provedores;
- geração automática de índices a partir de bandas brutas;
- cloud masks avançadas;
- mosaico de cenas;
- compra de imagens comerciais;
- decisão automática de melhor cena sem confirmação do usuário;
- sincronização offline/desktop.

## Gate de encerramento

A Sprint só deve ser considerada mesclada quando **Backend CI + Frontend CI + Platform CI + CodeQL** estiverem verdes no mesmo head do PR #56. O merge deve ser por squash e a Issue #53 deve ser encerrada somente depois do merge.
