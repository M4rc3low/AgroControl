# Sprint 7 — AgroControl Telemetry

## Objetivo

A Sprint 7 introduz o primeiro serviço especializado em **Java** do AgroControl. O `AgroControl Telemetry` recebe, valida, normaliza, persiste e disponibiliza dados de dispositivos agrícolas sem acoplar o monólito C# ao protocolo MQTT.

A fronteira arquitetural é deliberada:

```text
Sensores / GPS / estação / máquina
            │
            ▼
       MQTT / REST interno
            │
            ▼
AgroControl Telemetry
Java 21 + Spring Boot
            │
            ├── PostgreSQL próprio
            │
            ▼
AgroControl API (.NET)
            │
            ▼
         Usuário
```

O monólito continua dono da autenticação de usuários, assinatura, entitlement e `OrganizationId`. O serviço Java é dono do registro operacional de dispositivos e do histórico de telemetria.

## Stack

- Java 21;
- Spring Boot 4.1.1;
- Spring JDBC;
- PostgreSQL 17 dedicado ao serviço;
- Eclipse Paho MQTT;
- Eclipse Mosquitto no ambiente local;
- springdoc-openapi / Swagger UI;
- Docker e Docker Compose;
- GitHub Actions.

A escolha de Spring Boot 4.1 segue a linha estável atual do framework. O código usa Java 21 para manter uma base LTS moderna.

## Separação de dados

O Telemetry possui banco próprio no ambiente de desenvolvimento:

```text
agrocontrol_telemetry
```

Ele **não grava diretamente** nas tabelas de `Farm`, `Field`, `Machinery` ou qualquer outro módulo do backend C#.

As referências opcionais `machineId`, `farmId` e `fieldId` são identificadores de correlação, e não chaves estrangeiras para o banco principal. Isso preserva a autonomia do serviço e evita acoplamento de schema entre C# e Java.

## Registro de dispositivos

Cada dispositivo contém:

- `id` UUID interno;
- `organizationId`;
- `externalKey` estável e único dentro da organização;
- tipo;
- vínculo opcional com máquina, propriedade ou talhão;
- status `ACTIVE` ou `INACTIVE`;
- `credentialFingerprint` opcional para preparar identidades futuras sem armazenar segredo em código;
- timestamps de criação e alteração.

Tipos iniciais:

```text
MACHINE
GPS
WEATHER_STATION
FIELD_SENSOR
OTHER
```

Um dispositivo inativo não aceita novos eventos.

## Autoridade do tenant

Uma decisão crítica da Sprint é que o `OrganizationId` **não é aceito como autoridade a partir do payload MQTT**.

O tópico possui somente o identificador do dispositivo:

```text
agrocontrol/v1/devices/{deviceId}/telemetry
```

Quando a mensagem chega, o Java consulta o dispositivo registrado e obtém a organização associada a ele. Assim, alterar um campo no JSON não permite publicar em nome de outro tenant.

Quando o acesso vem pela API principal, o C# extrai o `OrganizationId` do JWT do usuário e monta a rota interna para o serviço Java. O usuário não escolhe o tenant arbitrariamente.

## Contrato de evento v1

Exemplo:

```json
{
  "eventId": "evt-2026-0001",
  "capturedAtUtc": "2026-09-07T21:00:00Z",
  "metric": "soil.moisture",
  "numericValue": 42.5,
  "unit": "%",
  "latitude": -23.5505,
  "longitude": -46.6333,
  "quality": "good",
  "source": "sensor-controller",
  "metadata": {
    "depth": "20cm"
  }
}
```

O contrato aceita exatamente um valor por evento:

- `numericValue`; ou
- `textValue`.

Nunca os dois ao mesmo tempo.

## Validação

O serviço rejeita:

- timestamp ausente;
- timestamp excessivamente no futuro;
- evento fora da janela de ingestão aceita;
- métrica estruturalmente inválida;
- unidade ausente;
- valor numérico não finito;
- texto acima do limite;
- somente uma das coordenadas latitude/longitude;
- latitude ou longitude fora das faixas geográficas;
- metadata excessiva ou estruturalmente inválida;
- payload MQTT acima do limite configurado.

Mensagens inválidas são registradas como rejeitadas no log do consumidor e não derrubam o processo MQTT.

## Métricas iniciais

Convenções iniciais:

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

O schema não possui enum rígido para métrica. Novas métricas podem ser introduzidas desde que respeitem o contrato e a convenção de nomenclatura.

## Append-only e idempotência

Eventos de telemetria são históricos e não são atualizados depois de inseridos.

Quando `eventId` é informado, existe unicidade por:

```text
deviceId + eventId
```

Se a mesma mensagem chegar novamente, ela é reconhecida como duplicada e não gera um segundo evento. Isso é importante porque protocolos e gateways podem reenviar mensagens.

## MQTT

Broker local:

```text
Eclipse Mosquitto 2
```

QoS padrão:

```text
1
```

O consumidor utiliza reconexão automática. O tópico é:

```text
agrocontrol/v1/devices/+/telemetry
```

A configuração em `infra/mosquitto/mosquitto.conf` permite acesso anônimo **somente no ambiente de desenvolvimento**.

Em produção o desenho exige:

- TLS;
- credenciais ou certificados por dispositivo/gateway;
- ACL de tópicos;
- rotação e revogação de credenciais;
- limites por dispositivo;
- observabilidade e auditoria do broker.

## API interna do serviço Java

Health:

```text
GET /health
```

Dispositivos:

```text
POST  /api/v1/organizations/{organizationId}/devices
GET   /api/v1/organizations/{organizationId}/devices
GET   /api/v1/organizations/{organizationId}/devices/{deviceId}
PATCH /api/v1/organizations/{organizationId}/devices/{deviceId}/status
```

Telemetria:

```text
POST /api/v1/organizations/{organizationId}/devices/{deviceId}/events
GET  /api/v1/organizations/{organizationId}/devices/{deviceId}/latest
GET  /api/v1/organizations/{organizationId}/devices/{deviceId}/events
```

Swagger UI:

```text
/swagger-ui.html
```

Os endpoints `/api/v1/**` exigem:

```text
X-AgroControl-Internal-Key
```

A credencial é uma identidade **serviço-a-serviço**, separada do JWT de usuário.

## API principal C#

O usuário acessa Telemetry pela API principal:

```text
GET   /api/v1/telemetry/devices
POST  /api/v1/telemetry/devices
GET   /api/v1/telemetry/devices/{deviceId}
PATCH /api/v1/telemetry/devices/{deviceId}/status
POST  /api/v1/telemetry/devices/{deviceId}/events
GET   /api/v1/telemetry/devices/{deviceId}/latest
GET   /api/v1/telemetry/devices/{deviceId}/events
```

Esses endpoints exigem autenticação e entitlement `Telemetry`. O backend envia ao Java apenas o `OrganizationId` do token autenticado.

Falhas são separadas em:

- `404` recurso inexistente;
- `422` validação;
- `409` conflito;
- `504` timeout;
- `503` serviço Telemetry indisponível.

## Docker Compose

O ambiente local passa a conter:

```text
AgroControl API          :8080
AgroControl Intelligence :8090
AgroControl Telemetry    :8100
PostgreSQL principal     :5432
PostgreSQL Telemetry     :5433
Mosquitto MQTT           :1883
```

Subida completa:

```bash
docker compose up --build
```

## Qualidade

O `Telemetry CI` executa:

1. compilação e testes Java;
2. testes de persistência contra PostgreSQL 17 real;
3. teste de idempotência;
4. teste de isolamento entre organizações;
5. build da imagem Docker;
6. subida do Mosquitto;
7. subida da imagem Telemetry;
8. registro real de dispositivo via REST;
9. publicação real de uma mensagem MQTT;
10. consulta da última leitura para confirmar persistência ponta a ponta.

O Backend CI continua compilando e testando a integração C#.

## Limites deliberados

Esta Sprint não implementa:

- controle remoto ou comandos para máquinas;
- firmware OTA;
- CAN bus/ISOBUS direto;
- LoRaWAN completo;
- integração específica com fabricantes;
- mapas em tempo real de alta frequência;
- manutenção preditiva;
- sincronização automática de horímetro com `Machinery`.

Esses recursos exigem decisões próprias de segurança, confiabilidade e volume de dados.
