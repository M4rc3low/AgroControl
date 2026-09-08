# Sprint 15 — Importação geoespacial e zonas de manejo

## Objetivo

A Sprint 15 evolui o módulo de Agricultura de Precisão para receber e devolver dados geoespaciais produzidos fora do AgroControl, além de manter **zonas de manejo** vinculadas a um talhão. O foco é interoperabilidade com ferramentas GIS, GPS, drones e softwares de campo sem alterar silenciosamente o cadastro agronômico.

O módulo permanece protegido pelo entitlement `PrecisionAgriculture` e todos os acessos são isolados por `OrganizationId`.

## ManagementZone

Uma zona de manejo pertence obrigatoriamente a uma organização e a um talhão. Os tipos disponíveis no MVP são:

- `Soil` — solo;
- `Yield` — produtividade;
- `Vegetation` — vegetação/vigor;
- `Prescription` — prescrição;
- `Custom` — classificação livre.

Metadados suportados: nome, descrição, classificação, valor numérico opcional e unidade. A geometria é persistida como `geography(Polygon,4326)` e a área é calculada pelo PostGIS em hectares.

A remoção é lógica (`IsActive = false`), preservando o registro para rastreabilidade.

## Regras espaciais

Todo polígono passa primeiro pela validação de contrato GeoJSON do AgroControl: tipo `Polygon`, coordenadas finitas, longitude/latitude dentro do WGS84, anel com pelo menos três vértices distintos e fechamento do anel.

O PostgreSQL/PostGIS executa a segunda camada de validação com `ST_IsValid`, `ST_IsEmpty` e tipo geométrico. Quando o talhão possui limite geográfico cadastrado, a zona precisa permanecer dentro dele. É aplicada uma tolerância técnica de **0,5 metro** ao limite do talhão para absorver diferenças mínimas de precisão/serialização nas bordas. Essa tolerância não autoriza zonas materialmente fora do talhão.

A importação ou criação de zonas **não altera a área cadastral do talhão**. A área geoespacial continua sendo um indicador calculado separadamente.

## Importação GeoJSON

Endpoint:

```text
POST /api/v1/precision/fields/{fieldId}/zones/import
```

O corpo pode ser um GeoJSON `Feature` ou `FeatureCollection`. Por segurança e previsibilidade:

- limite de 2 MB por requisição;
- limite de 250 features por importação;
- `fieldId` vem da rota e nunca é aceito das propriedades do arquivo;
- `properties.name` é obrigatório;
- `properties.zoneType` é opcional e assume `Custom` quando ausente;
- `description`, `classification`, `value` e `unit` são opcionais;
- `value`, quando informado, deve ser numérico;
- propriedades desconhecidas são ignoradas;
- cada geometria passa pelas mesmas validações usadas no cadastro manual.

Exemplo:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": {
        "name": "Vigor alto",
        "zoneType": "Vegetation",
        "classification": "Alta",
        "value": 0.78,
        "unit": "NDVI"
      },
      "geometry": {
        "type": "Polygon",
        "coordinates": [[
          [-47.095, -15.695],
          [-47.090, -15.695],
          [-47.090, -15.690],
          [-47.095, -15.690],
          [-47.095, -15.695]
        ]]
      }
    }
  ]
}
```

### Atomicidade

A persistência em lote ocorre dentro de uma transação. Se qualquer feature falhar na validação final do PostGIS, a importação inteira é revertida; não ficam zonas parciais no banco.

## Exportação

Endpoint:

```text
GET /api/v1/precision/fields/{fieldId}/zones/export
```

O retorno é um `FeatureCollection` com a geometria e os metadados gerenciais da zona, incluindo a área calculada em hectares. O workspace web permite baixar esse retorno como `.geojson`.

## CRUD e filtros

```text
GET    /api/v1/precision/zones
GET    /api/v1/precision/zones/{zoneId}
POST   /api/v1/precision/zones
PUT    /api/v1/precision/zones/{zoneId}
DELETE /api/v1/precision/zones/{zoneId}
```

A listagem aceita filtros por `fieldId`, `type`, `classification` e `includeInactive`.

## Interface web

O workspace `/precision` agora combina:

- limites dos talhões;
- zonas de manejo como camada independente no MapLibre;
- alternância de visibilidade;
- upload `.geojson`/`.json`;
- validação inicial de tamanho e formato no navegador;
- pré-visualização da camada antes da confirmação;
- confirmação da importação no backend;
- foco de mapa por zona;
- desativação de zonas;
- exportação GeoJSON.

A validação do navegador é apenas UX. O backend e o PostGIS continuam sendo a autoridade de segurança e integridade.

## Persistência PostGIS e EF Core

A tabela `management_zones` é criada pela migration explícita `20260908134500_ManagementZonesCore`. Ela contém FKs para organização/talhão, índices relacionais e índice espacial GiST na coluna `Geometry`.

Assim como o `Field.Boundary` introduzido na Sprint 10, a geometria desta sprint é mantida por **SQL PostGIS explícito e repositório espacial**, em vez de entrar no modelo CLR do EF Core. Por isso, o `AgroControlDbContextModelSnapshot` não representa `management_zones`: essa ausência é intencional e documentada, não uma migration esquecida. As migrations EF tradicionais continuam responsáveis pelo restante do modelo relacional.

## Testes

A cobertura inclui:

- domínio de `ManagementZone`;
- normalização e validação dos metadados;
- persistência real em PostgreSQL + PostGIS;
- cálculo de área;
- isolamento entre organizações;
- rejeição de zona fora do limite do talhão;
- rollback de importação em lote inválida;
- exportação como `FeatureCollection`;
- testes frontend do contrato de inspeção de `Feature`/`FeatureCollection` e limites de quantidade.

## Limites desta sprint

Não fazem parte do escopo: Shapefile, KML, reprojeção arbitrária de CRS, processamento raster, download automático de imagens de satélite, cálculo de NDVI a partir de bandas e geração automática de prescrições. Esses itens devem evoluir em sprints próprias para não misturar ingestão vetorial com processamento de imagem e recomendação agronômica.
