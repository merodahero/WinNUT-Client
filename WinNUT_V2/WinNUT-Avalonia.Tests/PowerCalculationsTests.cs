using System.Collections.Generic;
using WinNUT_Avalonia.Services;
using Xunit;

namespace WinNUT_Avalonia.Tests;

public sealed class PowerCalculationsTests
{
    private static readonly PowerCalculationSettings Settings = new(0.95, 2400, 3000, 0.80);

    [Fact]
    public void UsesReportedRealPowerBeforeAnyEstimate()
    {
        var result = PowerCalculations.CalculateOutputPower(new Dictionary<string, string>
        {
            ["output.realpower"] = "720",
            ["output.power"] = "900"
        }, Settings);

        Assert.NotNull(result);
        Assert.Equal(720, result.Value.Watts);
        Assert.Equal("Measured output", result.Value.Source);
    }

    [Fact]
    public void CalculatesWattsFromVoltageCurrentAndConfiguredPowerFactor()
    {
        var result = PowerCalculations.CalculateOutputPower(new Dictionary<string, string>
        {
            ["output.voltage"] = "230",
            ["output.current"] = "3.35"
        }, Settings);

        Assert.NotNull(result);
        Assert.Equal(731.975, result.Value.Watts, 4);
        Assert.Contains("Configured PF 0.95", result.Value.Source);
    }

    [Fact]
    public void FallsBackToConfiguredNominalWattsAndLoad()
    {
        var result = PowerCalculations.CalculateOutputPower(new Dictionary<string, string>
        {
            ["ups.load"] = "24"
        }, Settings);

        Assert.NotNull(result);
        Assert.Equal(576, result.Value.Watts);
        Assert.Contains("configured 2,400 W", result.Value.Source);
    }

    [Fact]
    public void UsesVaRatingWhenNoWattRatingIsConfigured()
    {
        var result = PowerCalculations.CalculateOutputPower(new Dictionary<string, string>
        {
            ["ups.load"] = "24"
        }, Settings with { NominalOutputPowerWatts = 0 });

        Assert.NotNull(result);
        Assert.Equal(576, result.Value.Watts);
        Assert.Contains("3,000 VA", result.Value.Source);
    }

    [Fact]
    public void CalculatesPowerFactorFromRealAndApparentPower()
    {
        var result = PowerCalculations.ResolvePowerFactor(new Dictionary<string, string>
        {
            ["output.realpower"] = "720",
            ["output.power"] = "900"
        }, "output.powerfactor", "output.realpower", "output.power");

        Assert.NotNull(result);
        Assert.Equal(0.8, result.Value.Value, 3);
        Assert.Equal("Calculated (W ÷ VA)", result.Value.Source);
    }
}
