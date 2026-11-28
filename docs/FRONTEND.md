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
Protegidas no AppShell: `/`, `/categorias`, `/categorias/:id`, `/trabalhador`, `/mensagens`, `/perfil`, `/configuracoes`. A página inicial consulta categorias e `GET /api/ofertas/feed`, permite busca por texto, categoria, preço e cidade, solicita localização do navegador por ação do usuário, e oferece ordenação, paginação e favoritos. A busca por cidade continua disponível quando a permissão de localização é negada.

## Adicionar uma pagina

Crie um componente em `src/pages`, use `PageHeader` e o estado adequado do design system, registre um import `lazy()` e uma rota no `src/App.jsx`. Paginas que precisam de sessao devem ficar dentro de `ProtectedRoute` e `AppShell`. Para buscar dados, use TanStack Query com as funcoes de `services/api.js`.

## Rodar e verificar

Na pasta `web`: `npm install`, `npm run dev`, `npm run build` e `npm test`. A suite de interface usa Vitest e Testing Library.
## Painel do Trabalhador

`/trabalhador` lista as ofertas da sessão com filtro por status e paginação. A ação Nova oferta cria um rascunho no servidor e abre `/trabalhador/ofertas/nova?rascunho={id}`. Ofertas existentes também podem ser abertas em `/trabalhador/ofertas/:id`. O editor recupera a versão salva e usa essa versão para controlar edições concorrentes. Pausa, reativação e exclusão são executadas no painel.

O editor divide os campos disponíveis entre dados do serviço, características, área de atendimento, fotografias, preço, disponibilidade e revisão. A etapa de características carrega o modelo da categoria selecionada e aceita até cinco tags. O autosave aguarda 800 ms após a última alteração e envia uma requisição por vez. Se houver uma edição enquanto a requisição estiver em andamento, a edição local é preservada e o próximo salvamento usa a versão devolvida pelo servidor. O botão de salvar permite tentar novamente se a rede falhar. A área de atendimento aceita a localização do navegador ou o preenchimento manual.

Na etapa Fotografias, o proprietário pode selecionar várias fotos, acompanhar quantas foram enviadas, reordenar, definir a capa e excluir fotos cadastradas. Os envios são sequenciais para usar a versão atual da oferta. A revisão indica os requisitos faltantes e libera a publicação quando a oferta está completa e salva.
