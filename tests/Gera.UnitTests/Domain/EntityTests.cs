using FluentAssertions;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using NetTopologySuite.Geometries;
using Xunit;

namespace Gera.UnitTests.Domain;

public class EntityTests
{
    [Fact]
    public void Usuario_DeveInicializarComValoresPadraoCorretos()
    {
        // Act
        var usuario = new Usuario
        {
            Nome = "Carlos Eletricista",
            Email = "carlos@teste.com"
        };

        // Assert
        usuario.Nome.Should().Be("Carlos Eletricista");
        usuario.PapelAtivo.Should().Be("Cliente");
        usuario.Ativo.Should().BeTrue();
        usuario.NotificacoesAtivas.Should().BeTrue();
        usuario.RaioAtendimentoPadraoKm.Should().Be(15);
        usuario.Ofertas.Should().BeEmpty();
        usuario.SolicitacoesCliente.Should().BeEmpty();
    }

    [Fact]
    public void Oferta_DeveInicializarComGeometriaNetTopologySuiteEStatusAtivo()
    {
        // Arrange
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var localizacao = geometryFactory.CreatePoint(new Coordinate(-47.9292, -15.7801)); // Brasília

        // Act
        var oferta = new Oferta
        {
            Titulo = "Serviços de Pintura Residencial",
            Descricao = "Pintura de paredes internas e externas com acabamento fino.",
            PrestadorId = 1,
            CategoriaId = 4,
            Localizacao = localizacao,
            Latitude = -15.7801,
            Longitude = -47.9292,
            RaioKm = 20,
            TipoPreco = TipoPreco.Diaria,
            Valor = 250.00m,
            Status = StatusOferta.Ativo
        };

        // Assert
        oferta.Status.Should().Be(StatusOferta.Ativo);
        oferta.Localizacao.Should().NotBeNull();
        oferta.Localizacao.Coordinate.X.Should().Be(-47.9292);
        oferta.Localizacao.Coordinate.Y.Should().Be(-15.7801);
        oferta.Valor.Should().Be(250.00m);
    }

    [Fact]
    public void Solicitacao_DeveIniciarNaEtapaSolicitacoes()
    {
        // Act
        var solicitacao = new Solicitacao
        {
            ClienteId = 1,
            PrestadorId = 2,
            OfertaId = 10,
            Descricao = "Gostaria de um orçamento para pintar 2 quartos."
        };

        // Assert
        solicitacao.Etapa.Should().Be(EtapaKanban.Solicitacoes);
        solicitacao.AceiteCliente.Should().BeFalse();
        solicitacao.AceitePrestador.Should().BeFalse();
        solicitacao.ConcluidoPorCliente.Should().BeFalse();
        solicitacao.ConcluidoPorPrestador.Should().BeFalse();
    }

    [Fact]
    public void TransicaoInvalidaException_DeveConterEtapasDeOrigemEDestino()
    {
        // Act
        var ex = new TransicaoInvalidaException(
            EtapaKanban.Solicitacoes,
            EtapaKanban.Concluidos,
            "Não é permitido concluir sem execução prévia.");

        // Assert
        ex.De.Should().Be(EtapaKanban.Solicitacoes);
        ex.Para.Should().Be(EtapaKanban.Concluidos);
        ex.Message.Should().Contain("Solicitacoes");
        ex.Message.Should().Contain("Concluidos");
    }
}

