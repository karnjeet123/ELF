namespace Elf.Brewery.Domain.Exceptions;

public class BreweryNotFoundException(string id)
    : Exception($"Brewery '{id}' was not found.");