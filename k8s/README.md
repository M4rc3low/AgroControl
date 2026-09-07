# Kubernetes — AgroControl

Os manifests desta pasta representam a base de implantação do AgroControl. Eles **não** tentam transformar um conjunto de YAMLs em uma plataforma de produção completa.

## Decisões importantes

- API C#, Intelligence Python e Telemetry Java são os workloads da aplicação.
- PostgreSQL de produção **não** é criado por estes manifests. Use banco gerenciado ou uma instalação operada separadamente.
- O broker MQTT de produção também é uma dependência operacional separada e deve usar TLS, autenticação por dispositivo/gateway e ACLs.
- Segredos reais não ficam no Git.
- `Telemetry` permanece com uma réplica por padrão enquanto não houver coordenação explícita de consumidores MQTT para eventos sem `eventId`.
- API e Intelligence possuem `PodDisruptionBudget` e duas réplicas na base.
- HPA não foi adicionado ainda porque não existe, neste momento, uma métrica de capacidade validada que justifique auto-scaling automático.

## Validar os manifests

```bash
kubectl kustomize k8s/base
kubectl kustomize k8s/overlays/local
```

## Segredos

Copie `k8s/secrets.example.env` para um arquivo fora do controle de versão, preencha os valores e crie o Secret:

```bash
kubectl create namespace agrocontrol --dry-run=client -o yaml | kubectl apply -f -
kubectl -n agrocontrol create secret generic agrocontrol-secrets \
  --from-env-file=secrets.env
```

Nunca faça commit de `secrets.env`.

## Execução local com kind

Depois de construir as imagens localmente:

```bash
docker build -f src/backend/AgroControl.Api/Dockerfile -t agrocontrol-api:local .
docker build -f src/intelligence/Dockerfile -t agrocontrol-intelligence:local .
docker build -f src/telemetry/Dockerfile -t agrocontrol-telemetry:local .

kind load docker-image agrocontrol-api:local
kind load docker-image agrocontrol-intelligence:local
kind load docker-image agrocontrol-telemetry:local
```

Aplique então:

```bash
kubectl apply -k k8s/overlays/local
```

O overlay local reduz API e Intelligence para uma réplica. Banco e MQTT continuam externos ao overlay; para desenvolvimento cotidiano, `docker compose up --build` permanece o caminho mais simples.

## Observabilidade no Kubernetes

A base mantém `OTEL_ENABLED=false`. Em um ambiente real, configure um OpenTelemetry Collector operado pela plataforma e injete os endpoints OTLP por configuração do ambiente. A stack Prometheus/Grafana/Tempo presente em `docker-compose.yml` é voltada ao laboratório local e não deve ser copiada cegamente para produção.
