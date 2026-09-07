# ADR-005 — Observabilidade e Kubernetes após a separação dos serviços especializados

**Status:** Aceito  
**Data:** 2026-09-07

## Contexto

O AgroControl começou corretamente como um monólito modular em C#. Nas Sprints 6 e 7 surgiram duas separações com responsabilidade técnica própria: Intelligence em Python e Telemetry em Java/MQTT. A plataforma passou a ter três runtimes, dois bancos no ambiente de desenvolvimento, broker MQTT e comunicação HTTP entre processos.

Antes dessa evolução, Kubernetes criaria mais complexidade operacional do que valor. Agora existem workloads independentes, probes diferentes, limites de recursos, deploys separados e necessidade real de rastrear chamadas entre serviços.

## Decisão

Adotar duas camadas operacionais complementares:

1. **Docker Compose continua sendo o padrão de desenvolvimento local**, com um profile opcional `observability`.
2. **Kubernetes passa a existir como base de implantação**, usando Kustomize, sem assumir que manifests simples resolvem banco, MQTT, secrets ou alta disponibilidade de produção.

A observabilidade usa OpenTelemetry como padrão de instrumentação e propagação de traces. No laboratório local, o OpenTelemetry Collector encaminha traces ao Tempo e métricas ao Prometheus; Grafana fornece visualização. Telemetry também expõe métricas Prometheus via Actuator.

## Consequências

### Positivas

- C#, Python e Java compartilham uma estratégia de tracing compatível.
- liveness e readiness ficam explícitos.
- manifests de deployment são validados no CI.
- recursos e políticas de interrupção deixam de ser decisões implícitas.
- Docker Compose continua simples para quem só precisa desenvolver funcionalidades.

### Custos

- mais arquivos operacionais e pipelines precisam ser mantidos.
- versões de imagens e componentes de observabilidade passam a exigir atualização contínua.
- Kubernetes não elimina a necessidade de operar banco, broker, secrets, TLS e observabilidade de forma competente.

## Limites deliberados

- PostgreSQL de produção não é implantado pelos manifests base.
- MQTT de produção não é criado como broker anônimo dentro do cluster.
- HPA não é ativado sem uma métrica de capacidade validada.
- Telemetry permanece com uma réplica por padrão até existir coordenação segura do consumo MQTT para todos os tipos de evento.
- service mesh, GitOps e multi-região ficam fora desta decisão.
