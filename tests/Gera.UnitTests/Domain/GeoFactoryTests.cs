using Gera.Domain;

namespace Gera.UnitTests.Domain;

public sealed class GeoFactoryTests
{
    [Fact]
    public void CriaPontoComLongitudeEmXLatitudeEmY()
    {
        var ponto = GeoFactory.CriarPonto(-3.119, -60.021);

        Assert.Equal(-60.021, ponto.X);
        Assert.Equal(-3.119, ponto.Y);
        Assert.Equal(4326, ponto.SRID);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void RejeitaCoordenadasInvalidas(double latitude, double longitude) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoFactory.CriarPonto(latitude, longitude));
}
