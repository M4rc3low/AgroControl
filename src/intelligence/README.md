# AgroControl Intelligence

Serviço Python especializado em análise de dados, previsão de produtividade e processamento geoespacial para o AgroControl.

## Princípios

- contrato HTTP versionado (`v1`);
- comportamento determinístico e reproduzível;
- baseline simples antes de qualquer modelo supervisionado;
- modelo supervisionado só é usado quando não piora o MAE do baseline na avaliação leave-one-out;
- ausência de dados suficientes é informada explicitamente;
- Rasterio + NumPy para processamento raster e estatísticas zonais;
- o serviço científico não escreve diretamente no banco do monólito;
- assets remotos ficam desabilitados por padrão;
- resultados são apoio à decisão, não recomendação agronômica profissional.

## Endpoints

```text
GET  /health
GET  /health/live
GET  /health/ready
GET  /api/v1/model
POST /api/v1/yield/predict
POST /api/v1/raster/zonal-statistics
```

A documentação OpenAPI fica disponível automaticamente em `/docs` quando o FastAPI está em execução.

## Execução local

```bash
cd src/intelligence
python -m venv .venv
source .venv/bin/activate   # Linux/macOS
# .venv\Scripts\activate  # Windows
pip install -e ".[dev]"
uvicorn agrocontrol_intelligence.main:app --reload --port 8090
```

## Testes e lint

```bash
pytest
ruff check .
```

## Estratégia inicial de previsão

Sem histórico, o serviço só usa a produtividade esperada cadastrada quando ela existe e marca o resultado como `limited`. Sem histórico e sem produtividade esperada, retorna `insufficient_data`.

Com histórico abaixo do mínimo de amostras, usa a média histórica. Com pelo menos quatro amostras, compara a média histórica com uma regressão Ridge usando área e produtividade esperada como features. A escolha é feita pela menor MAE em leave-one-out, sem aleatoriedade.

## Processamento raster

O contrato `POST /api/v1/raster/zonal-statistics` processa um asset GeoTIFF/COG e uma coleção de geometrias `Polygon`/`MultiPolygon`. Para cada alvo são calculados mínimo, máximo, média, mediana, desvio-padrão, cobertura válida e quantidade de amostras.

O motor reprojeta a geometria para o CRS do raster quando necessário e ignora máscara, NoData e valores não finitos. O MVP limita a leitura a 20 milhões de pixels por raster e 250 alvos por requisição.

Referências locais precisam existir dentro do ambiente do Intelligence. Referências remotas (`http`, `https`, `s3`, `gs`) são recusadas por padrão. Para habilitá-las de forma consciente, configure:

```bash
export AGROCONTROL_RASTER_ALLOW_REMOTE=true
```

URLs HTTP(S) com credenciais embutidas ou hosts literais privados/loopback são rejeitadas. Em produção, a evolução recomendada é complementar isso com allowlist de provedores/buckets, credenciais gerenciadas e política de egress.
