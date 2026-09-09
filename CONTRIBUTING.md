# Contribuindo com o AgroControl

Este documento define o fluxo de contribuição e merge esperado para o repositório.

## Branches

- `main` representa o estado integrado e testável do produto.
- trabalho funcional deve partir de uma branch curta e específica;
- exemplos: `feat/...`, `fix/...`, `chore/...`, `docs/...`;
- evite desenvolver diretamente na `main`.

## Pull requests

Toda alteração relevante deve chegar à `main` por Pull Request.

O PR deve:

- explicar objetivo e impacto;
- listar migrations quando existirem;
- descrever mudanças de segurança/autorização quando aplicável;
- registrar alterações de contrato/API;
- indicar testes adicionados ou alterados;
- manter documentação e versionamento consistentes.

## Gates obrigatórios

Antes do merge, o mesmo head do PR deve estar verde em:

- Backend CI;
- Frontend CI;
- Platform CI;
- Desktop CI;
- CodeQL.

Se um novo commit for enviado depois dos checks, os gates devem ser considerados inválidos até rodarem novamente no novo head.

## Estratégia de merge

Preferência do projeto:

1. PR revisado;
2. todos os checks obrigatórios verdes no mesmo head;
3. squash merge;
4. issue da sprint/entrega fechada somente após o merge real;
5. branch de trabalho removida quando não houver trabalho pendente.

## Segurança

Alterações que afetem dados de negócio devem preservar:

- isolamento por `OrganizationId`;
- autorização horizontal por `FarmAccessScope`;
- entitlement server-side;
- ausência de confiança no frontend como fronteira de autorização;
- logs/métricas sem secrets ou identificadores de alta cardinalidade desnecessários;
- idempotência e concorrência quando a operação puder ser repetida.

Mudanças no offline devem definir explicitamente:

- namespace local;
- estratégia de conflito;
- idempotência;
- comportamento em revogação de acesso;
- purge;
- testes Farm A × Farm B e, quando aplicável, Organização A × Organização B.

## Migrations

Toda migration deve:

- ser determinística;
- manter compatibilidade com PostgreSQL/PostGIS suportado pelo projeto;
- evitar perda silenciosa de dados;
- possuir teste de integração quando introduzir regra estrutural relevante;
- documentar backfill e irreversibilidade quando necessário.

## Versionamento

A release consolidada deve manter coerência entre documentação, API e Desktop.

O pacote npm privado `@agrocontrol/web` possui versionamento interno e não precisa coincidir com a versão pública do produto, desde que essa distinção esteja documentada.

## Documentação

Quando uma sprint ou entrega é concluída, atualizar conforme aplicável:

- `README.md`;
- `docs/ARCHITECTURE.md`;
- `docs/ROADMAP.md`;
- documento específico da sprint;
- catálogo de módulos/contratos, quando afetado.

Status como `concluído`, `merged` ou `completed` só devem ser registrados depois que o evento realmente acontecer.

## Proteção da main

A política acima deve ser reforçada por branch protection/ruleset no GitHub. O acompanhamento administrativo dessa configuração está na Issue #52.
