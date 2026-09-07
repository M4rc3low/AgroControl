# Sprint 5 — Máquinas, manutenção e mercado

## Objetivo

A Sprint 5 adiciona duas capacidades operacionais ao AgroControl:

1. **Machinery** — cadastro e histórico de máquinas/implementos, horímetro, abastecimentos, manutenção e custo operacional rastreado.
2. **Market** — commodities acompanhadas pela organização, histórico de cotações, variação de preço e alertas por preço-alvo.

Os dois módulos pertencem ao plano `Pro` ou superior e são protegidos no backend pelos filtros `ModuleKey.Machinery` e `ModuleKey.Market`.

---

# Machinery

## Modelo

```text
Organization
  └── Machine
        ├── Farm?
        ├── HourMeterReading[]
        ├── Fueling[]
        └── MaintenanceRecord[]
```

### Machine

Campos principais:

- nome;
- código interno único por organização;
- tipo (`Tractor`, `Harvester`, `Sprayer`, `Seeder`, `Implement`, `Truck`, `UtilityVehicle`, `Other`);
- fabricante e modelo opcionais;
- ano opcional;
- propriedade opcional;
- status (`Active`, `Maintenance`, `Inactive`);
- horímetro atual.

O horímetro nunca pode regredir. Ao criar a máquina, o valor inicial também é armazenado como primeira leitura histórica.

### Horímetro

`HourMeterReading` preserva as leituras ao longo do tempo. Abastecimentos e manutenções que informam um horímetro maior que o atual também avançam automaticamente o horímetro da máquina e criam uma leitura de rastreabilidade.

Uma máquina inativa não aceita novas leituras, abastecimentos ou manutenções.

### Abastecimentos

Cada `Fueling` registra:

- litros;
- custo total;
- custo unitário calculado;
- horímetro;
- data/hora;
- observações.

A quantidade deve ser maior que zero, o custo não pode ser negativo e o horímetro informado não pode ser menor que o horímetro atual da máquina.

### Manutenção

`MaintenanceRecord` suporta:

- `Preventive`;
- `Corrective`.

Cada registro guarda descrição, data, horímetro opcional, custo de peças, mão de obra e outros custos. Também pode informar a próxima manutenção por data e/ou horímetro.

O custo total é calculado como:

```text
custo de manutenção = peças + mão de obra + outros custos
```

## Custo operacional rastreado

O resumo por máquina retorna:

```text
custo total = custo de combustível + custo de manutenção
horas rastreadas = maior leitura - menor leitura
custo/hora rastreada = custo total / horas rastreadas
```

`costPerTrackedHour` só é informado quando há intervalo positivo de horímetro. Isso evita apresentar um custo/hora enganoso quando ainda não existe histórico suficiente.

O indicador representa **custos registrados no AgroControl**, não um custo contábil completo. Depreciação, juros, seguro, operador e outros componentes poderão ser incorporados posteriormente.

## Endpoints Machinery

Todos exigem autenticação e acesso ao módulo `Machinery`.

```text
GET    /api/v1/machinery/machines
GET    /api/v1/machinery/machines/{id}
POST   /api/v1/machinery/machines
PUT    /api/v1/machinery/machines/{id}
DELETE /api/v1/machinery/machines/{id}

POST /api/v1/machinery/machines/{id}/hour-meter
GET  /api/v1/machinery/machines/{id}/hour-meter

POST /api/v1/machinery/machines/{id}/fuelings
GET  /api/v1/machinery/machines/{id}/fuelings

POST /api/v1/machinery/machines/{id}/maintenance
GET  /api/v1/machinery/machines/{id}/maintenance

GET /api/v1/machinery/machines/{id}/cost-summary
```

A listagem de máquinas aceita paginação, status, propriedade e busca textual. Históricos de horímetro, abastecimento e manutenção são paginados.

---

# Market

## Modelo

```text
Organization
  └── Commodity
        ├── MarketQuote[]
        └── PriceAlert[]
```

### Commodity

Cada organização define quais commodities deseja acompanhar. O cadastro possui:

- nome;
- símbolo único por organização;
- moeda padrão;
- unidade padrão;
- status ativo/inativo.

Exemplo conceitual:

```text
Nome: Soja
Símbolo: SOJA
Moeda: BRL
Unidade: sc
```

O modelo não presume uma bolsa ou provedor específico.

### Cotações

`MarketQuote` é append-only e preserva:

- preço;
- moeda;
- unidade;
- fonte;
- data/hora da cotação.

No MVP, as cotações são registradas manualmente ou por integração futura. Se moeda ou unidade não forem enviadas, o sistema usa os padrões cadastrados na commodity.

O histórico nunca é substituído por uma única cotação atual. Isso deixa a base preparada para gráficos, séries temporais e o futuro serviço Python de inteligência.

## Variação de mercado

O resumo de uma commodity compara as duas cotações mais recentes:

```text
variação absoluta = preço atual - preço anterior
variação percentual = variação absoluta / preço anterior × 100
```

Sem duas cotações, a variação permanece nula.

## Alertas de preço

Um `PriceAlert` possui preço-alvo e direção:

- `AboveOrEqual` — acionado quando o último preço é maior ou igual ao alvo;
- `BelowOrEqual` — acionado quando o último preço é menor ou igual ao alvo.

O endpoint de alertas devolve a cotação mais recente conhecida e o campo `Triggered`. Nesta etapa o alerta é uma condição consultável; notificações push/e-mail não fazem parte do MVP.

## Provedores externos

A camada de aplicação contém o contrato `IMarketQuoteProvider`. Um futuro adaptador pode consumir uma API externa e normalizar o resultado para:

```text
symbol
price
currency
unit
source
quotedAtUtc
```

O domínio e o banco de dados, portanto, não ficam acoplados a uma API de cotações específica.

## Endpoints Market

Todos exigem autenticação e acesso ao módulo `Market`.

```text
GET    /api/v1/market/commodities
GET    /api/v1/market/commodities/{id}
POST   /api/v1/market/commodities
PUT    /api/v1/market/commodities/{id}
DELETE /api/v1/market/commodities/{id}

POST /api/v1/market/commodities/{id}/quotes
GET  /api/v1/market/commodities/{id}/quotes
GET  /api/v1/market/commodities/{id}/summary

POST   /api/v1/market/commodities/{id}/alerts
GET    /api/v1/market/alerts
DELETE /api/v1/market/alerts/{id}
```

---

# Multi-tenancy e integridade

- todos os registros armazenam `OrganizationId`;
- repositórios filtram todas as consultas pela organização autenticada;
- máquinas só podem ser relacionadas a propriedades da mesma organização;
- código interno de máquina é único por organização;
- símbolo de commodity é único por organização;
- cotações e alertas só são criados para commodities pertencentes à organização;
- módulos desabilitados são bloqueados também na API, não apenas na interface.

# Testes

A Sprint inclui testes unitários para regras do domínio e testes de integração com PostgreSQL real para:

- isolamento de máquinas entre organizações;
- histórico de horímetro;
- agregação de combustível e manutenção;
- isolamento de commodities;
- preservação do histórico de cotações;
- ordenação das últimas cotações;
- alertas de preço.

# Integrações futuras

## Finance

Os custos de combustível e manutenção estão estruturados para uma futura integração com `Finance`. A automação deverá definir idempotência, categoria financeira, centro de custo e política de estorno antes de criar lançamentos automaticamente.

## Telemetry

A Sprint 7 poderá enviar horímetro, combustível e eventos de máquina automaticamente. Nesta sprint, os dados operacionais entram pela API principal.

## Intelligence

O histórico de commodities poderá alimentar o serviço Python da Sprint 6 para análise de séries temporais, comparação de cenários e impacto de preços na margem da safra.

# Fora do escopo

- telemetria em tempo real e MQTT;
- manutenção preditiva com IA;
- depreciação contábil completa;
- integração bancária ou fiscal;
- negociação automática;
- ordens em bolsa ou integração com corretoras;
- garantia de cotações em tempo real;
- recomendações de compra/venda de commodities.
