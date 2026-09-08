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

O cadastro valida UF brasileira, coordenadas e timezone conhecido. Dados antigos são migrados inicialmente com `BR`, UF derivada do estado existente quando possível e `America/Sao_Paulo` como fallback de compatibilidade. A migration de hardening `20260908193000_MultiFarmTimezoneBackfillHardening` corrige automaticamente o fallback para MT, MS, AM, AC, RO e RR quando a UF está disponível. Propriedades do Amazonas que operem no fuso ocidental ou registros sem UF normalizada continuam podendo receber `TimeZoneId` explicitamente.

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

Registros genuinamente organizacionais sem vínculo com uma propriedade, quando o domínio permite esse conceito, continuam visíveis no tenant. Eles não são atribuídos artificialmente a uma fazenda.

Além dos filtros, as operações de escrita normalizam e validam referências Farm/Field/Season no mesmo tenant. IDs de uma propriedade fora do escopo não devem ser usados para inferir existência de recursos de outra fazenda.

Para registros legados, a migration `20260908194500_MultiFarmIndirectFarmBackfill` preenche `FarmId` quando a fazenda já pode ser inferida por `FieldId` ou `SeasonId` em Estoque, Financeiro, Sustentabilidade, Exportação e Comercial. Isso elimina a ambiguidade entre um registro realmente organizacional e um registro antigo que já pertencia a uma fazenda por referência indireta.

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

O dashboard usa o contexto selecionado para propriedades, talhões, safras e resumo financeiro. O mapa MapLibre mostra apenas propriedades acessíveis que possuem coordenadas, ajusta o enquadramento às propriedades visíveis e permite entrar diretamente em uma fazenda.

A comparação por propriedade apresenta apenas métricas que podem ser comparadas com segurança no contexto atual: área, quantidade de talhões, safras ativas e safras concluídas. Produtividade não é agregada entre safras sem que a unidade seja explicitamente compatível.

## Fusos horários

Datas técnicas continuam armazenadas em UTC. A propriedade informa o timezone IANA utilizado para apresentação operacional. Quando uma única fazenda está ativa, a interface pode exibir o instante no horário local dela.

Em consolidações de várias propriedades, timestamps continuam comparáveis em UTC e o sistema não finge que todo o grupo possui um único horário local.

A suíte frontend valida explicitamente o mesmo instante UTC em `America/Sao_Paulo`, `America/Cuiaba`, `America/Manaus` e `America/Rio_Branco`, cobrindo o cenário operacional SP × MT × AM × AC.

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

## Migrations

A migration estrutural da Sprint 18 é:

```text
20260908173917_MultiFarmRegionalOperations
```

Ela cria `operational_regions`, `farm_access_assignments`, amplia `farms`, cria índices e executa o backfill compatível com os registros existentes.

O hardening de timezone acrescenta:

```text
20260908193000_MultiFarmTimezoneBackfillHardening
```

Essa migration corrige os timezones operacionais de UFs brasileiras fora do horário de Brasília quando o registro legado ainda conserva o fallback `America/Sao_Paulo`. O `Down` não reverte dados para evitar sobrescrever correções explícitas feitas depois da migração.

A normalização de vínculos indiretos acrescenta:

```text
20260908194500_MultiFarmIndirectFarmBackfill
```

Essa migration materializa `FarmId` a partir de Field/Season em registros legados dos módulos que aceitam vínculos opcionais. O `Down` também é intencionalmente irreversível, porque apagar o `FarmId` recuperado destruiria informação e poderia reabrir uma ambiguidade de autorização horizontal.

## Matriz de segurança horizontal

A validação automatizada usa uma organização com duas propriedades, Fazenda A e Fazenda B, e um usuário cujo escopo efetivo contém somente A.

Os testes existentes cobrem Financeiro, Agricultura de Precisão, Sensoriamento Remoto e Raster. O hardening final acrescenta uma matriz explícita para:

- Estoque;
- Máquinas;
- Irrigação;
- Sustentabilidade;
- Exportação;
- Comercial.

A matriz verifica listagem e tentativa de acesso direto por ID. Exportação e Comercial também verificam entidades-filhas para impedir vazamento indireto por documentos, eventos ou históricos ligados à Fazenda B.

## Segurança

O desenho adota defesa em profundidade:

1. `OrganizationId` isola tenants;
2. escopo efetivo limita as propriedades dentro do tenant;
3. EF Core aplica query filters nos módulos relacionais;
4. repositórios SQL/PostGIS aplicam a restrição explicitamente;
5. Telemetry valida o vínculo na API C# antes de acessar o serviço Java;
6. IDs fora do escopo são tratados como não encontrados sempre que possível para reduzir enumeração;
7. dados legados com vínculo indireto recebem `FarmId` canônico antes de depender do novo escopo;
8. testes A × B na mesma organização verificam a autorização horizontal nos módulos vinculados a fazenda.

O seletor da Web jamais é tratado como mecanismo de autorização.

## Critério de fechamento

Implementação funcional não encerra a Sprint por si só. A Sprint 18 só deve ser considerada concluída quando o pull request contra `main` tiver passado, no mesmo head revisado, pelos gates aplicáveis:

- Backend CI;
- Frontend CI;
- Platform CI;
- CodeQL.

Falha em qualquer gate mantém a Sprint aberta. O merge final deve ser `squash`, e a Issue #50 só deve ser encerrada após o merge aprovado pelos checks.

## Fora do escopo

Não fazem parte desta sprint regras fiscais estaduais, NF-e, folha, roteirização interestadual, sincronização offline completa, meteorologia municipal, CAR/SIGEF ou integração oficial com IBGE. O código municipal foi preparado apenas como identificador opcional para integrações futuras.
