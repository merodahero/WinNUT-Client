using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinNUT_Avalonia.Services;

/// <summary>
/// Small async NUT/upsd protocol client for the Avalonia UI layer.
/// It deliberately owns no UI timers or controls.
/// </summary>
public sealed class NutClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool IsConnected => _tcpClient?.Connected == true;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        var stream = _tcpClient.GetStream();
        _reader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
        _writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };
    }

    public async Task LoginAsync(string upsName, string? username, string? password, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            await SendAsync($"USERNAME {username}", cancellationToken);
            if (!string.IsNullOrWhiteSpace(password))
                await SendAsync($"PASSWORD {password}", cancellationToken);
        }

        await SendAsync($"LOGIN {upsName}", cancellationToken);
    }

    public async Task<string?> GetVariableAsync(string upsName, string variable, CancellationToken cancellationToken = default)
    {
        string response;
        try
        {
            response = await SendAsync($"GET VAR {upsName} {variable}", cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("VAR-NOT-SUPPORTED", StringComparison.OrdinalIgnoreCase))
        {
            // NUT servers are allowed to omit variables. The dashboard should
            // simply leave that metric unavailable rather than aborting a poll.
            return null;
        }
        // A GET VAR response is: VAR <ups-name> <variable-name> <quoted value>.
        // Limit the split so values containing spaces remain intact.
        var tokens = response.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4 || !string.Equals(tokens[0], "VAR", StringComparison.Ordinal))
            return null;

        return tokens[3].Trim('"');
    }

    public async Task<IReadOnlyDictionary<string, string>> GetVariablesAsync(string upsName, IEnumerable<string> variables, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            var value = await GetVariableAsync(upsName, variable, cancellationToken);
            if (value is not null)
                values[variable] = value;
        }
        return values;
    }

    public async Task<IReadOnlyDictionary<string, string>> ListVariablesAsync(string upsName, CancellationToken cancellationToken = default)
    {
        if (_reader is null || _writer is null)
            throw new InvalidOperationException("The NUT client is not connected.");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync($"LIST VAR {upsName}".AsMemory(), cancellationToken);
            while (true)
            {
                var response = await _reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(response))
                    throw new IOException("The NUT server closed the connection.");
                if (response.StartsWith("ERR ", StringComparison.Ordinal))
                    throw new InvalidOperationException($"NUT command failed: {response}");
                if (response.StartsWith("END LIST VAR", StringComparison.Ordinal)) break;
                if (!response.StartsWith("VAR ", StringComparison.Ordinal)) continue;

                var tokens = response.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 4) values[tokens[2]] = tokens[3].Trim('"');
            }
        }
        finally
        {
            _gate.Release();
        }

        return values;
    }

    private async Task<string> SendAsync(string command, CancellationToken cancellationToken)
    {
        if (_reader is null || _writer is null)
            throw new InvalidOperationException("The NUT client is not connected.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(command.AsMemory(), cancellationToken);
            var response = await _reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(response))
                throw new IOException("The NUT server closed the connection.");
            if (response.StartsWith("ERR ", StringComparison.Ordinal))
                throw new InvalidOperationException($"NUT command failed: {response}");
            return response;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _tcpClient?.Dispose();
        _reader = null;
        _writer = null;
        _tcpClient = null;
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
    }
}
