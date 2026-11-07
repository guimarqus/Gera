# BemFeito — marketplace de serviços locais

Protótipo inicial responsivo para aproximar clientes e profissionais de serviços gerais. A interface está em português e oferece busca por texto, categorias, ordenação, cards de profissionais, perfil com pedido de orçamento e publicação de oferta. Anúncios criados no navegador ficam no `localStorage`.

## Estrutura

```
ServicosGerais/
├── web/                         # React + Vite, interface responsiva
│   └── src/App.jsx               # Fluxo do protótipo e dados demonstrativos
└── src/ServicosGerais.Api/       # ASP.NET Core Minimal API (.NET 10)
    └── Program.cs               # Endpoints iniciais de categorias e ofertas
```

## Executar

Requer .NET 10, Node.js e npm. Instale as dependências do frontend quando houver acesso ao npm registry:

```powershell
cd web
npm install
npm run dev
```

Em outro terminal, inicie a API:

```powershell
cd src/ServicosGerais.Api
dotnet run --urls http://localhost:5080
```

Rotas atuais da API: `GET /api/health`, `GET /api/categorias`, `GET /api/ofertas?busca=&categoria=&ordem=`, `GET /api/ofertas/{id}` e `POST /api/ofertas`. A API permite CORS para `http://localhost:5173`.

## Estado atual e decisões

- A interface funciona com ofertas locais de demonstração; o formulário grava novas ofertas no navegador. Ela ainda não consome a API.
- A API guarda os dados em memória: reiniciar o processo restaura a lista inicial. É uma base de demonstração, não persistência de produção.
- Não há autenticação, Identity/JWT, banco SQL Server, upload de mídia, SignalR, Kanban ou pagamentos nesta primeira entrega.
- O requisito original descreve um produto grande. Implementar todas as fases com segurança requer etapas incrementais; este recorte entrega primeiro o fluxo de descoberta e anúncio, deixando integrações e persistência como próximos passos explícitos.
- Os nomes, avaliações, preços e distâncias do feed são dados ilustrativos, não ofertas reais. A localização fixa exibida é Manaus; geolocalização real e consentimento LGPD ainda precisam ser implementados antes de lançamento.

## Próximos passos recomendados

1. Criar projetos Domain/Application/Infrastructure e adicionar EF Core com SQL Server, migrations e índices espaciais.
2. Conectar a tela ao endpoint de ofertas e mover persistência do anúncio para o backend.
3. Adicionar ASP.NET Core Identity, JWT/refresh, recuperação de senha e validação de entrada.
4. Implementar solicitação e transições do Kanban com histórico e controle de concorrência.
5. Adicionar chat SignalR, mídia em object storage, avaliações, notificações e testes automatizados.
6. Implementar geolocalização com consentimento explícito, pagamentos e os recursos de IA em fases posteriores.

## Contrato inicial de criação de oferta

`POST /api/ofertas` aceita JSON como:

```json
{
  "nome": "João da Silva",
  "titulo": "Pedreiro • Alvenaria",
  "categoria": "Construção",
  "preco": 180,
  "unidade": "diária",
  "descricao": "Faço muros e pequenas reformas."
}
```

O serviço deve receber identidade do prestador por token autenticado quando a autenticação for adicionada; o campo `nome` do exemplo é apenas para demonstrar o protótipo.
