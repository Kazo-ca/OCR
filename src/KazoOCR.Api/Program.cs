using KazoOCR.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
var port = Environment.GetEnvironmentVariable("KAZO_API_PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Core services
builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
builder.Services.AddSingleton<IWatcherService, WatcherService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KazoOCR API v1");
    c.RoutePrefix = "swagger";
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithName("HealthCheck");

// Placeholder endpoints - to be implemented in issue #29
app.MapPost("/api/ocr/process", () => Results.Ok(new { message = "Not yet implemented" }))
   .WithName("ProcessOcr");

app.MapGet("/api/ocr/jobs", () => Results.Ok(new { jobs = Array.Empty<object>() }))
   .WithName("ListJobs");

app.MapGet("/api/ocr/jobs/{id}", (string id) => Results.Ok(new { id, status = "pending" }))
   .WithName("GetJob");

app.MapDelete("/api/ocr/jobs/{id}", (string id) => Results.Ok(new { deleted = id }))
   .WithName("DeleteJob");

app.Run();
