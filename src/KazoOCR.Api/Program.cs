using System.Reflection;
using KazoOCR.Api.Middleware;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Check for OpenAPI generation mode
if (args.Length >= 2 && args[0] == "--generate-openapi")
{
    // Build minimal app just to generate OpenAPI spec
    ConfigureServices(builder);
    var genApp = builder.Build();
    ConfigureApp(genApp);

    // Generate OpenAPI JSON
    var outputPath = args[1];
    await GenerateOpenApiSpec(genApp, outputPath);
    return;
}

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables("KAZO_");

ConfigureServices(builder);

var app = builder.Build();

ConfigureApp(app);

var port = builder.Configuration.GetValue<int?>("API_PORT") ?? 5000;
app.Urls.Add($"http://*:{port}");

app.Run();

void ConfigureServices(WebApplicationBuilder webAppBuilder)
{
    // Add controllers
    webAppBuilder.Services.AddControllers();

    // Register Core services
    webAppBuilder.Services.AddSingleton<IOcrFileService, OcrFileService>();
    webAppBuilder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
    webAppBuilder.Services.AddSingleton<IWatcherService, WatcherService>();

    // Register Auth service
    webAppBuilder.Services.AddSingleton<IAuthService, AuthService>();

    // Register API services (JobStore)
    webAppBuilder.Services.AddSingleton<IJobStore, InMemoryJobStore>();

// Register Core services
builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
builder.Services.AddSingleton<IWatcherService, WatcherService>();

// Register API services
builder.Services.AddSingleton<OcrJobService>();
builder.Services.AddSingleton<IOcrJobService>(sp => sp.GetRequiredService<OcrJobService>());

// Register background services
builder.Services.AddHostedService<OcrJobProcessorService>();
builder.Services.AddHostedService<OcrWorkerBackgroundService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
const string openApiRoutePattern = "/openapi/{documentName}.json";
const string apiReferenceRoutePrefix = "/docs";

app.MapOpenApi(openApiRoutePattern);
app.MapScalarApiReference(apiReferenceRoutePrefix, options =>
{
    // Enable Swagger UI
    webApp.UseSwagger();
    webApp.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "KazoOCR API v1");
        options.RoutePrefix = "swagger";
    });

    // API Key middleware (only enforced if KAZO_API_KEY is set)
    webApp.UseApiKeyAuthentication();

    webApp.UseAuthorization();

    webApp.MapControllers();
}
    options.WithOpenApiRoutePattern(openApiRoutePattern);
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program accessible for WebApplicationFactory in tests
namespace KazoOCR.Api
{
    public partial class Program { }
}
