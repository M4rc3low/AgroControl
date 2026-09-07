# AgroControl Web

Frontend oficial do AgroControl em **React + TypeScript + Vite**.

## Desenvolvimento

Com a API principal disponível em `http://localhost:8080`:

```bash
npm install
npm run dev
```

O Vite encaminha `/api` e `/health` para a API. A aplicação usa `sessionStorage` para o JWT emitido pelo backend; não existe token de demonstração embutido no frontend.

## Scripts

```bash
npm run typecheck
npm test
npm run build
```

## Produção

O `Dockerfile` usa build multi-stage com Node e Nginx. O Nginx serve a SPA e encaminha `/api` para o serviço `api` da rede do Docker Compose, mantendo a interface e a API no mesmo origin do navegador.

## Segurança

O frontend apresenta módulos bloqueados para orientar o usuário, mas **não é a autoridade de autorização**. O backend continua validando JWT, organização, papel e entitlement.
