namespace Gera.Domain.Exceptions;

public class NaoAutorizadoException : DomainException
{
    public NaoAutorizadoException(string mensagem = "Você não tem permissão para realizar esta operação.") 
        : base(mensagem) { }
}

