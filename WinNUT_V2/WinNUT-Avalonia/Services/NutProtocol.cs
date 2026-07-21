using System;

namespace WinNUT_Avalonia.Services;

public static class NutProtocol
{
    public static bool TryParseVariable(string response, out string variable, out string value)
    {
        variable = string.Empty;
        value = string.Empty;
        var tokens = response.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4 || !string.Equals(tokens[0], "VAR", StringComparison.Ordinal))
            return false;

        variable = tokens[2];
        value = tokens[3].Trim('"');
        return true;
    }

    public static bool IsUnsupportedVariableError(Exception exception) =>
        exception is InvalidOperationException &&
        exception.Message.Contains("VAR-NOT-SUPPORTED", StringComparison.OrdinalIgnoreCase);
}
