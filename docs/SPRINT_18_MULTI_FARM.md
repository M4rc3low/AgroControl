# Sprint 18 — Operação multi-fazenda

## Objetivo

A Sprint 18 transforma `Farm` em uma fronteira operacional abaixo de `Organization`, permitindo que um mesmo grupo rural administre propriedades em diferentes cidades, estados e fusos horários sem criar tenants separados.

`OrganizationId` continua sendo a fronteira máxima de multi-tenancy. O novo escopo de fazenda não substitui o tenant: ele adiciona autorização horizontal dentro dele.

## Modelo operacional

```text
Organization
├── OperationalRegion
│   ├── Farm
│   │   └── Field
│   └── Farm
└── Farm
```

`OperationalRegion` é opcional e pode agrupar propriedades de um ou vários estados. Ela é uma unidade de gestão, não um novo tenant.

## Localização estruturada

A propriedade mantém compatibilidade com `City` e `State` e acrescenta:

- `CountryCode` ISO 3166-1 alpha-2;
- `StateCode` para UF normalizada no Brasil;
- código municipal opcional;
- CEP opcional;
- latitude/longitude da sede ou centro operacional;
- `OperationalRegionId` opcional;
- timezone IANA por propriedade.

O cadastro valida UF brasileira, coordenadas e timezone conhecido. Dados antigos são migrados com `BR`, UF derivada do estado existente quando possível e `America/Sao_Paulo` como fallback de compatibilidade; esse fallback deve ser revisado em propriedades localizadas em outros fusos.

## Autorização horizontal

A autorização por propriedade é explícita:

- `AllFarms`: acesso a todas as propriedades da organização;
- `Region`: acesso às propriedades atualmente integrantes da região;
- `Farm`: acesso somente à propriedade informada.

Owner/Admin não recebem acesso global por inferência de papel durante a leitura. O cadastro inicial cria explicitamente `AllFarms`, e a migration registra explicitamente esse escopo para Owner/Admin já existentes.

A API resolve o escopo efetivo após autenticação e cria um `OperationalScopeContext` por request. Repositórios EF Core recebem filtros globais para dados vinculados a propriedades. Assim, chamadas de módulos que historicamente recebiam apenas `OrganizationId` também respeitam o escopo horizontal.

## Módulos protegidos

O filtro operacional cobre, conforme a relação de domínio:

- Farm, Field e Season;
- Warehouse e StockMovement;
- FinancialTransaction;
- Machine, HourMeterReading, Fueling e MaintenanceRecord;
- IrrigationZone e IrrigationApplication;
- EmissionActivity;
- ExportOrder e seus documentos, custos e eventos de status;
- CommercialOpportunity e histórico de estágio.

Registros genuinamente organizacionais sem `FarmId`, quando o domínio permite esse conceito, continuam visíveis no tenant. Eles não são atribuídos artificialmente a uma fazenda.

## SQL espacial e raster

Agricultura de Precisão, Sensoriamento Remoto e Raster usam SQL/PostGIS explícito em trechos que não passam pelo LINQ do EF Core. Esses repositórios recebem o mesmo `IOperationalScopeContext` e adicionam a restrição de fazenda diretamente ao SQL.

Isso impede que listagens, contagens, cenas, zonas, resultados raster e operações de escrita revelem ou alterem dados de uma propriedade não autorizada dentro da mesma organização.

## Telemetria

Telemetry possui banco e serviço Java próprios. A API C# acrescenta uma fachada de acesso que:

- filtra dispositivos pela propriedade efetivamente permitida;
- valida Farm, Field e Machine antes de criar um dispositivo;
- oculta dispositivo fora do escopo como recurso inexistente;
- protege status, ingestão, última leitura e histórico pelo mesmo vínculo.

Dispositivos explicitamente organizacionais, sem Farm/Field/Machine, permanecem recursos compartilhados da organização.

## Web

A Web mantém um contexto operacional persistido em `sessionStorage` por organização. O seletor global suporta:

```text
Todas as fazendas
Região
UF
Fazenda
```

`Todas as fazendas` só é oferecido como contexto global quando o escopo efetivo contém `AllFarms`. A seleção é UX; a segurança continua no backend.

O dashboard usa o contexto selecionado para propriedades, talhões, safras e resumo financeiro. O mapa MapLibre mostra apenas propriedades acessíveis que possuem coordenadas e permite entrar diretamente em uma fazenda.

A comparação por propriedade apresenta área, quantidade de talhões, safras ativas e produtividade realizada média quando disponível.

## Fusos horários

Datas técnicas continuam armazenadas em UTC. A propriedade informa o timezone IANA utilizado para apresentação operacional. Quando uma única fazenda está ativa, a interface pode exibir o instante no horário local dela.

Em consolidações de várias propriedades, timestamps continuam comparáveis em UTC e o sistema não finge que todo o grupo possui um único horário local.

Exemplos brasileiros suportados incluem `America/Sao_Paulo`, `America/Cuiaba`, `America/Manaus` e `America/Rio_Branco`.

## Financeiro consolidado

O Finance atual opera em BRL. No contexto `AllFarms`, o resumo usa a consulta consolidada já filtrada pelo escopo efetivo. Para Região/UF, a Web soma os resumos de cada propriedade selecionada e recalcula a margem a partir dos totais, em vez de fazer média de percentuais.

Uma futura evolução multimoeda deverá impedir qualquer soma sem conversão e snapshot de câmbio explícitos.

## Endpoints principais

```text
GET    /api/v1/operations/regions
POST   /api/v1/operations/regions
PUT    /api/v1/operations/regions/{id}
DELETE /api/v1/operations/regions/{id}

GET    /api/v1/operations/farm-access/me
GET    /api/v1/operations/farm-access/users/{userId}
POST   /api/v1/operations/farm-access
DELETE /api/v1/operations/farm-access/{assignmentId}

GET    /api/v1/farms?regionId=&stateCode=
```

A gestão de regiões e permissões exige papel administrativo apropriado. A consulta do próprio escopo fica disponível ao usuário autenticado.

## Migration

A migration oficial é:

```text
20260908173917_MultiFarmRegionalOperations
```

Ela cria `operational_regions`, `farm_access_assignments`, amplia `farms`, cria índices e executa o backfill compatível com os registros existentes.

## Segurança

O desenho adota defesa em profundidade:

1. `OrganizationId` isola tenants;
2. escopo efetivo limita as propriedades dentro do tenant;
3. EF Core aplica query filters nos módulos relacionais;
4. repositórios SQL/PostGIS aplicam a restrição explicitamente;
5. Telemetry valida o vínculo na API C# antes de acessar o serviço Java;
6. IDs fora do escopo são tratados como não encontrados sempre que possível para reduzir enumeração.

O seletor da Web jamais é tratado como mecanismo de autorização.

## Fora do escopo

Não fazem parte desta sprint regras fiscais estaduais, NF-e, folha, roteirização interestadual, sincronização offline completa, meteorologia municipal, CAR/SIGEF ou integração oficial com IBGE. O código municipal foi preparado apenas como identificador opcional para integrações futuras.
