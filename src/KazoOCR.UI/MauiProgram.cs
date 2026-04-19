using CommunityToolkit.Maui;
using KazoOCR.Core;
using KazoOCR.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace KazoOCR.UI;

/// <summary>
/// MAUI application builder for KazoOCR.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Core services
        builder.Services.AddSingleton<IOcrFileService, OcrFileService>();
        builder.Services.AddSingleton<IOcrProcessRunner, OcrProcessRunner>();

        // Register ViewModels
        builder.Services.AddTransient<MainPageViewModel>();

        // Register Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
