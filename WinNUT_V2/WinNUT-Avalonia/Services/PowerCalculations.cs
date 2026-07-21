using System;
using System.Collections.Generic;
using System.Globalization;

namespace WinNUT_Avalonia.Services;

public readonly record struct PowerFactorResult(double Value, string Source);

public readonly record struct PowerEstimate(double Watts, string Source, string Detail);

public readonly record struct PowerCalculationSettings(
    double OutputLoadPowerFactor,
    int NominalOutputPowerWatts,
    int NominalOutputPowerVa,
    double RatedOutputPowerFactor);

/// <summary>
/// Electrical calculations used by the dashboard when a NUT driver does not
/// expose every real-power or power-factor variable.
/// </summary>
public static class PowerCalculations
{
    public static PowerFactorResult? ResolvePowerFactor(
        IReadOnlyDictionary<string, string> values,
        string directVariable,
        string realPowerVariable,
        string apparentPowerVariable)
    {
        if (TryGetNumber(values, directVariable, out var reported) && reported > 0 && reported <= 1)
            return new PowerFactorResult(reported, "NUT reported");

        if (TryGetNumber(values, realPowerVariable, out var realPower) &&
            TryGetNumber(values, apparentPowerVariable, out var apparentPower) && apparentPower > 0)
        {
            var calculated = realPower / apparentPower;
            if (calculated > 0 && calculated <= 1)
                return new PowerFactorResult(calculated, "Calculated (W ÷ VA)");
        }

        return null;
    }

    public static PowerEstimate? CalculateOutputPower(
        IReadOnlyDictionary<string, string> values,
        PowerCalculationSettings settings)
    {
        if (TryGetNumber(values, "output.realpower", out var outputRealPower))
            return new PowerEstimate(outputRealPower, "Measured output", "NUT reported output.realpower");

        var outputPowerFactor = ResolvePowerFactor(values, "output.powerfactor", "output.realpower", "output.power");
        if (TryGetNumber(values, "output.power", out var outputApparentPower) && outputPowerFactor is not null)
            return new PowerEstimate(outputApparentPower * outputPowerFactor.Value.Value,
                $"Estimated from output VA ({outputPowerFactor.Value.Source})",
                $"{outputApparentPower:0} VA × PF {outputPowerFactor.Value.Value:0.00} = {outputApparentPower * outputPowerFactor.Value.Value:0} W");

        if (TryGetNumber(values, "ups.realpower", out var upsRealPower))
            return new PowerEstimate(upsRealPower, "Measured UPS real power", "NUT reported ups.realpower");

        if (TryGetNumber(values, "output.current", out var outputCurrent) &&
            TryGetNumber(values, "output.voltage", out var outputVoltage))
        {
            var powerFactor = outputPowerFactor ?? new PowerFactorResult(settings.OutputLoadPowerFactor,
                $"Configured PF {settings.OutputLoadPowerFactor:0.00}");
            return new PowerEstimate(outputCurrent * outputVoltage * powerFactor.Value,
                $"Calculated from output V × A ({powerFactor.Source})",
                $"{outputVoltage:0.0} V × {outputCurrent:0.00} A × PF {powerFactor.Value:0.00} = {outputCurrent * outputVoltage * powerFactor.Value:0} W");
        }

        if (!TryGetNumber(values, "ups.load", out var load))
            return null;

        if (TryGetFirstNumber(values, out var nominalRealPower, "ups.realpower.nominal", "output.realpower.nominal"))
            return new PowerEstimate(nominalRealPower * load / 100d, "Estimated from NUT nominal real power",
                $"{nominalRealPower:0} W nominal × {load:0.#}% = {nominalRealPower * load / 100d:0} W");

        if (TryGetFirstNumber(values, out var nominalApparentPower, "ups.power.nominal", "output.power.nominal"))
            return new PowerEstimate(nominalApparentPower * settings.RatedOutputPowerFactor * load / 100d,
                $"Estimated from NUT nominal VA (rated PF {settings.RatedOutputPowerFactor:0.00})",
                $"{nominalApparentPower:0} VA × PF {settings.RatedOutputPowerFactor:0.00} × {load:0.#}% = {nominalApparentPower * settings.RatedOutputPowerFactor * load / 100d:0} W");

        if (settings.NominalOutputPowerWatts > 0)
            return new PowerEstimate(settings.NominalOutputPowerWatts * load / 100d,
                $"Estimated from configured {settings.NominalOutputPowerWatts:N0} W rating",
                $"{settings.NominalOutputPowerWatts:N0} W nominal × {load:0.#}% = {settings.NominalOutputPowerWatts * load / 100d:0} W");

        return new PowerEstimate(settings.NominalOutputPowerVa * settings.RatedOutputPowerFactor * load / 100d,
            $"Estimated from configured {settings.NominalOutputPowerVa:N0} VA (rated PF {settings.RatedOutputPowerFactor:0.00})",
            $"{settings.NominalOutputPowerVa:N0} VA × PF {settings.RatedOutputPowerFactor:0.00} × {load:0.#}% = {settings.NominalOutputPowerVa * settings.RatedOutputPowerFactor * load / 100d:0} W");
    }

    private static bool TryGetFirstNumber(IReadOnlyDictionary<string, string> values, out double value, params string[] variables)
    {
        foreach (var variable in variables)
        {
            if (TryGetNumber(values, variable, out value)) return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetNumber(IReadOnlyDictionary<string, string> values, string variable, out double value)
    {
        value = default;
        return values.TryGetValue(variable, out var text) &&
               double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
