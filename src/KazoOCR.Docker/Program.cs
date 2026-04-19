using KazoOCR.Core;
using KazoOCR.Docker;

var builder = Host.CreateApplicationBuilder(args);

// Register Core services
builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
builder.Services.AddSingleton<IWatcherService, WatcherService>();

// Register the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
