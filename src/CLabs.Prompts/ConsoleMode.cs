using System.Runtime.InteropServices;

namespace CLabs.Prompts;

/// <summary>
/// Enables Virtual Terminal Processing on Windows stdout for ANSI escape sequences.
/// No-op on non-Windows platforms.
///
/// Inspired by Spectre.Console's Windows console handling.
/// Spectre.Console is MIT licensed: https://github.com/spectreconsole/spectre.console
/// </summary>
internal static class ConsoleMode
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static IDisposable Enable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return NoOpDisposable.Instance;

        try
        {
            var stdout = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(stdout, out var originalMode))
                return NoOpDisposable.Instance;

            if (!SetConsoleMode(stdout, originalMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING))
                return NoOpDisposable.Instance;

            return new ModeRestorer(stdout, originalMode);
        }
        catch
        {
            return NoOpDisposable.Instance;
        }
    }

    private sealed class ModeRestorer(IntPtr handle, uint originalMode) : IDisposable
    {
        public void Dispose()
        {
            try { SetConsoleMode(handle, originalMode); }
            catch { /* best effort */ }
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }
}
