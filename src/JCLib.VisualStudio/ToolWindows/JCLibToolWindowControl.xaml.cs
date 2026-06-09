using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using JCLib.VisualStudio.Models;
using JCLib.VisualStudio.Services;
using Microsoft.Win32;

namespace JCLib.VisualStudio;

public partial class JCLibToolWindowControl : UserControl
{
    private CatalogLoadResult? _catalog;
    private CatalogEntry? _selectedEntry;
    private CatalogPackInfo? _selectedPack;
    private IReadOnlyList<SnippetParameterValue> _parameterValues = Array.Empty<SnippetParameterValue>();
    private readonly List<ParameterEditorBinding> _parameterEditors = new List<ParameterEditorBinding>();
    private readonly List<FileSystemWatcher> _packWatchers = new List<FileSystemWatcher>();
    private readonly DispatcherTimer _reloadDebounceTimer;
    private bool _buildingParameterUi;
    private bool _initialCatalogLoaded;
    private string _pendingReloadReason = string.Empty;
    private readonly UserPreferences _preferences;
    private bool _updatingFilters;

    public JCLibToolWindowControl()
    {
        InitializeComponent();
        _preferences = UserPreferencesStore.Load();
        ThemeService.ApplyTheme(this, _preferences.Theme);
        SearchTextBox.Text = _preferences.SearchText;
        FavoritesOnlyCheckBox.IsChecked = _preferences.FavoritesOnly;
        IncludeBundledPackCheckBox.IsChecked = _preferences.IncludeBundledPack;

        _reloadDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(650),
        };
        _reloadDebounceTimer.Tick += OnReloadDebounceTimerTick;

        Loaded += OnControlLoaded;
        Unloaded += OnControlUnloaded;
    }

    public void OpenFindSymbolDialog()
    {
        if (_catalog is null)
        {
            LoadCatalog();
        }

        IReadOnlyList<CatalogEntry> entries = _catalog?.Entries ?? Array.Empty<CatalogEntry>();
        var dialog = new FindSymbolDialog(entries, _preferences.Theme);
        Window? owner = Window.GetWindow(this);
        if (owner is not null) dialog.Owner = owner;

        bool? accepted = dialog.ShowDialog();
        if (accepted != true || dialog.SelectedEntry is null) return;

        // The modal picker is independent from the persistent inline filter.
        // Opening a symbol must return to the normal tree and reveal its preview.
        SearchTextBox.Clear();
        ShowEntry(dialog.SelectedEntry);
        SelectTreeEntry(dialog.SelectedEntry.CanonicalPath);
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        if (!_initialCatalogLoaded)
        {
            _initialCatalogLoaded = true;
            LoadCatalog();
            return;
        }

        StartWatchingPackDirectories();
    }

    private void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        StopWatchingPackDirectories();
        _reloadDebounceTimer.Stop();
    }

    private void LoadCatalog(string? completionMessage = null)
    {
        StopWatchingPackDirectories();

        try
        {
            _catalog = CatalogLoader.LoadCatalog(_preferences.IncludeBundledPack);
            PackManagementListBox.ItemsSource = _catalog.Packs;
            ResetPackManagementSelection();
            CatalogSummaryText.Text = BuildCatalogSummary(_catalog);
            UpdateDiagnostics(_catalog);
            PopulateFilters();
            RefreshQuickAccessLists();
            RefreshNavigationTree();
            RestoreLastSelectedEntry();
            ApplySearch();
            StartWatchingPackDirectories();

            if (!string.IsNullOrWhiteSpace(completionMessage))
            {
                StatusText.Text = completionMessage;
            }
        }
        catch (Exception ex)
        {
            _catalog = null;
            CatalogTreeView.ItemsSource = null;
            SearchResultsListBox.ItemsSource = null;
            PackManagementListBox.ItemsSource = null;
            ResetPackManagementSelection();
            CatalogSummaryText.Text = "Catalogues indisponibles";
            DiagnosticsExpander.Header = "Sources et diagnostics — erreur de chargement";
            DiagnosticsTextBox.Text = ex.ToString();
            StatusText.Text = $"Erreur de chargement : {ex.Message}";
            ClearPreview();
        }
    }

    private static string BuildCatalogSummary(CatalogLoadResult catalog)
    {
        return $"{catalog.ActivePacks.Count:N0}/{catalog.Packs.Count:N0} pack(s) actif(s) — " +
               $"fallback embarqué {(catalog.IncludeBundledPack ? "inclus" : "exclu")} — " +
               $"{catalog.Entries.Count:N0} élément(s) effectif(s) — " +
               $"{catalog.ShadowedEntries.Count:N0} élément(s) masqué(s) — " +
               $"{catalog.Conflicts.Count:N0} conflit(s) — {catalog.Issues.Count:N0} erreur(s) de chargement";
    }

    private void UpdateDiagnostics(CatalogLoadResult catalog)
    {
        DiagnosticsExpander.Header =
            $"Sources et diagnostics — {catalog.ActivePacks.Count:N0}/{catalog.Packs.Count:N0} pack(s) actif(s), " +
            $"{catalog.ShadowedEntries.Count:N0} élément(s) masqué(s), {catalog.Issues.Count:N0} erreur(s)";

        var text = new StringBuilder();
        text.AppendLine("POLITIQUE DE PRIORITÉ");
        text.AppendLine("  solution (300) > global utilisateur (200) > embarqué (100)");
        text.AppendLine($"  Pack embarqué fallback : {(catalog.IncludeBundledPack ? "inclus" : "exclu")}");
        text.AppendLine("  En cas de doublon de chemin logique, seule l'entrée prioritaire reste visible dans l'arborescence et la recherche.");
        text.AppendLine();
        text.AppendLine("RÉPERTOIRES");
        text.AppendLine($"  Packs globaux   : {catalog.GlobalPacksDirectory}");
        text.AppendLine($"  Packs solution  : {catalog.SolutionPacksDirectory ?? "<aucune solution ouverte ou dossier .jclib\\packs absent>"}");
        text.AppendLine($"  État local      : {catalog.DisabledPacksStateFile}");
        text.AppendLine();
        text.AppendLine("PACKS DÉTECTÉS");
        foreach (CatalogPackInfo pack in catalog.Packs)
        {
            text.AppendLine($"  - {pack.DisplayLabel}");
            text.AppendLine($"    id={pack.Id} | fichier={pack.SourcePath}");
        }

        text.AppendLine();
        text.AppendLine($"CONFLITS RÉSOLUS ({catalog.Conflicts.Count:N0})");
        AppendLimitedDiagnostics(
            text,
            catalog.Conflicts.Select(conflict =>
                $"  - {conflict.Message} | prioritaire={conflict.Winner?.Name ?? "?"} [{conflict.Winner?.SourceLabel ?? "?"}] | " +
                $"packs={string.Join(", ", conflict.Packs.Select(pack => pack.Name + " [" + pack.SourceLabel + "]"))}"),
            maxLines: 120);

        text.AppendLine();
        text.AppendLine($"ENTRÉES MASQUÉES ({catalog.ShadowedEntries.Count:N0})");
        AppendLimitedDiagnostics(
            text,
            catalog.ShadowedEntries.Select(item => "  - " + item.Message),
            maxLines: 120);

        text.AppendLine();
        text.AppendLine($"ERREURS DE CHARGEMENT ({catalog.Issues.Count:N0})");
        AppendLimitedDiagnostics(
            text,
            catalog.Issues.Select(issue => $"  - {issue.SourcePath} : {issue.Message}"),
            maxLines: 80);

        DiagnosticsTextBox.Text = text.ToString();
    }

    private static void AppendLimitedDiagnostics(StringBuilder text, IEnumerable<string> lines, int maxLines)
    {
        string[] buffered = lines.Take(maxLines + 1).ToArray();
        if (buffered.Length == 0)
        {
            text.AppendLine("  Aucun.");
            return;
        }

        foreach (string line in buffered.Take(maxLines))
        {
            text.AppendLine(line);
        }

        if (buffered.Length > maxLines)
        {
            text.AppendLine($"  ... affichage limité aux {maxLines:N0} premières lignes.");
        }
    }

    private void StartWatchingPackDirectories()
    {
        StopWatchingPackDirectories();

        if (AutoReloadCheckBox.IsChecked != true || _catalog is null)
        {
            return;
        }

        WatchDirectory(_catalog.GlobalPacksDirectory);
        if (!string.IsNullOrWhiteSpace(_catalog.SolutionPacksDirectory))
        {
            WatchDirectory(_catalog.SolutionPacksDirectory);
        }
    }

    private void WatchDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var watcher = new FileSystemWatcher(directory, "*.json")
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        watcher.Changed += OnPackFileChanged;
        watcher.Created += OnPackFileChanged;
        watcher.Deleted += OnPackFileChanged;
        watcher.Renamed += OnPackFileRenamed;
        watcher.Error += OnPackWatcherError;
        watcher.EnableRaisingEvents = true;
        _packWatchers.Add(watcher);
    }

    private void StopWatchingPackDirectories()
    {
        foreach (FileSystemWatcher watcher in _packWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnPackFileChanged;
            watcher.Created -= OnPackFileChanged;
            watcher.Deleted -= OnPackFileChanged;
            watcher.Renamed -= OnPackFileRenamed;
            watcher.Error -= OnPackWatcherError;
            watcher.Dispose();
        }
        _packWatchers.Clear();
    }

    private void OnPackFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (AutoReloadCheckBox.IsChecked != true)
            {
                return;
            }

            _pendingReloadReason = $"Modification détectée : {Path.GetFileName(e.FullPath)}";
            _reloadDebounceTimer.Stop();
            _reloadDebounceTimer.Start();
            StatusText.Text = _pendingReloadReason + ". Rechargement en attente...";
        }));
    }

    private void OnPackFileRenamed(object sender, RenamedEventArgs e)
    {
        OnPackFileChanged(sender, e);
    }

    private void OnPackWatcherError(object sender, ErrorEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            StatusText.Text = $"Surveillance des packs interrompue : {e.GetException()?.Message ?? "erreur inconnue"}. Utilise Recharger pour relancer la surveillance.";
        }));
    }

    private void OnReloadDebounceTimerTick(object? sender, EventArgs e)
    {
        _reloadDebounceTimer.Stop();
        string reason = _pendingReloadReason;
        _pendingReloadReason = string.Empty;
        LoadCatalog(string.IsNullOrWhiteSpace(reason)
            ? "Catalogues rechargés automatiquement."
            : reason + ". Catalogues rechargés automatiquement.");
    }

    private void ApplySearch()
    {
        string query = (SearchTextBox.Text ?? string.Empty).Trim();
        _preferences.SearchText = SearchTextBox.Text ?? string.Empty;
        SavePreferences();
        bool searchActive = query.Length > 0;

        CatalogTreeView.Visibility = searchActive ? Visibility.Collapsed : Visibility.Visible;
        SearchResultsListBox.Visibility = searchActive ? Visibility.Visible : Visibility.Collapsed;

        IReadOnlyList<CatalogEntry> availableEntries = GetFilteredEntries();
        if (!searchActive)
        {
            SearchResultsListBox.ItemsSource = null;
            StatusText.Text = _catalog is null
                ? "Catalogues indisponibles."
                : $"{availableEntries.Count:N0} élément(s) disponible(s) avec les filtres courants.";
            return;
        }

        string[] tokens = query
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.ToUpperInvariant())
            .ToArray();

        List<CatalogEntry> results = availableEntries
            .Where(entry => tokens.All(token => entry.SearchText.Contains(token)))
            .OrderBy(entry => entry.Name)
            .ThenBy(entry => entry.Path)
            .ThenBy(entry => entry.PackName)
            .Take(500)
            .ToList();

        SearchResultsListBox.ItemsSource = results;
        StatusText.Text = results.Count == 500
            ? "Recherche limitée aux 500 premiers résultats. Affine la requête pour réduire la liste."
            : $"{results.Count:N0} résultat(s) pour « {query} ». Double-clique pour une insertion rapide.";
    }

    private IReadOnlyList<CatalogEntry> GetFilteredEntries()
    {
        if (_catalog is null) return Array.Empty<CatalogEntry>();
        IEnumerable<CatalogEntry> entries = _catalog.Entries;
        string environment = EnvironmentFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        string library = LibraryFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        if (!string.Equals(environment, UserPreferencesStore.AllFilterLabel, StringComparison.OrdinalIgnoreCase))
            entries = entries.Where(entry => string.Equals(entry.Environment, environment, StringComparison.OrdinalIgnoreCase));
        if (!string.Equals(library, UserPreferencesStore.AllFilterLabel, StringComparison.OrdinalIgnoreCase))
            entries = entries.Where(entry => string.Equals(entry.Library, library, StringComparison.OrdinalIgnoreCase));
        if (FavoritesOnlyCheckBox.IsChecked == true)
            entries = entries.Where(entry => UserPreferencesStore.IsFavorite(_preferences, entry.CanonicalPath));
        return entries.ToArray();
    }

    private void PopulateFilters()
    {
        if (_catalog is null) return;
        _updatingFilters = true;
        try
        {
            string currentEnvironment = string.IsNullOrWhiteSpace(_preferences.FilterEnvironment) ? UserPreferencesStore.AllFilterLabel : _preferences.FilterEnvironment;
            var environments = new[] { UserPreferencesStore.AllFilterLabel }
                .Concat(_catalog.Entries.Select(entry => entry.Environment).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value))
                .ToArray();
            EnvironmentFilterComboBox.ItemsSource = environments;
            EnvironmentFilterComboBox.SelectedItem = environments.FirstOrDefault(value => string.Equals(value, currentEnvironment, StringComparison.OrdinalIgnoreCase)) ?? UserPreferencesStore.AllFilterLabel;
            PopulateLibraryFilter();
            FavoritesOnlyCheckBox.IsChecked = _preferences.FavoritesOnly;
        }
        finally { _updatingFilters = false; }
    }

    private void PopulateLibraryFilter()
    {
        if (_catalog is null) return;
        string environment = EnvironmentFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        IEnumerable<CatalogEntry> entries = _catalog.Entries;
        if (!string.Equals(environment, UserPreferencesStore.AllFilterLabel, StringComparison.OrdinalIgnoreCase))
            entries = entries.Where(entry => string.Equals(entry.Environment, environment, StringComparison.OrdinalIgnoreCase));
        var libraries = new[] { UserPreferencesStore.AllFilterLabel }
            .Concat(entries.Select(entry => entry.Library).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value))
            .ToArray();
        string desired = string.IsNullOrWhiteSpace(_preferences.FilterLibrary) ? UserPreferencesStore.AllFilterLabel : _preferences.FilterLibrary;
        LibraryFilterComboBox.ItemsSource = libraries;
        LibraryFilterComboBox.SelectedItem = libraries.FirstOrDefault(value => string.Equals(value, desired, StringComparison.OrdinalIgnoreCase)) ?? UserPreferencesStore.AllFilterLabel;
    }

    private void RefreshNavigationTree()
    {
        CatalogTreeView.ItemsSource = new[] { BuildFilteredRoot(GetFilteredEntries()) };
    }

    private static CatalogNode BuildFilteredRoot(IEnumerable<CatalogEntry> entries)
    {
        var root = new CatalogNode("JC Lib — catalogue filtré", CatalogNodeKind.Root) { IsExpanded = true };
        foreach (CatalogEntry entry in entries.OrderBy(value => value.SourceDisplay).ThenBy(value => value.Path, StringComparer.OrdinalIgnoreCase))
        {
            CatalogNode pack = GetOrAddChild(root, entry.SourceDisplay, CatalogNodeKind.Pack);
            CatalogNode hierarchyParent = pack;
            if (!string.IsNullOrWhiteSpace(entry.Environment))
            {
                hierarchyParent = GetOrAddChild(pack, entry.Environment, CatalogNodeKind.Environment);
            }
            CatalogNode library = GetOrAddChild(hierarchyParent, entry.Library, CatalogNodeKind.Library);
            CatalogNode category = GetOrAddChild(library, entry.Category, CatalogNodeKind.Category);
            CatalogNode parent = category;
            foreach (string group in (entry.Group ?? string.Empty).Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0))
                parent = GetOrAddChild(parent, group, CatalogNodeKind.Group);
            parent.Children.Add(new CatalogNode(entry.Name, CatalogNodeKind.Element, entry));
        }
        return root;
    }

    private static CatalogNode GetOrAddChild(CatalogNode parent, string name, CatalogNodeKind kind)
    {
        string safeName = string.IsNullOrWhiteSpace(name) ? $"<{kind}>" : name.Trim();
        CatalogNode? existing = parent.Children.FirstOrDefault(child => child.Kind == kind && string.Equals(child.Name, safeName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var created = new CatalogNode(safeName, kind);
        parent.Children.Add(created);
        return created;
    }

    private void RefreshQuickAccessLists()
    {
        if (_catalog is null)
        {
            FavoritesListBox.ItemsSource = null;
            RecentListBox.ItemsSource = null;
            return;
        }
        FavoritesListBox.ItemsSource = ResolvePreferenceEntries(_preferences.Favorites);
        RecentListBox.ItemsSource = ResolvePreferenceEntries(_preferences.RecentEntries);
    }

    private IReadOnlyList<CatalogEntry> ResolvePreferenceEntries(IEnumerable<string> keys)
    {
        if (_catalog is null) return Array.Empty<CatalogEntry>();
        var byKey = _catalog.Entries.GroupBy(entry => entry.CanonicalPath, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        return keys.Where(key => byKey.ContainsKey(key)).Select(key => byKey[key]).ToArray();
    }

    private void RestoreLastSelectedEntry()
    {
        if (_catalog is null || string.IsNullOrWhiteSpace(_preferences.SelectedEntryKey)) return;
        CatalogEntry? entry = _catalog.Entries.FirstOrDefault(value => string.Equals(value.CanonicalPath, _preferences.SelectedEntryKey, StringComparison.OrdinalIgnoreCase));
        if (entry is null) return;
        ShowEntry(entry);
        SelectTreeEntry(entry.CanonicalPath);
    }

    private void SelectTreeEntry(string key)
    {
        if (CatalogTreeView.ItemsSource is not IEnumerable<CatalogNode> roots) return;
        foreach (CatalogNode root in roots)
        {
            if (TrySelectTreeEntry(root, key)) break;
        }
    }

    private static bool TrySelectTreeEntry(CatalogNode node, string key)
    {
        if (node.Entry is not null && string.Equals(node.Entry.CanonicalPath, key, StringComparison.OrdinalIgnoreCase))
        {
            node.IsSelected = true;
            return true;
        }
        foreach (CatalogNode child in node.Children)
        {
            if (!TrySelectTreeEntry(child, key)) continue;
            node.IsExpanded = true;
            return true;
        }
        return false;
    }

    private void SavePreferences()
    {
        try { UserPreferencesStore.Save(_preferences); }
        catch { /* Preferences must never break the browser. */ }
    }

    private void ShowEntry(CatalogEntry? entry)
    {
        _selectedEntry = entry;

        if (entry is null)
        {
            ClearPreview();
            return;
        }

        _preferences.SelectedEntryKey = entry.CanonicalPath;
        SavePreferences();
        FavoriteToggleButton.IsEnabled = true;
        FavoriteToggleButton.Content = UserPreferencesStore.IsFavorite(_preferences, entry.CanonicalPath)
            ? "★ Retirer des favoris"
            : "☆ Ajouter aux favoris";
        SelectedNameText.Text = entry.Name;
        SelectedPathText.Text = entry.Path;
        SelectedMetaText.Text = BuildMetadata(entry);
        SignatureTextBox.Text = FirstNonEmpty(entry.Signature, entry.Declaration, "Aucun prototype renseigné.");
        DescriptionText.Text = entry.Description;

        BuildParameterEditor(entry);
        RefreshGeneratedSnippet();

        bool hasSnippet = !string.IsNullOrWhiteSpace(SnippetTextBox.Text);
        CopySnippetButton.IsEnabled = hasSnippet;
        InsertSnippetButton.IsEnabled = hasSnippet;
        QuickInsertSnippetButton.IsEnabled = hasSnippet;
        ExecuteSnippetButton.IsEnabled = hasSnippet && IsTerminalExecutableEntry(entry);
        StatusText.Text = hasSnippet
            ? "Élément sélectionné. Ajuste les paramètres puis insère le snippet généré ou copie-le."
            : "Élément sélectionné. Aucun snippet n'est défini pour cette entrée.";
    }

    private void BuildParameterEditor(CatalogEntry entry)
    {
        _buildingParameterUi = true;
        try
        {
            ParametersPanel.Children.Clear();
            _parameterEditors.Clear();
            _parameterValues = SnippetParameterService.CreateEditorState(entry);
            ReturnTargetTextBox.Text = string.Empty;

            bool showParameters = _parameterValues.Count > 0 || entry.HasReturnValue;
            ParametersExpander.Visibility = showParameters ? Visibility.Visible : Visibility.Collapsed;
            ReturnTargetGrid.Visibility = entry.HasReturnValue ? Visibility.Visible : Visibility.Collapsed;
            ParametersSummaryText.Text = _parameterValues.Count == 0
                ? "Cette fonction ne possède aucun argument."
                : $"{_parameterValues.Count:N0} paramètre(s). Les modifications sont appliquées immédiatement au snippet généré.";

            foreach (SnippetParameterValue parameterValue in _parameterValues)
            {
                ParametersPanel.Children.Add(CreateParameterEditor(parameterValue));
            }
        }
        finally
        {
            _buildingParameterUi = false;
        }
    }

    private UIElement CreateParameterEditor(SnippetParameterValue parameterValue)
    {
        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(6),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
        };

        var content = new StackPanel();
        border.Child = content;
        content.Children.Add(new TextBlock
        {
            Text = $"{parameterValue.Parameter.Name} : {parameterValue.Parameter.Type}",
            FontWeight = FontWeights.SemiBold,
        });
        content.Children.Add(new TextBlock
        {
            Text = $"Éditeur : {parameterValue.EditorType}" + (parameterValue.Parameter.Optional ? " — facultatif" : string.Empty),
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 2, 0, 2),
        });
        if (!string.IsNullOrWhiteSpace(parameterValue.Parameter.Description))
        {
            content.Children.Add(new TextBlock
            {
                Text = parameterValue.Parameter.Description,
                Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var editorGrid = new Grid();
        editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(editorGrid);

        bool isMultiline = string.Equals(parameterValue.EditorType, "multiline", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterValue.EditorType, "textarea", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterValue.EditorType, "code", StringComparison.OrdinalIgnoreCase)
            || (parameterValue.Value ?? string.Empty).IndexOf('\n') >= 0
            || (parameterValue.Value ?? string.Empty).IndexOf('\r') >= 0;
        var textBox = new TextBox
        {
            Text = parameterValue.Value,
            Padding = new Thickness(5, 3, 5, 3),
            ToolTip = $"Valeur insérée pour {parameterValue.Parameter.Name}. Exemple : {parameterValue.Placeholder}",
            AcceptsReturn = isMultiline,
            AcceptsTab = isMultiline,
            TextWrapping = isMultiline ? TextWrapping.NoWrap : TextWrapping.Wrap,
            VerticalScrollBarVisibility = isMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = isMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
            MinHeight = isMultiline ? 132 : 0,
        };
        textBox.TextChanged += OnParameterTextChanged;
        editorGrid.Children.Add(textBox);

        var binding = new ParameterEditorBinding(parameterValue, textBox, border);
        textBox.Tag = binding;
        _parameterEditors.Add(binding);

        if (parameterValue.SuggestedChoices.Count > 0)
        {
            var suggestions = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(6, 0, 0, 0),
                ItemsSource = parameterValue.SuggestedChoices,
                DisplayMemberPath = "DisplayLabel",
                ToolTip = "Valeurs usuelles documentées",
            };
            suggestions.SelectionChanged += OnSuggestionSelectionChanged;
            suggestions.Tag = binding;
            binding.DependentControls.Add(suggestions);
            Grid.SetColumn(suggestions, 1);
            editorGrid.Children.Add(suggestions);
        }

        CatalogPickerConfig? pickerConfig = SnippetParameterService.CreateEffectivePickerConfig(parameterValue);
        if (pickerConfig is not null && pickerConfig.FlattenChoices().Count > 0)
        {
            var pickerButton = new Button
            {
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(8, 3, 8, 3),
                Content = pickerConfig.MultiSelect ? "Choix multiples..." : "Choisir...",
                ToolTip = pickerConfig.MultiSelect ? "Ouvrir la fenêtre de sélection multiple" : "Ouvrir la fenêtre de choix documentés",
                Tag = binding,
            };
            pickerButton.Click += OnStructuredPickerClick;
            binding.DependentControls.Add(pickerButton);
            Grid.SetColumn(pickerButton, 2);
            editorGrid.Children.Add(pickerButton);
        }

        if (parameterValue.EditorType == "pathFile" || parameterValue.EditorType == "pathFolder")
        {
            var browseButton = new Button
            {
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(8, 3, 8, 3),
                Content = "...",
                ToolTip = parameterValue.EditorType == "pathFile" ? "Choisir un fichier" : "Choisir un dossier",
                Tag = binding,
            };
            browseButton.Click += OnBrowseParameterClick;
            binding.DependentControls.Add(browseButton);
            Grid.SetColumn(browseButton, 3);
            editorGrid.Children.Add(browseButton);
        }

        return border;
    }

    private void RefreshGeneratedSnippet()
    {
        if (_selectedEntry is null)
        {
            SnippetTextBox.Text = string.Empty;
            return;
        }

        foreach (ParameterEditorBinding binding in _parameterEditors)
        {
            binding.ParameterValue.Value = binding.TextBox.Text;
        }

        RefreshConditionalParameterEditors();

        SnippetTextBox.Text = SnippetParameterService.BuildInsertText(
            _selectedEntry,
            _parameterValues,
            ReturnTargetTextBox.Text ?? string.Empty);

        bool hasSnippet = !string.IsNullOrWhiteSpace(SnippetTextBox.Text);
        CopySnippetButton.IsEnabled = hasSnippet;
        InsertSnippetButton.IsEnabled = hasSnippet;
        QuickInsertSnippetButton.IsEnabled = hasSnippet;
        ExecuteSnippetButton.IsEnabled = hasSnippet && IsTerminalExecutableEntry(_selectedEntry);
    }

    private void RefreshConditionalParameterEditors()
    {
        for (int index = 0; index < _parameterEditors.Count; index++)
        {
            bool enabled = SnippetParameterService.IsEnabled(_parameterValues, index);
            _parameterEditors[index].SetEnabled(enabled);
        }
    }

    private void ClearPreview()
    {
        _selectedEntry = null;
        _parameterValues = Array.Empty<SnippetParameterValue>();
        _parameterEditors.Clear();
        ParametersPanel.Children.Clear();
        ParametersExpander.Visibility = Visibility.Collapsed;
        ReturnTargetGrid.Visibility = Visibility.Collapsed;
        ReturnTargetTextBox.Text = string.Empty;
        SelectedNameText.Text = "Sélectionne un élément";
        SelectedPathText.Text = string.Empty;
        SelectedMetaText.Text = string.Empty;
        SignatureTextBox.Text = string.Empty;
        SnippetTextBox.Text = string.Empty;
        DescriptionText.Text = string.Empty;
        CopySnippetButton.IsEnabled = false;
        InsertSnippetButton.IsEnabled = false;
        QuickInsertSnippetButton.IsEnabled = false;
        ExecuteSnippetButton.IsEnabled = false;
        FavoriteToggleButton.IsEnabled = false;
        FavoriteToggleButton.Content = "☆ Ajouter aux favoris";
    }

    private static string BuildMetadata(CatalogEntry entry)
    {
        var values = new List<string>
        {
            $"Pack : {entry.PackName} v{entry.PackVersion}",
            $"Source : {entry.PackSourceLabel}",
        };
        if (!string.IsNullOrWhiteSpace(entry.SymbolKind)) values.Add($"Type : {entry.SymbolKind}");
        if (!string.IsNullOrWhiteSpace(entry.ReturnType)) values.Add($"Retour : {entry.ReturnType}");
        if (!string.IsNullOrWhiteSpace(entry.Header)) values.Add($"Header : {entry.Header}");
        if (entry.Parameters.Count > 0) values.Add($"Paramètres : {entry.Parameters.Count}");
        return string.Join(" | ", values);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private string GetGeneratedSnippet()
    {
        RefreshGeneratedSnippet();
        return SnippetTextBox.Text ?? string.Empty;
    }

    private void CopySelectedSnippet()
    {
        string snippet = GetGeneratedSnippet();
        if (_selectedEntry is null || string.IsNullOrWhiteSpace(snippet))
        {
            StatusText.Text = "Aucun snippet copiable n'est sélectionné.";
            return;
        }

        try
        {
            Clipboard.SetText(snippet);
            RecordRecent(_selectedEntry);
            StatusText.Text = $"Snippet « {_selectedEntry.Name} » copié dans le presse-papiers.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Copie impossible : {ex.Message}";
        }
    }

    private static bool IsTerminalExecutableEntry(CatalogEntry? entry)
    {
        if (entry is null) return false;
        string kind = (entry.SymbolKind ?? string.Empty).Trim();
        if (string.Equals(kind, "command", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals((entry.Environment ?? string.Empty).Trim(), "Scripting / System", StringComparison.OrdinalIgnoreCase)
            && string.Equals(kind, "snippet", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPotentiallyDestructiveTerminalText(string snippet)
    {
        string text = (snippet ?? string.Empty).Trim();
        if (text.Length == 0) return false;
        return Regex.IsMatch(text,
            @"(^|[;&|]\s*|\n\s*)(rm\s+-|rmdir\b|del\s+/|erase\b|format\b|diskpart\b|shutdown\b|reboot\b|poweroff\b|taskkill\b|stop-process\b|remove-item\b|docker\s+system\s+prune\b|docker\s+(container|image|volume)\s+rm\b|git\s+reset\s+--hard\b|git\s+clean\s+-|git\s+stash\s+clear\b|git\s+branch\s+-d\b|git\s+worktree\s+remove\b|sc\s+delete\b|reg\s+delete\b|apt\s+remove\b|systemctl\s+(stop|disable|restart)\b|setx\b|ipconfig\s+/flushdns\b|clear-dnsclientcache\b|ssh-copy-id\b|authorized_keys\b|setenvironmentvariable\b|add-content\b[^\n]*hosts\b|set-content\b[^\n]*hosts\b|tee\s+-a\s+/etc/hosts\b|sed\s+-i[^\n]*/etc/hosts\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string QuoteProcessArgument(string value)
    {
        return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string? FindExecutableOnPath(params string[] candidates)
    {
        string[] directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string candidate in candidates)
        {
            foreach (string directory in directories)
            {
                try
                {
                    string path = Path.Combine(directory.Trim(), candidate);
                    if (File.Exists(path)) return path;
                }
                catch
                {
                    // Ignore malformed PATH entries and continue searching.
                }
            }
        }
        return null;
    }

    private static ProcessStartInfo BuildConsoleStartInfo(CatalogEntry entry, string snippet)
    {
        string library = (entry.Library ?? string.Empty).Trim();
        string workingDirectory = SolutionPathService.TryGetCurrentSolutionDirectory() ?? Environment.CurrentDirectory;
        if (string.Equals(library, "PowerShell 7", StringComparison.OrdinalIgnoreCase))
        {
            string executable = FindExecutableOnPath("pwsh.exe", "powershell.exe") ?? "powershell.exe";
            return new ProcessStartInfo(executable, "-NoExit -Command " + QuoteProcessArgument(snippet))
            {
                UseShellExecute = true,
                WorkingDirectory = workingDirectory,
            };
        }

        if (string.Equals(library, "Bash & POSIX Shell", StringComparison.OrdinalIgnoreCase)
            || string.Equals(library, "Linux systemd & Administration", StringComparison.OrdinalIgnoreCase))
        {
            string? wsl = FindExecutableOnPath("wsl.exe");
            if (!string.IsNullOrWhiteSpace(wsl))
            {
                return new ProcessStartInfo(wsl, "-- bash -lc " + QuoteProcessArgument(snippet + "; exec bash"))
                {
                    UseShellExecute = true,
                    WorkingDirectory = workingDirectory,
                };
            }
            string? bash = FindExecutableOnPath("bash.exe");
            if (!string.IsNullOrWhiteSpace(bash))
            {
                return new ProcessStartInfo(bash, "-lc " + QuoteProcessArgument(snippet + "; exec bash"))
                {
                    UseShellExecute = true,
                    WorkingDirectory = workingDirectory,
                };
            }
        }

        return new ProcessStartInfo("cmd.exe", "/K " + snippet)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory,
        };
    }

    private void ExecuteSelectedSnippetInConsole()
    {
        string snippet = GetGeneratedSnippet();
        if (_selectedEntry is null || string.IsNullOrWhiteSpace(snippet))
        {
            StatusText.Text = "Aucune commande exécutable n'est sélectionnée.";
            return;
        }
        if (!IsTerminalExecutableEntry(_selectedEntry))
        {
            StatusText.Text = "Cette entrée n'est pas une commande console ni une recette Scripting / System.";
            return;
        }

        bool requiresConfirmation = snippet.Contains("\n") || IsPotentiallyDestructiveTerminalText(snippet);
        if (requiresConfirmation)
        {
            MessageBoxResult result = MessageBox.Show(
                Window.GetWindow(this),
                "Vérifie attentivement la preview avant exécution. La commande sera transmise telle quelle à une console externe.\n\n" + snippet,
                "JC Lib — confirmer l'exécution",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            Process.Start(BuildConsoleStartInfo(_selectedEntry, snippet));
            RecordRecent(_selectedEntry);
            StatusText.Text = $"Commande « {_selectedEntry.Name} » transmise à une console externe.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Exécution impossible : {ex.Message}";
        }
    }

    private void ImportPack(CatalogPackSourceKind targetKind)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            Title = targetKind == CatalogPackSourceKind.GlobalUser
                ? "Importer un pack JC Lib global"
                : "Importer un pack JC Lib pour la solution ouverte",
            Filter = "Packs JC Lib JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            PackValidationResult validation = CatalogLoader.ValidatePackFile(dialog.FileName);
            string? targetDirectory = CatalogLoader.GetPackDirectory(targetKind, create: true);
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                StatusText.Text = "Import solution impossible : ouvre d'abord une solution Visual Studio.";
                return;
            }

            string destinationPath = Path.Combine(targetDirectory, Path.GetFileName(dialog.FileName));
            bool overwrite = false;
            if (File.Exists(destinationPath) &&
                !string.Equals(Path.GetFullPath(destinationPath), Path.GetFullPath(dialog.FileName), StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxResult answer = MessageBox.Show(
                    $"Le fichier existe déjà :\n{destinationPath}\n\nLe remplacer ?",
                    "JC Lib — import de pack",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    StatusText.Text = "Import annulé : le pack existant a été conservé.";
                    return;
                }
                overwrite = true;
            }

            string importedPath = CatalogLoader.ImportPack(dialog.FileName, targetKind, overwrite);
            LoadCatalog($"Pack « {validation.Name} » v{validation.Version} importé : {importedPath}");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Import impossible : {ex.Message}";
        }
    }

    private void OpenPacksFolder(CatalogPackSourceKind targetKind)
    {
        try
        {
            string? directory = CatalogLoader.GetPackDirectory(targetKind, create: true);
            if (string.IsNullOrWhiteSpace(directory))
            {
                StatusText.Text = "Aucun dossier solution disponible : ouvre d'abord une solution Visual Studio.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directory}\"",
                UseShellExecute = true,
            });
            StatusText.Text = $"Ouverture du dossier : {directory}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ouverture du dossier impossible : {ex.Message}";
        }
    }

    private void ResetPackManagementSelection()
    {
        _selectedPack = null;
        PackManagementListBox.SelectedItem = null;
        ToggleSelectedPackButton.IsEnabled = false;
        EditSelectedPackButton.IsEnabled = false;
        DuplicateSelectedPackButton.IsEnabled = false;
        DeleteSelectedPackButton.IsEnabled = false;
        OpenSelectedPackFolderButton.IsEnabled = false;
        PackManagementHintText.Text = "Sélectionne un pack externe pour le gérer. Le pack embarqué est un fallback optionnel en lecture seule.";
    }

    private void RefreshPackManagementButtons()
    {
        CatalogPackInfo? pack = _selectedPack;
        bool hasSelection = pack is not null;
        bool external = hasSelection && !pack!.IsReadOnly;
        ToggleSelectedPackButton.IsEnabled = external;
        EditSelectedPackButton.IsEnabled = external;
        DuplicateSelectedPackButton.IsEnabled = hasSelection;
        DeleteSelectedPackButton.IsEnabled = external;
        OpenSelectedPackFolderButton.IsEnabled = external;

        if (pack is null)
        {
            PackManagementHintText.Text = "Sélectionne un pack externe pour le gérer. Le pack embarqué est un fallback optionnel en lecture seule.";
            return;
        }

        PackManagementHintText.Text = pack.IsReadOnly
            ? "Le pack embarqué est un fallback en lecture seule. Utilise la case dédiée pour l’inclure ou l’exclure du catalogue."
            : $"Pack {pack.StateLabel}. Source : {pack.SourcePath}";
    }

    private void OnPackManagementSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPack = PackManagementListBox.SelectedItem as CatalogPackInfo;
        RefreshPackManagementButtons();
    }

    private void OnToggleSelectedPackClick(object sender, RoutedEventArgs e)
    {
        CatalogPackInfo? pack = _selectedPack;
        if (pack is null) return;

        try
        {
            bool newState = !pack.IsEnabled;
            CatalogLoader.SetPackEnabled(pack, newState);
            LoadCatalog($"Pack « {pack.Name} » {(newState ? "activé" : "désactivé")}.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Modification impossible : {ex.Message}";
        }
    }

    private void OnEditSelectedPackClick(object sender, RoutedEventArgs e)
    {
        CatalogPackInfo? pack = _selectedPack;
        if (pack is null || pack.IsReadOnly) return;
        OpenPackEditor(pack.SourcePath, pack.Name);
    }

    private void OnCreatePackClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PackCreationDialog(
            "Créer un pack JC Lib",
            "Un pack de démarrage library-first sera créé avec une bibliothèque et une catégorie vides.",
            "jclib.custom.pack",
            "JC Lib Custom Pack",
            "1.0.0",
            "jclib_custom_pack.json")
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true || dialog.Request is null) return;

        try
        {
            PackCreationRequest request = dialog.Request;
            string path = CatalogLoader.CreatePack(request.TargetKind, request.Id, request.Name, request.Version, request.FileName);
            LoadCatalog($"Pack « {request.Name} » créé.");
            OpenPackEditor(path, request.Name);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Création du pack impossible : {ex.Message}";
        }
    }

    private void OnDuplicateSelectedPackClick(object sender, RoutedEventArgs e)
    {
        CatalogPackInfo? pack = _selectedPack;
        if (pack is null) return;
        string safeName = new string(pack.Name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray());
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "jclib_pack";
        var dialog = new PackCreationDialog(
            "Dupliquer un pack JC Lib",
            $"Une copie éditable sera créée à partir de « {pack.Name} » ({pack.SourceLabel}).",
            pack.Id + ".copy",
            pack.Name + " Copy",
            pack.Version,
            safeName + "_copy.json")
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true || dialog.Request is null) return;

        try
        {
            PackCreationRequest request = dialog.Request;
            string path = CatalogLoader.DuplicatePack(pack, request.TargetKind, request.Id, request.Name, request.Version, request.FileName);
            LoadCatalog($"Pack « {pack.Name} » dupliqué vers « {request.Name} ».");
            OpenPackEditor(path, request.Name);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Duplication du pack impossible : {ex.Message}";
        }
    }

    private void OpenPackEditor(string path, string packName)
    {
        try
        {
            StopWatchingPackDirectories();
            var editor = new PackEditorWindow(path)
            {
                Owner = Window.GetWindow(this),
            };
            editor.ShowDialog();

            LoadCatalog(editor.WasSaved
                ? $"Pack « {packName} » sauvegardé puis rechargé."
                : $"Éditeur fermé pour « {packName} ». Aucune sauvegarde détectée.");
        }
        catch (Exception ex)
        {
            StartWatchingPackDirectories();
            StatusText.Text = $"Ouverture du Visual Pack Editor impossible : {ex.Message}";
        }
    }

    private void OnDeleteSelectedPackClick(object sender, RoutedEventArgs e)
    {
        CatalogPackInfo? pack = _selectedPack;
        if (pack is null) return;

        MessageBoxResult answer = MessageBox.Show(
            $"Supprimer définitivement ce pack externe ?\n\n{pack.SourcePath}",
            "JC Lib — suppression de pack",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            CatalogLoader.DeletePack(pack);
            LoadCatalog($"Pack « {pack.Name} » supprimé.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Suppression impossible : {ex.Message}";
        }
    }

    private void OnOpenSelectedPackFolderClick(object sender, RoutedEventArgs e)
    {
        CatalogPackInfo? pack = _selectedPack;
        if (pack is null || pack.IsReadOnly) return;

        try
        {
            string? directory = Path.GetDirectoryName(pack.SourcePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                StatusText.Text = "Dossier du pack introuvable.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directory}\"",
                UseShellExecute = true,
            });
            StatusText.Text = $"Ouverture du dossier : {directory}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ouverture du dossier impossible : {ex.Message}";
        }
    }

    private void OnReloadCatalogClick(object sender, RoutedEventArgs e)
    {
        LoadCatalog("Catalogues rechargés manuellement.");
    }

    private void OnImportGlobalPackClick(object sender, RoutedEventArgs e)
    {
        ImportPack(CatalogPackSourceKind.GlobalUser);
    }

    private void OnImportSolutionPackClick(object sender, RoutedEventArgs e)
    {
        ImportPack(CatalogPackSourceKind.Solution);
    }

    private void OnOpenGlobalPacksFolderClick(object sender, RoutedEventArgs e)
    {
        OpenPacksFolder(CatalogPackSourceKind.GlobalUser);
    }

    private void OnOpenSolutionPacksFolderClick(object sender, RoutedEventArgs e)
    {
        OpenPacksFolder(CatalogPackSourceKind.Solution);
    }

    private void OnAutoReloadChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (AutoReloadCheckBox.IsChecked == true)
        {
            StartWatchingPackDirectories();
            StatusText.Text = "Rechargement automatique activé.";
        }
        else
        {
            StopWatchingPackDirectories();
            _reloadDebounceTimer.Stop();
            StatusText.Text = "Rechargement automatique désactivé.";
        }
    }

    private void OnIncludeBundledPackChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _preferences.IncludeBundledPack = IncludeBundledPackCheckBox.IsChecked == true;
        SavePreferences();
        LoadCatalog(_preferences.IncludeBundledPack
            ? "Pack embarqué fallback inclus."
            : "Pack embarqué fallback exclu. Seuls les packs externes sont utilisés.");
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        SearchTextBox.Focus();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        ApplySearch();
    }

    private void OnCatalogTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is CatalogNode node && node.Entry is not null) ShowEntry(node.Entry);
    }

    private void OnSearchResultSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowEntry(SearchResultsListBox.SelectedItem as CatalogEntry);
    }

    private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        QuickInsertEntry(SearchResultsListBox.SelectedItem as CatalogEntry);
    }

    private void OnCopySnippetClick(object sender, RoutedEventArgs e)
    {
        CopySelectedSnippet();
    }

    private void OnExecuteSnippetClick(object sender, RoutedEventArgs e)
    {
        ExecuteSelectedSnippetInConsole();
    }

    private void OnInsertSnippetClick(object sender, RoutedEventArgs e)
    {
        string snippet = GetGeneratedSnippet();
        if (_selectedEntry is null || string.IsNullOrWhiteSpace(snippet))
        {
            StatusText.Text = "Aucun snippet insérable n'est sélectionné.";
            return;
        }

        try
        {
            EditorInsertionResult result = EditorInsertionService.InsertSnippet(snippet);
            if (result.Success) RecordRecent(_selectedEntry);
            StatusText.Text = result.Success
                ? $"Snippet « {_selectedEntry.Name} » inséré. {result.Message}"
                : result.Message;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Insertion impossible : {ex.Message}";
        }
    }

    private void OnParameterTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_buildingParameterUi) return;
        RefreshGeneratedSnippet();
    }

    private void OnReturnTargetTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_buildingParameterUi) return;
        RefreshGeneratedSnippet();
    }

    private void OnSuggestionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_buildingParameterUi || sender is not ComboBox combo || combo.Tag is not ParameterEditorBinding binding || combo.SelectedItem is not CatalogChoice choice)
        {
            return;
        }
        binding.TextBox.Text = choice.Value;
        combo.SelectedIndex = -1;
    }

    private void OnStructuredPickerClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ParameterEditorBinding binding) return;
        CatalogPickerConfig? config = SnippetParameterService.CreateEffectivePickerConfig(binding.ParameterValue);
        if (config is null) return;

        var dialog = new StructuredChoiceDialog(config, binding.TextBox.Text)
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() == true)
        {
            binding.TextBox.Text = dialog.SelectedValue;
            int linkedIndex = config.DefaultTargetIndex;
            if (config.ApplyDefaultIfEmpty && linkedIndex >= 0 && linkedIndex < _parameterEditors.Count && dialog.SelectedChoice is CatalogChoice selectedChoice && !string.IsNullOrWhiteSpace(selectedChoice.DefaultValue))
            {
                TextBox linkedTextBox = _parameterEditors[linkedIndex].TextBox;
                if (string.IsNullOrWhiteSpace(linkedTextBox.Text))
                {
                    linkedTextBox.Text = selectedChoice.DefaultValue;
                }
            }
        }
    }

    private void OnBrowseParameterClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ParameterEditorBinding binding) return;

        if (binding.ParameterValue.EditorType == "pathFile")
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Title = $"Choisir un fichier pour {binding.ParameterValue.Parameter.Name}",
                Filter = "Tous les fichiers (*.*)|*.*|Ressources CVI (*.uir)|*.uir",
            };
            if (dialog.ShowDialog() == true)
            {
                binding.TextBox.Text = SnippetParameterService.FormatPathForTemplate(_selectedEntry, binding.ParameterValue.Parameter, dialog.FileName);
            }
            return;
        }

        using (var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = $"Choisir un dossier pour {binding.ParameterValue.Parameter.Name}",
            ShowNewFolderButton = true,
        })
        {
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                binding.TextBox.Text = SnippetParameterService.FormatPathForTemplate(_selectedEntry, binding.ParameterValue.Parameter, dialog.SelectedPath);
            }
        }
    }

    private void OnResetParametersClick(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry is null) return;
        BuildParameterEditor(_selectedEntry);
        RefreshGeneratedSnippet();
        StatusText.Text = "Paramètres réinitialisés à partir du snippet du catalogue.";
    }

    private void OnFindSymbolClick(object sender, RoutedEventArgs e)
    {
        OpenFindSymbolDialog();
    }

    private void OnAppearanceClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AppearanceDialog(_preferences.Theme.Clone()) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true || dialog.SelectedTheme is null) return;
        _preferences.Theme = dialog.SelectedTheme;
        SavePreferences();
        ThemeService.ApplyTheme(this, _preferences.Theme);
        StatusText.Text = "Apparence appliquée. Le prochain Visual Pack Editor utilisera également ces couleurs.";
    }

    private void OnEnvironmentFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingFilters) return;
        _preferences.FilterEnvironment = EnvironmentFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        _updatingFilters = true;
        try { PopulateLibraryFilter(); } finally { _updatingFilters = false; }
        _preferences.FilterLibrary = LibraryFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        SavePreferences();
        RefreshNavigationTree(); ApplySearch();
    }

    private void OnLibraryFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingFilters) return;
        _preferences.FilterLibrary = LibraryFilterComboBox.SelectedItem as string ?? UserPreferencesStore.AllFilterLabel;
        SavePreferences(); RefreshNavigationTree(); ApplySearch();
    }

    private void OnFavoritesOnlyChanged(object sender, RoutedEventArgs e)
    {
        _preferences.FavoritesOnly = FavoritesOnlyCheckBox.IsChecked == true;
        SavePreferences(); RefreshNavigationTree(); ApplySearch();
    }

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
    {
        _preferences.FilterEnvironment = UserPreferencesStore.AllFilterLabel;
        _preferences.FilterLibrary = UserPreferencesStore.AllFilterLabel;
        _preferences.FavoritesOnly = false;
        PopulateFilters(); RefreshNavigationTree(); ApplySearch();
    }

    private void OnToggleFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry is null) return;
        bool enabled = UserPreferencesStore.ToggleFavorite(_preferences, _selectedEntry.CanonicalPath);
        SavePreferences(); RefreshQuickAccessLists();
        FavoriteToggleButton.Content = enabled ? "★ Retirer des favoris" : "☆ Ajouter aux favoris";
        if (FavoritesOnlyCheckBox.IsChecked == true) { RefreshNavigationTree(); ApplySearch(); }
        StatusText.Text = enabled ? $"« {_selectedEntry.Name} » ajouté aux favoris." : $"« {_selectedEntry.Name} » retiré des favoris.";
    }

    private void RecordRecent(CatalogEntry entry)
    {
        UserPreferencesStore.AddRecent(_preferences, entry.CanonicalPath);
        SavePreferences(); RefreshQuickAccessLists();
    }

    private void OnQuickAccessSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is CatalogEntry entry) ShowEntry(entry);
    }

    private void OnQuickAccessDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox) QuickInsertEntry(listBox.SelectedItem as CatalogEntry);
    }

    private void OnQuickInsertSelectedClick(object sender, RoutedEventArgs e) => QuickInsertEntry(_selectedEntry);

    private void QuickInsertEntry(CatalogEntry? entry)
    {
        if (entry is null) return;
        try
        {
            IReadOnlyList<SnippetParameterValue> values = SnippetParameterService.CreateEditorState(entry);
            string snippet = SnippetParameterService.BuildInsertText(entry, values, string.Empty);
            if (string.IsNullOrWhiteSpace(snippet)) { StatusText.Text = "Cette entrée ne contient aucun snippet insérable."; return; }
            EditorInsertionResult result = EditorInsertionService.InsertSnippet(snippet);
            if (result.Success) RecordRecent(entry);
            StatusText.Text = result.Success ? $"Insertion rapide de « {entry.Name} ». {result.Message}" : result.Message;
        }
        catch (Exception ex) { StatusText.Text = $"Insertion rapide impossible : {ex.Message}"; }
    }

    private sealed class ParameterEditorBinding
    {
        public ParameterEditorBinding(SnippetParameterValue parameterValue, TextBox textBox, Border container)
        {
            ParameterValue = parameterValue;
            TextBox = textBox;
            Container = container;
        }

        public SnippetParameterValue ParameterValue { get; }
        public TextBox TextBox { get; }
        public Border Container { get; }
        public List<Control> DependentControls { get; } = new List<Control>();

        public void SetEnabled(bool enabled)
        {
            TextBox.IsEnabled = enabled;
            foreach (Control control in DependentControls) control.IsEnabled = enabled;
            Container.Opacity = enabled ? 1.0 : 0.56;
        }
    }
}
