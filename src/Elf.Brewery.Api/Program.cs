using Asp.Versioning;
using Elf.Brewery.Api.ExceptionHandling;
using Elf.Brewery.Api.Middleware;
using Elf.Brewery.Api.Swagger;
using Elf.Brewery.Application;
using Elf.Brewery.Application.Options;
using Elf.Brewery.Infrastructure;
using Elf.Brewery.Infrastructure.Data;
using Elf.Brewery.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
.Enrich.FromLogContext()
.WriteTo.Console()
.WriteTo.File("logs/BreweryLog-.log", rollingInterval: RollingInterval.Day)
);


builder.Services.AddOptions<OpenBreweryDbOptions>()
    .Bind(builder.Configuration.GetSection("OpenBreweryDb"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
    
builder.Services.AddOptions<CacheOptions>()
    .Bind(builder.Configuration.GetSection("Cache"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
    
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<StaticUserOptions>(builder.Configuration.GetSection("StaticUser"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
})
.AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});


var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerSetup.Configure);
builder.Services.AddHealthChecks();


var app = builder.Build();

await app.Services.InitializeInfrastructureDatabaseAsync();

app.MapHealthChecks("/health");
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Brewery API v1 (Sqlite)");
        o.SwaggerEndpoint("/swagger/v2/swagger.json", "Brewery API v2 (In-Memory)");
    });
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class so WebApplicationFactory<Program> can bootstrap it in tests.
public partial class Program { }
