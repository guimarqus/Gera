# Guia de contribuição

## Antes de alterar

- Leia este documento e a documentação de arquitetura, API, regras de negócio e banco.
- Confira `git status` e preserve alterações que já estejam na árvore de trabalho.
- Siga os padrões existentes e mantenha controllers finos, DTOs nas fronteiras HTTP e regras na Application/Domain.
- Não introduza dependências ou mude a stack sem necessidade documentada.

## Desenvolvimento local

Requisitos: .NET 10 SDK, Node.js compatível com o `package-lock.json`, npm e SQL Server para os testes de integração.

```powershell
dotnet restore Gera.sln
dotnet test Gera.sln
cd web
npm ci
npm run test
npm run build
```

Os testes de integração aplicam migrations em banco isolado. Configure `GeraTest__SqlServer` com uma instância de teste se LocalDB não estiver disponível. Nunca use banco de produção nos testes.

## Qualidade e segurança

- Valide autorização no servidor e derive identidade do usuário autenticado.
- Não exponha entidades EF diretamente nem coordenadas precisas em respostas públicas.
- Não registre tokens ou segredos e não versione credenciais.
- Para alterações de banco, adicione migration nova; não reescreva migration aplicada sem avaliar os bancos existentes.
- Documente comportamento de API e regras alteradas.
- Revise `git diff --check`, o diff completo e os arquivos incluídos antes do commit.

## Commits

Use Conventional Commits, com mensagens curtas em português e uma intenção por commit, por exemplo `feat(ofertas): permite retomar rascunho` ou `docs(api): documenta envio de fotos`.
