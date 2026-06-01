using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace JCLib.VisualStudio.Services;

public static class ThemeService
{
    private static readonly Regex HexColor = new Regex("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public static void ApplyTheme(FrameworkElement root, ThemePreferences theme)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));
        theme ??= ThemePreferences.CreateAccessibleDark();
        Set(root, "BackgroundBrush", theme.Background);
        Set(root, "PanelBrush", theme.Panel);
        Set(root, "InputBrush", theme.Input);
        Set(root, "DropdownBackgroundBrush", theme.DropdownBackground);
        Set(root, "DropdownTextBrush", theme.DropdownText);
        Set(root, "TextBrush", theme.Text);
        Set(root, "SecondaryTextBrush", theme.SecondaryText);
        Set(root, "AccentBrush", theme.Accent);
        Set(root, "BorderBrush", theme.Border);
        Set(root, "ButtonTextBrush", theme.ButtonText);
    }

    public static IReadOnlyList<string> Validate(ThemePreferences theme)
    {
        var issues = new List<string>();
        if (theme is null)
        {
            issues.Add("Le thème est absent.");
            return issues;
        }
        ValidateOne(issues, "Fond", theme.Background);
        ValidateOne(issues, "Panneaux", theme.Panel);
        ValidateOne(issues, "Champs", theme.Input);
        ValidateOne(issues, "Fond des listes déroulantes", theme.DropdownBackground);
        ValidateOne(issues, "Texte des listes déroulantes", theme.DropdownText);
        ValidateOne(issues, "Texte principal", theme.Text);
        ValidateOne(issues, "Texte secondaire", theme.SecondaryText);
        ValidateOne(issues, "Accent", theme.Accent);
        ValidateOne(issues, "Bordures", theme.Border);
        ValidateOne(issues, "Texte des boutons", theme.ButtonText);
        return issues;
    }

    public static bool IsValidHexColor(string? value) => !string.IsNullOrWhiteSpace(value) && HexColor.IsMatch(value.Trim());

    public static Brush CreateBrush(string value)
    {
        string normalized = IsValidHexColor(value) ? value.Trim() : "#FF00FF";
        return (Brush)new BrushConverter().ConvertFromString(normalized)!;
    }

    private static void Set(FrameworkElement root, string key, string value) => root.Resources[key] = CreateBrush(value);

    private static void ValidateOne(ICollection<string> issues, string label, string? value)
    {
        if (!IsValidHexColor(value)) issues.Add($"{label} : utilise #RRGGBB.");
    }
}
