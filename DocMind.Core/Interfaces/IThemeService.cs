namespace DocMind.Core.Interfaces;

public interface IThemeService
{
    bool IsDarkMode { get; }
    void SetTheme(bool isDark);
}
