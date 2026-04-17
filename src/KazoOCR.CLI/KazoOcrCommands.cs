using CommandDotNet;
using KazoOCR.Core;

namespace KazoOCR.CLI;

/// <summary>
/// CLI commands for KazoOCR.
/// </summary>
public sealed class KazoOcrCommands
{
    private readonly IEnvironmentDetector _detector;
    private readonly IEnvironmentInstaller _installer;

    /// <summary>
    /// Initializes a new instance of the <see cref="KazoOcrCommands"/> class.
    /// </summary>
    public KazoOcrCommands()
        : this(new EnvironmentDetector(), new EnvironmentInstaller())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KazoOcrCommands"/> class.
    /// </summary>
    /// <param name="detector">The environment detector.</param>
    /// <param name="installer">The environment installer.</param>
    public KazoOcrCommands(IEnvironmentDetector detector, IEnvironmentInstaller installer)
    {
        _detector = detector;
        _installer = installer;
    }

    /// <summary>
    /// Check environment and install missing dependencies.
    /// </summary>
    [Command("install", Description = "Check environment and install missing dependencies")]
    public async Task<int> Install(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("=== KazoOCR Environment Check ===\n");

        var isWindows = _detector.IsWindows();
        var isMacOs = OperatingSystem.IsMacOS();
        var operatingSystemLabel = isWindows ? "Windows" : isMacOs ? "macOS" : "Linux";
        Console.WriteLine($"Operating System: {operatingSystemLabel}");
        Console.WriteLine();

        // Check for unsupported macOS
        if (isMacOs)
        {
            Console.WriteLine("ERROR: Automatic dependency installation is not supported on macOS.");
            Console.WriteLine("This command currently supports Windows (with WSL) and Linux systems using apt-get.");
            Console.WriteLine();
            Console.WriteLine("Please install the required dependencies manually using Homebrew:");
            Console.WriteLine("  brew install ocrmypdf tesseract tesseract-lang unpaper");
            return 1;
        }

        // Check WSL (Windows only)
        if (isWindows)
        {
            Console.Write("Checking WSL availability... ");
            var wslAvailable = await _detector.IsWslAvailableAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine(wslAvailable ? "✓ Available" : "✗ Not available");

            if (!wslAvailable)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR: WSL is not available on this system.");
                Console.WriteLine("Please install WSL first:");
                Console.WriteLine("  1. Open PowerShell as Administrator");
                Console.WriteLine("  2. Run: wsl --install");
                Console.WriteLine("  3. Restart your computer");
                Console.WriteLine("  4. Run this command again");
                return 1;
            }

            Console.WriteLine();
        }

        // Check dependencies
        Console.WriteLine("Checking dependencies:\n");

        Console.Write("  OCRmyPDF... ");
        var ocrMyPdfInstalled = await _detector.IsOcrMyPdfInstalledAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine(ocrMyPdfInstalled ? "✓ Installed" : "✗ Not installed");

        Console.Write("  Tesseract (fra)... ");
        var tesseractFraInstalled = await _detector.IsTesseractLangInstalledAsync("fra", cancellationToken).ConfigureAwait(false);
        Console.WriteLine(tesseractFraInstalled ? "✓ Installed" : "✗ Not installed");

        Console.Write("  Tesseract (eng)... ");
        var tesseractEngInstalled = await _detector.IsTesseractLangInstalledAsync("eng", cancellationToken).ConfigureAwait(false);
        Console.WriteLine(tesseractEngInstalled ? "✓ Installed" : "✗ Not installed");

        Console.Write("  Unpaper... ");
        var unpaperInstalled = await _detector.IsUnpaperInstalledAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine(unpaperInstalled ? "✓ Installed" : "✗ Not installed");

        Console.WriteLine();

        // Check if all dependencies are installed (including eng)
        if (ocrMyPdfInstalled && tesseractFraInstalled && tesseractEngInstalled && unpaperInstalled)
        {
            Console.WriteLine("All dependencies are installed. KazoOCR is ready to use!");
            return 0;
        }

        // Offer to install missing dependencies
        Console.WriteLine("Some dependencies are missing. Installing...\n");
        Console.WriteLine("Running: sudo apt-get update && sudo apt-get install -y ocrmypdf tesseract-ocr-fra tesseract-ocr-eng unpaper\n");

        var result = await _installer.InstallDependenciesAsync(cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            Console.WriteLine("\n✓ Dependencies installed successfully!");
            Console.WriteLine("KazoOCR is now ready to use.");
            return 0;
        }
        else
        {
            Console.WriteLine($"\n✗ Installation failed (exit code: {result.ExitCode})");
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                Console.WriteLine($"Error: {result.StandardError}");
            }
            Console.WriteLine("\nPlease try running the command with elevated privileges:");
            if (isWindows)
            {
                Console.WriteLine("  Run the terminal as Administrator");
            }
            else
            {
                Console.WriteLine("  Use: sudo kazoocr install");
            }
            return 1;
        }
    }
}
