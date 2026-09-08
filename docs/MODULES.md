# Catálogo de módulos

## Fonte de verdade

Este documento descreve os módulos já implementados no AgroControl. A decisão de acesso por plano é aplicada no backend pelo `PlanEntitlementCatalog` e pode ser sobrescrita por entitlement específico da organização.

A interface pode indicar um módulo como bloqueado, mas **a autorização real é sempre validada pela API**.

## Módulos implementados

| ModuleKey | Estado | Escopo principal |
|---|---|---|
| `Identity` | Active | usuários, autenticação e sessão |
| `Organizations` | Active | organização, membership, papéis e tenant |
| `Farms` | Active | propriedades, regiões operacionais e escopo multi-fazenda |
| `Fields` | Active | talhões e áreas produtivas |
| `Crops` | Active | catálogo de culturas |
| `Seasons` | Active | safras e ciclos produtivos |
| `Inventory` | Active | categorias, itens, depósitos, movimentações e saldo |
| `Finance` | Active | despesas, receitas, caixa, resultado e rentabilidade |
| `Machinery` | Active | máquinas, horímetro, combustível e manutenção |
| `Market` | Active | commodities, cotações e alertas |
| `PrecisionAgriculture` | Active | PostGIS, GeoJSON, zonas, sensoriamento remoto e raster |
| `Intelligence` | Active | previsão, analytics e processamento científico Python |
| `Irrigation` | Active | zonas de irrigação, umidade e aplicações de água |
| `Sustainability` | Active | fatores, emissões e indicadores gerenciais de CO₂e |
| `Commercial` | Active | clientes, contatos, oportunidades e pipeline |
| `Export` | Active | pedidos, câmbio, documentos, custos e logística |
| `Telemetry` | Active | dispositivos, MQTT, última leitura e histórico |

Todos os `ModuleKey` acima possuem implementação real no produto. `Locked` na experiência significa ausência de entitlement para aquela organização/plano, e não ausência de código.

## Capacidades transversais

Algumas capacidades não possuem `ModuleKey` próprio porque fazem parte de uma fronteira existente:

- **Operação multi-fazenda / regiões / acesso por Farm** — protegida pelo módulo `Farms`;
- **Sensoriamento remoto** — protegido por `PrecisionAgriculture`;
- **Processamento raster e estatísticas zonais** — protegido por `PrecisionAgriculture`, com processamento científico no serviço Intelligence;
- **Observabilidade / DevOps** — capacidade de plataforma, não módulo comercial;
- **Autorização horizontal** — regra transversal aplicada sobre os módulos ligados a propriedades.

## Planos atuais

A tabela abaixo reflete o `PlanEntitlementCatalog` do backend.

| Módulo / capacidade | Basic | Pro | Intelligence | Enterprise |
|---|:---:|:---:|:---:|:---:|
| Identity / Organizations | ✅ | ✅ | ✅ | ✅ |
| Produção — Farms / Fields / Crops / Seasons | ✅ | ✅ | ✅ | ✅ |
| Estoque | ✅ | ✅ | ✅ | ✅ |
| Financeiro | ✅ | ✅ | ✅ | ✅ |
| Máquinas | — | ✅ | ✅ | ✅ |
| Mercado | — | ✅ | ✅ | ✅ |
| Agricultura de Precisão | — | ✅ | ✅ | ✅ |
| Irrigação | — | ✅ | ✅ | ✅ |
| Sustentabilidade | — | ✅ | ✅ | ✅ |
| Comercial / CRM | — | ✅ | ✅ | ✅ |
| Intelligence | — | — | ✅ | ✅ |
| Telemetry / IoT | — | — | ✅ | ✅ |
| Exportação | — | — | — | ✅ |

`Enterprise` inclui todos os `ModuleKey` atuais. Overrides por organização podem habilitar ou desabilitar módulos individualmente conforme a política de assinatura.

## Segurança

O catálogo visual do frontend não é uma fronteira de segurança.

Para um caso de uso protegido, a API deve verificar cumulativamente quando aplicável:

1. autenticação do usuário;
2. `OrganizationId` do tenant;
3. entitlement do módulo;
4. papel administrativo quando a operação exigir;
5. `FarmAccessScope` quando o recurso estiver vinculado a uma propriedade.

A Sprint 18 acrescentou a quinta dimensão sem substituir as anteriores.

## Estado atual

Catálogo sincronizado com a plataforma até a **Sprint 18 / API 0.18.0**.