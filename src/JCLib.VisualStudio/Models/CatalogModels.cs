using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JCLib.VisualStudio.Models;

public enum CatalogNodeKind
{
    Root,
    Pack,
    Environment,
    Library,
    Category,
    Group,
    Element,
}

public enum CatalogPackSourceKind
{
    Bundled,
    GlobalUser,
    Solution,
}

public enum CatalogConflictKind
{
    DuplicatePackId,
    DuplicateElementPath,
}

public sealed class CatalogNode : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    public CatalogNode(string name, CatalogNodeKind kind, CatalogEntry? entry = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "<sans nom>" : name.Trim();
        Kind = kind;
        Entry = entry;
    }

    public string Name { get; }

    public CatalogNodeKind Kind { get; }

    public CatalogEntry? Entry { get; }

    public ObservableCollection<CatalogNode> Children { get; } = new ObservableCollection<CatalogNode>();

    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded == value) return; _isExpanded = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); }
    }

    public string Header => Kind == CatalogNodeKind.Element
        ? Name
        : $"{Name} ({Children.Count})";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class CatalogChoice
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    public IReadOnlyList<string> SourceTypes { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> IncompatibleWith { get; set; } = Array.Empty<string>();

    public string DisplayLabel => string.IsNullOrWhiteSpace(Value) && !string.IsNullOrWhiteSpace(Label)
        ? Label
        : string.IsNullOrWhiteSpace(Label) || string.Equals(Label, Value, StringComparison.Ordinal)
            ? Value
            : $"{Label} — {Value}";

    public string HelpText => string.Join(" — ", new[] { Description, Detail }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class CatalogPickerGroup
{
    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<CatalogChoice> Items { get; set; } = Array.Empty<CatalogChoice>();
}

public sealed class CatalogPickerSection
{
    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<CatalogPickerGroup> Groups { get; set; } = Array.Empty<CatalogPickerGroup>();
}

public sealed class CatalogPickerConfig
{
    public string Title { get; set; } = string.Empty;

    public string SelectionLabel { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public IReadOnlyList<string> SourceTypes { get; set; } = Array.Empty<string>();

    public IReadOnlyList<CatalogPickerSection> Sections { get; set; } = Array.Empty<CatalogPickerSection>();

    public bool ApplyDefaultIfEmpty { get; set; }

    public bool MultiSelect { get; set; }

    public string ValueSeparator { get; set; } = " | ";

    public string EmptyValue { get; set; } = string.Empty;

    public int DefaultTargetIndex { get; set; } = -1;

    public int MinimumSelections { get; set; }

    public string DefaultValue { get; set; } = string.Empty;

    public string ValidationMessage { get; set; } = string.Empty;

    public IReadOnlyList<CatalogChoice> FlattenChoices() => Sections
        .SelectMany(section => section.Groups)
        .SelectMany(group => group.Items)
        .Where(item => !string.IsNullOrWhiteSpace(item.Value) || !string.IsNullOrWhiteSpace(item.Label) || !string.IsNullOrWhiteSpace(item.Description))
        .GroupBy(item => item.Value, StringComparer.Ordinal)
        .Select(group => group.First())
        .ToArray();
}

public sealed class CatalogEnabledWhen
{
    public string Parameter { get; set; } = string.Empty;

    public int? Index { get; set; }

    public bool NotEmpty { get; set; }

    public bool Empty { get; set; }

    public string EqualsValue { get; set; } = string.Empty;

    public string NotEqualsValue { get; set; } = string.Empty;

    public IReadOnlyList<string> Values { get; set; } = Array.Empty<string>();
}

public sealed class CatalogParameter
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EditorType { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    public bool HasExplicitDefaultValue { get; set; }

    public string Placeholder { get; set; } = string.Empty;

    public bool Optional { get; set; }

    public IReadOnlyList<CatalogChoice> Presets { get; set; } = Array.Empty<CatalogChoice>();

    public IReadOnlyList<CatalogChoice> Options { get; set; } = Array.Empty<CatalogChoice>();

    public CatalogPickerConfig? PickerConfig { get; set; }

    public CatalogEnabledWhen? EnabledWhen { get; set; }
}

public sealed class CatalogEntry
{
    public string Name { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string Library { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string SymbolKind { get; set; } = string.Empty;

    public string ReturnType { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string Declaration { get; set; } = string.Empty;

    public string InsertText { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Header { get; set; } = string.Empty;

    public IReadOnlyList<CatalogParameter> Parameters { get; set; } = Array.Empty<CatalogParameter>();

    public string PackId { get; set; } = string.Empty;

    public string PackName { get; set; } = string.Empty;

    public string PackVersion { get; set; } = string.Empty;

    public CatalogPackSourceKind PackSourceKind { get; set; }

    public string PackSourcePath { get; set; } = string.Empty;

    public int PackPriority => CatalogPackInfo.GetSourcePriority(PackSourceKind);

    public bool IsCallable => new[] { "function", "method", "metamethod", "macro", "command" }
        .Contains((SymbolKind ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

    // Compatibility alias retained for older UI code and external integrations.
    public bool IsFunction => IsCallable;

    public bool HasReturnValue => IsCallable &&
        !string.Equals((SymbolKind ?? string.Empty).Trim(), "command", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(ReturnType) &&
        !string.Equals(ReturnType.Trim(), "void", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(ReturnType.Trim(), "none", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(ReturnType.Trim(), "snippet", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(ReturnType.Trim(), "keyword", StringComparison.OrdinalIgnoreCase);

    public string Path
    {
        get
        {
            var parts = new[] { Environment, Library, Category, Group, Name }
                .Where(part => !string.IsNullOrWhiteSpace(part));
            return string.Join(" > ", parts);
        }
    }

    public string CanonicalPath => string.Join("\u001f", new[]
    {
        Environment,
        Library,
        Category,
        Group,
        Name,
    }.Select(value => value.Trim().ToUpperInvariant()));

    public string PackSourceLabel => CatalogPackInfo.GetSourceLabel(PackSourceKind);

    public string SourceDisplay => $"{PackName} v{PackVersion} — {PackSourceLabel}";

    public string SearchText => string.Join("\n", new[]
    {
        Name,
        Environment,
        Library,
        Category,
        Group,
        SymbolKind,
        ReturnType,
        Signature,
        Declaration,
        InsertText,
        Description,
        Header,
        PackId,
        PackName,
        PackVersion,
        PackSourceLabel,
        string.Join(" ", Parameters.Select(parameter => $"{parameter.Type} {parameter.Name} {parameter.EditorType} {parameter.Description} {parameter.Placeholder} " +
            string.Join(" ", parameter.Options.Concat(parameter.Presets).Select(choice => $"{choice.Value} {choice.Label} {choice.Description} {choice.Detail}")))),
    }).ToUpperInvariant();

    public string SearchDisplay => string.IsNullOrWhiteSpace(Signature)
        ? $"{Path}\n{SourceDisplay}"
        : $"{Path}\n{Signature}\n{SourceDisplay}";

    public string QuickDisplay => $"{Name} — {Path}";

    public override string ToString() => SearchDisplay;
}

public sealed class CatalogPackInfo
{
    public CatalogPackInfo(
        string id,
        string name,
        string version,
        CatalogPackSourceKind sourceKind,
        string sourcePath,
        CatalogNode root,
        IReadOnlyList<CatalogEntry> entries,
        bool isEnabled)
    {
        Id = id;
        Name = name;
        Version = version;
        SourceKind = sourceKind;
        SourcePath = sourcePath;
        Root = root;
        Entries = entries;
        IsEnabled = isEnabled;
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public CatalogPackSourceKind SourceKind { get; }

    public string SourcePath { get; }

    public CatalogNode Root { get; }

    public IReadOnlyList<CatalogEntry> Entries { get; }

    public bool IsEnabled { get; }

    public bool IsReadOnly => SourceKind == CatalogPackSourceKind.Bundled;

    public int Priority => GetSourcePriority(SourceKind);

    public string SourceLabel => GetSourceLabel(SourceKind);

    public string StateLabel => IsEnabled ? "actif" : "désactivé";

    public string DisplayLabel => $"{Name} v{Version} — {SourceLabel} — {StateLabel} — priorité {Priority} — {Entries.Count:N0} élément(s)";

    public string ManagementDisplay => $"[{(IsEnabled ? "ACTIF" : "INACTIF")}] {Name} v{Version} — {SourceLabel} — priorité {Priority} — {Entries.Count:N0} élément(s)";

    public static string GetSourceLabel(CatalogPackSourceKind kind)
    {
        switch (kind)
        {
            case CatalogPackSourceKind.Bundled:
                return "embarqué";
            case CatalogPackSourceKind.GlobalUser:
                return "global utilisateur";
            case CatalogPackSourceKind.Solution:
                return "solution";
            default:
                return "inconnu";
        }
    }

    public static int GetSourcePriority(CatalogPackSourceKind kind)
    {
        switch (kind)
        {
            case CatalogPackSourceKind.Solution:
                return 300;
            case CatalogPackSourceKind.GlobalUser:
                return 200;
            case CatalogPackSourceKind.Bundled:
                return 100;
            default:
                return 0;
        }
    }
}

public sealed class CatalogShadowedEntry
{
    public CatalogShadowedEntry(CatalogEntry shadowedEntry, CatalogEntry winner)
    {
        ShadowedEntry = shadowedEntry;
        Winner = winner;
    }

    public CatalogEntry ShadowedEntry { get; }

    public CatalogEntry Winner { get; }

    public string Message => $"{ShadowedEntry.Path} | masqué={ShadowedEntry.SourceDisplay} | prioritaire={Winner.SourceDisplay}";
}

public sealed class CatalogConflict
{
    public CatalogConflict(
        CatalogConflictKind kind,
        string key,
        string message,
        IReadOnlyList<CatalogPackInfo> packs,
        CatalogPackInfo? winner = null)
    {
        Kind = kind;
        Key = key;
        Message = message;
        Packs = packs;
        Winner = winner;
    }

    public CatalogConflictKind Kind { get; }

    public string Key { get; }

    public string Message { get; }

    public IReadOnlyList<CatalogPackInfo> Packs { get; }

    public CatalogPackInfo? Winner { get; }
}

public sealed class CatalogLoadIssue
{
    public CatalogLoadIssue(string sourcePath, string message)
    {
        SourcePath = sourcePath;
        Message = message;
    }

    public string SourcePath { get; }

    public string Message { get; }
}

public sealed class CatalogLoadResult
{
    public CatalogLoadResult(
        CatalogNode root,
        IReadOnlyList<CatalogEntry> entries,
        IReadOnlyList<CatalogEntry> allActiveEntries,
        IReadOnlyList<CatalogShadowedEntry> shadowedEntries,
        IReadOnlyList<CatalogPackInfo> packs,
        IReadOnlyList<CatalogPackInfo> activePacks,
        IReadOnlyList<CatalogConflict> conflicts,
        IReadOnlyList<CatalogLoadIssue> issues,
        string globalPacksDirectory,
        string? solutionPacksDirectory,
        string disabledPacksStateFile,
        bool includeBundledPack)
    {
        Root = root;
        Entries = entries;
        AllActiveEntries = allActiveEntries;
        ShadowedEntries = shadowedEntries;
        Packs = packs;
        ActivePacks = activePacks;
        Conflicts = conflicts;
        Issues = issues;
        GlobalPacksDirectory = globalPacksDirectory;
        SolutionPacksDirectory = solutionPacksDirectory;
        DisabledPacksStateFile = disabledPacksStateFile;
        IncludeBundledPack = includeBundledPack;
    }

    public CatalogNode Root { get; }

    /// <summary>Entries visible in navigation and search after priority resolution.</summary>
    public IReadOnlyList<CatalogEntry> Entries { get; }

    /// <summary>All entries from enabled packs before priority resolution.</summary>
    public IReadOnlyList<CatalogEntry> AllActiveEntries { get; }

    public IReadOnlyList<CatalogShadowedEntry> ShadowedEntries { get; }

    public IReadOnlyList<CatalogPackInfo> Packs { get; }

    public IReadOnlyList<CatalogPackInfo> ActivePacks { get; }

    public IReadOnlyList<CatalogConflict> Conflicts { get; }

    public IReadOnlyList<CatalogLoadIssue> Issues { get; }

    public string GlobalPacksDirectory { get; }

    public string? SolutionPacksDirectory { get; }

    public string DisabledPacksStateFile { get; }

    public bool IncludeBundledPack { get; }
}

public sealed class PackValidationResult
{
    public PackValidationResult(string id, string name, string version, int entryCount)
    {
        Id = id;
        Name = name;
        Version = version;
        EntryCount = entryCount;
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public int EntryCount { get; }
}
