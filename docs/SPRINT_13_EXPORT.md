# Sprint 13 — Exportação

## Objetivo

O módulo de Exportação organiza o fluxo comercial internacional do AgroControl desde a negociação até a entrega, conectando pedido, comprador, destino, moeda, câmbio, logística, custos e checklist documental.

A entrega é **operacional e gerencial**. O AgroControl não substitui Siscomex, despachante aduaneiro, emissão fiscal, contratação bancária de câmbio, certificação oficial ou tracking do armador.

## Modelo operacional

### ExportOrder

Cada pedido pertence a uma organização e possui número único por tenant. O pedido registra comprador, país ISO alpha-2, produto, quantidade/unidade, moeda ISO 4217, preço unitário, snapshot da taxa de câmbio para BRL, Incoterm e referências logísticas.

Valores derivados:

```text
commercialValue = quantity × unitPrice
estimatedValueBrl = commercialValue × exchangeRateToBrl
```

O snapshot de câmbio preserva a estimativa histórica. Uma cotação futura não reescreve pedidos já registrados.

### Status e timeline

Fluxo principal:

```text
Draft → Negotiation → Contracted → InTransit → Delivered
```

`Draft` pode ir diretamente para `Contracted`. `Draft`, `Negotiation`, `Contracted` e `InTransit` podem ser cancelados conforme as regras de domínio. `Delivered` e `Cancelled` são terminais.

Cada mudança gera `ExportOrderStatusEvent`, formando uma timeline append-only.

### Custos logísticos

`ExportCost` registra frete, seguro, porto, aduana, inspeção ou outros custos com moeda e taxa de câmbio próprias. `AmountBrl` é persistido como snapshot para evitar alteração retroativa da estimativa.

### Documentos

Ao criar um pedido, o AgroControl gera o checklist inicial:

- Commercial Invoice;
- Packing List;
- Certificate of Origin;
- Phytosanitary Certificate;
- Bill of Lading.

Também é possível adicionar item `Custom`. O módulo guarda metadados, status, referência e data de emissão, não o documento oficial assinado.

## Produção rural

Farm, Field, Crop e Season são opcionais, mas quando informados precisam pertencer à mesma organização. Se uma safra é informada, o sistema normaliza e valida automaticamente o talhão, a propriedade e a cultura correspondentes.

## API

```text
GET    /api/v1/export/orders
GET    /api/v1/export/orders/{id}
POST   /api/v1/export/orders
PUT    /api/v1/export/orders/{id}
POST   /api/v1/export/orders/{id}/status
GET    /api/v1/export/orders/{id}/timeline
GET    /api/v1/export/orders/{id}/documents
POST   /api/v1/export/orders/{id}/documents
PUT    /api/v1/export/orders/{orderId}/documents/{documentId}
GET    /api/v1/export/orders/{id}/costs
POST   /api/v1/export/orders/{id}/costs
GET    /api/v1/export/summary
```

Todos exigem autenticação, organização válida e entitlement `Export`.

## Resumo comercial

O resumo consolida número de pedidos, pedidos contratados/em trânsito/entregues, valor comercial estimado em BRL, custos logísticos e margem operacional estimada, além de breakdown por status, país e moeda.

A margem é gerencial e não representa lucro contábil, tributário ou fiscal oficial.

## Fora do escopo

Siscomex, NF-e, despacho aduaneiro automático, assinatura digital, contratação automática de câmbio, integração bancária, tracking de armadores em tempo real, emissão de certificados oficiais, armazenamento binário completo de documentos e CRM avançado permanecem fora desta sprint.
