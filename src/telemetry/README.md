# AgroControl Telemetry

Serviço especializado em Java para ingestão e consulta de telemetria agrícola.

## Stack

- Java 21
- Spring Boot 4.1.1
- Spring JDBC
- PostgreSQL 17
- Eclipse Paho MQTT
- Eclipse Mosquitto no ambiente de desenvolvimento
- springdoc-openapi / Swagger UI

## Responsabilidade

O serviço Telemetry possui seu próprio banco de dados e não escreve diretamente nas tabelas internas do monólito C#. A API principal continua responsável por autenticação de usuários, entitlement do módulo e seleção do `OrganizationId`. A comunicação C# → Java usa uma credencial interna configurada fora do código.

No MQTT, o tenant não vem do payload. O serviço extrai o `deviceId` do tópico e resolve a organização a partir do dispositivo previamente registrado.

## Tópico MQTT v1

```text
agrocontrol/v1/devices/{deviceId}/telemetry
```

Payload:

```json
{
  "eventId": "evt-001",
  "capturedAtUtc": "2026-09-07T21:00:00Z",
  "metric": "soil.moisture",
  "numericValue": 42.5,
  "unit": "%",
  "latitude": -23.55,
  "longitude": -46.63,
  "quality": "good"
}
```

`eventId` é opcional. Quando informado, a combinação dispositivo + eventId é idempotente. Eventos são append-only.

## Métricas iniciais convencionadas

```text
machine.hour_meter
machine.fuel_level
machine.latitude
machine.longitude
air.temperature
air.humidity
rain.precipitation
soil.moisture
```

O campo `metric` é extensível; novas métricas que respeitem o contrato não exigem mudança de schema.

## API interna

```text
GET  /health
POST /api/v1/organizations/{organizationId}/devices
GET  /api/v1/organizations/{organizationId}/devices
GET  /api/v1/organizations/{organizationId}/devices/{deviceId}
PATCH /api/v1/organizations/{organizationId}/devices/{deviceId}/status
POST /api/v1/organizations/{organizationId}/devices/{deviceId}/events
GET  /api/v1/organizations/{organizationId}/devices/{deviceId}/latest
GET  /api/v1/organizations/{organizationId}/devices/{deviceId}/events
```

Swagger UI: `/swagger-ui.html`.

Todos os endpoints `/api/v1/**` exigem `X-AgroControl-Internal-Key`. `/health` é público para orquestração.

## Segurança

O Mosquitto incluído no repositório é **somente para desenvolvimento** e aceita conexão anônima. Produção deve usar TLS, identidade por dispositivo, ACLs de tópico, rotação de credenciais e rate limiting no broker/gateway.

Não há controle remoto de máquinas nesta Sprint.
