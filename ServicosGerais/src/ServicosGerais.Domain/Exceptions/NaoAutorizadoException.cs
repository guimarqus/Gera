namespace ServicosGerais.Domain.Exceptions;

public class NaoAutorizadoException : DomainException
{
    public NaoAutorizadoException(string message = "Ação não autorizada.") : base(message) { }
}

