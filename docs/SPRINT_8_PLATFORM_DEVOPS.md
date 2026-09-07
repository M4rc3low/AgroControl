# Sprint 8 — Plataforma DevOps, observabilidade e Kubernetes

## Objetivo

A Sprint 8 transforma a base do AgroControl em uma plataforma mais observável, validável e preparada para implantação. O foco não é apenas adicionar ferramentas: é criar contratos operacionais claros para C#, Python e Java sem abandonar a simplicidade do fluxo local.

## Observabilidade

### API C#

- logs JSON no console;
- OpenTelemetry opcional por configuração;
- traces de ASP.NET Core e HttpClient;
- métricas de runtime HTTP enviadas por OTLP;
- `/health/live` para liveness;
- `/health/ready` com teste real de conectividade PostgreSQL;
- falha de Intelligence ou Telemetry não derruba a readiness global da API, evitando cascata de reinícios por dependências que são opcionais para partes do produto.

### Intelligence Python

- instrumentação FastAPI com OpenTelemetry;
- exportação OTLP/HTTP;
- service name próprio;
- `/health/live` e `/health/ready`;
- observabilidade desativada por padrão fora do profile local.

### Telemetry Java

- Spring Boot Actuator;
- métricas Prometheus;
- Micrometer Tracing com bridge OpenTelemetry;
- exportação OTLP configurável;
- liveness e readiness do Actuator;
- readiness inclui conectividade com o banco;
- MQTT continua separado da readiness de banco para que o estado operacional possa ser diagnosticado sem induzir restart loops desnecessários.

## Stack local de observabilidade

O profile `observability` do Docker Compose adiciona:

- OpenTelemetry Collector;
- Tempo para traces;
- Prometheus para métricas;
- Grafana com datasources provisionados;
- dashboard inicial `AgroControl — Visão operacional`.

Inicialização:

```bash
cp .env.example .env
OTEL_ENABLED=true docker compose --profile observability up --build
```

Endpoints locais padrão:

```text
API                 http://localhost:8080
Intelligence        http://localhost:8090
Telemetry           http://localhost:8100
Prometheus          http://localhost:9090
Grafana             http://localhost:3000
Tempo               http://localhost:3200
OTLP gRPC            localhost:4317
OTLP HTTP            localhost:4318
```

A senha padrão do Grafana no `.env.example` é apenas para desenvolvimento. Produção precisa de secrets management.

## CI/CD

### Pipelines por serviço

Backend, Intelligence e Telemetry continuam com pipelines independentes. Isso preserva feedback rápido e impede que uma mudança puramente Python obrigue a reconstruir tudo sem necessidade.

### Platform CI

O pipeline de plataforma valida:

1. sintaxe do Docker Compose normal e do profile de observabilidade;
2. renderização de `k8s/base` e `k8s/overlays/local` com Kustomize;
3. build das três imagens da aplicação;
4. subida integrada de PostgreSQL, Telemetry DB, Mosquitto, API, Intelligence, Telemetry, Collector, Tempo, Prometheus e Grafana;
5. readiness dos serviços;
6. contratos HTTP mínimos;
7. targets Prometheus.

### Publicação de imagens

`publish-images.yml` publica no GHCR quando existe push em `main` ou tag `v*`:

```text
ghcr.io/m4rc3low/agrocontrol-api
ghcr.io/m4rc3low/agrocontrol-intelligence
ghcr.io/m4rc3low/agrocontrol-telemetry
```

Tags são geradas por SHA, `main` e SemVer quando a origem é uma tag de release.

## Segurança de supply chain

- Dependabot cobre NuGet, pip, Maven e GitHub Actions;
- CodeQL cobre C#, Java/Kotlin e Python;
- imagens só são publicadas depois que o job de build correspondente consegue executar;
- segredos reais permanecem fora do repositório;
- `k8s/secrets.example.env` documenta apenas os nomes esperados, sem credenciais válidas.

A Sprint não afirma que isso sozinho forma uma cadeia de suprimentos completa. Assinatura de imagens, SBOM por imagem e política de admission control podem ser adicionadas quando o processo de release estiver estabilizado.

## Kubernetes

A base Kubernetes usa Kustomize e inclui:

- namespace `agrocontrol`;
- ConfigMap apenas para valores não sensíveis;
- Deployments e Services de API, Intelligence e Telemetry;
- liveness/readiness probes;
- requests e limits;
- execução sem privilégios;
- duas réplicas para API e Intelligence;
- uma réplica para Telemetry;
- PodDisruptionBudget para workloads stateless replicados;
- overlay local com uma réplica de cada serviço.

### O que não está no cluster base

PostgreSQL e MQTT de produção são dependências externas. Essa decisão é intencional: um `Deployment` simples de banco ou broker não representa operação madura de dados, backup, failover, TLS ou credenciais.

HPA também não foi adicionado. Auto-scaling sem uma métrica de capacidade validada costuma apenas automatizar decisões ruins.

Consulte [`../k8s/README.md`](../k8s/README.md) para execução local e criação de secrets.

## Health e resiliência

A convenção operacional é:

```text
liveness  → o processo está vivo e consegue continuar executando?
readiness → ele está apto a receber tráfego agora?
```

A API testa PostgreSQL na readiness. Intelligence não depende de banco próprio nesta versão. Telemetry usa os health groups do Spring Boot.

Timeouts de Intelligence e Telemetry continuam configuráveis. Retries não foram adicionados de forma indiscriminada: chamadas que podem gerar efeitos ou duplicação precisam de idempotência explícita antes de receber retry automático.

## Versionamento

O AgroControl adota SemVer para releases públicas da plataforma:

```text
MAJOR.MINOR.PATCH
```

Durante a fase de construção, mudanças seguem commits convencionais. Releases são criadas por tags `vX.Y.Z`; o pipeline de imagens produz tags equivalentes no GHCR.

## Runbook mínimo

### API indisponível

1. verificar `/health/live`;
2. verificar `/health/ready`;
3. validar PostgreSQL e connection string;
4. inspecionar logs JSON e trace correspondente.

### Intelligence indisponível

1. verificar `/health/ready`;
2. confirmar timeout/base URL na API;
3. validar container e exportação OTLP;
4. a API deve retornar indisponibilidade do recurso sem fingir uma previsão.

### Telemetry sem ingestão MQTT

1. verificar `/actuator/health/readiness`;
2. validar banco;
3. validar conectividade com broker e tópico;
4. confirmar que o dispositivo está registrado;
5. procurar falhas de payload e trace da requisição/ingestão.

### Banco indisponível

Não use restart infinito da aplicação como substituto de diagnóstico. Verifique credenciais, rede, disponibilidade do serviço de banco e capacidade antes de reiniciar workloads.

## Critérios técnicos alcançados

A Sprint é considerada pronta para merge quando Backend CI, Intelligence CI, Telemetry CI, Platform CI e CodeQL aplicáveis ao PR terminarem sem falhas relevantes, e quando os manifests Kustomize e o Docker Compose forem validados pelo pipeline.
