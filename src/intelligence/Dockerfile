FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY src/intelligence/pyproject.toml ./pyproject.toml
COPY src/intelligence/agrocontrol_intelligence ./agrocontrol_intelligence

RUN pip install --no-cache-dir --upgrade pip \
    && pip install --no-cache-dir .

EXPOSE 8090

CMD ["uvicorn", "agrocontrol_intelligence.main:app", "--host", "0.0.0.0", "--port", "8090"]
