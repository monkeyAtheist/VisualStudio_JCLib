using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JCLib.VisualStudio.Services;

namespace JCLib.VisualStudio;

public partial class AppearanceDialog : Window
{
    private bool _loading;

    public AppearanceDialog(ThemePreferences theme)
    {
        InitializeComponent();
        LoadTheme(theme ?? ThemePreferences.CreateAccessibleDark());
    }

    public ThemePreferences? SelectedTheme { get; private set; }

    private void LoadTheme(ThemePreferences theme)
    {
        _loading = true;
        try
        {
            BackgroundTextBox.Text = theme.Background;
            PanelTextBox.Text = theme.Panel;
            InputTextBox.Text = theme.Input;
            DropdownBackgroundTextBox.Text = theme.DropdownBackground;
            DropdownTextTextBox.Text = theme.DropdownText;
            TextTextBox.Text = theme.Text;
            SecondaryTextBox.Text = theme.SecondaryText;
            AccentTextBox.Text = theme.Accent;
            BorderTextBox.Text = theme.Border;
            ButtonTextBox.Text = theme.ButtonText;
        }
        finally { _loading = false; }
        RefreshPreview();
    }

    private ThemePreferences ReadTheme() => new ThemePreferences
    {
        Background = BackgroundTextBox.Text.Trim(),
        Panel = PanelTextBox.Text.Trim(),
        Input = InputTextBox.Text.Trim(),
        DropdownBackground = DropdownBackgroundTextBox.Text.Trim(),
        DropdownText = DropdownTextTextBox.Text.Trim(),
        Text = TextTextBox.Text.Trim(),
        SecondaryText = SecondaryTextBox.Text.Trim(),
        Accent = AccentTextBox.Text.Trim(),
        Border = BorderTextBox.Text.Trim(),
        ButtonText = ButtonTextBox.Text.Trim(),
    };

    private void RefreshPreview()
    {
        ThemePreferences theme = ReadTheme();
        var issues = ThemeService.Validate(theme);
        SaveButton.IsEnabled = issues.Count == 0;
        ValidationText.Text = issues.Count == 0 ? "Couleurs valides." : string.Join(" ", issues);
        if (issues.Count == 0) ThemeService.ApplyTheme(this, theme);
    }

    private void OnThemeFieldChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) RefreshPreview();
    }

    private void OnChooseColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string textBoxName || FindName(textBoxName) is not TextBox textBox) return;
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        if (ThemeService.IsValidHexColor(textBox.Text))
        {
            string hex = textBox.Text.Trim();
            dialog.Color = ColorTranslator.FromHtml(hex.Substring(0, 7));
        }
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            textBox.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
    }

    private void OnResetDarkClick(object sender, RoutedEventArgs e) => LoadTheme(ThemePreferences.CreateAccessibleDark());

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        ThemePreferences theme = ReadTheme();
        if (ThemeService.Validate(theme).Count != 0) return;
        SelectedTheme = theme;
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
