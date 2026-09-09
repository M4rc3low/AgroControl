# Sprint 20 — Web instalável + Desktop Tauri

## Objetivo

Entregar o AgroControl como uma única aplicação React distribuída em três experiências:

1. navegador;
2. PWA instalável;
3. desktop Windows via Tauri.

A regra central é **não duplicar produto**. Telas, contratos, autorização e regras continuam compartilhados.

## Arquitetura

```text
                    ┌───────────────────┐
                    │ AgroControl React │
                    │ Vite / TypeScript │
                    └─────────┬─────────┘
                              │
                 ┌────────────┼────────────┐
                 │            │            │
                 v            v            v
             Browser         PWA      Tauri Desktop
                 │            │            │
                 └────────────┴────────────┘
                              │ HTTPS
                              v
                      AgroControl API
                              │
              ┌───────────────┼───────────────┐
              v               v               v
          PostgreSQL      Intelligence     Telemetry
           + PostGIS        Python           Java
```

O desktop **não** embute API C#, PostgreSQL/PostGIS, Python Intelligence, Java Telemetry ou MQTT.

## PWA

A PWA adiciona:

- `manifest.webmanifest`;
- fonte vetorial canônica `public/icons/agrocontrol.svg` declarada para os tamanhos de instalação 192x192 e 512x512;
- `display: standalone`;
- service worker;
- cache somente do app shell e assets estáticos;
- fallback do `index.html` para abrir a interface sem conexão;
- aviso visual quando o navegador perde conectividade.

Os antigos PNGs gerados durante a implementação inicial foram removidos depois que o build detectou arquivos truncados. O SVG é a única fonte canônica; formatos nativos são derivados dele pelo Tauri CLI.

### Limite de offline nesta sprint

O service worker não intercepta/cacheia:

- `/api/*`;
- `/health` e `/health/*`;
- requests externos;
- requests que não sejam GET.

Isso significa que **offline nesta sprint é apenas o shell da interface**. Dados de fazenda, financeiro, estoque, mapas operacionais, telemetria e demais informações autenticadas continuam exigindo a API.

Sincronização offline de dados será uma evolução separada com armazenamento local, fila, versionamento e resolução de conflitos próprios.

O service worker não é registrado quando a mesma SPA está rodando dentro da origem Tauri. O desktop não precisa de uma segunda camada de cache do app shell.

## Desktop Tauri

A aplicação desktop usa Tauri v2 com o mesmo build Vite.

Estrutura:

```text
src/frontend/
├── src/                 # React compartilhado
├── public/              # manifest, service worker e fonte canônica dos ícones
├── dist/                # build Web compartilhado
└── src-tauri/           # shell nativo mínimo
    ├── Cargo.toml
    ├── build.rs
    ├── tauri.conf.json
    └── src/
```

Antes de `desktop:dev` e `desktop:build`, o Tauri CLI gera PNG/ICO/ICNS nativos a partir de `public/icons/agrocontrol.svg`. O CI usa a mesma fonte e o mesmo processo, evitando ícones binários artesanais no repositório.

### Superfície nativa inicial

A Sprint 20 não expõe comandos Rust para o JavaScript e não instala plugins Tauri privilegiados. `withGlobalTauri` permanece desabilitado e a lista de capabilities começa vazia.

A janela desktop:

- reutiliza a SPA React;
- não abre DevTools na build de distribuição;
- usa CSP explícita;
- continua autenticando na API via JWT;
- continua sujeita a `OrganizationId`, entitlements e `FarmAccessScope`.

### Sessão

Browser, PWA e Desktop continuam usando o contrato Bearer existente e `sessionStorage` para a sessão JWT. A Sprint 20 não copia token para arquivo, Registry, banco local ou plugin nativo.

A escolha é deliberada: o shell desktop não cria uma nova camada de persistência privilegiada para credenciais. Uma futura evolução para cookie HttpOnly/BFF ou armazenamento nativo seguro deve ser tratada como mudança de modelo de autenticação e não como detalhe do instalador.

## URL da API e CORS

No navegador normal, o AgroControl pode continuar usando proxy same-origin.

No build desktop, `VITE_API_BASE_URL` deve ser definido durante a compilação para o endpoint da API. O CI usa `http://localhost:8080` apenas como configuração de build/teste.

No Windows, o shell empacotado usa `http://tauri.localhost` como origem. A API aceita essa origem por uma política CORS configurada por allowlist; não há `AllowAnyOrigin` e a autorização continua sendo realizada pelo JWT e pelas fronteiras `OrganizationId`/`FarmAccessScope`.

**Um release de produção não deve ser publicado enquanto a URL HTTPS pública da API e a CSP correspondente não estiverem definidas.**

## Segurança

Princípios:

- autorização continua exclusivamente no backend;
- nenhum secret é empacotado no desktop;
- service worker não cria cache de dados autenticados;
- sem comandos nativos até existir necessidade concreta;
- capabilities Tauri seguem least privilege;
- CORS usa origens explícitas;
- o instalador Windows só será considerado distribuição oficial depois de assinatura de código real.

A configuração de CI/Desktop atual permite conexão apenas com:

- a própria origem do shell;
- `http://localhost:8080` para a API de desenvolvimento/CI;
- `https://demotiles.maplibre.org` para o estilo/tiles padrão do mapa.

Scripts e CSS do MapLibre permanecem limitados ao host padrão já utilizado pela Web (`https://unpkg.com`). A origem HTTPS real da API deverá ser adicionada de forma explícita ao CSP no momento em que definirmos o ambiente público; não será usado um wildcard `https:` para isso.

## Windows e WebView2

O alvo inicial do desktop é Windows. Tauri usa a WebView do sistema em vez de empacotar Chromium completo, reduzindo tamanho e superfície de atualização do runtime.

O pipeline gera:

- NSIS `.exe`;
- MSI `.msi`.

Os artefatos de CI são explicitamente **não assinados**.

## Atualizações

### PWA

O service worker troca de versão, remove caches antigos do app shell e ativa a versão nova. Dados de API não participam desse cache.

### Desktop

Atualização automática não é habilitada nesta fase. Antes disso é necessário definir:

- domínio/API de produção;
- estratégia de releases;
- assinatura dos artefatos;
- chave/segredo do mecanismo de update armazenados corretamente;
- política de rollback.

## CI

Gates da Sprint 20:

- Frontend CI;
- Desktop CI Windows;
- Backend CI;
- Platform CI;
- CodeQL.

`Frontend CI` verifica manifest, service worker e SVG canônico. `Desktop CI` gera os ícones nativos a partir do SVG e compila os installers Windows usando o mesmo frontend.

## Fora do escopo

- PostgreSQL local;
- backend local dentro do instalador;
- Python/Java como sidecar;
- sincronização offline de dados;
- banco SQLite local;
- conflito/merge de registros offline;
- cache offline de raster/mapas;
- publicação em lojas;
- certificado de assinatura inexistente;
- auto-update sem cadeia de assinatura definida.

## Critérios de encerramento

A Sprint só pode ser encerrada quando:

- Web/PWA continuam passando o Frontend CI;
- instalador Windows é gerado pelo Desktop CI;
- os gates Backend, Platform e CodeQL continuam verdes;
- README, ARCHITECTURE e ROADMAP estiverem atualizados;
- PR final for squash merged;
- Issue #57 for encerrada após os gates.
