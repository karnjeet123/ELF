using System.Text.Json;
using Elf.Brewery.Api.ExceptionHandling;
using Elf.Brewery.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elf.Brewery.Application.Tests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    private static async Task<(int StatusCode, string Body)> InvokeAsync(Exception exception)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        Assert.True(handled);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task TryHandleAsync_BreweryNotFoundException_Returns404()
    {
        var (statusCode, _) = await InvokeAsync(new BreweryNotFoundException("abc"));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400()
    {
        var (statusCode, _) = await InvokeAsync(new ValidationException("bad input"));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ExternalServiceException_Returns502()
    {
        var (statusCode, _) = await InvokeAsync(new ExternalServiceException("upstream down"));

        Assert.Equal(StatusCodes.Status502BadGateway, statusCode);
    }

    [Fact]
    public async Task TryHandleAsync_OperationCanceledException_Returns499()
    {
        var (statusCode, _) = await InvokeAsync(new OperationCanceledException());

        Assert.Equal(499, statusCode);
    }

    [Fact]
    public async Task TryHandleAsync_UnknownException_Returns500AndHidesDetail()
    {
        var (statusCode, body) = await InvokeAsync(new InvalidOperationException("secret internal detail"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        using var doc = JsonDocument.Parse(body);
        var detail = doc.RootElement.GetProperty("detail").GetString();
        Assert.Equal("An unexpected error occurred.", detail);
        Assert.DoesNotContain("secret internal detail", body);
    }
}
