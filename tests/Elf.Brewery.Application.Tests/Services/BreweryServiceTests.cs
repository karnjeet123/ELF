using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Application.Services;
using Elf.Brewery.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Services;

public class BreweryServiceTests
{
    private const string StorageKey = "test-storage";

    private static BreweryService CreateSut(
        IReadOnlyList<DomainBrewery> breweries,
        Mock<IBrewerySearchService> searchMock,
        Mock<IBrewerySorterFactory> sorterFactoryMock,
        Mock<IBreweryDtoMapper> mapperMock)
    {
        var facadeMock = new Mock<IBreweryDataFacade>();
        facadeMock.Setup(f => f.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(breweries);

        var services = new ServiceCollection();
        services.AddKeyedSingleton(StorageKey, (_, _) => facadeMock.Object);
        var provider = services.BuildServiceProvider();

        return new BreweryService(
            StorageKey,
            provider,
            searchMock.Object,
            sorterFactoryMock.Object,
            mapperMock.Object);
    }

    [Fact]
    public async Task GetBreweryByIdAsync_ExistingId_ReturnsMappedDto()
    {
        var brewery = new DomainBrewery { Id = "abc", Name = "Test Brewery" };
        var expectedDto = new BreweryDto("abc", "Test Brewery", null, null, null, null, null, null, null, null, null, null);

        var searchMock = new Mock<IBrewerySearchService>();
        var sorterFactoryMock = new Mock<IBrewerySorterFactory>();
        var mapperMock = new Mock<IBreweryDtoMapper>();
        mapperMock.Setup(m => m.ToDto(brewery, null)).Returns(expectedDto);

        var sut = CreateSut(new[] { brewery }, searchMock, sorterFactoryMock, mapperMock);

        var result = await sut.GetBreweryByIdAsync("abc", CancellationToken.None);

        Assert.Equal(expectedDto, result);
    }

    [Fact]
    public async Task GetBreweryByIdAsync_MissingId_ThrowsBreweryNotFoundException()
    {
        var searchMock = new Mock<IBrewerySearchService>();
        var sorterFactoryMock = new Mock<IBrewerySorterFactory>();
        var mapperMock = new Mock<IBreweryDtoMapper>();

        var sut = CreateSut(Array.Empty<DomainBrewery>(), searchMock, sorterFactoryMock, mapperMock);

        await Assert.ThrowsAsync<BreweryNotFoundException>(
            () => sut.GetBreweryByIdAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task GetCitiesAsync_ReturnsDistinctSortedCities()
    {
        var breweries = new[]
        {
            new DomainBrewery { Id = "1", Name = "A", City = "Denver" },
            new DomainBrewery { Id = "2", Name = "B", City = "austin" },
            new DomainBrewery { Id = "3", Name = "C", City = "Denver" },
            new DomainBrewery { Id = "4", Name = "D", City = null },
        };

        var searchMock = new Mock<IBrewerySearchService>();
        var sorterFactoryMock = new Mock<IBrewerySorterFactory>();
        var mapperMock = new Mock<IBreweryDtoMapper>();

        var sut = CreateSut(breweries, searchMock, sorterFactoryMock, mapperMock);

        var cities = await sut.GetCitiesAsync(CancellationToken.None);

        Assert.Equal(new[] { "austin", "Denver" }, cities);
    }

    [Fact]
    public async Task RefreshBreweriesAsync_DelegatesToFacade()
    {
        var facadeMock = new Mock<IBreweryDataFacade>();
        facadeMock.Setup(f => f.RefreshFromExternalAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);

        var services = new ServiceCollection();
        services.AddKeyedSingleton(StorageKey, (_, _) => facadeMock.Object);
        var provider = services.BuildServiceProvider();

        var sut = new BreweryService(
            StorageKey,
            provider,
            new Mock<IBrewerySearchService>().Object,
            new Mock<IBrewerySorterFactory>().Object,
            new Mock<IBreweryDtoMapper>().Object);

        var result = await sut.RefreshBreweriesAsync(CancellationToken.None);

        Assert.Equal(42, result);
    }
}
