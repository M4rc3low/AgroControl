# Convenções da API

## Base URL

```text
/api/v1
```

## Recursos

Preferir substantivos no plural:

```text
GET    /api/v1/farms
POST   /api/v1/farms
GET    /api/v1/farms/{id}
PUT    /api/v1/farms/{id}
DELETE /api/v1/farms/{id}
```

## Status HTTP

- `200` — leitura/alteração com sucesso;
- `201` — criação;
- `204` — operação sem corpo de resposta;
- `400` — entrada inválida;
- `401` — não autenticado;
- `403` — autenticado sem permissão ou módulo;
- `404` — recurso não encontrado;
- `409` — conflito de regra/estado;
- `422` — validação semântica, se adotado;
- `500` — erro não tratado.

## Erros

A API deverá convergir para `ProblemDetails`.

```json
{
  "type": "https://agrocontrol.dev/problems/module-not-enabled",
  "title": "Module not enabled",
  "status": 403,
  "detail": "The Intelligence module is not enabled for this organization."
}
```

## Paginação

```text
GET /api/v1/farms?page=1&pageSize=20
```

## Datas

ISO 8601.

## IDs

UUID/Guid serializado como string.

## Versionamento

Mudanças incompatíveis exigem nova versão de API. Evoluções compatíveis devem permanecer na mesma versão.
