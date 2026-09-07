using Elf.Brewery.Domain.Enums;
namespace Elf.Brewery.Application.Contracts;

public interface IBrewerySorterFactory
{
    IBrewerySorter Resolve(BrewerySortField field);
}
