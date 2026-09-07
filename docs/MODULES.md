# Catálogo de módulos

## Estados

- **Active** — módulo disponível na fase atual.
- **ComingSoon** — módulo já previsto na experiência do produto, ainda em implementação.
- **Locked** — módulo planejado, inicialmente bloqueado por plano ou fase.

## Catálogo

| Módulo | Estado inicial | Escopo |
|---|---|---|
| Identity | Active | Usuários, autenticação, papéis e acesso |
| Organizations | Active | Empresas/grupos e escopo de tenant |
| Farms | Active | Propriedades rurais |
| Fields | Active | Talhões e áreas |
| Crops | Active | Catálogo de culturas |
| Seasons | Active | Safras e ciclos |
| Inventory | Active | Insumos, entradas, saídas e saldo |
| Finance | Active | Custos, receitas, margem e ponto de equilíbrio |
| Machinery | ComingSoon | Máquinas, horímetro, combustível e manutenção |
| Market | ComingSoon | Commodities, preços e alertas |
| Precision Agriculture | Locked | Mapas, GPS, drone e georreferenciamento |
| Agro Intelligence | Locked | Analytics, previsão, IA e visão computacional |
| Irrigation | Locked | Umidade, chuva, clima e irrigação |
| Sustainability | Locked | Carbono, emissões e indicadores ambientais |
| Export | Locked | Pedidos, câmbio, documentos e logística |
| Telemetry | Locked | IoT, sensores e telemetria de máquinas |

## Planos — proposta inicial

Os nomes ainda são provisórios.

| Módulo | Basic | Pro | Intelligence | Enterprise |
|---|:---:|:---:|:---:|:---:|
| Produção | ✅ | ✅ | ✅ | ✅ |
| Estoque | ✅ | ✅ | ✅ | ✅ |
| Financeiro | ✅ | ✅ | ✅ | ✅ |
| Máquinas |  | ✅ | ✅ | ✅ |
| Mercado |  | ✅ | ✅ | ✅ |
| Agricultura de precisão |  | ✅ | ✅ | ✅ |
| IA / Analytics |  |  | ✅ | ✅ |
| IoT / Telemetria |  |  | ✅ | ✅ |
| Sustentabilidade |  |  | ✅ | ✅ |
| Exportação |  |  |  | ✅ |

## Regra de segurança

A flag visual de bloqueio não é segurança. O backend deverá validar entitlement antes de executar o caso de uso protegido.
