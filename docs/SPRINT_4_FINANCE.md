# Sprint 4 — Financeiro e rentabilidade por safra

## Objetivo

Transformar os dados operacionais do AgroControl em informação econômica útil, sem tentar substituir um sistema contábil ou fiscal. O módulo registra receitas e despesas, acompanha obrigações pendentes e liquidadas e calcula indicadores de resultado por organização, propriedade, talhão e safra.

## Modelo

```text
Organization
  ├── FinancialCategory
  ├── CostCenter
  └── FinancialTransaction
          ├── Farm? ── Field? ── Season?
          ├── FinancialCategory?
          └── CostCenter?
```

Todas as consultas exigem `OrganizationId`. Referências a categoria, centro de custo, propriedade, talhão e safra são validadas contra a organização autenticada.

## Lançamentos financeiros

`FinancialTransaction` possui dois tipos:

- `Expense` — despesa;
- `Revenue` — receita.

O lançamento nasce como `Pending`. Uma despesa liquidada passa para `Paid`; uma receita liquidada passa para `Received`. Um lançamento pendente também pode ser `Cancelled`.

Lançamentos liquidados ou cancelados não podem ser editados no MVP. Isso evita alterar silenciosamente o histórico financeiro depois da liquidação.

Campos principais:

- descrição;
- contraparte textual (fornecedor/cliente);
- valor monetário em `decimal`;
- data de competência;
- vencimento opcional;
- data de liquidação;
- categoria e centro de custo opcionais;
- propriedade, talhão e safra opcionais;
- observações.

## Competência x caixa

O módulo mantém duas leituras diferentes:

**Competência:** considera lançamentos não cancelados na data econômica registrada, mesmo que ainda estejam pendentes.

**Caixa:** considera somente receitas `Received` e despesas `Paid`.

Assim, uma compra a prazo pode aparecer como despesa por competência e simultaneamente como conta a pagar, sem reduzir o caixa até a liquidação.

## Indicadores

O endpoint de resumo calcula:

```text
resultado = receita - despesa
margem (%) = resultado / receita × 100
caixa líquido = recebimentos - pagamentos
```

Também informa valores pendentes a receber e a pagar.

### Resumo por safra

Quando uma safra possui produtividade realizada, o sistema calcula:

```text
produção = produtividade realizada/ha × área do talhão
custo/ha = despesas da safra / área do talhão
custo/unidade = despesas da safra / produção
ponto de equilíbrio/unidade = despesas da safra / produção
```

A unidade de produção acompanha a unidade usada pela operação para a produtividade da safra. Nesta fase o sistema não converte automaticamente sacas, toneladas ou outras unidades.

O ponto de equilíbrio implementado é o preço mínimo por unidade produzida necessário para cobrir as despesas atribuídas à safra. Custos indiretos não vinculados à safra só entram no cálculo se forem explicitamente atribuídos a ela.

## Endpoints

Todos exigem autenticação e acesso ao módulo `Finance`.

```text
GET    /api/v1/finance/categories
POST   /api/v1/finance/categories
PUT    /api/v1/finance/categories/{id}
DELETE /api/v1/finance/categories/{id}

GET    /api/v1/finance/cost-centers
POST   /api/v1/finance/cost-centers
PUT    /api/v1/finance/cost-centers/{id}
DELETE /api/v1/finance/cost-centers/{id}

GET  /api/v1/finance/transactions
GET  /api/v1/finance/transactions/{id}
POST /api/v1/finance/transactions
PUT  /api/v1/finance/transactions/{id}
POST /api/v1/finance/transactions/{id}/settle
POST /api/v1/finance/transactions/{id}/cancel

GET /api/v1/finance/summary
GET /api/v1/finance/seasons/{seasonId}/summary
```

Listagens de transações aceitam filtros por tipo, status, período de competência, propriedade, talhão, safra e texto. O resumo aceita período e os mesmos relacionamentos operacionais.

## Multi-tenancy e integridade

- todo registro financeiro armazena `OrganizationId`;
- repositórios filtram por organização;
- referências externas são verificadas antes da gravação;
- categorias e centros de custo possuem nome único por organização;
- relações EF usam chaves estrangeiras;
- endpoints são protegidos pelo `ModuleAccessEndpointFilter(ModuleKey.Finance)`.

## Migration

A migration `20260907191652_FinanceCore` adiciona:

- `financial_categories`;
- `cost_centers`;
- `financial_transactions`;
- índices e chaves estrangeiras para organização, categoria, centro de custo e estrutura de produção.

## Testes

A Sprint 4 cobre:

- rejeição de valor financeiro não positivo;
- liquidação correta de despesa e receita;
- bloqueio de edição após liquidação;
- cancelamento de lançamento pendente;
- isolamento multi-tenant em PostgreSQL;
- separação entre competência, caixa e valores pendentes;
- cálculo econômico por safra.

## Integração futura com Inventory

O consumo de estoque já pode ser ligado a `Farm`, `Field` e `Season`, e o financeiro usa os mesmos identificadores. Isso deixa preparado um processo futuro que transforme consumo valorizado de insumos em despesa financeira sem acoplar rigidamente os dois módulos agora.

Antes dessa automação será necessário definir política de valorização de estoque (por exemplo, custo médio), tratamento de estornos e idempotência para impedir contabilização duplicada.

## Fora do escopo

- integração bancária;
- emissão fiscal;
- conciliação automática;
- contabilidade oficial;
- gateway de pagamentos;
- conversão automática entre unidades de produção;
- rateio automático de custos indiretos.
