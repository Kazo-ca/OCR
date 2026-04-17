using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build service provider for dependency injection
var services = new ServiceCollection();

// Register services
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddSingleton<IOcrFileService, OcrFileService>();
services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
services.AddSingleton<IEnvironmentInstaller, EnvironmentInstaller>();
services.AddTransient<OcrCommand>();
services.AddTransient<KazoOcrCommands>();

await using var serviceProvider = services.BuildServiceProvider();

// Configure and run the AppRunner
return await new AppRunner<RootCommand>()
    .UseMicrosoftDependencyInjection(serviceProvider)
    .UseDefaultMiddleware()
    .RunAsync(args);
