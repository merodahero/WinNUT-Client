using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinNUT_Avalonia.Services;

public sealed class AppearanceSettings
{
    private const string FileName = "appearance.json";

    public int ThemeIndex { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3493;
    public string UpsName { get; set; } = "ups";
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public bool ReconnectOnStartup { get; set; }
    public bool AutoReconnect { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 1;
    public double InputPowerFactor { get; set; } = 0.95;
    public double OutputLoadPowerFactor { get; set; } = 0.95;
    public int NominalOutputPowerWatts { get; set; } = 2400;
    public int NominalOutputPowerVa { get; set; } = 3000;
    public double RatedOutputPowerFactor { get; set; } = 0.80;
    public int InputVoltageMinimum { get; set; } = 180;
    public int InputVoltageMaximum { get; set; } = 260;
    public int NominalInputFrequency { get; set; } = 50;
    public int InputFrequencyMinimum { get; set; } = 45;
    public int InputFrequencyMaximum { get; set; } = 55;
    public int OutputVoltageMinimum { get; set; } = 200;
    public int OutputVoltageMaximum { get; set; } = 250;
    public int BatteryVoltageMinimum { get; set; } = 20;
    public int BatteryVoltageMaximum { get; set; } = 30;
    public bool NotifyOnLowBattery { get; set; } = true;
    public int LowBatteryChargePercent { get; set; } = 20;
    public double HighUpsTemperatureCelsius { get; set; } = 40;
    public bool NotifyOnConnectionChange { get; set; } = true;
    public bool ImmediateShutdown { get; set; }
    public bool RespectForcedShutdown { get; set; } = true;
    public int ShutdownBatteryChargePercent { get; set; } = 20;
    public int ShutdownRuntimeSeconds { get; set; } = 300;
    public int ShutdownType { get; set; }
    public int ShutdownDelaySeconds { get; set; } = 30;
    public bool ExtendShutdownDelay { get; set; }
    public int ExtendedShutdownDelaySeconds { get; set; } = 30;
    public bool ArmAutomaticSystemShutdown { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool MinimizeOnStart { get; set; }
    public bool CloseToTray { get; set; }
    public bool StartWithWindows { get; set; }
    public bool LogToFile { get; set; }
    public int LogLevel { get; set; } = 2;
    public bool CheckForUpdatesAtStartup { get; set; } = true;
    public int UpdateCheckDelayHours { get; set; } = 24;
    public string UpdateBranch { get; set; } = "Stable";
    public DateTime LastUpdateCheckUtc { get; set; }

    public void SetPassword(string? password)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            EncryptedPassword = string.Empty;
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(password);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        EncryptedPassword = Convert.ToBase64String(protectedBytes);
    }

    public string GetPassword()
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(EncryptedPassword))
        {
            return string.Empty;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(EncryptedPassword);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    public static AppearanceSettings Load()
    {
        try
        {
            var path = GetPath();
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppearanceSettings>(File.ReadAllText(path)) ?? new AppearanceSettings()
                : new AppearanceSettings();
        }
        catch
        {
            return new AppearanceSettings();
        }
    }

    public void Save()
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    private static string GetPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WinNUT",
        FileName);
}
