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
using System.Threading.RateLimiting;

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
builder.Services.AddOptions<StaticUserOptions>()
    .Bind(builder.Configuration.GetSection("StaticUser"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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

const string CorsPolicyName = "DefaultCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});
const string ApiRateLimitPolicyName = "ApiRateLimitPolicy";
var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
var segmentsPerWindow = builder.Configuration.GetValue<int>("RateLimiting:SegmentsPerWindow", 6);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ApiRateLimitPolicyName, context =>
    {
        var clientIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(clientIpAddress, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow = segmentsPerWindow,
            QueueLimit = 0
        });
    });
});
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
app.UseMiddleware<SecurityHeadersMiddleware>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting(ApiRateLimitPolicyName);

app.Run();

// Exposes the implicit Program class so WebApplicationFactory<Program> can bootstrap it in tests.
public partial class Program { }
