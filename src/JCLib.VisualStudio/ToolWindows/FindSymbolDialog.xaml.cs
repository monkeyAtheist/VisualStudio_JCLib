using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using JCLib.VisualStudio.Models;
using JCLib.VisualStudio.Services;

namespace JCLib.VisualStudio;

public partial class FindSymbolDialog : Window
{
    private readonly IReadOnlyList<CatalogEntry> _entries;

    public FindSymbolDialog(IEnumerable<CatalogEntry> entries, ThemePreferences theme)
    {
        InitializeComponent();
        _entries = (entries ?? Array.Empty<CatalogEntry>()).ToArray();
        ThemeService.ApplyTheme(this, theme);
        Loaded += OnDialogLoaded;
    }

    public CatalogEntry? SelectedEntry { get; private set; }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        // Deliberately clear every previous value. The shortcut always opens a
        // fresh text-entry surface with no list item and no preview selected.
        QueryTextBox.Clear();
        ClearResultsAndPreview("Saisis un terme pour rechercher un symbole.");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            QueryTextBox.Focus();
            Keyboard.Focus(QueryTextBox);
        }), DispatcherPriority.Input);
    }

    private void ApplySearch()
    {
        string query = (QueryTextBox.Text ?? string.Empty).Trim();
        if (query.Length == 0)
        {
            ClearResultsAndPreview("Saisis un terme pour rechercher un symbole.");
            return;
        }

        string[] tokens = query
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.ToUpperInvariant())
            .ToArray();

        List<CatalogEntry> results = _entries
            .Where(entry => tokens.All(token => entry.SearchText.Contains(token)))
            .OrderBy(entry => Score(entry, query))
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Take(300)
            .ToList();

        ResultsListBox.ItemsSource = results;
        ResultsStatusText.Text = results.Count == 300
            ? "Recherche limitée aux 300 premiers résultats. Affine la requête pour réduire la liste."
            : $"{results.Count:N0} résultat(s) pour « {query} ».";

        if (results.Count == 0)
        {
            ClearPreview();
            return;
        }

        ResultsListBox.SelectedIndex = 0;
    }

    private static int Score(CatalogEntry entry, string query)
    {
        string normalized = query.Trim();
        if (string.Equals(entry.Name, normalized, StringComparison.OrdinalIgnoreCase)) return 0;
        if (entry.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)) return 1;
        if (entry.Name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
        if (entry.Path.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0) return 3;
        return 4;
    }

    private void ClearResultsAndPreview(string status)
    {
        ResultsListBox.ItemsSource = null;
        ResultsListBox.SelectedItem = null;
        ResultsStatusText.Text = status;
        ClearPreview();
    }

    private void ClearPreview()
    {
        SelectedEntry = null;
        PreviewBorder.Visibility = Visibility.Collapsed;
        PreviewNameText.Text = string.Empty;
        PreviewPathText.Text = string.Empty;
        PreviewSignatureTextBox.Text = string.Empty;
        PreviewDescriptionText.Text = string.Empty;
        OpenButton.IsEnabled = false;
    }

    private void ShowPreview(CatalogEntry? entry)
    {
        if (entry is null)
        {
            ClearPreview();
            return;
        }

        SelectedEntry = entry;
        PreviewNameText.Text = entry.Name;
        PreviewPathText.Text = entry.Path;
        PreviewSignatureTextBox.Text = FirstNonEmpty(entry.Signature, entry.Declaration, entry.InsertText, "Aucun prototype ou snippet renseigné.");
        PreviewDescriptionText.Text = entry.Description;
        PreviewBorder.Visibility = Visibility.Visible;
        OpenButton.IsEnabled = true;
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private void AcceptSelection()
    {
        CatalogEntry? entry = ResultsListBox.SelectedItem as CatalogEntry ?? SelectedEntry;
        if (entry is null) return;
        SelectedEntry = entry;
        DialogResult = true;
        Close();
    }

    private void OnQueryTextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    private void OnResultSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowPreview(ResultsListBox.SelectedItem as CatalogEntry);

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e) => AcceptSelection();

    private void OnOpenClick(object sender, RoutedEventArgs e) => AcceptSelection();

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            AcceptSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && ResultsListBox.Items.Count > 0)
        {
            ResultsListBox.Focus();
            Keyboard.Focus(ResultsListBox);
            e.Handled = true;
        }
    }

    private void OnResultsKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AcceptSelection();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
