using KazoOCR.Api.Services;
using KazoOCR.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment variable
var port = Environment.GetEnvironmentVariable("KAZO_API_PORT") ?? "5000";
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
