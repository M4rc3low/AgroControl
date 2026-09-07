# ADR-001 — Usar monorepo

**Status:** Aceito

## Contexto

O AgroControl terá frontend, backend C#, serviço Python e serviço Java. No estágio inicial, manter tudo em repositórios separados aumentaria o custo de coordenação sem benefício proporcional.

## Decisão

Usar um monorepo com separação clara por componente.

## Consequências

### Positivas

- documentação centralizada;
- histórico de arquitetura no mesmo lugar;
- mudanças de contrato podem ser coordenadas;
- CI pode evoluir por caminhos afetados.

### Negativas

- exige disciplina de pastas e ownership;
- pipelines precisarão evitar builds desnecessários quando o projeto crescer.
