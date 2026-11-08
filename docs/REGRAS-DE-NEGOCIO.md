# Regras de autenticação

- Senhas têm pelo menos 8 caracteres, uma letra e um número. O Identity aplica também comprimento mínimo e dígito.
- Cadastro exige nome, e-mail único, nome de usuário único, telefone válido, senha e aceites explícitos dos Termos de Uso e da Política de Privacidade. A API grava os dois aceites com horário UTC.
- Cinco falhas consecutivas de senha bloqueiam login por 15 minutos. Login por usuário e por e-mail usam a mesma resposta genérica para credenciais inválidas.
- Access tokens JWT duram no máximo 15 minutos e são mantidos somente em memória pelo frontend.
- Refresh tokens são aleatórios; somente seu SHA-256 é persistido. Cada renovação revoga o token anterior e registra o hash substituto.
- A reutilização de refresh token revogado é tratada como comprometimento: todos os refresh tokens ativos daquele usuário são revogados.
- Logout revoga o refresh token apresentado e remove o cookie do navegador.
- Esqueci a senha sempre responde de forma genérica. O envio de e-mail é abstraído; em Development o link é escrito no log. Fora de Development, a implementação atual é no-op até integrar um provedor real.
