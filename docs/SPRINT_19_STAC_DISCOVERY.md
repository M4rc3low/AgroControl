# Sprint 19 — STAC e descoberta de cenas

## Objetivo

Adicionar descoberta de cenas geoespaciais via STAC ao workspace de Sensoriamento Remoto, preservando as fronteiras de segurança por `OrganizationId` e `FarmAccessScope` implementadas até a Sprint 18.

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

A integração lê STAC Item 1.1.0 e utiliza o contrato de Item Search da STAC API 1.0.0 como baseline interoperável.

O cliente interno normaliza apenas os campos necessários ao AgroControl:

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
RemoteSensingScene
      |
      v
Raster Processing (ação separada)
```

## Segurança de rede

- base URLs de STAC vêm apenas da configuração do backend;
- apenas HTTPS é permitido fora de desenvolvimento local explicitamente controlado;
- `href` de asset não é executado durante descoberta;
- assets processáveis passam por validação de scheme, media type e roles;
- query strings contendo credenciais não devem aparecer em logs;
- redirects não podem transformar um provider configurado em proxy HTTP aberto;
- respostas possuem limite de tamanho e timeout.

## Escopo multi-fazenda

A busca começa a partir de um `FieldId` interno, nunca de uma geometria arbitrária fornecida pelo browser.

Fluxo esperado:

1. API recebe `FieldId` e filtros;
2. backend carrega o talhão respeitando `OrganizationId` + `FarmAccessScope`;
3. backend obtém a geometria canônica do talhão;
4. cliente STAC recebe somente geometria, período e filtros externos necessários;
5. resultado é normalizado;
6. importação volta a validar Field/Season antes de criar `RemoteSensingScene`.

Um usuário limitado à Fazenda A não pode usar descoberta/importação para inferir ou vincular dados da Fazenda B.

## Fase 1 — Fundação

- contratos internos de descoberta;
- interface `IRemoteSceneDiscoveryClient`;
- configuração de providers;
- cliente STAC HTTP;
- parsing seguro de FeatureCollection/Item;
- seleção inicial de assets GeoTIFF/COG;
- testes unitários do parser/validação.

## Fase 2 — Serviço e API

- `RemoteSceneDiscoveryService`;
- consulta de Field/Season com escopo efetivo;
- endpoints `/providers`, `/search` e `/import`;
- idempotência de importação;
- ProblemDetails para timeout/indisponibilidade/payload inválido.

## Fase 3 — Web

- ação `Descobrir cenas`;
- filtros por período/provider/collection/nuvens;
- resultados paginados;
- footprint no mapa;
- detalhes de assets;
- importação explícita;
- estados de loading/vazio/erro/timeout.

## Fase 4 — Qualidade

- testes A × B na mesma organização;
- fake STAC server em integração;
- testes de timeout, 503 e payload inválido;
- frontend tests;
- observabilidade sem alta cardinalidade;
- README, ARCHITECTURE e ROADMAP;
- API `0.19.0`;
- Backend CI, Frontend CI, Platform CI e CodeQL verdes no mesmo head;
- squash merge e encerramento da Issue #53.

## Fora do escopo

- download/cache assíncrono de grandes assets;
- autenticação específica de marketplaces;
- geração automática de índices a partir de bandas brutas;
- cloud masks avançadas;
- mosaico de cenas;
- compra de imagens comerciais;
- decisão automática de melhor cena sem confirmação do usuário.
