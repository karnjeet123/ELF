namespace Elf.Brewery.Domain.ValueObjects;

public readonly record struct GeoCoordinate(double Latitude, double Longitude)
{
    private const double EarthRadiusKm = 6371.0;

    public double DistanceKmTo(double lat, double lon)
    {
        var dLat = Rad(lat - Latitude);
        var dLon = Rad(lon - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Rad(Latitude)) * Math.Cos(Rad(lat))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double Rad(double deg) => deg * Math.PI / 180.0;
}