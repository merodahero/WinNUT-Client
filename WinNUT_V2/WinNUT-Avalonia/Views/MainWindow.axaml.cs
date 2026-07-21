using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Win32;
using WinNUT_Avalonia.Services;

namespace WinNUT_Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly AppearanceSettings _appearanceSettings;
    private readonly NutClient _nutClient = new();
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _reconnectTimer;
    private readonly Queue<string> _recentEvents = new();
    private readonly HashSet<string> _activeAlerts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _alertMessages = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, string> _listedNutVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private bool _isRefreshing;
    private bool _isConnecting;
    private string? _lastStatus;
    private DateTime? _shutdownDueAt;
    private bool _shutdownTriggered;
    private bool _isExiting;

    public MainWindow()
    {
        _appearanceSettings = AppearanceSettings.Load();
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += PollTimer_OnTick;
        _reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _reconnectTimer.Tick += ReconnectTimer_OnTick;
        InitializeComponent();
        Opened += MainWindow_OnOpened;
        Closing += MainWindow_OnClosing;
        PropertyChanged += MainWindow_OnPropertyChanged;
        var selector = this.FindControl<ComboBox>("ThemeSelector");
        if (selector is not null)
        {
            selector.SelectedIndex = _appearanceSettings.ThemeIndex;
            ApplyTheme(_appearanceSettings.ThemeIndex);
        }
        this.FindControl<TextBox>("HostText")!.Text = _appearanceSettings.Host;
        this.FindControl<TextBox>("PortText")!.Text = _appearanceSettings.Port.ToString();
        this.FindControl<TextBox>("UpsNameText")!.Text = _appearanceSettings.UpsName;
        this.FindControl<TextBox>("UsernameText")!.Text = _appearanceSettings.Username;
        this.FindControl<TextBox>("PasswordText")!.Text = _appearanceSettings.GetPassword();
        this.FindControl<CheckBox>("AutoReconnectCheck")!.IsChecked = _appearanceSettings.AutoReconnect;
        this.FindControl<TextBox>("PollIntervalText")!.Text = _appearanceSettings.PollIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("InputPowerFactorText")!.Text = _appearanceSettings.InputPowerFactor.ToString("0.00", CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("OutputLoadPowerFactorText")!.Text = _appearanceSettings.OutputLoadPowerFactor.ToString("0.00", CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("NominalOutputPowerText")!.Text = _appearanceSettings.NominalOutputPowerWatts.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("NominalOutputPowerVaText")!.Text = _appearanceSettings.NominalOutputPowerVa.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("RatedOutputPowerFactorText")!.Text = _appearanceSettings.RatedOutputPowerFactor.ToString("0.00", CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("InputVoltageMinimumText")!.Text = _appearanceSettings.InputVoltageMinimum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("InputVoltageMaximumText")!.Text = _appearanceSettings.InputVoltageMaximum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("NominalInputFrequencyText")!.Text = _appearanceSettings.NominalInputFrequency.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("InputFrequencyMinimumText")!.Text = _appearanceSettings.InputFrequencyMinimum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("InputFrequencyMaximumText")!.Text = _appearanceSettings.InputFrequencyMaximum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("OutputVoltageMinimumText")!.Text = _appearanceSettings.OutputVoltageMinimum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("OutputVoltageMaximumText")!.Text = _appearanceSettings.OutputVoltageMaximum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("BatteryVoltageMinimumText")!.Text = _appearanceSettings.BatteryVoltageMinimum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("BatteryVoltageMaximumText")!.Text = _appearanceSettings.BatteryVoltageMaximum.ToString(CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("NotifyConnectionCheck")!.IsChecked = _appearanceSettings.NotifyOnConnectionChange;
        this.FindControl<CheckBox>("NotifyLowBatteryCheck")!.IsChecked = _appearanceSettings.NotifyOnLowBattery;
        this.FindControl<TextBox>("LowBatteryThresholdText")!.Text = _appearanceSettings.LowBatteryChargePercent.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("HighUpsTemperatureText")!.Text = _appearanceSettings.HighUpsTemperatureCelsius.ToString("0", CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("ImmediateShutdownCheck")!.IsChecked = _appearanceSettings.ImmediateShutdown;
        this.FindControl<CheckBox>("RespectFsdCheck")!.IsChecked = _appearanceSettings.RespectForcedShutdown;
        this.FindControl<TextBox>("ShutdownBatteryThresholdText")!.Text = _appearanceSettings.ShutdownBatteryChargePercent.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("ShutdownRuntimeText")!.Text = _appearanceSettings.ShutdownRuntimeSeconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<ComboBox>("ShutdownTypeSelector")!.SelectedIndex = Math.Clamp(_appearanceSettings.ShutdownType, 0, 2);
        this.FindControl<TextBox>("ShutdownDelayText")!.Text = _appearanceSettings.ShutdownDelaySeconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("ExtendShutdownDelayCheck")!.IsChecked = _appearanceSettings.ExtendShutdownDelay;
        this.FindControl<TextBox>("ExtendedShutdownDelayText")!.Text = _appearanceSettings.ExtendedShutdownDelaySeconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("ArmAutomaticSystemShutdownCheck")!.IsChecked = _appearanceSettings.ArmAutomaticSystemShutdown;
        this.FindControl<CheckBox>("MinimizeToTrayCheck")!.IsChecked = _appearanceSettings.MinimizeToTray;
        this.FindControl<CheckBox>("MinimizeOnStartCheck")!.IsChecked = _appearanceSettings.MinimizeOnStart;
        this.FindControl<CheckBox>("CloseToTrayCheck")!.IsChecked = _appearanceSettings.CloseToTray;
        this.FindControl<CheckBox>("StartWithWindowsCheck")!.IsChecked = _appearanceSettings.StartWithWindows;
        this.FindControl<CheckBox>("LogToFileCheck")!.IsChecked = _appearanceSettings.LogToFile;
        this.FindControl<TextBox>("LogLevelText")!.Text = _appearanceSettings.LogLevel.ToString(CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("CheckUpdatesAtStartupCheck")!.IsChecked = _appearanceSettings.CheckForUpdatesAtStartup;
        this.FindControl<TextBox>("UpdateCheckDelayText")!.Text = _appearanceSettings.UpdateCheckDelayHours.ToString(CultureInfo.InvariantCulture);
        this.FindControl<ComboBox>("UpdateBranchSelector")!.SelectedIndex = _appearanceSettings.UpdateBranch == "Preview" ? 1 : 0;
        ResetDashboard();
    }

    private void ThemeSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedIndex: var index }) return;
        ApplyTheme(index);
        _appearanceSettings.ThemeIndex = index;
        _appearanceSettings.Save();
    }

    private void SettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindControl<Grid>("DashboardContent")!.IsVisible = false;
        this.FindControl<Border>("EventsMainPanel")!.IsVisible = false;
        this.FindControl<Border>("NutMainPanel")!.IsVisible = false;
        this.FindControl<Border>("SettingsFlyout")!.IsVisible = true;
    }

    private void CloseSettingsButton_OnClick(object? sender, RoutedEventArgs e) => SaveAndCloseSettings();

    private void SettingsBackdrop_OnPointerPressed(object? sender, PointerPressedEventArgs e) => SaveAndCloseSettings();

    private void SaveAndCloseSettings()
    {
        SaveAllSettings();
        this.FindControl<Border>("SettingsFlyout")!.IsVisible = false;
        this.FindControl<Grid>("DashboardContent")!.IsVisible = true;
    }

    private void SaveAllSettings()
    {
        _appearanceSettings.Host = this.FindControl<TextBox>("HostText")!.Text?.Trim() ?? string.Empty;
        _appearanceSettings.Port = GetPositiveInt("PortText", 1, 65535, 3493);
        _appearanceSettings.UpsName = this.FindControl<TextBox>("UpsNameText")!.Text?.Trim() ?? string.Empty;
        _appearanceSettings.Username = this.FindControl<TextBox>("UsernameText")!.Text?.Trim() ?? string.Empty;
        _appearanceSettings.SetPassword(this.FindControl<TextBox>("PasswordText")!.Text);
        _appearanceSettings.AutoReconnect = this.FindControl<CheckBox>("AutoReconnectCheck")!.IsChecked == true;
        _appearanceSettings.PollIntervalSeconds = GetPositiveInt("PollIntervalText", 1, 3600, 1);
        _pollTimer.Interval = TimeSpan.FromSeconds(_appearanceSettings.PollIntervalSeconds);
        SaveCalibrationButton_OnClick(null, new RoutedEventArgs());
        SaveNotificationsButton_OnClick(null, new RoutedEventArgs());
        SaveShutdownButton_OnClick(null, new RoutedEventArgs());
        SaveApplicationButton_OnClick(null, new RoutedEventArgs());
        SaveUpdatesButton_OnClick(null, new RoutedEventArgs());
        _appearanceSettings.Save();
    }

    private async void MainWindow_OnOpened(object? sender, EventArgs e)
    {
        if (_appearanceSettings.ReconnectOnStartup)
            await ConnectAsync(startup: true);
        if (_appearanceSettings.CheckForUpdatesAtStartup &&
            DateTime.UtcNow - _appearanceSettings.LastUpdateCheckUtc >= TimeSpan.FromHours(_appearanceSettings.UpdateCheckDelayHours))
            await CheckForUpdatesAsync();
        if (_appearanceSettings.MinimizeOnStart)
        {
            if (_appearanceSettings.MinimizeToTray) HideToTray();
            else WindowState = WindowState.Minimized;
        }
    }

    private void GeneralSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("General");

    private void CalibrationSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Calibration");

    private void NotificationsSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Notifications");

    private void ShutdownSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Shutdown");

    private void ApplicationSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Application");

    private void UpdatesSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Updates");

    private void EventsNavigationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindControl<Grid>("DashboardContent")!.IsVisible = false;
        this.FindControl<Border>("SettingsFlyout")!.IsVisible = false;
        this.FindControl<Border>("NutMainPanel")!.IsVisible = false;
        this.FindControl<Border>("EventsMainPanel")!.IsVisible = true;
        RenderEventLog();
    }

    private async void NutNavigationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.FindControl<Grid>("DashboardContent")!.IsVisible = false;
        this.FindControl<Border>("SettingsFlyout")!.IsVisible = false;
        this.FindControl<Border>("EventsMainPanel")!.IsVisible = false;
        this.FindControl<Border>("NutMainPanel")!.IsVisible = true;
        await LoadNutVariablesAsync();
    }

    private void OverviewButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Border>("SettingsFlyout")!.IsVisible)
        {
            SaveAllSettings();
            this.FindControl<Border>("SettingsFlyout")!.IsVisible = false;
        }
        this.FindControl<Border>("EventsMainPanel")!.IsVisible = false;
        this.FindControl<Border>("NutMainPanel")!.IsVisible = false;
        this.FindControl<Grid>("DashboardContent")!.IsVisible = true;
    }

    private void AppearanceSettingsButton_OnClick(object? sender, RoutedEventArgs e) => ShowSettingsPage("Appearance");

    private void ShowSettingsPage(string page)
    {
        this.FindControl<StackPanel>("GeneralSettingsPanel")!.IsVisible = page == "General";
        this.FindControl<StackPanel>("CalibrationSettingsPanel")!.IsVisible = page == "Calibration";
        this.FindControl<StackPanel>("NotificationsSettingsPanel")!.IsVisible = page == "Notifications";
        this.FindControl<StackPanel>("ShutdownSettingsPanel")!.IsVisible = page == "Shutdown";
        this.FindControl<StackPanel>("ApplicationSettingsPanel")!.IsVisible = page == "Application";
        this.FindControl<StackPanel>("UpdatesSettingsPanel")!.IsVisible = page == "Updates";
        this.FindControl<StackPanel>("AppearanceSettingsPanel")!.IsVisible = page == "Appearance";
    }

    private async void ConnectButton_OnClick(object? sender, RoutedEventArgs e)
        => await ConnectAsync();

    private async System.Threading.Tasks.Task ConnectAsync(bool startup = false)
    {
        if (_isConnecting) return;
        _isConnecting = true;
        var state = this.FindControl<TextBlock>("ConnectionState")!;
        var host = this.FindControl<TextBox>("HostText")!.Text?.Trim() ?? string.Empty;
        var upsName = this.FindControl<TextBox>("UpsNameText")!.Text?.Trim() ?? string.Empty;
        var username = this.FindControl<TextBox>("UsernameText")!.Text?.Trim();
        var password = this.FindControl<TextBox>("PasswordText")!.Text;
        if (!int.TryParse(this.FindControl<TextBox>("PortText")!.Text, out var port) || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(upsName))
        {
            state.Text = "Host, port, and UPS name are required.";
            _isConnecting = false;
            return;
        }

        try
        {
            state.Text = "Connecting…";
            await _nutClient.ConnectAsync(host, port);
            await _nutClient.LoginAsync(upsName, username, password);
            _reconnectTimer.Stop();
            _appearanceSettings.Host = host;
            _appearanceSettings.Port = port;
            _appearanceSettings.UpsName = upsName;
            _appearanceSettings.Username = username ?? string.Empty;
            _appearanceSettings.SetPassword(password);
            _appearanceSettings.AutoReconnect = this.FindControl<CheckBox>("AutoReconnectCheck")!.IsChecked == true;
            _appearanceSettings.PollIntervalSeconds = GetPositiveInt("PollIntervalText", 1, 3600, 1);
            _pollTimer.Interval = TimeSpan.FromSeconds(_appearanceSettings.PollIntervalSeconds);
            _appearanceSettings.ReconnectOnStartup = true;
            _appearanceSettings.Save();
            state.Text = $"Connected to {host}:{port}.";
            SetAlert("connection", false, string.Empty);
            await RefreshDashboardAsync();
            AddEvent($"Connected to {host}:{port}");
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            await _nutClient.DisconnectAsync();
            ResetDashboard();
            SetAlert("connection", true, "Connection to the NUT server failed");
            state.Text = startup ? $"Reconnect failed: {ex.Message}" : $"Connection failed: {ex.Message}";
            if (_appearanceSettings.AutoReconnect) _reconnectTimer.Start();
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async void ReconnectTimer_OnTick(object? sender, EventArgs e)
    {
        if (_nutClient.IsConnected)
        {
            _reconnectTimer.Stop();
            return;
        }

        await ConnectAsync(startup: true);
    }

    private async void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var state = this.FindControl<TextBlock>("ConnectionState")!;
        if (!_nutClient.IsConnected)
        {
            state.Text = "Connect before refreshing.";
            return;
        }

        try
        {
            await RefreshDashboardAsync();
            state.Text = "Dashboard refreshed.";
        }
        catch (Exception ex)
        {
            state.Text = $"Refresh failed: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task RefreshDashboardAsync()
    {
        var values = await _nutClient.GetVariablesAsync(_appearanceSettings.UpsName, new[]
        {
            "battery.charge", "battery.charge.low", "battery.runtime", "battery.runtime.low", "ups.load", "ups.status", "ups.realpower",
            "output.realpower", "output.voltage", "output.frequency", "output.current",
            "ups.realpower.nominal", "output.realpower.nominal", "ups.power.nominal", "output.power.nominal",
            "input.current.nominal", "input.voltage.nominal", "input.voltage", "input.frequency", "input.power", "output.power",
            "battery.voltage", "battery.voltage.nominal", "battery.temperature", "ups.temperature", "battery.date",
            "device.mfr", "device.model", "device.serial", "ups.mfr", "ups.model", "ups.serial",
            "input.powerfactor", "output.powerfactor", "input.realpower"
        });

        values.TryGetValue("battery.charge", out var batteryCharge);
        values.TryGetValue("battery.charge.low", out var batteryChargeLow);
        values.TryGetValue("battery.runtime", out var runtime);
        values.TryGetValue("battery.runtime.low", out var runtimeLow);
        values.TryGetValue("ups.load", out var load);
        values.TryGetValue("ups.status", out var status);
        values.TryGetValue("output.voltage", out var outputVoltage);
        values.TryGetValue("output.frequency", out var outputFrequency);
        values.TryGetValue("input.voltage", out var inputVoltage);
        values.TryGetValue("input.frequency", out var inputFrequency);
        values.TryGetValue("output.current", out var outputCurrent);
        values.TryGetValue("battery.voltage", out var batteryVoltage);
        values.TryGetValue("battery.voltage.nominal", out var nominalBatteryVoltage);
        values.TryGetValue("battery.temperature", out var batteryTemperature);
        values.TryGetValue("ups.temperature", out var upsTemperature);
        var inputPowerFactor = ResolvePowerFactor(values, "input.powerfactor", "input.realpower", "input.power");
        var outputPowerFactor = ResolvePowerFactor(values, "output.powerfactor", "output.realpower", "output.power");
        var displayedOutputPowerFactor = outputPowerFactor ??
            (_appearanceSettings.OutputLoadPowerFactor, "Configured fallback");

        var online = status?.Contains("OL", StringComparison.OrdinalIgnoreCase) == true;
        var onBattery = status?.Contains("OB", StringComparison.OrdinalIgnoreCase) == true;
        var power = CalculateOutputPower(values);
        var powerText = power is null ? "—" : $"{power.Value.Watts.ToString("0", CultureInfo.InvariantCulture)} W";

        this.FindControl<TextBlock>("StatusText")!.Text = FormatStatus(status);
        this.FindControl<TextBlock>("BatteryText")!.Text = batteryCharge is null ? "—" : $"{batteryCharge}%";
        this.FindControl<TextBlock>("BatteryStateText")!.Text = online ? "Line power" : "On battery";
        this.FindControl<TextBlock>("BatteryDetailText")!.Text = FormatBatteryDetail(batteryVoltage, nominalBatteryVoltage, batteryTemperature);
        this.FindControl<TextBlock>("LoadText")!.Text = load is null ? "—" : $"{load}%";
        this.FindControl<TextBlock>("LoadDetailText")!.Text = power is null ? "Power unavailable" : $"{powerText} · {power.Value.Source}";
        this.FindControl<TextBlock>("OutputPowerText")!.Text = powerText;
        this.FindControl<TextBlock>("OutputDetailText")!.Text = $"{outputVoltage ?? "—"} V   {outputFrequency ?? "—"} Hz";
        this.FindControl<TextBlock>("PowerCalculationText")!.Text = power?.Detail ?? "Power unavailable";
        this.FindControl<TextBlock>("RuntimeText")!.Text = FormatRuntime(runtime);
        this.FindControl<TextBlock>("FooterConnectionText")!.Text = $"Connected to {_appearanceSettings.Host}:{_appearanceSettings.Port}";
        var statusColor = onBattery ? Color.Parse("#FFB454") : online ? Color.Parse("#6EE7A0") : Color.Parse("#FF6B6B");
        this.FindControl<TextBlock>("HeaderConnectionText")!.Text = online ? "●  ONLINE" : $"●  {FormatStatus(status).ToUpperInvariant()}";
        this.FindControl<TextBlock>("HeaderConnectionText")!.Foreground = new SolidColorBrush(statusColor);
        this.FindControl<TextBlock>("StatusText")!.Foreground = new SolidColorBrush(statusColor);
        this.FindControl<TextBlock>("FlowStatusText")!.Text = online ? "ONLINE" : FormatStatus(status).ToUpperInvariant();
        this.FindControl<TextBlock>("FlowStatusText")!.Foreground = new SolidColorBrush(statusColor);
        this.FindControl<TextBlock>("InputVoltageText")!.Text = inputVoltage is null ? "—" : $"{inputVoltage} V";
        this.FindControl<TextBlock>("InputFrequencyText")!.Text = inputFrequency is null ? "—" : $"{inputFrequency} Hz";
        this.FindControl<TextBlock>("FlowOutputVoltageText")!.Text = outputVoltage is null ? "—" : $"{outputVoltage} V";
        this.FindControl<TextBlock>("OutputCurrentText")!.Text = outputCurrent is null ? "—" : $"{outputCurrent} A";
        this.FindControl<TextBlock>("FlowLoadText")!.Text = load is null ? "—" : $"{load}% · {powerText}";
        this.FindControl<TextBlock>("UpsNameDisplayText")!.Text = _appearanceSettings.UpsName.ToUpperInvariant();
        this.FindControl<TextBlock>("UpsIdentityText")!.Text = FormatUpsIdentity(values);
        this.FindControl<TextBlock>("UpsTemperatureText")!.Text = FormatTemperature("UPS", upsTemperature);
        this.FindControl<TextBlock>("BatteryTemperatureText")!.Text = FormatTemperature("Battery", batteryTemperature);
        this.FindControl<TextBlock>("InputPowerFactorDisplayText")!.Text = FormatPowerFactor("Input", inputPowerFactor);
        this.FindControl<TextBlock>("OutputPowerFactorDisplayText")!.Text = FormatPowerFactor("Output", displayedOutputPowerFactor);
        UpdateNutCalibrationReadings(values);

        RecordStatusChange(status);
        EvaluateMonitoring(status, batteryCharge, batteryChargeLow, runtime, runtimeLow, upsTemperature);
    }

    private async void PollTimer_OnTick(object? sender, EventArgs e)
    {
        if (_isRefreshing || !_nutClient.IsConnected) return;
        _isRefreshing = true;
        try
        {
            await RefreshDashboardAsync();
            if (this.FindControl<Border>("NutMainPanel")!.IsVisible)
                await LoadNutVariablesAsync();
        }
        catch
        {
            _pollTimer.Stop();
            ResetDashboard("Connection lost");
            SetAlert("connection", true, "Connection to the NUT server was lost");
            if (_appearanceSettings.AutoReconnect)
                _reconnectTimer.Start();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private static string FormatRuntime(string? value) =>
        double.TryParse(value, out var seconds) && seconds >= 0
            ? TimeSpan.FromSeconds(seconds).ToString(@"h\:mm")
            : "—";

    private static string FormatBatteryDetail(string? voltage, string? nominalVoltage, string? temperature)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(voltage))
            details.Add(nominalVoltage is null ? $"{voltage} V" : $"{voltage} / {nominalVoltage} V");
        if (!string.IsNullOrWhiteSpace(temperature)) details.Add($"{temperature} °C");
        return details.Count == 0 ? "—" : string.Join(" · ", details);
    }

    private static string FormatTemperature(string label, string? temperature) =>
        string.IsNullOrWhiteSpace(temperature) ? $"{label}: —" : $"{label}: {temperature} °C · NUT reported";

    private static string FormatPowerFactor(string label, (double Value, string Source)? powerFactor) =>
        powerFactor is null ? $"{label}: —" : $"{label}: {powerFactor.Value.Value:0.00} · {powerFactor.Value.Source}";

    private static (double Value, string Source)? ResolvePowerFactor(
        IReadOnlyDictionary<string, string> values, string directVariable, string realPowerVariable, string apparentPowerVariable)
    {
        var result = PowerCalculations.ResolvePowerFactor(values, directVariable, realPowerVariable, apparentPowerVariable);
        return result is null ? null : (result.Value.Value, result.Value.Source);
    }

    private static string FormatUpsIdentity(IReadOnlyDictionary<string, string> values)
    {
        var manufacturer = FirstValue(values, "device.mfr", "ups.mfr");
        var model = FirstValue(values, "device.model", "ups.model");
        var serial = FirstValue(values, "device.serial", "ups.serial");
        var identity = string.Join(" ", new[] { manufacturer, model }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(serial)
            ? (string.IsNullOrWhiteSpace(identity) ? "UPS identity not reported" : identity)
            : $"{identity}\nS/N {serial}";
    }

    private static string? FirstValue(IReadOnlyDictionary<string, string> values, params string[] variables) =>
        variables.Select(variable => values.TryGetValue(variable, out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private void UpdateNutCalibrationReadings(IReadOnlyDictionary<string, string> values)
    {
        this.FindControl<TextBox>("NutInputVoltageText")!.Text = FormatReportedValue(FirstValue(values, "input.voltage"), "V");
        this.FindControl<TextBox>("NutOutputVoltageText")!.Text = FormatReportedValue(FirstValue(values, "output.voltage"), "V");
        this.FindControl<TextBox>("NutInputFrequencyText")!.Text = FormatReportedValue(FirstValue(values, "input.frequency"), "Hz");
        this.FindControl<TextBox>("NutOutputFrequencyText")!.Text = FormatReportedValue(FirstValue(values, "output.frequency"), "Hz");
        this.FindControl<TextBox>("NutBatteryVoltageText")!.Text = FormatReportedValue(FirstValue(values, "battery.voltage"), "V");

        var inputPowerFactor = ResolvePowerFactor(values, "input.powerfactor", "input.realpower", "input.power");
        var outputPowerFactor = ResolvePowerFactor(values, "output.powerfactor", "output.realpower", "output.power");
        this.FindControl<TextBox>("NutInputPowerFactorText")!.Text = inputPowerFactor is null ? "Not reported" : $"{inputPowerFactor.Value.Value:0.00} ({inputPowerFactor.Value.Source})";
        this.FindControl<TextBox>("NutOutputPowerFactorText")!.Text = outputPowerFactor is null ? "Not reported" : $"{outputPowerFactor.Value.Value:0.00} ({outputPowerFactor.Value.Source})";
    }

    private static string FormatReportedValue(string? value, string? unit) =>
        string.IsNullOrWhiteSpace(value) ? "Not reported" : unit is null ? value : $"{value} {unit}";

    private void RecordStatusChange(string? status)
    {
        if (!string.Equals(status, _lastStatus, StringComparison.Ordinal))
        {
            AddEvent($"Status: {FormatStatus(status)}");
            _lastStatus = status;
        }
    }

    private void EvaluateMonitoring(string? status, string? batteryCharge, string? batteryChargeLow,
        string? runtime, string? runtimeLow, string? upsTemperature)
    {
        var onBattery = status?.Contains("OB", StringComparison.OrdinalIgnoreCase) == true;
        var overload = status?.Contains("OVER", StringComparison.OrdinalIgnoreCase) == true;
        var lowBattery = status?.Contains("LB", StringComparison.OrdinalIgnoreCase) == true;
        var charge = ParseNumber(batteryCharge);
        var reportedChargeFloor = ParseNumber(batteryChargeLow);
        var runtimeSeconds = ParseNumber(runtime);
        var reportedRuntimeFloor = ParseNumber(runtimeLow);
        var temperature = ParseNumber(upsTemperature);
        var warningChargeFloor = reportedChargeFloor > 0 ? reportedChargeFloor : _appearanceSettings.LowBatteryChargePercent;

        SetAlert("overload", overload, "UPS overload reported");
        SetAlert("on-battery", onBattery, "Utility power lost — UPS is on battery");
        SetAlert("low-battery", _appearanceSettings.NotifyOnLowBattery &&
            (lowBattery || (charge >= 0 && charge <= warningChargeFloor)),
            $"Low battery: {batteryCharge ?? "—"}% remaining");
        SetAlert("high-temperature", temperature > _appearanceSettings.HighUpsTemperatureCelsius,
            $"UPS temperature high: {upsTemperature} °C");

        var shutdownChargeReached = charge >= 0 && charge <= _appearanceSettings.ShutdownBatteryChargePercent;
        var runtimeFloor = reportedRuntimeFloor > 0 ? reportedRuntimeFloor : _appearanceSettings.ShutdownRuntimeSeconds;
        var shutdownRuntimeReached = runtimeSeconds >= 0 && runtimeSeconds <= runtimeFloor;
        var shutdownNeeded = onBattery && (shutdownChargeReached || shutdownRuntimeReached);

        if (!shutdownNeeded)
        {
            if (_shutdownDueAt is not null) AddEvent("Shutdown countdown cancelled — UPS condition recovered");
            _shutdownDueAt = null;
            _shutdownTriggered = false;
            this.FindControl<TextBlock>("ShutdownStatusText")!.Text = "Not scheduled";
            return;
        }

        if (_shutdownDueAt is null)
        {
            var delay = _appearanceSettings.ImmediateShutdown
                ? 0
                : _appearanceSettings.ExtendShutdownDelay
                    ? _appearanceSettings.ExtendedShutdownDelaySeconds
                    : _appearanceSettings.ShutdownDelaySeconds;
            _shutdownDueAt = DateTime.Now.AddSeconds(delay);
            AddEvent($"Low-battery shutdown countdown started ({delay} seconds)");
        }

        var remaining = _shutdownDueAt.Value - DateTime.Now;
        if (remaining > TimeSpan.Zero)
        {
            this.FindControl<TextBlock>("ShutdownStatusText")!.Text =
                $"Scheduled in {Math.Ceiling(remaining.TotalSeconds):0}s";
            return;
        }

        if (_shutdownTriggered) return;
        _shutdownTriggered = true;
        if (!_appearanceSettings.ArmAutomaticSystemShutdown)
        {
            this.FindControl<TextBlock>("ShutdownStatusText")!.Text = "Due now — automatic shutdown is not armed";
            AddEvent("Shutdown due, but automatic Windows shutdown is not armed");
            return;
        }

        if (_appearanceSettings.ShutdownType == 2)
        {
            this.FindControl<TextBlock>("ShutdownStatusText")!.Text = "Due now — UPS-only shutdown is not implemented";
            AddEvent("UPS-only shutdown requested, but no UPS command was sent");
            return;
        }

        this.FindControl<TextBlock>("ShutdownStatusText")!.Text = "Windows shutdown requested";
        AddEvent("Automatic Windows shutdown requested for low battery");
        Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0 /d p:0:0 /c \"WinNUT low-battery shutdown\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private void SetAlert(string key, bool isActive, string message)
    {
        if (isActive)
        {
            if (_activeAlerts.Add(key)) AddEvent($"Alert: {message}");
            _alertMessages[key] = message;
        }
        else
        {
            _activeAlerts.Remove(key);
            _alertMessages.Remove(key);
        }

        var banner = this.FindControl<Border>("AlertBanner")!;
        banner.IsVisible = _activeAlerts.Count > 0;
        this.FindControl<TextBlock>("AlertBannerText")!.Text = _alertMessages.Values.FirstOrDefault() ?? string.Empty;
    }

    private static double ParseNumber(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : -1;

    private void AddEvent(string message)
    {
        _recentEvents.Enqueue($"{DateTime.Now:t}  {message}");
        while (_recentEvents.Count > 200) _recentEvents.Dequeue();
        this.FindControl<TextBlock>("RecentEventsText")!.Text = string.Join(Environment.NewLine, _recentEvents.TakeLast(4).Reverse());
        if (_appearanceSettings.LogToFile) AppLogger.Write(message);
        RenderEventLog();
    }

    private void RenderEventLog()
    {
        var control = this.FindControl<TextBox>("EventLogText");
        if (control is null) return;
        var filter = this.FindControl<TextBox>("EventFilterText")?.Text?.Trim();
        var events = _recentEvents.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter))
            events = events.Where(entry => entry.Contains(filter, StringComparison.OrdinalIgnoreCase));
        control.Text = string.Join(Environment.NewLine, events.Reverse());
    }

    private async void RefreshNutVariablesButton_OnClick(object? sender, RoutedEventArgs e) => await LoadNutVariablesAsync();

    private async System.Threading.Tasks.Task LoadNutVariablesAsync()
    {
        var status = this.FindControl<TextBlock>("NutVariablesStatusText")!;
        if (!_nutClient.IsConnected)
        {
            _listedNutVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            RenderNutVariables();
            status.Text = "Connect to load NUT variables.";
            return;
        }

        try
        {
            status.Text = "Loading variables…";
            _listedNutVariables = await _nutClient.ListVariablesAsync(_appearanceSettings.UpsName);
            RenderNutVariables();
            status.Text = $"Loaded {_listedNutVariables.Count} variables from {_appearanceSettings.UpsName}.";
        }
        catch (Exception ex)
        {
            status.Text = $"Could not load variables: {ex.Message}";
        }
    }

    private void NutFilterText_OnTextChanged(object? sender, TextChangedEventArgs e) => RenderNutVariables();

    private void RenderNutVariables()
    {
        var control = this.FindControl<TextBox>("NutVariablesText");
        if (control is null) return;

        var expected = new[]
        {
            "battery.charge", "battery.runtime", "battery.temperature", "battery.voltage", "battery.voltage.nominal",
            "input.frequency", "input.voltage", "input.powerfactor", "input.realpower", "input.power",
            "output.current", "output.frequency", "output.voltage", "output.powerfactor", "output.realpower", "output.power",
            "ups.load", "ups.status", "ups.temperature", "ups.realpower", "ups.realpower.nominal", "ups.power.nominal"
        };
        var filter = this.FindControl<TextBox>("NutFilterText")?.Text?.Trim();
        IEnumerable<string> names = _listedNutVariables.Keys.Concat(expected).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name);
        if (!string.IsNullOrWhiteSpace(filter))
            names = names.Where(name => name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        var lines = names.Select(name =>
        {
            var value = _listedNutVariables.TryGetValue(name, out var reported) ? reported : "Not reported";
            var unit = GetNutUnit(name);
            return $"{name,-34} {value,-20} {unit}".TrimEnd();
        });
        control.Text = string.Join(Environment.NewLine, lines);
    }

    private static string GetNutUnit(string variable)
    {
        if (variable.EndsWith(".realpower", StringComparison.Ordinal) || variable.EndsWith(".realpower.nominal", StringComparison.Ordinal)) return "W";
        if (variable.EndsWith(".power", StringComparison.Ordinal) || variable.EndsWith(".power.nominal", StringComparison.Ordinal)) return "VA";
        if (variable.EndsWith(".voltage", StringComparison.Ordinal) || variable.Contains(".voltage.", StringComparison.Ordinal)) return "V";
        if (variable.EndsWith(".current", StringComparison.Ordinal)) return "A";
        if (variable.EndsWith(".frequency", StringComparison.Ordinal) || variable.EndsWith(".frequency.nominal", StringComparison.Ordinal)) return "Hz";
        if (variable.Contains("temperature", StringComparison.Ordinal)) return "°C";
        if (variable.EndsWith(".runtime", StringComparison.Ordinal) || variable.Contains(".delay", StringComparison.Ordinal) || variable.Contains(".timer", StringComparison.Ordinal)) return "s";
        if (variable.EndsWith(".charge", StringComparison.Ordinal) || variable.EndsWith(".load", StringComparison.Ordinal)) return "%";
        if (variable.EndsWith("powerfactor", StringComparison.Ordinal)) return "PF";
        return string.Empty;
    }

    private void EventFilterText_OnTextChanged(object? sender, TextChangedEventArgs e) => RenderEventLog();

    private void ExportEventsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppLogger.LogDirectory);
            var path = Path.Combine(AppLogger.LogDirectory, $"events-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllLines(path, _recentEvents.Reverse());
            this.FindControl<TextBlock>("LogStatusText")!.Text = $"Exported to {path}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBlock>("LogStatusText")!.Text = $"Export failed: {ex.Message}";
        }
    }

    private void OpenLogFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppLogger.LogDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", AppLogger.LogDirectory) { UseShellExecute = true });
    }

    private async void CheckForUpdatesButton_OnClick(object? sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        var status = this.FindControl<TextBlock>("UpdateStatusText");
        if (status is not null) status.Text = "Checking for updates…";
        try
        {
            var result = await UpdateService.CheckAsync(_appearanceSettings.UpdateBranch);
            _appearanceSettings.LastUpdateCheckUtc = DateTime.UtcNow;
            _appearanceSettings.Save();
            if (status is not null) status.Text = result;
            AddEvent(result);
        }
        catch (Exception ex)
        {
            if (status is not null) status.Text = $"Update check failed: {ex.Message}";
            AddEvent("Update check failed");
        }
    }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = Avalonia.Controls.WindowState.Normal;
        Activate();
    }

    public void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void MainWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isExiting && _appearanceSettings.CloseToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _appearanceSettings.ReconnectOnStartup = _nutClient.IsConnected;
        _appearanceSettings.Save();
    }

    private void MainWindow_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty && WindowState == Avalonia.Controls.WindowState.Minimized && _appearanceSettings.MinimizeToTray)
            HideToTray();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void SaveCalibrationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings.InputPowerFactor = GetValidDouble("InputPowerFactorText", 0.2, 1, 0.95);
        _appearanceSettings.OutputLoadPowerFactor = GetValidDouble("OutputLoadPowerFactorText", 0.2, 1, 0.95);
        _appearanceSettings.NominalOutputPowerWatts = GetNonNegativeInt("NominalOutputPowerText", 100000, 2400);
        _appearanceSettings.NominalOutputPowerVa = GetNonNegativeInt("NominalOutputPowerVaText", 100000, 3000);
        _appearanceSettings.RatedOutputPowerFactor = GetValidDouble("RatedOutputPowerFactorText", 0.1, 1, 0.80);
        _appearanceSettings.InputVoltageMinimum = GetPositiveInt("InputVoltageMinimumText", 1, 1000, 180);
        _appearanceSettings.InputVoltageMaximum = GetPositiveInt("InputVoltageMaximumText", 1, 1000, 260);
        _appearanceSettings.NominalInputFrequency = GetPositiveInt("NominalInputFrequencyText", 1, 1000, 50);
        _appearanceSettings.InputFrequencyMinimum = GetPositiveInt("InputFrequencyMinimumText", 1, 1000, 45);
        _appearanceSettings.InputFrequencyMaximum = GetPositiveInt("InputFrequencyMaximumText", 1, 1000, 55);
        _appearanceSettings.OutputVoltageMinimum = GetPositiveInt("OutputVoltageMinimumText", 1, 1000, 200);
        _appearanceSettings.OutputVoltageMaximum = GetPositiveInt("OutputVoltageMaximumText", 1, 1000, 250);
        _appearanceSettings.BatteryVoltageMinimum = GetPositiveInt("BatteryVoltageMinimumText", 1, 1000, 20);
        _appearanceSettings.BatteryVoltageMaximum = GetPositiveInt("BatteryVoltageMaximumText", 1, 1000, 30);
        _appearanceSettings.Save();
    }

    private void SaveNotificationsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings.NotifyOnConnectionChange = this.FindControl<CheckBox>("NotifyConnectionCheck")!.IsChecked == true;
        _appearanceSettings.NotifyOnLowBattery = this.FindControl<CheckBox>("NotifyLowBatteryCheck")!.IsChecked == true;
        _appearanceSettings.LowBatteryChargePercent = GetPositiveInt("LowBatteryThresholdText", 1, 100, 20);
        _appearanceSettings.HighUpsTemperatureCelsius = GetValidDouble("HighUpsTemperatureText", 1, 100, 40);
        _appearanceSettings.Save();
    }

    private void SaveShutdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings.ImmediateShutdown = this.FindControl<CheckBox>("ImmediateShutdownCheck")!.IsChecked == true;
        _appearanceSettings.RespectForcedShutdown = this.FindControl<CheckBox>("RespectFsdCheck")!.IsChecked == true;
        _appearanceSettings.ShutdownBatteryChargePercent = GetPositiveInt("ShutdownBatteryThresholdText", 1, 100, 20);
        _appearanceSettings.ShutdownRuntimeSeconds = GetPositiveInt("ShutdownRuntimeText", 1, 86400, 300);
        _appearanceSettings.ShutdownType = Math.Clamp(this.FindControl<ComboBox>("ShutdownTypeSelector")!.SelectedIndex, 0, 2);
        _appearanceSettings.ShutdownDelaySeconds = GetPositiveInt("ShutdownDelayText", 0, 86400, 30);
        _appearanceSettings.ExtendShutdownDelay = this.FindControl<CheckBox>("ExtendShutdownDelayCheck")!.IsChecked == true;
        _appearanceSettings.ExtendedShutdownDelaySeconds = GetPositiveInt("ExtendedShutdownDelayText", 0, 86400, 30);
        _appearanceSettings.ArmAutomaticSystemShutdown = this.FindControl<CheckBox>("ArmAutomaticSystemShutdownCheck")!.IsChecked == true;
        _appearanceSettings.Save();
    }

    private void SaveApplicationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings.MinimizeToTray = this.FindControl<CheckBox>("MinimizeToTrayCheck")!.IsChecked == true;
        _appearanceSettings.MinimizeOnStart = this.FindControl<CheckBox>("MinimizeOnStartCheck")!.IsChecked == true;
        _appearanceSettings.CloseToTray = this.FindControl<CheckBox>("CloseToTrayCheck")!.IsChecked == true;
        _appearanceSettings.StartWithWindows = this.FindControl<CheckBox>("StartWithWindowsCheck")!.IsChecked == true;
        _appearanceSettings.LogToFile = this.FindControl<CheckBox>("LogToFileCheck")!.IsChecked == true;
        _appearanceSettings.LogLevel = GetPositiveInt("LogLevelText", 0, 5, 2);
        ApplyStartupRegistration();
        _appearanceSettings.Save();
    }

    private void ApplyStartupRegistration()
    {
        if (!OperatingSystem.IsWindows()) return;
        const string keyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string valueName = "WinNUT Monitor";
        using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
        if (_appearanceSettings.StartWithWindows)
        {
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable)) key.SetValue(valueName, $"\"{executable}\"");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    private void SaveUpdatesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings.CheckForUpdatesAtStartup = this.FindControl<CheckBox>("CheckUpdatesAtStartupCheck")!.IsChecked == true;
        _appearanceSettings.UpdateCheckDelayHours = GetPositiveInt("UpdateCheckDelayText", 1, 720, 24);
        _appearanceSettings.UpdateBranch = this.FindControl<ComboBox>("UpdateBranchSelector")!.SelectedIndex == 1 ? "Preview" : "Stable";
        _appearanceSettings.Save();
    }

    private (double Watts, string Source, string Detail)? CalculateOutputPower(IReadOnlyDictionary<string, string> values)
    {
        var settings = new PowerCalculationSettings(
            _appearanceSettings.OutputLoadPowerFactor,
            _appearanceSettings.NominalOutputPowerWatts,
            _appearanceSettings.NominalOutputPowerVa,
            _appearanceSettings.RatedOutputPowerFactor);
        var result = PowerCalculations.CalculateOutputPower(values, settings);
        return result is null ? null : (result.Value.Watts, result.Value.Source, result.Value.Detail);
    }

    private static string FormatStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Connected";

        var labels = status.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.ToUpperInvariant() switch
            {
                "OL" => "Online",
                "OB" => "On battery",
                "LB" => "Low battery",
                "CHRG" => "Charging",
                "DISCHRG" => "Discharging",
                "OVER" => "Overload",
                "RB" => "Replace battery",
                "FSD" => "Shutdown imminent",
                "BYPASS" => "Bypass mode",
                "OFF" => "Output off",
                _ => code
            });

        return string.Join(" · ", labels);
    }

    private int GetPositiveInt(string controlName, int minimum, int maximum, int fallback) =>
        int.TryParse(this.FindControl<TextBox>(controlName)!.Text, out var value) && value >= minimum && value <= maximum
            ? value : fallback;

    private int GetNonNegativeInt(string controlName, int maximum, int fallback) =>
        int.TryParse(this.FindControl<TextBox>(controlName)!.Text, out var value) && value >= 0 && value <= maximum
            ? value : fallback;

    private double GetValidDouble(string controlName, double minimum, double maximum, double fallback) =>
        double.TryParse(this.FindControl<TextBox>(controlName)!.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= minimum && value <= maximum
            ? value : fallback;

    private void ResetDashboard(string connectionText = "Not connected")
    {
        if (_activeAlerts.Count == 0)
            this.FindControl<Border>("AlertBanner")!.IsVisible = false;
        this.FindControl<TextBlock>("HeaderConnectionText")!.Text = "●  DISCONNECTED";
        this.FindControl<TextBlock>("HeaderConnectionText")!.Foreground = new SolidColorBrush(Color.Parse("#FF6B6B"));
        this.FindControl<TextBlock>("FlowStatusText")!.Text = "DISCONNECTED";
        this.FindControl<TextBlock>("FlowStatusText")!.Foreground = new SolidColorBrush(Color.Parse("#FF6B6B"));
        this.FindControl<TextBlock>("StatusText")!.Text = "Disconnected";
        this.FindControl<TextBlock>("StatusText")!.Foreground = new SolidColorBrush(Color.Parse("#FF6B6B"));
        this.FindControl<TextBlock>("BatteryText")!.Text = "—";
        this.FindControl<TextBlock>("BatteryStateText")!.Text = "—";
        this.FindControl<TextBlock>("BatteryDetailText")!.Text = "—";
        this.FindControl<TextBlock>("LoadText")!.Text = "—";
        this.FindControl<TextBlock>("LoadDetailText")!.Text = "—";
        this.FindControl<TextBlock>("OutputPowerText")!.Text = "—";
        this.FindControl<TextBlock>("OutputDetailText")!.Text = "—";
        this.FindControl<TextBlock>("PowerCalculationText")!.Text = "—";
        this.FindControl<TextBlock>("RuntimeText")!.Text = "—";
        this.FindControl<TextBlock>("InputVoltageText")!.Text = "—";
        this.FindControl<TextBlock>("InputFrequencyText")!.Text = "—";
        this.FindControl<TextBlock>("FlowOutputVoltageText")!.Text = "—";
        this.FindControl<TextBlock>("OutputCurrentText")!.Text = "—";
        this.FindControl<TextBlock>("FlowLoadText")!.Text = "—";
        this.FindControl<TextBlock>("UpsNameDisplayText")!.Text = "UPS";
        this.FindControl<TextBlock>("UpsIdentityText")!.Text = "—";
        this.FindControl<TextBlock>("UpsTemperatureText")!.Text = "UPS: —";
        this.FindControl<TextBlock>("BatteryTemperatureText")!.Text = "Battery: —";
        this.FindControl<TextBlock>("InputPowerFactorDisplayText")!.Text = "Input: —";
        this.FindControl<TextBlock>("OutputPowerFactorDisplayText")!.Text = "Output: —";
        this.FindControl<TextBox>("NutInputVoltageText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutOutputVoltageText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutInputFrequencyText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutOutputFrequencyText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutBatteryVoltageText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutInputPowerFactorText")!.Text = "Not connected";
        this.FindControl<TextBox>("NutOutputPowerFactorText")!.Text = "Not connected";
        this.FindControl<TextBlock>("FooterConnectionText")!.Text = connectionText;
    }

    private static void ApplyTheme(int index)
    {
        Application.Current!.RequestedThemeVariant = index switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
