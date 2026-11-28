# Etapa 12 — Assistente de diagnóstico

## Entregue

- Página autenticada `/assistente` para descrever um problema e, opcionalmente, informar cidade/bairro.
- Consentimento explícito antes de enviar a descrição ao provedor de IA.
- API `POST /api/assistente/diagnostico`, protegida por autenticação, limite de 2.000 caracteres e rate limit de cinco análises por usuário por hora.
- Integração server-side com OpenAI Responses, saída JSON Schema estruturada, `store: false`, timeout de 25 segundos e limite de 350 tokens de saída.
- O modelo recebe somente a descrição e o catálogo ativo de categorias. IDs retornados são conferidos contra o catálogo do banco; sugestões de profissionais são consultadas no feed atual do Gera.
- Aviso de cautela determinístico para termos de risco elétrico, gás ou estrutura. A IA não determina emergência nem substitui profissional.
- Chave e nome do modelo vêm de variáveis de ambiente/User Secrets. A integração fica desativada por padrão e o endpoint retorna `503` até estar configurada.

## Configuração

Configure fora do repositório:

```text
Ai__OpenAI__Enabled=true
Ai__OpenAI__ApiKey=<chave da API>
Ai__OpenAI__Model=<modelo habilitado na conta>
```

O modelo não é fixado no código. A implantação deve configurar limites de gasto no projeto do provedor. O rate limit da API é local à instância e não impõe um teto de cobrança entre múltiplas instâncias.

## Privacidade e limites

- O endpoint não registra nem persiste descrição ou resposta.
- Não envia ao provedor nome, e-mail, cidade, coordenadas, ID de conta ou dados de solicitações. A cidade opcional só filtra ofertas no banco local.
- `store: false` desativa o armazenamento de respostas pela API, mas não constitui garantia sobre retenção operacional do provedor. Revise os controles e termos aplicáveis à conta antes de produção.
- A interface alerta para não incluir dados pessoais e exige consentimento por solicitação.
- Esta versão aceita apenas texto. Entrada por imagem/áudio e histórico conversacional não foram implementados.
- Orientações sobre gás, eletricidade e riscos estruturais são cautelares; diante de perigo, o usuário deve se afastar e procurar os serviços de emergência e profissionais habilitados.

## Validação

O contrato HTTP e leitura de respostas estruturadas são cobertos por teste com handler HTTP simulado. Sem chave de API e modelo habilitado neste ambiente, não houve chamada real nem validação de conteúdo gerado pelo provedor. O endpoint permanece inoperante até a configuração acima.
