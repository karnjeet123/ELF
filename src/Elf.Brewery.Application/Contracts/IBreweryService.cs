namespace Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
public interface IBreweryService
{
    Task<PagedResult<BreweryDto>> GetBreweriesAsync(BreweryQuery query, CancellationToken ct);
    Task<BreweryDto> GetBreweryByIdAsync(string id, CancellationToken ct);

    Task<IReadOnlyList<AutocompleteItemDto>> GetAutoCompleteAsync(string term, int limit, CancellationToken ct);
    Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken ct);

    Task<int> RefreshBreweriesAsync(CancellationToken ct);
}