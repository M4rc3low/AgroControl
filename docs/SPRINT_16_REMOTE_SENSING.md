# Sprint 16 — Sensoriamento remoto e índices vegetativos

## Objetivo

A Sprint 16 adiciona ao AgroControl um núcleo rastreável de **sensoriamento remoto** para registrar cenas de satélite/drone e acompanhar índices vegetativos por talhão, safra e zona de manejo.

O objetivo é preservar contexto, fonte, data e estatísticas usadas no acompanhamento da lavoura sem transformar NDVI, NDRE ou EVI em diagnóstico agronômico automático.

## Arquitetura

O módulo permanece dentro de `PrecisionAgriculture` e usa a mesma fronteira de autorização e multi-tenancy do restante da Agricultura de Precisão.

Responsabilidades:

- C# / ASP.NET Core: regras de domínio, contexto produtivo, API e metadados;
- PostgreSQL/PostGIS: persistência relacional, footprint espacial, índices e isolamento por organização;
- React/TypeScript: linha do tempo, filtros, indicadores, gráfico temporal e mapa;
- raster pesado: permanece fora do banco relacional principal e é representado por uma referência externa de asset.

A futura leitura/processamento de bandas e estatística zonal raster deve ser executada por uma camada especializada, preferencialmente o AgroControl Intelligence/Python, sem mover processamento raster pesado para o monólito C#.

## Domínio

### RemoteSensingScene

Cada cena possui:

- `OrganizationId` e `FieldId` obrigatórios;
- `SeasonId` opcional;
- `Provider` e `ExternalId`;
- plataforma `Satellite`, `Drone` ou `Other`;
- `AcquiredAtUtc`;
- cobertura de nuvens opcional entre 0% e 100%;
- resolução espacial opcional em metros;
- referência externa para o asset processado;
- observações;
- footprint opcional WGS84/PostGIS;
- status ativo e timestamps.

A chave `(OrganizationId, Provider, ExternalId)` é única. Ela funciona como idempotency key operacional: a mesma cena externa não pode ser cadastrada duas vezes no mesmo tenant/provedor.

`Provider` e `ExternalId` não são alterados depois da criação. Uma mudança nesses identificadores representa outra referência externa e deve ser tratada como outra cena.

A desativação é lógica para preservar o histórico de observações associado à cena.

### VegetationIndexObservation

As observações são **append-only**. Não existem endpoints de update ou delete.

Tipos iniciais:

- `NDVI`;
- `NDRE`;
- `EVI`;
- `Custom`.

Cada observação preserva:

- cena, talhão, safra e zona opcional;
- mínimo, máximo, média, mediana e desvio-padrão;
- percentual de cobertura válida;
- quantidade de amostras/pixels quando disponível;
- snapshot da fonte (`Provider` da cena);
- snapshot da data da observação (`AcquiredAtUtc` da cena);
- timestamp de criação.

Para `Custom`, o nome do índice é obrigatório.

No contrato normalizado atual do AgroControl, NDVI, NDRE e EVI aceitam valores entre `-1` e `1`. Essa faixa é uma convenção de validação da plataforma para os indicadores padronizados desta sprint; integrações futuras que utilizem outra convenção precisam normalizar explicitamente os valores antes do registro ou introduzir um contrato versionado próprio.

## Regras de contexto

- Field precisa pertencer à organização atual e estar ativo.
- Season, quando informada, precisa pertencer à mesma organização, ao mesmo Field e estar ativa no momento do cadastro/edição da cena.
- ManagementZone, quando informada numa observação, precisa pertencer à mesma organização, estar ativa e pertencer exatamente ao Field da cena.
- Uma cena inativa continua consultável, mas não recebe novas observações.
- Nenhuma consulta ou gravação atravessa `OrganizationId`.

O footprint representa a cobertura da cena e, por isso, pode legitimamente ultrapassar o limite de um talhão. Ele precisa ser um `Polygon` WGS84 topologicamente válido, mas não é recortado silenciosamente contra o Field.

## Persistência

Migration SQL espacial:

`20260908150000_RemoteSensingCore`

Tabelas:

- `remote_sensing_scenes`;
- `vegetation_index_observations`.

Principais índices:

- unicidade por organização/provedor/id externo;
- organização + talhão + data;
- organização + safra + data;
- organização + plataforma;
- GiST no footprint;
- organização + zona + data;
- organização + tipo de índice + data;
- índice por cena.

Assim como os limites de talhão e `management_zones`, o footprint PostGIS é controlado por migration SQL e repositório espacial explícito. A Sprint 16 não finge que essa tabela espacial está mapeada no model snapshot do EF Core quando ela não está.

## API

Base protegida por autenticação e entitlement `PrecisionAgriculture`:

`/api/v1/precision/remote-sensing`

### Cenas

- `GET /scenes`
- `GET /scenes/{sceneId}`
- `POST /scenes`
- `PUT /scenes/{sceneId}`
- `DELETE /scenes/{sceneId}` — desativação lógica

Filtros de cenas incluem talhão, safra, plataforma, provedor, intervalo de aquisição e inativos.

### Observações

- `GET /observations`
- `POST /observations`

Filtros incluem cena, talhão, safra, zona, tipo de índice e período.

### Série e resumo

- `GET /series`
- `GET /summary`

A série temporal pode ser filtrada por talhão, safra, zona, índice e período. O resumo retorna a quantidade de cenas no contexto e a observação mais recente por índice.

## AgroControl Web

A rota `/precision/remote-sensing` entrega:

- filtros por talhão, safra, zona e índice;
- cadastro de metadados de cena;
- linha do tempo de cenas;
- cobertura de nuvens e resolução espacial;
- cadastro de observações NDVI/NDRE/EVI/Custom;
- cards com índices mais recentes;
- gráfico temporal por índice;
- fonte e data visíveis em cada contexto relevante;
- mapa MapLibre com limite do talhão e footprint da cena selecionada;
- desativação de cena preservando o histórico;
- estados de loading, vazio, erro e módulo bloqueado;
- layout responsivo.

O raster não é carregado nem armazenado como blob nessa interface. A tela trabalha com metadados, referências e estatísticas processadas.

## Testes

### Unidade

Cobertura de:

- normalização de metadados e UTC da cena;
- cobertura de nuvens e resolução inválidas;
- faixa normalizada dos índices padronizados;
- nome obrigatório para índice customizado;
- preservação das estatísticas.

### Integração PostgreSQL/PostGIS

Cobertura de:

- isolamento multi-tenant;
- idempotência de `Provider + ExternalId` por organização;
- vínculo com Field/Season;
- footprint PostGIS;
- observação ligada à ManagementZone;
- rejeição de índice inválido;
- consulta de série temporal;
- resumo/latest metric;
- preservação do histórico após desativação da cena;
- bloqueio de novas observações em cena inativa.

### Frontend

Testes dos helpers de série temporal cobrem agrupamento por índice, ordenação cronológica e geração estável do path SVG.

## Limites e segurança de interpretação

NDVI, NDRE, EVI e índices customizados são **indicadores de sensoriamento remoto e apoio à decisão**. O AgroControl não interpreta uma queda ou aumento do índice como diagnóstico automático de deficiência nutricional, praga, doença, estresse hídrico ou produtividade.

A interpretação depende de cultura, estágio fenológico, sensor, calibração, atmosfera, solo, iluminação, qualidade da máscara e contexto de campo. A UI mantém fonte e data visíveis justamente para reduzir leitura fora de contexto.

## Fora do escopo

Ficam para fases posteriores:

- download automático de provedores que exigem credenciais;
- correção atmosférica;
- ortomosaico;
- processamento raster distribuído;
- mascaramento avançado de nuvens;
- voo autônomo de drone;
- classificação automática de doença/praga;
- recomendação agronômica automática;
- geração automática de mapa de prescrição;
- armazenamento de raster pesado no PostgreSQL.
