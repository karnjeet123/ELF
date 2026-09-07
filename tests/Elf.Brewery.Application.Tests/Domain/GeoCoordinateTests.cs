using Elf.Brewery.Domain.ValueObjects;
using Xunit;

namespace Elf.Brewery.Application.Tests.Domain;

public class GeoCoordinateTests
{
    [Fact]
    public void DistanceKmTo_SamePoint_ReturnsZero()
    {
        var point = new GeoCoordinate(40.7128, -74.0060); // New York

        var distance = point.DistanceKmTo(40.7128, -74.0060);

        Assert.Equal(0, distance, precision: 3);
    }

    [Fact]
    public void DistanceKmTo_KnownCities_ReturnsExpectedDistance()
    {
        // New York to Los Angeles is approximately 3936 km great-circle distance.
        var newYork = new GeoCoordinate(40.7128, -74.0060);

        var distance = newYork.DistanceKmTo(34.0522, -118.2437);

        Assert.InRange(distance, 3900, 3970);
    }

    [Fact]
    public void DistanceKmTo_IsSymmetric()
    {
        var pointA = new GeoCoordinate(51.5074, -0.1278); // London
        var pointB = new GeoCoordinate(48.8566, 2.3522);  // Paris

        var aToB = pointA.DistanceKmTo(pointB.Latitude, pointB.Longitude);
        var bToA = pointB.DistanceKmTo(pointA.Latitude, pointA.Longitude);

        Assert.Equal(aToB, bToA, precision: 6);
    }
}
