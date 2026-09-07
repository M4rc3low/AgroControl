# AgroControl Web

Frontend oficial do AgroControl em **React + TypeScript + Vite**.

## Desenvolvimento

Com a API principal disponível em `http://localhost:8080`:

```bash
npm ci
npm run dev
```

O Vite encaminha `/api` e `/health` para a API. A aplicação usa `sessionStorage` para o JWT emitido pelo backend; não existe token de demonstração embutido no frontend.

## Scripts

```bash
npm run typecheck
npm test
npm run build
```

As dependências são reproduzidas por `package-lock.json` e o CI usa `npm ci`.

## Produção

O `Dockerfile` usa build multi-stage com Node 24 e `nginx-unprivileged`. O Nginx roda sem root, serve a SPA na porta `8080` e encaminha `/api` para `API_UPSTREAM`. No Docker Compose o upstream é `api:8080`; no Kubernetes é `agrocontrol-api:8080`.

## Segurança

O frontend apresenta módulos bloqueados para orientar o usuário, mas **não é a autoridade de autorização**. O backend continua validando JWT, organização, papel e entitlement.

O JWT fica em `sessionStorage` porque o contrato atual da API retorna bearer token diretamente. Uma evolução futura pode migrar a sessão web para cookie HttpOnly/fluxo BFF se houver necessidade de endurecer a superfície de XSS e renovação de sessão.
