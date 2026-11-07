namespace ServicosGerais.Domain.Exceptions;

public class TransicaoInvalidaException : DomainException
{
    public TransicaoInvalidaException(string de, string para)
        : base($"Transição inválida de '{de}' para '{para}'.") { }
}

