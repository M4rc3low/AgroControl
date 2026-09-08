# Sprint 17 — Processamento raster e estatísticas zonais

Em desenvolvimento.

Esta sprint conecta o núcleo de sensoriamento remoto do AgroControl ao serviço Python de Intelligence para processamento científico de assets GeoTIFF/COG e persistência segura de estatísticas por talhão e zona de manejo.

Princípios:

- o raster pesado permanece fora do PostgreSQL principal;
- o serviço Python não escreve diretamente no banco do monólito;
- autorização, `OrganizationId`, contexto produtivo e persistência permanecem na API C#;
- processamento é idempotente e rastreável;
- NDVI, NDRE e EVI continuam sendo apoio à decisão, não diagnóstico agronômico automático.
