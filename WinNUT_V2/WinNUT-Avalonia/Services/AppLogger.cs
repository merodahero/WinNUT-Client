using System;
using System.IO;

namespace WinNUT_Avalonia.Services;

public static class AppLogger
{
    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinNUT", "Logs");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"winnut-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(path, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Monitoring must continue even if the optional log cannot be written.
        }
    }
}
