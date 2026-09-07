using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Application.Dtos;
namespace Elf.Brewery.Application.Contracts;

public interface IBreweryDtoMapper
{
    BreweryDto ToDto(DomainBrewery brewery, double? distanceKm);
}
