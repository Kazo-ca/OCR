using System.Reflection;
using KazoOCR.Api.Middleware;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.OpenApi.Models;

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

    // Register background worker
    webAppBuilder.Services.AddHostedService<OcrWorkerBackgroundService>();

    // Add Swagger / OpenAPI
    webAppBuilder.Services.AddEndpointsApiExplorer();
    webAppBuilder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "KazoOCR API",
            Version = "v1",
            Description = "REST API for KazoOCR - PDF OCR processing service that creates searchable PDF 'sandwiches' using OCRmyPDF.",
            Contact = new OpenApiContact
            {
                Name = "KazoOCR",
                Url = new Uri("https://github.com/Kazo-ca/OCR")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        // Add X-Api-Key security definition
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Description = "API key for authentication. Set KAZO_API_KEY environment variable to enable."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Enable annotations
        options.EnableAnnotations();

        // Include XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });
}

void ConfigureApp(WebApplication webApp)
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

async Task GenerateOpenApiSpec(WebApplication webApp, string outputPath)
{
    // Get the OpenAPI document from Swashbuckle
    var swaggerProvider = webApp.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
    var swagger = swaggerProvider.GetSwagger("v1");

    // Serialize using the OpenAPI library's writer
    await using var stream = File.Create(outputPath);
    await using var writer = new StreamWriter(stream);
    var openApiWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(writer);
    swagger.SerializeAsV3(openApiWriter);
    Console.WriteLine($"OpenAPI specification written to: {outputPath}");
}

// Make Program accessible for WebApplicationFactory in tests
namespace KazoOCR.Api
{
    public partial class Program { }
}
