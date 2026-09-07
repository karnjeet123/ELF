namespace Elf.Brewery.Domain.Exceptions;

public class ExternalServiceException(string message, Exception? inner = null)
    : Exception(message, inner);
