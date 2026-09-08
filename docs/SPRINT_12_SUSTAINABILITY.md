# Sprint 12 — Sustentabilidade

## Objetivo

O módulo de Sustentabilidade transforma atividades operacionais em um inventário **gerencial** de emissões estimadas. Ele registra fatores, preserva o fator utilizado em cada lançamento e calcula indicadores de CO₂e por organização, propriedade e safra.

O AgroControl **não certifica inventários, não emite créditos de carbono e não substitui auditoria, metodologia oficial ou avaliação ambiental especializada**.

## Modelo

### EmissionFactor

Cada fator pertence a uma organização e contém nome, categoria, unidade, valor em `kgCO2e/unidade`, referência metodológica, vigência e status. Alterar um fator não altera atividades já registradas porque cada atividade mantém um snapshot do nome, categoria, unidade e valor utilizado.

### EmissionActivity

O ledger de atividades é append-only. Cada lançamento guarda quantidade, data, qualidade do dado, origem, vínculos opcionais com propriedade/talhão/safra e a emissão calculada.

A fórmula é:

```text
kgCO2e = quantidade × fator kgCO2e/unidade

tCO2e = kgCO2e / 1000
```

Correções são novos lançamentos compensatórios e podem ter quantidade negativa. O histórico anterior não é reescrito.

## Idempotência e integração

Atividades externas usam `sourceModule + sourceReferenceId`. A combinação é única dentro da organização para impedir importações duplicadas. Isso deixa a arquitetura preparada para Machinery, Inventory e futuros conectores sem criar dependência direta desses módulos no domínio de Sustentabilidade.

## Indicadores

O resumo geral fornece atividades, total em kgCO₂e e tCO₂e, participação por categoria, quantidade de dados classificados como estimados e comparação com o período imediatamente anterior quando `from` e `to` são informados.

O resumo por safra acrescenta:

- tCO₂e por hectare;
- produção realizada quando disponível;
- kgCO₂e por unidade produzida.

A comparação entre períodos é descritiva. O AgroControl não atribui causalidade à variação.

## Endpoints

```text
GET    /api/v1/sustainability/emission-factors
GET    /api/v1/sustainability/emission-factors/{id}
POST   /api/v1/sustainability/emission-factors
PUT    /api/v1/sustainability/emission-factors/{id}
DELETE /api/v1/sustainability/emission-factors/{id}

GET    /api/v1/sustainability/activities
POST   /api/v1/sustainability/activities

GET    /api/v1/sustainability/summary
GET    /api/v1/sustainability/seasons/{seasonId}/summary
```

Todos exigem autenticação, organização válida e entitlement `Sustainability`.

## Fora do escopo

Certificação GHG Protocol/ISO, inventário regulatório, MRV certificado, auditoria externa, crédito de carbono, cálculo oficial completo de escopos 1/2/3 e comercialização de ativos ambientais permanecem fora desta entrega.
