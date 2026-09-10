using Microsoft.Extensions.Logging.Abstractions;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Options;
using Elf.Brewery.Application.Services;
using Moq;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Services;

public class BreweryDataFacadeTests
{
    private const string StorageKey = "test-storage";

    private static readonly IReadOnlyList<DomainBrewery> RepositoryData = new[]
    {
        new DomainBrewery { Id = "1", Name = "Repo Brewery" },
    };

    private static readonly IReadOnlyList<DomainBrewery> ExternalData = new[]
    {
        new DomainBrewery { Id = "2", Name = "External Brewery" },
    };

    private static BreweryDataFacade CreateSut(
        Mock<IBreweryRepository> repositoryMock,
        Mock<IBreweryProvider> providerMock,
        Mock<ICacheService> cacheMock,
        FakeTimeProvider timeProvider,
        int externalRefreshMinutes = 10)
    {
        var cacheOptions = Microsoft.Extensions.Options.Options.Create(new CacheOptions
        {
            ExpirationMinutes = 10,
            ExternalRefreshMinutes = externalRefreshMinutes,
        });

        return new BreweryDataFacade(
            repositoryMock.Object,
            providerMock.Object,
            cacheMock.Object,
            cacheOptions,
            NullLogger<BreweryDataFacade>.Instance,
            timeProvider,
            StorageKey);
    }

    private static Mock<ICacheService> CreateEmptyCacheMock()
    {
        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.TryGetAsync<IReadOnlyList<DomainBrewery>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (IReadOnlyList<DomainBrewery>?)null));
        return cacheMock;
    }

    [Fact]
    public async Task GetAllAsync_DataOlderThanRefreshInterval_FetchesFromExternalProvider()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var lastRefreshed = timeProvider.GetUtcNow() - TimeSpan.FromMinutes(11);

        var repositoryMock = new Mock<IBreweryRepository>();
        repositoryMock.Setup(r => r.GetLastRefreshUtcAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lastRefreshed);
        repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryData);

        var providerMock = new Mock<IBreweryProvider>();
        providerMock.Setup(p => p.FetchAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ExternalData);

        var cacheMock = CreateEmptyCacheMock();

        var sut = CreateSut(repositoryMock, providerMock, cacheMock, timeProvider, externalRefreshMinutes: 10);

        await sut.GetAllAsync(CancellationToken.None);

        providerMock.Verify(p => p.FetchAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        repositoryMock.Verify(r => r.UpsertRangeAsync(ExternalData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DataWithinRefreshInterval_DoesNotFetchFromExternalProvider()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var lastRefreshed = timeProvider.GetUtcNow() - TimeSpan.FromMinutes(5);

        var repositoryMock = new Mock<IBreweryRepository>();
        repositoryMock.Setup(r => r.GetLastRefreshUtcAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lastRefreshed);
        repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryData);

        var providerMock = new Mock<IBreweryProvider>();

        var cacheMock = CreateEmptyCacheMock();

        var sut = CreateSut(repositoryMock, providerMock, cacheMock, timeProvider, externalRefreshMinutes: 10);

        var result = await sut.GetAllAsync(CancellationToken.None);

        providerMock.Verify(p => p.FetchAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(RepositoryData, result);
    }

    [Fact]
    public async Task GetAllAsync_NeverRefreshed_FetchesFromExternalProvider()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var repositoryMock = new Mock<IBreweryRepository>();
        repositoryMock.Setup(r => r.GetLastRefreshUtcAsync(It.IsAny<CancellationToken>())).ReturnsAsync((DateTimeOffset?)null);
        repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ExternalData);

        var providerMock = new Mock<IBreweryProvider>();
        providerMock.Setup(p => p.FetchAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ExternalData);

        var cacheMock = CreateEmptyCacheMock();

        var sut = CreateSut(repositoryMock, providerMock, cacheMock, timeProvider);

        await sut.GetAllAsync(CancellationToken.None);

        providerMock.Verify(p => p.FetchAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_CacheHit_DoesNotCheckStalenessOrRepository()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var repositoryMock = new Mock<IBreweryRepository>();
        var providerMock = new Mock<IBreweryProvider>();

        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.TryGetAsync<IReadOnlyList<DomainBrewery>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, RepositoryData));

        var sut = CreateSut(repositoryMock, providerMock, cacheMock, timeProvider);

        var result = await sut.GetAllAsync(CancellationToken.None);

        repositoryMock.Verify(r => r.GetLastRefreshUtcAsync(It.IsAny<CancellationToken>()), Times.Never);
        providerMock.Verify(p => p.FetchAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(RepositoryData, result);
    }

    /// <summary>
    /// Minimal controllable clock so staleness can be tested deterministically without
    /// waiting on real time.
    /// </summary>
    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
