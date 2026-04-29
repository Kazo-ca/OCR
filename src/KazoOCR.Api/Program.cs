using System.Reflection;
using KazoOCR.Api.Middleware;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

genApp.MapDefaultEndpoints();

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables("KAZO_");

ConfigureServices(builder);

var app = builder.Build();

ConfigureApp(app);

// Only configure URL binding in non-Development environments (Docker)
// In Development, launchSettings.json takes precedence
if (!app.Environment.IsDevelopment())
{
    var port = builder.Configuration.GetValue<int?>("API_PORT") ?? 5000;
    app.Urls.Add($"http://*:{port}");
}

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

    // Register OCR Job services
    webAppBuilder.Services.AddSingleton<OcrJobService>();
    webAppBuilder.Services.AddSingleton<IOcrJobService>(sp => sp.GetRequiredService<OcrJobService>());

    // Register background services
    webAppBuilder.Services.AddHostedService<OcrJobProcessorService>();
    webAppBuilder.Services.AddHostedService<OcrWorkerBackgroundService>();

    // Add health checks
    webAppBuilder.Services.AddHealthChecks();

    // Add OpenAPI
    webAppBuilder.Services.AddOpenApi();
}

void ConfigureApp(WebApplication webApp)
{
    // Configure the HTTP request pipeline
    const string openApiRoutePattern = "/openapi/{documentName}.json";
    const string apiReferenceRoutePrefix = "/docs";

    webApp.MapOpenApi(openApiRoutePattern);
    webApp.MapScalarApiReference(apiReferenceRoutePrefix, options =>
    {
        options.WithOpenApiRoutePattern(openApiRoutePattern);
    });

    // API Key middleware (only enforced if KAZO_API_KEY is set)
    webApp.UseApiKeyAuthentication();

    webApp.UseAuthorization();

    webApp.MapControllers();
    webApp.MapHealthChecks("/health");
}

async Task GenerateOpenApiSpec(WebApplication webApp, string outputPath)
{
    // Start the app to generate the OpenAPI spec
    await webApp.StartAsync();

    try
    {
        using var httpClient = new HttpClient();
        var openApiJson = await httpClient.GetStringAsync("http://localhost:5000/openapi/v1.json");
        await File.WriteAllTextAsync(outputPath, openApiJson);
        Console.WriteLine($"OpenAPI spec written to {outputPath}");
    }
    finally
    {
        await webApp.StopAsync();
    }
}

// Make Program accessible for WebApplicationFactory in tests
namespace KazoOCR.Api
{
    public partial class Program { }
}
