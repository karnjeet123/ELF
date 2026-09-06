public class BreweryNotFoundException(string id)
    : Exception($"Brewery '{id}' was not found.");

public class ExternalServiceException(string message, Exception? inner = null)
    : Exception(message, inner);

public class ValidationException(string message) : Exception(message);