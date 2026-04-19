using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Check if running as Windows Service (--service flag)
if (args.Contains("--service"))
{
    // Remove the --service flag and get config path
    var argsList = args.ToList();
    argsList.Remove("--service");

    string? configPath = null;
    var configIndex = argsList.IndexOf("--config");
    if (configIndex >= 0 && configIndex + 1 < argsList.Count)
    {
        configPath = argsList[configIndex + 1];
        argsList.RemoveAt(configIndex + 1);
        argsList.RemoveAt(configIndex);
    }

    // Build host for Windows Service
    var builder = Host.CreateApplicationBuilder(argsList.ToArray());

    // Load service configuration
    if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
    {
        builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
    }

    // Configure services
    builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
    builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
    builder.Services.AddSingleton<IWatcherService, WatcherService>();
    builder.Services.AddHostedService<MultiWatcherBackgroundService>();

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = ServiceManager.DefaultServiceName;
    });

    var host = builder.Build();
    await host.RunAsync();
    return 0;
}

// Normal CLI mode
var services = new ServiceCollection();

// Register services
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddSingleton<IOcrFileService, OcrFileService>();
services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();
services.AddSingleton<IWatcherService, WatcherService>();
services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
services.AddSingleton<IEnvironmentInstaller, EnvironmentInstaller>();
services.AddSingleton<IServiceManager, ServiceManager>();
services.AddSingleton<IPrivilegeElevator, PrivilegeElevator>();
services.AddTransient<OcrCommand>();
services.AddTransient<WatchCommand>();
services.AddTransient<KazoOcrCommands>();
services.AddTransient<ServiceCommand>();

await using var serviceProvider = services.BuildServiceProvider();

// Configure and run the AppRunner
return await new AppRunner<RootCommand>()
    .UseMicrosoftDependencyInjection(serviceProvider)
    .UseDefaultMiddleware()
    .RunAsync(args);
