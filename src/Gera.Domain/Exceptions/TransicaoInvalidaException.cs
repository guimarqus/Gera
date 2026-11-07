using Gera.Domain.Enums;

namespace Gera.Domain.Exceptions;

public class TransicaoInvalidaException : DomainException
{
    public EtapaKanban De { get; }
    public EtapaKanban Para { get; }

    public TransicaoInvalidaException(EtapaKanban de, EtapaKanban para, string motivo)
        : base($"Transição inválida de '{de}' para '{para}': {motivo}")
    {
        De = de;
        Para = para;
    }
}

