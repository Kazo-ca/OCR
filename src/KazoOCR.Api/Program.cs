using KazoOCR.Api.Services;
using KazoOCR.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment variable
const int DefaultApiPort = 5000;
var portValue = Environment.GetEnvironmentVariable("KAZO_API_PORT");
var port = int.TryParse(portValue, out var parsedPort) && parsedPort is >= 1 and <= 65535
    ? parsedPort
    : DefaultApiPort;
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Partial class for WebApplicationFactory integration testing.
/// </summary>
public partial class Program { }
