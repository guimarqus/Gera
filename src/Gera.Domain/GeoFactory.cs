using NetTopologySuite.Geometries;

namespace Gera.Domain;

public static class GeoFactory
{
    public static Point CriarPonto(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        return new Point(longitude, latitude) { SRID = 4326 };
    }
}
