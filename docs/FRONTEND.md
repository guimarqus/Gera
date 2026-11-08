# Frontend Gera

## Estrutura

- `src/auth`: contexto da sessao, JWT apenas em memoria e papel ativo como preferencia visual.
- `src/services/api.js`: cliente HTTP unico, cookie com credenciais, refresh single-flight e funcoes de endpoint.
- `src/design`: tokens CSS e componentes reutilizaveis de interface.
- `src/layout`: AppShell responsivo, menu desktop e navegacao/drawer mobile.
- `src/pages`: paginas publicas de autenticacao e paginas protegidas.
- `src/routing`: protecao de rotas, carregamento e error boundary.

## Design system

Os tokens ficam em `design/tokens.css`: cores Gera, espacamentos, raio de 12 px, sombras e tipografia. `design/components.jsx` exporta Button, Field/Input, Card, Badge, Avatar, Skeleton, EmptyState, ErrorState, PageHeader e Toast. Controles usam alvos de 48 px, foco visivel e estilos respeitam `prefers-reduced-motion`.

## Rotas

Publicas: `/entrar`, `/cadastrar`, `/esqueci-senha`, `/redefinir-senha`.
Protegidas no AppShell: `/`, `/categorias`, `/categorias/:id`, `/trabalhador`, `/mensagens`, `/perfil`, `/configuracoes`. Categorias busca a arvore em `GET /api/categorias`; as demais paginas protegidas tem estrutura e estado provisoriamente vazio.

## Adicionar uma pagina

Crie um componente em `src/pages`, use `PageHeader` e o estado adequado do design system, registre um import `lazy()` e uma rota no `src/App.jsx`. Paginas que precisam de sessao devem ficar dentro de `ProtectedRoute` e `AppShell`. Para buscar dados, use TanStack Query com as funcoes de `services/api.js`.

## Rodar e verificar

Na pasta `web`: `npm install`, `npm run dev`, `npm run build` e `npm test`. A suite de interface usa Vitest e Testing Library.
