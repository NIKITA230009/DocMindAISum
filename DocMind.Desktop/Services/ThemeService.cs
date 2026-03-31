using Avalonia;
using Avalonia.Styling;
using DocMind.Core.Interfaces;

namespace DocMind.Desktop.Services;

public class ThemeService : IThemeService
{
    private bool _isDarkMode;

    public bool IsDarkMode => _isDarkMode;

    public void SetTheme(bool isDark)
    {
        _isDarkMode = isDark;
        Application.Current!.RequestedThemeVariant = isDark 
            ? ThemeVariant.Dark 
            : ThemeVariant.Light;
    }
}
