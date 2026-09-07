# Sprint 9 — AgroControl Web

## Objetivo

A Sprint 9 transforma a base de backend e plataforma em uma aplicação utilizável no navegador. O frontend oficial passa a ser um cliente **React + TypeScript**, mantendo a API C# como autoridade sobre autenticação, multi-tenancy, papéis e entitlements.

## Stack

- React 19;
- TypeScript 7;
- Vite 8;
- React Router 7;
- CSS próprio, sem framework pesado de componentes;
- Node 24 no build;
- Nginx unprivileged para a imagem de produção.

As versões são fixadas diretamente no `package.json`. Um lockfile deve ser incorporado quando o fluxo de atualização de dependências estiver estabilizado; até lá o CI usa `npm install` e Dependabot acompanha o ecossistema npm.

## Arquitetura da sessão

O login e o registro usam os endpoints existentes da API. O token JWT fica em `sessionStorage`, é enviado como `Bearer` pelo cliente HTTP e é descartado quando expira ou quando a API retorna `401`.

Essa escolha é coerente com o contrato atual da API, mas não é apresentada como solução final para qualquer nível de risco. Se o AgroControl evoluir para uma aplicação pública exposta a usuários externos, deve-se avaliar BFF/cookie HttpOnly, refresh token com rotação e política de CSP mais restrita.

## Mesmo origin em produção

No desenvolvimento, o Vite faz proxy de `/api` e `/health` para `http://localhost:8080`.

Na imagem de produção, Nginx serve a SPA e encaminha as mesmas rotas para `API_UPSTREAM`. Assim o navegador não precisa conhecer a topologia interna dos serviços e não depende de CORS para o fluxo padrão.

```text
Browser
  |
  v
AgroControl Web / Nginx
  |-- /              -> SPA React
  |-- /api/*         -> AgroControl API
  `-- /health*       -> AgroControl API
```

## Design e UX

A interface usa identidade visual própria: navegação lateral, contexto da organização, plano, usuário, estados de módulo e layout responsivo. Não foi introduzida uma biblioteca visual grande somente para acelerar a primeira tela; isso mantém o bundle menor e evita acoplamento precoce.

Estados essenciais estão previstos: carregamento, vazio, erro, bloqueio por plano e confirmação de desativação.

## Dashboard

O dashboard consulta dados reais da API:

- propriedades e área cadastrada;
- talhões;
- safras e status;
- alertas de estoque baixo quando `Inventory` está habilitado;
- resumo financeiro quando `Finance` está habilitado.

Chamadas opcionais degradam de forma independente. Um problema no Finance, por exemplo, não deve esconder toda a visão de produção.

## Produção rural

A Sprint entrega fluxos web reais para:

- `Farm`: listar, buscar, criar, editar e desativar;
- `Field`: listar, filtrar por propriedade, criar, editar e desativar;
- `Crop`: listar, buscar, criar, editar e desativar;
- `Season`: listar, filtrar, criar, editar status/produtividade e desativar.

As relações de organização continuam sendo impostas pelo backend. O frontend apenas envia IDs selecionados dentro do contexto carregado.

## Módulos e entitlement

A sidebar e o catálogo mostram acesso habilitado/bloqueado para orientar navegação. Isso **não substitui autorização**. Um usuário que tentar chamar diretamente um endpoint sem entitlement continua sujeito ao `403` do backend.

Estoque, Financeiro, Máquinas, Mercado, Intelligence e Telemetry recebem páginas de entrada nesta Sprint; os fluxos operacionais detalhados desses módulos podem ser aprofundados em incrementos posteriores.

## Container e Kubernetes

A imagem final usa `nginxinc/nginx-unprivileged`, porta `8080` e usuário não-root. O Docker Compose expõe a web em `http://localhost:3001` por padrão.

Kubernetes recebe `Deployment` e `Service` próprios para `agrocontrol-web`, probes HTTP, requests/limits, usuário não-root, capabilities removidas e PDB. O upstream interno é o Service `agrocontrol-api:8080`.

## CI/CD

`Frontend CI` executa:

1. instalação de dependências;
2. type-check;
3. testes unitários;
4. build de produção;
5. build da imagem;
6. smoke test do container Nginx.

`Platform CI` passa a construir e iniciar também a web, validar a página principal e testar o proxy `/api` através do Nginx.

A publicação no GHCR inclui `agrocontrol-web` junto às imagens da API, Intelligence e Telemetry.

## Limites conscientes

Não entram nesta Sprint mapas GIS, PWA/offline-first, app móvel nativo, white-label nem uma suíte visual completa para todos os módulos. A prioridade é entregar uma fundação web utilizável e tecnicamente coerente com a plataforma já existente.
