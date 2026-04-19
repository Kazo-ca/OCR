using KazoOCR.Api.Middleware;
using KazoOCR.Api.Services;
using KazoOCR.Core;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables("KAZO_");

// Configure port from environment variable
var port = Environment.GetEnvironmentVariable("KAZO_API_PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Core services
builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
builder.Services.AddSingleton<IWatcherService, WatcherService>();

// Register Auth service
builder.Services.AddSingleton<IAuthService, AuthService>();

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
if (app.Environment.IsDevelopment())
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

// API Key middleware (only enforced if KAZO_API_KEY is set)
app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

namespace KazoOCR.Api
{
    /// <summary>
    /// Partial class for WebApplicationFactory integration testing.
    /// </summary>
    public partial class Program { }
}
