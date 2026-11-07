namespace Gera.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entidade, object id) 
        : base($"O recurso '{entidade}' com identificador '{id}' não foi encontrado.") { }

    public NotFoundException(string mensagem) : base(mensagem) { }
}

