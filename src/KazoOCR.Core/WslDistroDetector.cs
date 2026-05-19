using System.Diagnostics;
using System.Text;

namespace KazoOCR.Core;

/// <summary>
/// Detects installed WSL distributions and their capabilities.
/// </summary>
public sealed class WslDistroDetector : IWslDistroDetector
{
    // Docker Desktop creates these internal distros — hide them from the user.
    private static readonly HashSet<string> HiddenDistros = new(StringComparer.OrdinalIgnoreCase)
    {
        "docker-desktop",
        "docker-desktop-data"
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListDistrosAsync(CancellationToken cancellationToken = default)
    {
        // "wsl --list --quiet" emits one distro name per line.
        // On older Windows builds the output is UTF-16LE with embedded NUL bytes — strip them.
        var result = await RunProcessAsync("wsl", "--list --quiet", cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return [];
        }

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim('\0', ' '))
            .Where(l => !string.IsNullOrWhiteSpace(l) && !HiddenDistros.Contains(l))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<string?> GetDefaultDistroAsync(CancellationToken cancellationToken = default)
    {
        // "wsl --list --verbose" marks the default with a leading '*'.
        // Example line: "* Ubuntu  Running  2"
        var result = await RunProcessAsync("wsl", "--list --verbose", cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return null;
        }

        foreach (var raw in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim('\0', ' ');
            if (!line.StartsWith('*'))
            {
                continue;
            }

            var name = line.TrimStart('*').Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (name is not null && !HiddenDistros.Contains(name))
            {
                return name;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListDistrosWithOcrMyPdfAsync(CancellationToken cancellationToken = default)
    {
        var distros = await ListDistrosAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<string>();

        foreach (var distro in distros)
        {
            if (await HasOcrMyPdfAsync(distro, cancellationToken).ConfigureAwait(false))
            {
                result.Add(distro);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> HasOcrMyPdfAsync(string distro, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(distro);

        // Use ArgumentList (individual entries) so the distro name is passed without manual
        // quoting — a raw Arguments string can be mis-parsed by wsl.exe for distro targeting.
        // Rely on exit code only: wsl.exe may emit NUL bytes making output appear empty.
        var result = await RunProcessWithArgumentListAsync(
            "wsl",
            ["-d", distro, "--", "which", "ocrmypdf"],
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);

        return result.ExitCode == 0;
    }

    private static Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken) =>
        // wsl --list commands emit UTF-16LE on Windows
        RunProcessAsync(fileName, arguments, Encoding.Unicode, cancellationToken);

    private static async Task<ProcessResult> RunProcessWithArgumentListAsync(
        string fileName,
        IReadOnlyList<string> argumentList,
        Encoding outputEncoding,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            StandardOutputEncoding = outputEncoding,
            StandardErrorEncoding = outputEncoding,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in argumentList)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        return await RunProcessCoreAsync(process, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        Encoding outputEncoding,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                StandardOutputEncoding = outputEncoding,
                StandardErrorEncoding = outputEncoding,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        return await RunProcessCoreAsync(process, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ProcessResult> RunProcessCoreAsync(Process process, CancellationToken cancellationToken)
    {

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOut.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErr.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            process.WaitForExit();

            return new ProcessResult(
                process.ExitCode,
                stdOut.ToString().TrimEnd(),
                stdErr.ToString().TrimEnd());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited, ignore
            }

            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }
}
