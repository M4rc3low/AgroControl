# ADR-002 — Começar com monólito modular

**Status:** Aceito

## Contexto

O domínio terá muitos módulos, mas microserviços precoces adicionariam rede, observabilidade distribuída, versionamento de contratos e complexidade operacional antes de existir carga que justifique isso.

## Decisão

A API C# será um monólito modular. Fronteiras internas serão explícitas, mas o deploy continuará único no início.

## Consequências

- desenvolvimento e debugging mais simples;
- transações locais;
- menor custo de infraestrutura;
- possibilidade de extrair serviços futuramente quando houver razão mensurável.
