using KazoOCR.Api.Middleware;
using KazoOCR.Api.Services;
using KazoOCR.Core;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables("KAZO_");

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API Key middleware (only enforced if KAZO_API_KEY is set)
app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithName("HealthCheck");

// Configure port from environment variable or default
var port = builder.Configuration.GetValue<int?>("KAZO_API_PORT") ?? 5000;

app.Run($"http://*:{port}");

// Make Program accessible for WebApplicationFactory in tests
namespace KazoOCR.Api
{
    public partial class Program { }
}
