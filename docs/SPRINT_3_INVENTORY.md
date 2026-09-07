# Sprint 3 — Estoque e movimentações de insumos

## Objetivo

Criar um estoque agrícola rastreável, multi-tenant e protegido pelo módulo `Inventory`, sem tratar saldo como um número editável diretamente.

## Modelo

```text
Organization
  ├── InventoryCategory
  ├── InventoryItem
  │     └── StockMovement
  └── Warehouse
         └── StockMovement
```

Um `Warehouse` pode ser vinculado a uma `Farm`. Uma movimentação pode, opcionalmente, apontar para `Farm`, `Field` e `Season` para registrar onde o insumo foi consumido.

## Catálogo de itens

Cada item possui:

- SKU único dentro da organização;
- nome;
- categoria opcional;
- unidade de medida;
- estoque mínimo;
- estado ativo/inativo.

As unidades iniciais são `Unit`, `Kilogram`, `Gram`, `Liter`, `Milliliter`, `Ton`, `Sack`, `Meter`, `SquareMeter` e `CubicMeter`.

## Ledger de estoque

O saldo é derivado do histórico de `StockMovement`; ele não é alterado manualmente. Os tipos são:

- `Entry`;
- `Exit`;
- `AdjustmentPositive`;
- `AdjustmentNegative`.

Entradas e ajustes positivos somam. Saídas e ajustes negativos subtraem. Uma saída/ajuste negativo é rejeitado quando o saldo atual do item naquele depósito é insuficiente.

As movimentações são append-only no fluxo da API: não há endpoint para editar ou apagar movimentos. Isso preserva rastreabilidade operacional.

> Limitação conhecida do MVP: a validação de saldo é consistente para operações sequenciais, mas ainda não implementa bloqueio pessimista/otimista específico para duas saídas concorrentes no mesmo item e depósito. O endurecimento de concorrência será tratado antes de uso produtivo de alta simultaneidade.

## Lote e validade

`BatchNumber` e `ExpirationDate` são opcionais. Quando uma validade é informada, o lote passa a ser obrigatório, evitando registros de validade sem referência rastreável.

## Estoque baixo

O endpoint de estoque baixo soma os movimentos por item e compara o saldo total da organização com `MinimumStock`, retornando também a quantidade faltante.

## Endpoints

Todos exigem autenticação, organização válida e acesso ao módulo `Inventory`.

```text
GET    /api/v1/inventory/categories
POST   /api/v1/inventory/categories
PUT    /api/v1/inventory/categories/{id}
DELETE /api/v1/inventory/categories/{id}

GET    /api/v1/inventory/items
GET    /api/v1/inventory/items/{id}
POST   /api/v1/inventory/items
PUT    /api/v1/inventory/items/{id}
DELETE /api/v1/inventory/items/{id}

GET    /api/v1/inventory/warehouses
POST   /api/v1/inventory/warehouses
PUT    /api/v1/inventory/warehouses/{id}
DELETE /api/v1/inventory/warehouses/{id}

GET    /api/v1/inventory/movements
POST   /api/v1/inventory/movements
GET    /api/v1/inventory/items/{itemId}/warehouses/{warehouseId}/balance
GET    /api/v1/inventory/low-stock
```

As listagens possuem paginação e filtros relevantes. Movimentações podem ser filtradas por item, depósito, tipo e intervalo de datas.

## Persistência

A migration `InventoryCore` cria:

- `inventory_categories`;
- `inventory_items`;
- `warehouses`;
- `stock_movements`.

As entidades guardam `OrganizationId` e as consultas do repositório sempre recebem a organização autenticada. O índice `(OrganizationId, Sku)` é único.

## Qualidade

A Sprint 3 inclui testes de domínio e integração com PostgreSQL real para validar:

- regras de quantidade e estoque mínimo;
- sinal de entradas/saídas;
- lote obrigatório quando há validade;
- isolamento de categorias por organização;
- cálculo do saldo do ledger;
- rejeição de saída que levaria o saldo abaixo de zero;
- aplicação das migrations no banco real do pipeline.
