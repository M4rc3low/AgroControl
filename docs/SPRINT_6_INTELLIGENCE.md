# Sprint 6 — AgroControl Intelligence

## Objetivo

A Sprint 6 introduz o primeiro serviço especializado em **Python** do AgroControl. O `AgroControl Intelligence` transforma dados de safras já existentes na plataforma em uma estimativa de produtividade rastreável, versionada e reproduzível.

A implementação foi desenhada para provar a integração **C# ↔ Python** sem fragmentar prematuramente o sistema em muitos microserviços.

## Arquitetura

```text
Cliente
  ↓
AgroControl API (.NET)
  ↓ valida tenant + entitlement Intelligence
IntelligenceService
  ↓
IIntelligenceDataSource ──→ PostgreSQL
  ↓
IIntelligenceClient
  ↓ HTTP
AgroControl Intelligence (FastAPI)
  ↓
Baseline + Ridge Regression
```

O backend principal continua responsável por autenticação, autorização, isolamento multi-tenant e seleção dos dados permitidos. O serviço Python recebe somente o dataset necessário à inferência.

## Serviço Python

Localização:

```text
src/intelligence
```

Stack inicial:

- Python 3.12;
- FastAPI;
- Pydantic;
- NumPy;
- scikit-learn;
- pytest;
- Ruff;
- Uvicorn.

### Endpoints

```text
GET  /health
GET  /api/v1/model
POST /api/v1/yield/predict
```

A documentação OpenAPI do serviço fica disponível em `/docs`.

## Contrato de previsão v1

Exemplo de entrada:

```json
{
  "contract_version": "v1",
  "crop_name": "Soja",
  "crop_variety": "Cultivar A",
  "area_hectares": 120,
  "expected_yield_per_hectare": 62,
  "historical_samples": [
    {
      "area_hectares": 110,
      "expected_yield_per_hectare": 60,
      "actual_yield_per_hectare": 58
    }
  ]
}
```

Exemplo de resposta:

```json
{
  "contract_version": "v1",
  "status": "limited",
  "predicted_yield_per_hectare": 58,
  "baseline_yield_per_hectare": 58,
  "model_kind": "historical-mean-baseline",
  "model_version": "yield-v1.0",
  "sample_count": 1,
  "metrics": null,
  "generated_at_utc": "2026-09-07T21:00:00Z",
  "warning": "At least 4 historical samples are required to evaluate the supervised model."
}
```

## Dataset

Para uma safra solicitada, o backend reúne:

- cultura;
- variedade, quando disponível;
- área do talhão;
- produtividade esperada da safra;
- histórico da mesma cultura dentro da mesma organização;
- área, produtividade esperada e produtividade realizada de safras históricas.

O `IntelligenceDataSource` aplica `OrganizationId` em todas as consultas. Uma organização não consegue utilizar histórico de outra organização no modelo.

O histórico é limitado às 200 amostras mais recentes nesta fase.

## Estratégia de previsão

A primeira versão prioriza explicabilidade e comportamento previsível.

### Sem histórico

Se existe produtividade esperada cadastrada, ela é devolvida como `expected-yield-baseline` e o status é `limited`.

Se não existe histórico nem produtividade esperada, o serviço retorna `insufficient_data` e não inventa uma previsão.

### Histórico pequeno

Com uma a três amostras históricas, o serviço usa a média da produtividade realizada e marca o resultado como `limited`.

### Quatro ou mais amostras

O serviço compara:

1. média histórica como baseline;
2. regressão Ridge usando área e produtividade esperada como features.

A avaliação usa **leave-one-out** de forma determinística. O modelo Ridge só é selecionado quando sua MAE não é pior que a MAE do baseline.

As métricas devolvidas são:

```text
MAE  = erro absoluto médio
RMSE = raiz do erro quadrático médio
```

Não existe aleatoriedade no pipeline inicial.

## Limites do modelo

Esta versão não utiliza ainda:

- precipitação;
- temperatura;
- tipo e análise de solo;
- cultivar codificada como feature;
- NDVI;
- imagens de drone ou satélite;
- manejo;
- fertilização;
- incidência de pragas ou doenças;
- telemetria de máquinas;
- dados meteorológicos externos.

Por isso, a previsão é uma **estimativa de apoio à decisão**, não uma recomendação agronômica nem uma garantia de produtividade.

A arquitetura permite adicionar novas features posteriormente sem quebrar o contrato atual.

## Integração C# ↔ Python

A API principal possui:

```text
POST /api/v1/intelligence/seasons/{seasonId}/yield-prediction
```

Antes de chamar Python, o backend:

1. exige autenticação;
2. exige acesso ao módulo `Intelligence`;
3. valida a safra dentro da organização autenticada;
4. monta o histórico somente daquela organização;
5. envia apenas os dados necessários para o serviço Python.

### Tratamento de falhas

```text
200  previsão produzida, inclusive status limited
404  safra não encontrada no tenant
422  payload inválido ou dados insuficientes
503  serviço Intelligence indisponível
504  timeout do serviço Intelligence
```

A URL e o timeout são configuráveis:

```text
Intelligence__BaseUrl
Intelligence__TimeoutSeconds
```

No Docker Compose, a API utiliza:

```text
http://intelligence:8090
```

## Docker

O serviço Python possui Dockerfile próprio e é iniciado junto da API e PostgreSQL:

```bash
docker compose up --build
```

Portas padrão:

```text
AgroControl API          http://localhost:8080
AgroControl Intelligence http://localhost:8090
PostgreSQL               localhost:5432
```

O serviço Intelligence possui health check antes de a API ser considerada pronta no ambiente de desenvolvimento.

## Qualidade

### Python

O workflow `Intelligence CI` executa:

```text
pip install
ruff check
pytest
```

Os testes cobrem:

- health check;
- informações do modelo;
- validação Pydantic;
- ausência de dados;
- baseline por produtividade esperada;
- baseline histórico;
- caminho supervisionado determinístico.

### C#

O `Backend CI` continua executando build e testes contra PostgreSQL real.

Os testes da Sprint 6 cobrem:

- orquestração do `IntelligenceService`;
- distinção de timeout, indisponibilidade e dados insuficientes;
- contrato JSON snake_case do cliente HTTP;
- isolamento multi-tenant do dataset;
- filtragem do histórico pela mesma cultura.

## Próximas evoluções

Depois de acumular dados suficientes, o Intelligence poderá receber:

- clima e precipitação;
- dados de solo;
- índices vegetativos;
- histórico por cultivar;
- dados de máquinas e telemetria;
- séries temporais de commodities;
- modelos mais robustos com validação temporal;
- registro persistente de previsões e drift de modelo;
- visão computacional.

Essas evoluções devem ser adicionadas com métricas comparáveis ao baseline atual. Um modelo mais complexo só deve substituir o anterior quando demonstrar ganho real.
