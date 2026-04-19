using Microsoft.Extensions.DependencyInjection;

namespace KazoOCR.UI;

/// <summary>
/// Main application class for the KazoOCR MAUI application.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(mainPage)
        {
            Title = "KazoOCR",
            Width = 900,
            Height = 700,
            MinimumWidth = 800,
            MinimumHeight = 600
        };
    }
}
