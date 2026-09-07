# Workflow de desenvolvimento no GitHub

## Branches

- `main` — branch estável;
- `feat/<tema>` — funcionalidades;
- `fix/<tema>` — correções;
- `docs/<tema>` — documentação;
- `chore/<tema>` — infraestrutura/manutenção.

Exemplos:

```text
feat/farms-module
feat/inventory-module
fix/season-validation
docs/architecture
```

## Commits

Usar mensagens curtas e objetivas, preferencialmente no padrão Conventional Commits:

```text
feat: add farm creation use case
fix: prevent negative inventory balance
docs: document module entitlement
chore: configure backend CI
```

## Pull requests

Cada PR deve:

- ter escopo pequeno e compreensível;
- explicar o problema e a solução;
- indicar como foi testado;
- atualizar documentação quando necessário;
- evitar misturar refatoração ampla com nova funcionalidade sem necessidade.

## Definition of Done

Uma funcionalidade é considerada concluída quando:

- regras principais estão implementadas;
- testes relevantes passam;
- CI está verde;
- documentação foi atualizada;
- não há secrets no repositório;
- autorização e tenant foram considerados;
- migrations foram revisadas quando houver alteração de banco.
