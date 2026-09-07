# AgroControl.Infrastructure

Camada responsável por persistência e integrações externas da aplicação principal.

Próximas entregas previstas:

- Entity Framework Core e `AgroControlDbContext`;
- migrations e PostgreSQL;
- implementações de repositórios;
- provedores de autenticação;
- clientes para os serviços Python e Java;
- mensageria quando houver necessidade operacional.

`Infrastructure` pode depender de `Application` e `Domain`. `Domain` não depende desta camada.
