using System;
using WinNUT_Avalonia.Services;
using Xunit;

namespace WinNUT_Avalonia.Tests;

public sealed class NutProtocolTests
{
    [Fact]
    public void ParsesQuotedValuesContainingSpaces()
    {
        var parsed = NutProtocol.TryParseVariable("VAR ups device.model \"Easy UPS SRVSPM3KIL\"", out var variable, out var value);

        Assert.True(parsed);
        Assert.Equal("device.model", variable);
        Assert.Equal("Easy UPS SRVSPM3KIL", value);
    }

    [Theory]
    [InlineData("ERR VAR-NOT-SUPPORTED")]
    [InlineData("nut command failed: ERR VAR-NOT-SUPPORTED")]
    public void IdentifiesUnsupportedVariableErrors(string message) =>
        Assert.True(NutProtocol.IsUnsupportedVariableError(new InvalidOperationException(message)));

    [Fact]
    public void RejectsNonVariableResponses() =>
        Assert.False(NutProtocol.TryParseVariable("ERR UNKNOWN-COMMAND", out _, out _));
}
