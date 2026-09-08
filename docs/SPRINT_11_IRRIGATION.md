# Sprint 11 — Irrigação

## Objetivo

A Sprint 11 transforma o módulo `Irrigation` em uma capacidade operacional do AgroControl. A solução conecta talhões, zonas de irrigação, aplicações reais de água e a leitura mais recente de umidade do solo já armazenada pelo serviço de Telemetry.

O módulo oferece **apoio à decisão**. Ele não aciona bombas, pivôs, válvulas, CLPs ou qualquer equipamento de forma autônoma.

## Modelo

### IrrigationZone

Cada zona pertence a uma organização e a um talhão. Mantém área, método de irrigação, limites mínimo/alvo/máximo de umidade e um vínculo opcional com um dispositivo de telemetria. A área deve ser positiva e não pode superar a área cadastral do talhão.

Os limites seguem `0 <= minimum < target < maximum <= 100`.

### IrrigationApplication

Aplicações são append-only. O registro preserva a área da zona no momento do lançamento e grava o volume estimado em m³, usando:

`volume m³ = lâmina mm × área ha × 10`

Correções são novos registros com origem `Correction`; o histórico anterior não é reescrito.

## Telemetria e decisão

A métrica canônica é `soil_moisture_percent`. O banco principal não duplica a série temporal: a API consulta o serviço Java de Telemetry pelo `ITelemetryClient`.

Classificação:

- abaixo do mínimo: `Critical` → `Irrigate`;
- entre mínimo e alvo: `Dry` → `Irrigate`;
- entre alvo e máximo: `Target` → `Monitor`;
- acima do máximo: `Wet` → `AvoidIrrigation`.

Ausência de dispositivo ou leitura gera orientação de monitoramento. Timeout e indisponibilidade do serviço de Telemetry são expostos como `504` e `503`.

## API

```text
GET    /api/v1/irrigation/zones
GET    /api/v1/irrigation/zones/{id}
POST   /api/v1/irrigation/zones
PUT    /api/v1/irrigation/zones/{id}
DELETE /api/v1/irrigation/zones/{id}
GET    /api/v1/irrigation/zones/{id}/status
GET    /api/v1/irrigation/applications
POST   /api/v1/irrigation/applications
GET    /api/v1/irrigation/summary
```

Todos os endpoints exigem autenticação, `OrganizationId` e entitlement `Irrigation`.

## Web

A rota `/irrigation` apresenta indicadores agregados, cartões por zona, última umidade disponível, classificação hídrica, recomendação, histórico de aplicações e formulários para criação de zona e lançamento manual de água.

## Persistência e testes

A migration `IrrigationCore` cria `irrigation_zones` e `irrigation_applications`, com índices por organização, talhão, zona e data. Os testes unitários cobrem regras de domínio, volume e decisão; a integração usa PostgreSQL real e valida isolamento multi-tenant e integração do serviço com um `ITelemetryClient` controlado.
