# Sprint 17 — Processamento raster e estatísticas zonais

A Sprint 17 conecta o núcleo de sensoriamento remoto do AgroControl ao serviço Python de Intelligence para processar produtos raster GeoTIFF/COG e persistir estatísticas zonais rastreáveis por talhão e zona de manejo.

## Objetivos entregues

- processamento raster no serviço Python com Rasterio e NumPy;
- leitura de GeoTIFF/COG por referência de asset;
- reprojeção da geometria de análise para o CRS do raster;
- tratamento de máscara, NoData e valores não finitos;
- estatísticas de mínimo, máximo, média, mediana, desvio-padrão, cobertura válida e quantidade de amostras;
- processamento do talhão e, opcionalmente, das zonas de manejo ativas;
- limite explícito de 20 milhões de pixels e 250 geometrias por requisição no MVP;
- bloqueio de assets remotos por padrão, com habilitação explícita por ambiente;
- proteção contra credenciais embutidas e endereços IP privados/loopback em URLs HTTP(S);
- orquestração pela API C#, mantendo autorização, tenant e persistência fora do serviço científico;
- lifecycle de processamento `Pending`, `Processing`, `Succeeded` e `Failed`;
- idempotência por chave de processamento e persistência transacional dos resultados;
- endpoints para solicitar processamento e consultar produtos, execuções e estatísticas zonais;
- integração do workspace de sensoriamento remoto com disparo, status e comparação dos resultados;
- testes Python determinísticos com raster sintético e NoData;
- testes C# de domínio, contrato HTTP, idempotência e persistência PostgreSQL/PostGIS;
- execução do Intelligence em container com as dependências nativas exigidas pelo Rasterio.

## Arquitetura

O fluxo mantém a separação de responsabilidades:

```text
AgroControl Web
      |
      v
AgroControl.Api (C#)
  - autenticação e entitlement
  - OrganizationId
  - contexto Field/Season/ManagementZone
  - idempotência e lifecycle
  - persistência dos resultados
      |
      | HTTP interno versionado
      v
AgroControl Intelligence (Python)
  - Rasterio + NumPy
  - leitura do raster
  - reprojeção de geometria
  - máscara/NoData
  - estatísticas zonais
```

O serviço Python não escreve diretamente no PostgreSQL principal. O raster pesado também não é armazenado no banco relacional; o AgroControl persiste referências, metadados e resultados derivados.

## Contrato científico

O endpoint interno `POST /api/v1/raster/zonal-statistics` recebe a referência do asset, banda, CRS das geometrias e uma coleção de alvos identificados por chave. A resposta contém metadados do raster e estatísticas por alvo.

Os alvos aceitam `Polygon` ou `MultiPolygon`. Quando o CRS do alvo difere do raster, o Rasterio reprojeta a geometria antes de aplicar a máscara. Pixels NoData, mascarados ou não finitos não entram nas estatísticas.

## Segurança de assets

Assets locais devem existir no ambiente do Intelligence. Assets remotos (`http`, `https`, `s3` e `gs`) ficam desabilitados por padrão e só são aceitos quando `AGROCONTROL_RASTER_ALLOW_REMOTE=true` é configurado conscientemente. URLs HTTP(S) com credenciais embutidas ou host literal privado, loopback, link-local ou reservado são rejeitadas.

Essa proteção reduz a superfície de SSRF no MVP. Uma evolução futura deve usar allowlists de provedores/buckets, credenciais gerenciadas e política de egress da infraestrutura.

## Persistência e idempotência

A migration `20260908154500_RasterProcessingCore` cria as estruturas de produtos raster, execuções e resultados zonais. A API preserva o isolamento por `OrganizationId`, valida o vínculo da cena/talhão/safra e mantém unicidade da chave de processamento para impedir trabalho duplicado.

Uma repetição idempotente retorna a execução já registrada. Corridas concorrentes na criação são tratadas recuperando a execução vencedora após a restrição de unicidade do PostgreSQL.

## Interface web

O workspace de sensoriamento remoto apresenta o processamento raster junto ao contexto da cena. O usuário pode iniciar uma execução, acompanhar o estado, consultar metadados como CRS e resolução e comparar o resultado do talhão com as zonas de manejo processadas.

## Limites do MVP

A Sprint 17 calcula estatísticas sobre produtos raster já disponíveis. Ela não realiza descoberta automática de cenas, download de satélite, mosaico, correção atmosférica ou geração das bandas NDVI/NDRE/EVI a partir de bandas brutas. Essas responsabilidades podem evoluir em uma camada de catálogo/processamento assíncrono sem acoplar o monólito transacional ao pipeline científico.

## Princípios agronômicos

NDVI, NDRE, EVI e demais produtos derivados são indicadores de apoio à decisão. O AgroControl não os apresenta como diagnóstico agronômico automático nem como substituto da avaliação profissional em campo.
