using Gera.Application.Ofertas;
using Gera.Domain.Exceptions;

namespace Gera.UnitTests.Domain;

public sealed class TagsOfertaTests
{
    [Fact]
    public void NormalizaEspacosEAgrupaDuplicatasSemDistinguirCaixaEAcento()
    {
        var tags = TagsOferta.Normalizar(["  Instalação   elétrica ", "INSTALACAO ELETRICA", "Reparos"]);

        Assert.Equal(["Instalação elétrica", "Reparos"], tags);
    }

    [Fact]
    public void RejeitaMaisDeCincoTags()
    {
        Assert.Throws<DomainException>(() => TagsOferta.Normalizar(["A", "B", "C", "D", "E", "F"]));
    }
}
