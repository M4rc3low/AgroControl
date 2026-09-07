# AgroControl Intelligence

Serviço Python especializado em análise de dados e previsão de produtividade para o AgroControl.

## Princípios

- contrato HTTP versionado (`v1`);
- comportamento determinístico e reproduzível;
- baseline simples antes de qualquer modelo supervisionado;
- modelo supervisionado só é usado quando não piora o MAE do baseline na avaliação leave-one-out;
- ausência de dados suficientes é informada explicitamente;
- resultados são apoio à decisão, não recomendação agronômica profissional.

## Endpoints

```text
GET  /health
GET  /api/v1/model
POST /api/v1/yield/predict
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
