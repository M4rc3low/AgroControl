# Sprint 10 — Agricultura de Precisão

## Objetivo

A Sprint 10 introduz a fundação geoespacial do AgroControl. Um talhão passa a poder ter um limite geográfico real, persistido no PostgreSQL/PostGIS e exposto em GeoJSON para consumo pela aplicação web e por futuras integrações agronômicas.

A geometria **não substitui** o cadastro produtivo. `Field.AreaHectares` continua sendo a área cadastral usada pelas regras atuais; a área espacial é calculada separadamente e sua diferença é apresentada ao usuário para conferência.

## Modelo espacial

O banco principal usa a imagem `postgis/postgis:17-3.5-alpine`. A migration `20260908001500_PrecisionAgricultureCore`:

- garante a extensão `postgis`;
- adiciona `fields."Boundary" geography(Polygon,4326)`;
- cria índice espacial GiST;
- não remove a extensão no `Down`, pois ela pode ser compartilhada por evoluções futuras.

O limite é armazenado como **geography WGS84 / SRID 4326**. A área é calculada pelo PostgreSQL com `ST_Area(geography)`, convertida de m² para hectares. O GeoJSON retornado é produzido por `ST_AsGeoJSON`.

### Por que a coluna não está mapeada no `Field` do EF Core?

Nesta primeira fundação espacial, o domínio continua livre de tipos específicos de uma biblioteca geométrica. A migration adiciona a coluna e `PrecisionAgricultureRepository` a manipula com SQL parametrizado/PostGIS. Isso evita introduzir tipos de provider no domínio e mantém o restante do modelo EF inalterado. Caso operações geométricas passem a fazer parte central do domínio, uma ADR futura pode reavaliar o uso de NetTopologySuite.

## Validação

A validação ocorre em duas camadas:

1. **Application:** confirma `Polygon`, presença de anéis, pelo menos três vértices distintos, números finitos, longitude entre -180/180, latitude entre -90/90 e fecha o anel quando necessário.
2. **PostGIS:** `ST_IsValid`, `ST_IsEmpty` e `ST_GeometryType` fazem a validação topológica final antes do `UPDATE`.

Polígonos inválidos não são persistidos. O backend também confirma que o talhão pertence ao `OrganizationId` autenticado e que o módulo `PrecisionAgriculture` está habilitado.

## API

```text
GET    /api/v1/precision/fields
GET    /api/v1/precision/fields/{fieldId}/boundary
PUT    /api/v1/precision/fields/{fieldId}/boundary
DELETE /api/v1/precision/fields/{fieldId}/boundary
```

`GET /fields` aceita `farmId` e `includeInactive`. O `PUT` recebe diretamente um GeoJSON Polygon:

```json
{
  "type": "Polygon",
  "coordinates": [
    [
      [-47.1000, -15.7000],
      [-47.0900, -15.7000],
      [-47.0900, -15.6900],
      [-47.1000, -15.6900],
      [-47.1000, -15.7000]
    ]
  ]
}
```

A resposta inclui `registeredAreaHectares`, `spatialAreaHectares`, `areaDifferenceHectares` e `areaDifferencePercent`.

## Web

A rota `/precision` adiciona um workspace de mapa ao AgroControl Web:

- seleção de propriedade e talhão;
- visualização dos limites já cadastrados;
- desenho de polígono por cliques;
- redesenho do limite existente;
- remoção com confirmação;
- comparação da área cadastral com a área calculada pelo PostGIS;
- tratamento explícito de acesso bloqueado, loading e erro.

O mapa usa **MapLibre GL**. Nesta sprint o runtime pode carregar script/CSS e um estilo público configurável. As variáveis são:

```text
VITE_MAPLIBRE_SCRIPT_URL
VITE_MAPLIBRE_CSS_URL
VITE_MAP_STYLE_URL
```

Nenhuma chave de provedor é armazenada no repositório. Para produção, os assets podem ser self-hosted e o estilo/tile provider deve ser escolhido conforme disponibilidade, termos de uso, cobertura e política de segurança.

## Testes

### Backend

- testes unitários do normalizador/validador GeoJSON;
- integração com PostgreSQL/PostGIS real;
- persistência e cálculo de área;
- isolamento multi-tenant.

O Backend CI usa `postgis/postgis:17-3.5-alpine`, garantindo que a migration e as funções espaciais executem em uma instância real.

### Frontend

Os helpers de geometria possuem testes para fechamento de anel, geração de Polygon e preparação de um limite existente para redesenho. O Frontend CI continua executando type-check, Vitest, build e smoke test da imagem Nginx.

## Decisões e limites

Esta sprint é deliberadamente uma fundação. Não inclui Shapefile/KML, imagens de satélite/drone, NDVI, zonas de manejo, mapas de produtividade, prescrições, roteamento de máquinas nem atuação autônoma. Esses recursos devem evoluir sobre a mesma base GeoJSON/PostGIS sem misturar ingestão de arquivos, processamento raster e decisões agronômicas em uma única entrega.
