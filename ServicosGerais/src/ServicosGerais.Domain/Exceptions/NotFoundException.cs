namespace ServicosGerais.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string recurso) : base($"{recurso} não encontrado(a).") { }
}

