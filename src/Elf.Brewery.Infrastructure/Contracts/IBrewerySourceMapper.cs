using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Infrastructure.External.Models;

namespace Elf.Brewery.Infrastructure.Contracts;

public interface IBrewerySourceMapper
{
    DomainBrewery ToDomain(BrewerySourceDto source);
}