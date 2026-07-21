using System;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WinNUT_Avalonia.Services;

public static class UpdateService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task<string> CheckAsync(string branch)
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("WinNUT-Avalonia/1.0");
        var url = branch == "Preview"
            ? "https://api.github.com/repos/merodahero/WinNUT-Client/releases"
            : "https://api.github.com/repos/merodahero/WinNUT-Client/releases/latest";
        var json = await Client.GetStringAsync(url);
        using var document = JsonDocument.Parse(json);
        var release = document.RootElement;
        if (branch == "Preview" && document.RootElement.ValueKind == JsonValueKind.Array)
        {
            release = document.RootElement.EnumerateArray()
                .FirstOrDefault(item => item.TryGetProperty("prerelease", out var isPrerelease) && isPrerelease.GetBoolean());
            if (release.ValueKind == JsonValueKind.Undefined)
                release = document.RootElement[0];
        }
        var tag = release.TryGetProperty("tag_name", out var tagName) ? tagName.GetString() : null;
        return string.IsNullOrWhiteSpace(tag) ? "No published release found." : $"Latest {branch} release: {tag}";
    }
}
