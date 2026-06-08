using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JCLib.VisualStudio.Models;

namespace JCLib.VisualStudio;

internal sealed class StructuredChoiceDialog : Window
{
    private readonly CatalogPickerConfig _config;
    private readonly ListBox _choicesListBox;
    private readonly TextBox _selectionTextBox;
    private readonly TextBlock _validationTextBlock;

    public StructuredChoiceDialog(CatalogPickerConfig config, string currentValue)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Title = string.IsNullOrWhiteSpace(config.Title) ? "JC Lib — choisir une valeur" : config.Title;
        Width = 760;
        Height = 620;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        var root = new DockPanel { Margin = new Thickness(12) };
        Content = root;

        var bottom = new StackPanel { Orientation = Orientation.Vertical };
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        bottom.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(config.SelectionLabel) ? "Valeur sélectionnée" : config.SelectionLabel,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 3),
        });
        _selectionTextBox = new TextBox
        {
            IsReadOnly = true,
            Padding = new Thickness(5, 3, 5, 3),
            TextWrapping = TextWrapping.Wrap,
        };
        bottom.Children.Add(_selectionTextBox);
        _validationTextBlock = new TextBlock
        {
            Foreground = Brushes.IndianRed,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        bottom.Children.Add(_validationTextBlock);

        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        bottom.Children.Add(buttons);
        var clearButton = new Button { Content = "Effacer", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 6, 0) };
        clearButton.Click += (_, _) => { _choicesListBox.UnselectAll(); UpdateSelectionPreview(); };
        buttons.Children.Add(clearButton);
        var cancelButton = new Button { Content = "Annuler", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 6, 0), IsCancel = true };
        buttons.Children.Add(cancelButton);
        var applyButton = new Button { Content = "Appliquer", Padding = new Thickness(12, 4, 12, 4), IsDefault = true };
        applyButton.Click += (_, _) =>
        {
            EnsureMinimumSelectionFallback();
            SelectedChoice = GetSelectedChoices().FirstOrDefault();
            SelectedValue = BuildSelectedValue();
            DialogResult = true;
        };
        buttons.Children.Add(applyButton);

        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);
        heading.Children.Add(new TextBlock
        {
            Text = Title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(config.Subtitle))
        {
            heading.Children.Add(new TextBlock
            {
                Text = config.Subtitle,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        heading.Children.Add(new TextBlock
        {
            Text = config.MultiSelect
                ? $"Sélection multiple active. Les valeurs seront combinées avec « {config.ValueSeparator} »."
                : "Sélectionne une valeur documentée.",
            Margin = new Thickness(0, 4, 0, 0),
            FontStyle = FontStyles.Italic,
            Foreground = Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
        });

        _choicesListBox = new ListBox
        {
            SelectionMode = config.MultiSelect ? SelectionMode.Multiple : SelectionMode.Single,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        _choicesListBox.SelectionChanged += (_, _) =>
        {
            UpdateCompatibilityState();
            UpdateSelectionPreview();
        };
        root.Children.Add(_choicesListBox);

        PopulateChoices(config, currentValue ?? string.Empty);
        UpdateSelectionPreview();
    }

    public string SelectedValue { get; private set; } = string.Empty;

    public CatalogChoice? SelectedChoice { get; private set; }

    private void PopulateChoices(CatalogPickerConfig config, string currentValue)
    {
        var selectedValues = new HashSet<string>(SplitValues(currentValue, config.ValueSeparator), StringComparer.Ordinal);
        foreach (CatalogPickerSection section in config.Sections)
        {
            if (!string.IsNullOrWhiteSpace(section.Label))
            {
                _choicesListBox.Items.Add(new SeparatorLabel(section.Label, section.Description));
            }

            foreach (CatalogPickerGroup group in section.Groups)
            {
                if (!string.IsNullOrWhiteSpace(group.Label))
                {
                    _choicesListBox.Items.Add(new SeparatorLabel("  " + group.Label, group.Description));
                }

                foreach (CatalogChoice choice in group.Items)
                {
                    var item = new ListBoxItem
                    {
                        Tag = choice,
                        Padding = new Thickness(6, 4, 6, 4),
                        Content = BuildChoiceContent(choice),
                    };
                    _choicesListBox.Items.Add(item);
                    if (selectedValues.Contains(choice.Value)) item.IsSelected = true;
                }
            }
        }
    }

    private static UIElement BuildChoiceContent(CatalogChoice choice)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = choice.DisplayLabel,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(choice.Description))
        {
            panel.Children.Add(new TextBlock
            {
                Text = choice.Description,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        if (!string.IsNullOrWhiteSpace(choice.Detail))
        {
            panel.Children.Add(new TextBlock
            {
                Text = choice.Detail,
                Margin = new Thickness(0, 1, 0, 0),
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        return panel;
    }

    private IEnumerable<CatalogChoice> GetSelectedChoices() => _choicesListBox.SelectedItems
        .OfType<ListBoxItem>()
        .Select(item => item.Tag as CatalogChoice)
        .Where(choice => choice is not null)
        .Select(choice => choice!);

    private string BuildSelectedValue()
    {
        string[] selectedValues = GetSelectedChoices()
            .Select(choice => choice.Value)
            .ToArray();
        if (selectedValues.Length == 0) return _config.EmptyValue ?? string.Empty;
        if (!_config.MultiSelect) return selectedValues[0];

        string[] nonEmptyValues = selectedValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return nonEmptyValues.Length > 0
            ? string.Join(_config.ValueSeparator, nonEmptyValues)
            : _config.EmptyValue ?? string.Empty;
    }

    private void UpdateSelectionPreview()
    {
        _selectionTextBox.Text = BuildSelectedValue();
        bool valid = !_config.MultiSelect || GetSelectedChoices().Count() >= Math.Max(0, _config.MinimumSelections);
        _validationTextBlock.Text = valid
            ? string.Empty
            : string.IsNullOrWhiteSpace(_config.ValidationMessage)
                ? $"Sélectionne au moins {_config.MinimumSelections} valeur(s). Appliquer restaure la valeur par défaut."
                : _config.ValidationMessage;
        foreach (ListBoxItem item in _choicesListBox.Items.OfType<ListBoxItem>().Where(item => item.Tag is CatalogChoice))
        {
            item.Foreground = valid ? SystemColors.ControlTextBrush : Brushes.IndianRed;
        }
    }

    private void UpdateCompatibilityState()
    {
        string[] selectedValues = GetSelectedChoices().Select(choice => choice.Value).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        foreach (ListBoxItem item in _choicesListBox.Items.OfType<ListBoxItem>().Where(item => item.Tag is CatalogChoice))
        {
            if (item.Tag is not CatalogChoice choice) continue;
            bool incompatible = !item.IsSelected && selectedValues.Any(selected => choice.IncompatibleWith.Contains(selected, StringComparer.Ordinal)
                || GetChoice(selected)?.IncompatibleWith.Contains(choice.Value, StringComparer.Ordinal) == true);
            item.IsEnabled = !incompatible;
            item.Opacity = incompatible ? 0.45 : 1.0;
        }
    }

    private CatalogChoice? GetChoice(string value) => _config.FlattenChoices()
        .FirstOrDefault(choice => string.Equals(choice.Value, value, StringComparison.Ordinal));

    private void EnsureMinimumSelectionFallback()
    {
        if (!_config.MultiSelect || GetSelectedChoices().Count() >= Math.Max(0, _config.MinimumSelections)) return;
        _choicesListBox.UnselectAll();
        string fallback = string.IsNullOrWhiteSpace(_config.DefaultValue) ? _config.EmptyValue : _config.DefaultValue;
        var required = new HashSet<string>(SplitValues(fallback ?? string.Empty, _config.ValueSeparator), StringComparer.Ordinal);
        foreach (ListBoxItem item in _choicesListBox.Items.OfType<ListBoxItem>().Where(item => item.Tag is CatalogChoice))
        {
            if (item.Tag is CatalogChoice choice && required.Contains(choice.Value)) item.IsSelected = true;
        }
        if (GetSelectedChoices().Count() < Math.Max(0, _config.MinimumSelections))
        {
            foreach (ListBoxItem item in _choicesListBox.Items.OfType<ListBoxItem>().Where(item => item.Tag is CatalogChoice && item.IsEnabled))
            {
                item.IsSelected = true;
                if (GetSelectedChoices().Count() >= Math.Max(0, _config.MinimumSelections)) break;
            }
        }
        UpdateCompatibilityState();
        UpdateSelectionPreview();
    }

    private static IEnumerable<string> SplitValues(string text, string separator)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        string effectiveSeparator = string.IsNullOrEmpty(separator) ? " | " : separator;
        if (effectiveSeparator.Trim() == "|")
        {
            foreach (string value in text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0))
                yield return value;
            yield break;
        }
        foreach (string value in text.Split(new[] { effectiveSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0))
            yield return value;
    }

    private sealed class SeparatorLabel : ListBoxItem
    {
        public SeparatorLabel(string label, string description)
        {
            IsHitTestVisible = false;
            IsTabStop = false;
            Margin = new Thickness(0, 6, 0, 2);
            Padding = new Thickness(4, 2, 4, 2);
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold });
            if (!string.IsNullOrWhiteSpace(description))
                panel.Children.Add(new TextBlock { Text = description, Foreground = Brushes.DimGray, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap });
            Content = panel;
        }
    }
}
