using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using JCLib.VisualStudio.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JCLib.VisualStudio.Services;

public static class CatalogLoader
{
    private const string EmbeddedPackResource = "JCLib.VisualStudio.Assets.Packs.default_pack.json";
    private const string BundledPackDisplayPath = "[embarqué] Assets/Packs/default_pack.json";
    private const string GlobalDirectoryName = "Packs";
    private const string SolutionRelativeDirectory = ".jclib\\packs";

    public static CatalogLoadResult LoadCatalog(bool includeBundledPack = false)
    {
        string globalDirectory = GetGlobalPacksDirectory(create: true);
        string? solutionDirectory = GetSolutionPacksDirectory(create: false);
        string disabledStateFile = PackStateStore.GetStateFilePath(createDirectory: true);
        ISet<string> disabledPaths = PackStateStore.LoadDisabledPaths();
        var packs = new List<CatalogPackInfo>();
        var issues = new List<CatalogLoadIssue>();

        // The bundled catalog remains available as an optional read-only fallback.
        // Normal daily use can rely exclusively on packs exported from the VS Code extension.
        if (includeBundledPack)
        {
            packs.Add(BuildPack(
                ReadBundledPack(),
                CatalogPackSourceKind.Bundled,
                BundledPackDisplayPath,
                isEnabled: true));
        }

        LoadExternalDirectory(globalDirectory, CatalogPackSourceKind.GlobalUser, disabledPaths, packs, issues);
        if (!string.IsNullOrWhiteSpace(solutionDirectory))
        {
            LoadExternalDirectory(solutionDirectory, CatalogPackSourceKind.Solution, disabledPaths, packs, issues);
        }

        CatalogPackInfo[] activePacks = packs
            .Where(pack => pack.IsEnabled)
            .OrderByDescending(pack => pack.Priority)
            .ThenBy(pack => pack.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        CatalogEntry[] allActiveEntries = activePacks
            .SelectMany(pack => pack.Entries)
            .ToArray();

        (CatalogEntry[] effectiveEntries, CatalogShadowedEntry[] shadowedEntries) = ResolveEntries(allActiveEntries);
        IReadOnlyList<CatalogConflict> conflicts = DetectConflicts(activePacks, allActiveEntries);
        CatalogNode root = BuildResolvedRoot(activePacks, effectiveEntries);

        return new CatalogLoadResult(
            root,
            effectiveEntries,
            allActiveEntries,
            shadowedEntries,
            packs,
            activePacks,
            conflicts,
            issues,
            globalDirectory,
            solutionDirectory,
            disabledStateFile,
            includeBundledPack);
    }

    public static PackValidationResult ValidatePackFile(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Le chemin du pack est vide.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Le fichier JSON sélectionné est introuvable.", sourcePath);
        }

        CatalogPackInfo pack = BuildPack(
            File.ReadAllText(sourcePath, Encoding.UTF8),
            CatalogPackSourceKind.GlobalUser,
            sourcePath,
            isEnabled: true);

        return new PackValidationResult(pack.Id, pack.Name, pack.Version, pack.Entries.Count);
    }

    public static string ImportPack(string sourcePath, CatalogPackSourceKind targetKind, bool overwrite)
    {
        if (targetKind == CatalogPackSourceKind.Bundled)
        {
            throw new ArgumentException("Le catalogue embarqué est en lecture seule.", nameof(targetKind));
        }

        ValidatePackFile(sourcePath);

        string? targetDirectory = GetPackDirectory(targetKind, create: true);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException(
                "Aucune solution Visual Studio n'est ouverte. Ouvre une solution avant d'importer un pack local à la solution.");
        }

        string fileName = Path.GetFileName(sourcePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Le fichier sélectionné ne possède pas de nom exploitable.");
        }

        string destinationPath = Path.Combine(targetDirectory, fileName);
        string sourceFullPath = Path.GetFullPath(sourcePath);
        string destinationFullPath = Path.GetFullPath(destinationPath);
        if (!string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourceFullPath, destinationFullPath, overwrite);
        }

        // Imported packs are enabled by default, even when a previous file with the same path was disabled.
        PackStateStore.SetEnabled(destinationFullPath, isEnabled: true);
        return destinationFullPath;
    }


    public static string CreatePack(CatalogPackSourceKind targetKind, string id, string name, string version, string fileName)
    {
        string directory = RequireExternalDirectory(targetKind);
        string destinationPath = Path.Combine(directory, NormalizeFileName(fileName));
        string createdPath = PackEditorDocument.CreateStarterPack(destinationPath, id, name, version);
        PackStateStore.SetEnabled(createdPath, isEnabled: true);
        return createdPath;
    }

    public static string DuplicatePack(CatalogPackInfo sourcePack, CatalogPackSourceKind targetKind, string id, string name, string version, string fileName)
    {
        if (sourcePack is null) throw new ArgumentNullException(nameof(sourcePack));
        string directory = RequireExternalDirectory(targetKind);
        string destinationPath = Path.Combine(directory, NormalizeFileName(fileName));
        string json = sourcePack.IsReadOnly
            ? ReadBundledPack()
            : File.ReadAllText(sourcePack.SourcePath, Encoding.UTF8);

        JObject root = JObject.Parse(json);
        root["id"] = (id ?? string.Empty).Trim();
        root["name"] = (name ?? string.Empty).Trim();
        root["version"] = (version ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(root["id"]?.Value<string>())) throw new ArgumentException("L'identifiant du pack est obligatoire.", nameof(id));
        if (string.IsNullOrWhiteSpace(root["name"]?.Value<string>())) throw new ArgumentException("Le nom du pack est obligatoire.", nameof(name));
        if (string.IsNullOrWhiteSpace(root["version"]?.Value<string>())) throw new ArgumentException("La version du pack est obligatoire.", nameof(version));
        if (File.Exists(destinationPath)) throw new IOException($"Le fichier existe déjà : {destinationPath}");
        File.WriteAllText(destinationPath, root.ToString(Formatting.Indented), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        PackStateStore.SetEnabled(destinationPath, isEnabled: true);
        return destinationPath;
    }

    private static string RequireExternalDirectory(CatalogPackSourceKind targetKind)
    {
        if (targetKind == CatalogPackSourceKind.Bundled) throw new ArgumentException("Le catalogue embarqué est en lecture seule.", nameof(targetKind));
        string? directory = GetPackDirectory(targetKind, create: true);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Aucune solution Visual Studio n'est ouverte. Ouvre une solution avant de créer un pack local à la solution.");
        }
        return directory;
    }

    private static string NormalizeFileName(string fileName)
    {
        string normalized = (fileName ?? string.Empty).Trim();
        if (!normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) normalized += ".json";
        if (string.IsNullOrWhiteSpace(normalized) || !string.Equals(normalized, Path.GetFileName(normalized), StringComparison.Ordinal))
        {
            throw new ArgumentException("Le nom du fichier doit être un nom JSON simple, sans chemin de dossier.", nameof(fileName));
        }
        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Le nom du fichier contient un caractère interdit.", nameof(fileName));
        return normalized;
    }

    public static void SetPackEnabled(CatalogPackInfo pack, bool isEnabled)
    {
        if (pack is null) throw new ArgumentNullException(nameof(pack));
        if (pack.IsReadOnly)
        {
            throw new InvalidOperationException("Le pack embarqué est en lecture seule. Utilise l’option « Inclure le pack embarqué » pour l’afficher ou le masquer.");
        }

        PackStateStore.SetEnabled(pack.SourcePath, isEnabled);
    }

    public static void DeletePack(CatalogPackInfo pack)
    {
        if (pack is null) throw new ArgumentNullException(nameof(pack));
        if (pack.IsReadOnly)
        {
            throw new InvalidOperationException("Le pack embarqué est en lecture seule et ne peut pas être supprimé.");
        }

        string path = Path.GetFullPath(pack.SourcePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        PackStateStore.RemovePath(path);
    }

    public static string GetGlobalPacksDirectory(bool create)
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string directory = Path.Combine(localApplicationData, "JCLib", "VisualStudio", GlobalDirectoryName);
        if (create)
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    public static string? GetSolutionPacksDirectory(bool create)
    {
        string? solutionDirectory = SolutionPathService.TryGetCurrentSolutionDirectory();
        if (string.IsNullOrWhiteSpace(solutionDirectory))
        {
            return null;
        }

        string directory = Path.Combine(solutionDirectory, SolutionRelativeDirectory);
        if (create)
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    public static string? GetPackDirectory(CatalogPackSourceKind targetKind, bool create)
    {
        switch (targetKind)
        {
            case CatalogPackSourceKind.GlobalUser:
                return GetGlobalPacksDirectory(create);
            case CatalogPackSourceKind.Solution:
                return GetSolutionPacksDirectory(create);
            default:
                return null;
        }
    }

    private static void LoadExternalDirectory(
        string directory,
        CatalogPackSourceKind sourceKind,
        ISet<string> disabledPaths,
        ICollection<CatalogPackInfo> packs,
        ICollection<CatalogLoadIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string fullPath = Path.GetFullPath(file);
                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                bool isEnabled = !disabledPaths.Contains(fullPath);
                packs.Add(BuildPack(json, sourceKind, fullPath, isEnabled));
            }
            catch (Exception ex)
            {
                issues.Add(new CatalogLoadIssue(file, ex.Message));
            }
        }
    }

    private static CatalogPackInfo BuildPack(
        string json,
        CatalogPackSourceKind sourceKind,
        string sourcePath,
        bool isEnabled)
    {
        PackDto pack = DeserializePack(json);
        string packName = Normalize(pack.Name, "JC Lib pack");
        string packId = Normalize(pack.Id, packName);
        string packVersion = Normalize(pack.Version, "version inconnue");
        var entries = new List<CatalogEntry>();
        var packNode = new CatalogNode(
            $"{packName} — v{packVersion} [{CatalogPackInfo.GetSourceLabel(sourceKind)}]",
            CatalogNodeKind.Pack);

        if (OrEmpty(pack.Libraries).Any())
        {
            foreach (LibraryDto library in OrEmpty(pack.Libraries))
            {
                AddVisibleLibrary(library, packNode, entries, packId, packName, packVersion, sourceKind, sourcePath);
            }
        }
        else
        {
            foreach (EnvironmentDto environment in OrEmpty(pack.Environments))
            {
                string environmentName = Normalize(environment.Name, "Environnement");
                bool syntheticRoot = string.Equals(environmentName, "General", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(environmentName, "Default", StringComparison.OrdinalIgnoreCase);
                if (syntheticRoot)
                {
                    foreach (LibraryDto library in OrEmpty(environment.Libraries))
                    {
                        AddVisibleLibrary(library, packNode, entries, packId, packName, packVersion, sourceKind, sourcePath);
                    }
                    continue;
                }

                var visibleLibraryNode = new CatalogNode(environmentName, CatalogNodeKind.Library);
                packNode.Children.Add(visibleLibraryNode);
                foreach (LibraryDto nestedLibrary in OrEmpty(environment.Libraries))
                {
                    string visibleCategoryName = Normalize(nestedLibrary.Name, "Catégorie");
                    var visibleCategoryNode = new CatalogNode(visibleCategoryName, CatalogNodeKind.Category);
                    visibleLibraryNode.Children.Add(visibleCategoryNode);
                    foreach (CategoryDto legacyCategory in OrEmpty(nestedLibrary.Categories))
                    {
                        string groupName = Normalize(legacyCategory.Name, "Groupe");
                        var groupNode = new CatalogNode(groupName, CatalogNodeKind.Group);
                        visibleCategoryNode.Children.Add(groupNode);
                        AddFunctions(legacyCategory.Functions, groupNode, entries, packId, packName, packVersion,
                            sourceKind, sourcePath, string.Empty, environmentName, visibleCategoryName, groupName);
                        AddGroups(legacyCategory.Groups, groupNode, entries, packId, packName, packVersion,
                            sourceKind, sourcePath, string.Empty, environmentName, visibleCategoryName, groupName);
                    }
                }
            }
        }

        if (entries.Count == 0 && sourceKind == CatalogPackSourceKind.Bundled)
        {
            throw new InvalidDataException($"Le pack embarqué « {packName} » ne contient aucun élément exploitable.");
        }

        return new CatalogPackInfo(packId, packName, packVersion, sourceKind, sourcePath, packNode, entries, isEnabled);
    }

    private static void AddVisibleLibrary(
        LibraryDto library,
        CatalogNode packNode,
        ICollection<CatalogEntry> entries,
        string packId,
        string packName,
        string packVersion,
        CatalogPackSourceKind sourceKind,
        string sourcePath)
    {
        string libraryName = Normalize(library.Name, "Bibliothèque");
        var libraryNode = new CatalogNode(libraryName, CatalogNodeKind.Library);
        packNode.Children.Add(libraryNode);
        foreach (CategoryDto category in OrEmpty(library.Categories))
        {
            string categoryName = Normalize(category.Name, "Catégorie");
            var categoryNode = new CatalogNode(categoryName, CatalogNodeKind.Category);
            libraryNode.Children.Add(categoryNode);
            AddFunctions(category.Functions, categoryNode, entries, packId, packName, packVersion,
                sourceKind, sourcePath, string.Empty, libraryName, categoryName, string.Empty);
            AddGroups(category.Groups, categoryNode, entries, packId, packName, packVersion,
                sourceKind, sourcePath, string.Empty, libraryName, categoryName, string.Empty);
        }
    }

    private static (CatalogEntry[] EffectiveEntries, CatalogShadowedEntry[] ShadowedEntries) ResolveEntries(
        IReadOnlyList<CatalogEntry> activeEntries)
    {
        var effective = new List<CatalogEntry>();
        var shadowed = new List<CatalogShadowedEntry>();

        foreach (IGrouping<string, CatalogEntry> group in activeEntries
                     .GroupBy(entry => entry.CanonicalPath, StringComparer.OrdinalIgnoreCase))
        {
            CatalogEntry[] ordered = group
                .OrderByDescending(entry => entry.PackPriority)
                .ThenBy(entry => entry.PackSourcePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.PackName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            CatalogEntry winner = ordered[0];
            effective.Add(winner);
            foreach (CatalogEntry hidden in ordered.Skip(1))
            {
                shadowed.Add(new CatalogShadowedEntry(hidden, winner));
            }
        }

        return (
            effective.OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray(),
            shadowed.OrderBy(item => item.ShadowedEntry.Path, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static CatalogNode BuildResolvedRoot(
        IReadOnlyList<CatalogPackInfo> activePacks,
        IReadOnlyList<CatalogEntry> effectiveEntries)
    {
        var root = new CatalogNode("JC Lib — catalogue résolu", CatalogNodeKind.Root);

        foreach (CatalogPackInfo pack in activePacks)
        {
            CatalogEntry[] entries = effectiveEntries
                .Where(entry => string.Equals(entry.PackSourcePath, pack.SourcePath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (entries.Length == 0)
            {
                continue;
            }

            var packNode = new CatalogNode(
                $"{pack.Name} — v{pack.Version} [{pack.SourceLabel}]",
                CatalogNodeKind.Pack);
            root.Children.Add(packNode);

            foreach (CatalogEntry entry in entries)
            {
                CatalogNode hierarchyParent = packNode;
                if (!string.IsNullOrWhiteSpace(entry.Environment))
                {
                    hierarchyParent = GetOrAddChild(packNode, entry.Environment, CatalogNodeKind.Environment);
                }
                CatalogNode library = GetOrAddChild(hierarchyParent, entry.Library, CatalogNodeKind.Library);
                CatalogNode category = GetOrAddChild(library, entry.Category, CatalogNodeKind.Category);
                CatalogNode parent = category;
                foreach (string groupName in SplitGroupPath(entry.Group))
                {
                    parent = GetOrAddChild(parent, groupName, CatalogNodeKind.Group);
                }
                parent.Children.Add(new CatalogNode(entry.Name, CatalogNodeKind.Element, entry));
            }
        }

        return root;
    }

    private static CatalogNode GetOrAddChild(CatalogNode parent, string name, CatalogNodeKind kind)
    {
        string normalized = Normalize(name, kind.ToString());
        CatalogNode? existing = parent.Children.FirstOrDefault(child =>
            child.Kind == kind && string.Equals(child.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var node = new CatalogNode(normalized, kind);
        parent.Children.Add(node);
        return node;
    }

    private static IEnumerable<string> SplitGroupPath(string groupPath)
    {
        return (groupPath ?? string.Empty)
            .Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0);
    }

    private static IReadOnlyList<CatalogConflict> DetectConflicts(
        IReadOnlyList<CatalogPackInfo> activePacks,
        IReadOnlyList<CatalogEntry> activeEntries)
    {
        var conflicts = new List<CatalogConflict>();

        foreach (IGrouping<string, CatalogPackInfo> group in activePacks
                     .GroupBy(pack => pack.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            CatalogPackInfo[] ordered = OrderPacksByPriority(group).ToArray();
            conflicts.Add(new CatalogConflict(
                CatalogConflictKind.DuplicatePackId,
                group.Key,
                $"Identifiant de pack dupliqué : {group.Key}",
                ordered,
                ordered[0]));
        }

        foreach (IGrouping<string, CatalogEntry> group in activeEntries
                     .GroupBy(entry => entry.CanonicalPath, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            CatalogEntry[] orderedEntries = group
                .OrderByDescending(entry => entry.PackPriority)
                .ThenBy(entry => entry.PackSourcePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            CatalogPackInfo[] relatedPacks = orderedEntries
                .Select(entry => activePacks.First(pack =>
                    string.Equals(pack.SourcePath, entry.PackSourcePath, StringComparison.OrdinalIgnoreCase)))
                .Distinct()
                .ToArray();
            CatalogEntry winnerEntry = orderedEntries[0];
            CatalogPackInfo winnerPack = relatedPacks[0];

            conflicts.Add(new CatalogConflict(
                CatalogConflictKind.DuplicateElementPath,
                winnerEntry.Path,
                $"Élément présent dans plusieurs packs : {winnerEntry.Path}",
                relatedPacks,
                winnerPack));
        }

        return conflicts;
    }

    private static IEnumerable<CatalogPackInfo> OrderPacksByPriority(IEnumerable<CatalogPackInfo> packs)
    {
        return packs
            .OrderByDescending(pack => pack.Priority)
            .ThenBy(pack => pack.SourcePath, StringComparer.OrdinalIgnoreCase);
    }

    private static PackDto DeserializePack(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<PackDto>(json)
                ?? throw new InvalidDataException("Le fichier JSON ne contient pas un objet pack valide.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Le fichier JSON ne respecte pas le schéma JC Lib attendu.", ex);
        }
    }

    private static IReadOnlyList<CatalogChoice> ParseChoices(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null) return Array.Empty<CatalogChoice>();
        IEnumerable<JToken> values = token is JArray array ? array : new[] { token };
        return values
            .Select(ParseChoice)
            .Where(choice => choice is not null && (!string.IsNullOrWhiteSpace(choice.Value) || !string.IsNullOrWhiteSpace(choice.Label) || !string.IsNullOrWhiteSpace(choice.Description)))
            .Cast<CatalogChoice>()
            .GroupBy(choice => choice.Value.Trim(), StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static CatalogChoice? ParseChoice(JToken token)
    {
        if (token.Type == JTokenType.String || token.Type == JTokenType.Integer || token.Type == JTokenType.Float || token.Type == JTokenType.Boolean)
        {
            string value = token.ToString();
            return new CatalogChoice { Value = value, Label = value };
        }

        if (token is not JObject item) return null;
        bool hasExplicitValue = item.TryGetValue("value", out JToken? valueToken);
        string valueText = hasExplicitValue
            ? valueToken?.Value<string>() ?? string.Empty
            : FirstNonEmpty(
                item["constant"]?.Value<string>() ?? string.Empty,
                item["label"]?.Value<string>() ?? string.Empty);
        string labelText = item["label"]?.Value<string>() ?? string.Empty;
        string descriptionText = item["description"]?.Value<string>() ?? string.Empty;
        if (!hasExplicitValue && string.IsNullOrWhiteSpace(valueText)) return null;
        if (hasExplicitValue && string.IsNullOrWhiteSpace(valueText) && string.IsNullOrWhiteSpace(labelText) && string.IsNullOrWhiteSpace(descriptionText)) return null;
        return new CatalogChoice
        {
            Value = valueText.Trim(),
            Label = Normalize(labelText, valueText),
            Description = Normalize(item["description"]?.Value<string>(), string.Empty, trim: false),
            Detail = Normalize(item["detail"]?.Value<string>(), string.Empty, trim: false),
            DefaultValue = Normalize(item["defaultValue"]?.Value<string>(), string.Empty, trim: false),
            SourceTypes = ParseStringArray(item["sourceTypes"]),
            IncompatibleWith = ParseStringArray(item["incompatibleWith"]),
        };
    }

    private static CatalogPickerConfig? ParsePickerConfig(JToken? token)
    {
        if (token is not JObject picker) return null;
        CatalogPickerSection[] sections = OrEmpty(picker["sections"] as JArray)
            .OfType<JObject>()
            .Select(section => new CatalogPickerSection
            {
                Label = Normalize(section["label"]?.Value<string>(), "Choix"),
                Description = Normalize(section["description"]?.Value<string>(), string.Empty, trim: false),
                Groups = OrEmpty(section["groups"] as JArray)
                    .OfType<JObject>()
                    .Select(group => new CatalogPickerGroup
                    {
                        Label = Normalize(group["label"]?.Value<string>(), "Valeurs"),
                        Description = Normalize(group["description"]?.Value<string>(), string.Empty, trim: false),
                        Items = ParseChoices(group["items"]),
                    })
                    .Where(group => group.Items.Count > 0)
                    .ToArray(),
            })
            .Where(section => section.Groups.Count > 0)
            .ToArray();

        return new CatalogPickerConfig
        {
            Title = Normalize(picker["title"]?.Value<string>(), "Choisir une valeur"),
            SelectionLabel = Normalize(picker["selectionLabel"]?.Value<string>(), "Valeur sélectionnée"),
            Subtitle = Normalize(picker["subtitle"]?.Value<string>(), string.Empty, trim: false),
            SourceTypes = ParseStringArray(picker["sourceTypes"] ?? picker["controlTypes"]),
            Sections = sections,
            ApplyDefaultIfEmpty = picker["applyDefaultIfEmpty"]?.Value<bool?>() ?? true,
            MultiSelect = picker["multiSelect"]?.Value<bool?>() ?? false,
            ValueSeparator = Normalize(picker["valueSeparator"]?.Value<string>(), " | ", trim: false),
            EmptyValue = Normalize(picker["emptyValue"]?.Value<string>(), string.Empty, trim: false),
            DefaultTargetIndex = picker["defaultTargetIndex"]?.Value<int?>() ?? -1,
            MinimumSelections = Math.Max(0, picker["minimumSelections"]?.Value<int?>() ?? picker["minSelections"]?.Value<int?>() ?? 0),
            DefaultValue = Normalize(picker["defaultValue"]?.Value<string>(), string.Empty, trim: false),
            ValidationMessage = Normalize(picker["validationMessage"]?.Value<string>(), string.Empty, trim: false),
        };
    }

    private static CatalogEnabledWhen? ParseEnabledWhen(JToken? token)
    {
        if (token is not JObject condition) return null;
        string parameter = Normalize(condition["parameter"]?.Value<string>() ?? condition["parameterName"]?.Value<string>(), string.Empty);
        int? index = condition["index"]?.Value<int?>();
        bool notEmpty = condition["notEmpty"]?.Value<bool?>() ?? false;
        bool empty = condition["empty"]?.Value<bool?>() ?? false;
        string equals = Normalize(condition["equals"]?.Value<string>(), string.Empty, trim: false);
        string notEquals = Normalize(condition["notEquals"]?.Value<string>(), string.Empty, trim: false);
        IReadOnlyList<string> values = ParseStringArray(condition["values"]);
        if (string.IsNullOrWhiteSpace(parameter) && index is null && !notEmpty && !empty && string.IsNullOrEmpty(equals) && string.IsNullOrEmpty(notEquals) && values.Count == 0)
        {
            return null;
        }
        return new CatalogEnabledWhen
        {
            Parameter = parameter,
            Index = index,
            NotEmpty = notEmpty,
            Empty = empty,
            EqualsValue = equals,
            NotEqualsValue = notEquals,
            Values = values,
        };
    }

    private static IReadOnlyList<string> ParseStringArray(JToken? token)
    {
        if (token is not JArray array) return Array.Empty<string>();
        return array
            .Values<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddGroups(
        IEnumerable<GroupDto>? groups,
        CatalogNode parentNode,
        ICollection<CatalogEntry> entries,
        string packId,
        string packName,
        string packVersion,
        CatalogPackSourceKind sourceKind,
        string sourcePath,
        string environment,
        string library,
        string category,
        string parentGroupPath)
    {
        foreach (GroupDto group in OrEmpty(groups))
        {
            string groupName = Normalize(group.Name, "Groupe");
            string groupPath = string.IsNullOrWhiteSpace(parentGroupPath) ? groupName : $"{parentGroupPath} > {groupName}";
            var groupNode = new CatalogNode(groupName, CatalogNodeKind.Group);
            parentNode.Children.Add(groupNode);

            AddFunctions(group.Functions, groupNode, entries, packId, packName, packVersion, sourceKind,
                sourcePath, environment, library, category, groupPath);
            AddGroups(group.Groups, groupNode, entries, packId, packName, packVersion, sourceKind,
                sourcePath, environment, library, category, groupPath);
        }
    }

    private static void AddFunctions(
        IEnumerable<FunctionDto>? functions,
        CatalogNode parentNode,
        ICollection<CatalogEntry> entries,
        string packId,
        string packName,
        string packVersion,
        CatalogPackSourceKind sourceKind,
        string sourcePath,
        string environment,
        string library,
        string category,
        string groupPath)
    {
        foreach (FunctionDto function in OrEmpty(functions))
        {
            var entry = new CatalogEntry
            {
                Name = Normalize(function.Name, "Élément"),
                Environment = Normalize(function.Environment, environment),
                Library = Normalize(function.Library, library),
                Category = Normalize(function.Category, category),
                Group = groupPath,
                SymbolKind = Normalize(function.SymbolKind, "element"),
                ReturnType = Normalize(function.ReturnType, string.Empty),
                Signature = Normalize(function.Signature, string.Empty),
                Declaration = Normalize(function.Declaration, string.Empty),
                InsertText = Normalize(function.InsertText, string.Empty, trim: false),
                Description = FirstNonEmpty(
                    Normalize(function.LongDescription, string.Empty, trim: false),
                    Normalize(function.Description, string.Empty, trim: false)),
                Header = Normalize(function.Header, string.Empty),
                Parameters = OrEmpty(function.Parameters)
                    .Select(parameter => new CatalogParameter
                    {
                        Name = Normalize(parameter.Name, "param"),
                        Type = Normalize(parameter.Type, "int"),
                        Description = Normalize(parameter.Description, string.Empty, trim: false),
                        EditorType = Normalize(parameter.EditorType, string.Empty),
                        DefaultValue = Normalize(parameter.DefaultValue, string.Empty, trim: false),
                        HasExplicitDefaultValue = parameter.DefaultValue is not null,
                        Placeholder = Normalize(parameter.Placeholder, string.Empty, trim: false),
                        Optional = parameter.Optional,
                        Presets = ParseChoices(parameter.Presets),
                        Options = ParseChoices(parameter.Options),
                        PickerConfig = ParsePickerConfig(parameter.PickerConfig),
                        EnabledWhen = ParseEnabledWhen(parameter.EnabledWhen),
                    })
                    .ToArray(),
                PackId = packId,
                PackName = packName,
                PackVersion = packVersion,
                PackSourceKind = sourceKind,
                PackSourcePath = sourcePath,
            };

            entries.Add(entry);
            parentNode.Children.Add(new CatalogNode(entry.Name, CatalogNodeKind.Element, entry));
        }
    }

    private static string ReadBundledPack()
    {
        Assembly assembly = typeof(CatalogLoader).Assembly;
        using (Stream? stream = assembly.GetManifestResourceStream(EmbeddedPackResource))
        {
            if (stream is not null)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        string? assemblyDirectory = Path.GetDirectoryName(assembly.Location);
        if (!string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            string diskPath = Path.Combine(assemblyDirectory, "Assets", "Packs", "default_pack.json");
            if (File.Exists(diskPath))
            {
                return File.ReadAllText(diskPath, Encoding.UTF8);
            }
        }

        throw new FileNotFoundException(
            "Impossible de trouver Assets/Packs/default_pack.json dans les ressources embarquées ou à côté de l'extension.");
    }

    private static IEnumerable<T> OrEmpty<T>(IEnumerable<T>? values) => values ?? Enumerable.Empty<T>();

    private static string Normalize(string? value, string fallback, bool trim = true)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return trim ? value.Trim() : value;
    }

    private static string FirstNonEmpty(params string[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate)) ?? string.Empty;

    [DataContract]
    private sealed class PackDto
    {
        [DataMember(Name = "id")] public string? Id { get; set; }
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "version")] public string? Version { get; set; }
        [DataMember(Name = "environments")] public List<EnvironmentDto>? Environments { get; set; }
        [DataMember(Name = "libraries")] public List<LibraryDto>? Libraries { get; set; }
    }

    [DataContract]
    private sealed class EnvironmentDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "libraries")] public List<LibraryDto>? Libraries { get; set; }
    }

    [DataContract]
    private sealed class LibraryDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "categories")] public List<CategoryDto>? Categories { get; set; }
    }

    [DataContract]
    private sealed class CategoryDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "functions")] public List<FunctionDto>? Functions { get; set; }
        [DataMember(Name = "groups")] public List<GroupDto>? Groups { get; set; }
    }

    [DataContract]
    private sealed class GroupDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "functions")] public List<FunctionDto>? Functions { get; set; }
        [DataMember(Name = "groups")] public List<GroupDto>? Groups { get; set; }
    }

    [DataContract]
    private sealed class FunctionDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "environment")] public string? Environment { get; set; }
        [DataMember(Name = "library")] public string? Library { get; set; }
        [DataMember(Name = "category")] public string? Category { get; set; }
        [DataMember(Name = "symbolKind")] public string? SymbolKind { get; set; }
        [DataMember(Name = "returnType")] public string? ReturnType { get; set; }
        [DataMember(Name = "signature")] public string? Signature { get; set; }
        [DataMember(Name = "declaration")] public string? Declaration { get; set; }
        [DataMember(Name = "insertText")] public string? InsertText { get; set; }
        [DataMember(Name = "description")] public string? Description { get; set; }
        [DataMember(Name = "longDescription")] public string? LongDescription { get; set; }
        [DataMember(Name = "header")] public string? Header { get; set; }
        [DataMember(Name = "parameters")] public List<ParameterDto>? Parameters { get; set; }
    }

    [DataContract]
    private sealed class ParameterDto
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "type")] public string? Type { get; set; }
        [DataMember(Name = "description")] public string? Description { get; set; }
        [DataMember(Name = "editorType")] public string? EditorType { get; set; }
        [DataMember(Name = "defaultValue")] public string? DefaultValue { get; set; }
        [DataMember(Name = "placeholder")] public string? Placeholder { get; set; }
        [DataMember(Name = "optional")] public bool Optional { get; set; }
        [DataMember(Name = "presets")] public JToken? Presets { get; set; }
        [DataMember(Name = "options")] public JToken? Options { get; set; }
        [DataMember(Name = "pickerConfig")] public JToken? PickerConfig { get; set; }
        [DataMember(Name = "enabledWhen")] public JToken? EnabledWhen { get; set; }
    }
}
