using FluentAssertions;
using Gera.Application.Catalog;
using Gera.Domain.Entities;

namespace Gera.UnitTests.Catalog;

public sealed class CategoriaTreeBuilderTests
{
    [Fact]
    public void Build_OrdersChildrenAndOmitsInactiveCategories()
    {
        var categories = new[]
        {
            new Categoria { Id = 1, Nome = "Obras", Ordem = 1 },
            new Categoria { Id = 2, Nome = "Elétrica", Ordem = 2 },
            new Categoria { Id = 11, CategoriaPaiId = 1, Nome = "Reboco", Ordem = 2 },
            new Categoria { Id = 10, CategoriaPaiId = 1, Nome = "Pisos", Ordem = 1 },
            new Categoria { Id = 12, CategoriaPaiId = 1, Nome = "Oculta", Ordem = 3, Ativo = false }
        };

        var tree = CategoriaTreeBuilder.Build(categories);
        tree.Select(c => c.Id).Should().Equal(1, 2);
        tree[0].Subcategorias.Select(c => c.Id).Should().Equal(10, 11);
        tree[0].Subcategorias.Should().NotContain(c => c.Id == 12);
    }
}
