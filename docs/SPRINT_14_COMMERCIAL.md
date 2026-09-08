# Sprint 14 — Comercial e CRM

## Objetivo

A Sprint 14 adiciona ao AgroControl um núcleo comercial para organizar clientes, contatos, oportunidades e pipeline de vendas sem transformar a plataforma em um ERP ou CRM omnichannel.

## Escopo implementado

- clientes por organização, com status `Lead`, `Prospect`, `Customer` e `Inactive`;
- contatos vinculados aos clientes, incluindo contato principal e desativação lógica;
- oportunidades com valor esperado, moeda ISO 4217, probabilidade, responsável, próximo passo e data prevista de fechamento;
- pipeline `Lead → Qualification → Proposal → Negotiation → Won/Lost` com regras explícitas;
- `Won` e `Lost` terminais no MVP;
- histórico append-only das mudanças de estágio;
- vínculos opcionais com propriedade, cultura, safra e pedido de exportação, sempre validados no mesmo `OrganizationId`;
- resumo comercial com contagem de oportunidades, ganhos, perdas, conversão e pipeline por moeda;
- valores monetários não são somados entre moedas diferentes;
- API protegida por autenticação, multi-tenancy e entitlement `Commercial`;
- interface web responsiva para clientes, oportunidades, pipeline e histórico.

## Endpoints

```text
GET/POST /api/v1/commercial/customers
GET/PUT  /api/v1/commercial/customers/{id}
GET/POST /api/v1/commercial/customers/{id}/contacts
PUT/DELETE /api/v1/commercial/contacts/{id}

GET/POST /api/v1/commercial/opportunities
GET/PUT  /api/v1/commercial/opportunities/{id}
POST     /api/v1/commercial/opportunities/{id}/stage
GET      /api/v1/commercial/opportunities/{id}/timeline
GET      /api/v1/commercial/summary
```

## Regras de negócio

Oportunidades ganhas ou perdidas não podem ser editadas nem reabertas no MVP. A probabilidade aceita valores de 0 a 100; ao ganhar, passa a 100%, e ao perder, a 0%. O resumo monetário é separado por moeda para não gerar uma soma economicamente incorreta sem uma taxa de câmbio definida.

O módulo é gerencial. Não substitui contrato comercial, emissão fiscal, faturamento, assinatura eletrônica ou CRM omnichannel.
