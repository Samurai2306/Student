using Microsoft.JSInterop;

namespace InternetShop.Client.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _isDark;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public bool IsDark => _isDark;

    public event Action? OnChanged;

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("theme.getStored");
            _isDark = string.Equals(stored, "dark", StringComparison.OrdinalIgnoreCase);
            await _js.InvokeVoidAsync("theme.apply", _isDark ? "dark" : "light");
        }
        catch
        {
            _isDark = false;
        }
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await _js.InvokeVoidAsync("theme.apply", _isDark ? "dark" : "light");
        await _js.InvokeVoidAsync("theme.store", _isDark ? "dark" : "light");
        OnChanged?.Invoke();
    }
}
