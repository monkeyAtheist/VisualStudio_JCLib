using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using JCLib.VisualStudio.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JCLib.VisualStudio.Services;

public sealed class PackEditorDocument
{
    private readonly JObject _root;
    private readonly HashSet<JObject> _batchSelectedObjects = new HashSet<JObject>(ReferenceEqualityComparer<JObject>.Instance);
    private readonly HashSet<JObject> _expandedObjects = new HashSet<JObject>(ReferenceEqualityComparer<JObject>.Instance);
    private JObject? _selectedObject;

    private PackEditorDocument(string filePath, JObject root)
    {
        FilePath = Path.GetFullPath(filePath);
        _root = root;
        NormalizeLibraryFirstInputForEditing(_root);
        RebuildTree();
    }

    public string FilePath { get; }

    public bool IsDirty { get; private set; }

    public ObservableCollection<PackEditorNode> RootNodes { get; } = new ObservableCollection<PackEditorNode>();

    public string PackId => ReadString(_root, "id");

    public string PackName => ReadString(_root, "name");

    public string PackVersion => ReadString(_root, "version");

    public static PackEditorDocument Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Le chemin du pack est vide.", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Le fichier JSON du pack est introuvable.", filePath);

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        JObject root = JObject.Parse(json);
        return new PackEditorDocument(filePath, root);
    }

    public static string CreateStarterPack(string filePath, string id, string name, string version)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Le chemin du pack est vide.", nameof(filePath));
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("L'identifiant du pack est obligatoire.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Le nom du pack est obligatoire.", nameof(name));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("La version du pack est obligatoire.", nameof(version));

        string fullPath = Path.GetFullPath(filePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Le dossier cible est introuvable.");
        Directory.CreateDirectory(directory);
        if (File.Exists(fullPath)) throw new IOException($"Le fichier existe déjà : {fullPath}");

        var root = new JObject
        {
            ["id"] = id.Trim(),
            ["name"] = name.Trim(),
            ["version"] = version.Trim(),
            ["libraries"] = new JArray
            {
                new JObject
                {
                    ["name"] = "Custom Library",
                    ["categories"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = "General",
                            ["functions"] = new JArray(),
                            ["groups"] = new JArray(),
                        },
                    },
                },
            },
        };

        File.WriteAllText(fullPath, root.ToString(Formatting.Indented), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return fullPath;
    }

    public static string DuplicatePack(string sourcePath, string destinationPath, string id, string name, string version)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Le chemin source est vide.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Le chemin cible est vide.", nameof(destinationPath));
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Le pack source est introuvable.", sourcePath);

        string destinationFullPath = Path.GetFullPath(destinationPath);
        string? directory = Path.GetDirectoryName(destinationFullPath);
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Le dossier cible est introuvable.");
        Directory.CreateDirectory(directory);
        if (File.Exists(destinationFullPath)) throw new IOException($"Le fichier existe déjà : {destinationFullPath}");

        JObject root = JObject.Parse(File.ReadAllText(sourcePath, Encoding.UTF8));
        SetString(root, "id", id);
        SetString(root, "name", name);
        SetString(root, "version", version);
        File.WriteAllText(destinationFullPath, root.ToString(Formatting.Indented), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return destinationFullPath;
    }

    public void SetPackMetadata(string id, string name, string version)
    {
        SetString(_root, "id", id);
        SetString(_root, "name", name);
        SetString(_root, "version", version);
        IsDirty = true;
        UpdatePackNodeHeader();
    }

    public void SetNodeName(PackEditorNode node, string value)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (node.Kind == PackEditorNodeKind.Pack) throw new InvalidOperationException("Le nom du pack se modifie dans les métadonnées générales.");
        SetString(node.JsonObject, "name", value);
        node.Name = value;
        SynchronizeElementMetadata();
        IsDirty = true;
    }

    public void SetElementProperty(PackEditorNode node, string propertyName, string value)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (node.Kind != PackEditorNodeKind.Element) throw new InvalidOperationException("Sélectionne un élément avant de modifier ce champ.");
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Le nom de propriété est vide.", nameof(propertyName));

        SetString(node.JsonObject, propertyName, value, trim: false);
        if (string.Equals(propertyName, "name", StringComparison.Ordinal))
        {
            node.Name = value;
        }
        IsDirty = true;
    }

    public PackEditorNode AddEnvironment()
    {
        JArray environments = EnsureArray(_root, "environments");
        string name = FindAvailableName(environments, "NewEnvironment");
        var json = new JObject
        {
            ["name"] = name,
            ["libraries"] = new JArray(),
        };
        environments.Add(json);
        IsDirty = true;
        RebuildTree();
        return FindNode(json) ?? throw new InvalidOperationException("L'environnement ajouté n'a pas pu être retrouvé.");
    }

    public PackEditorNode AddLibrary(PackEditorNode context)
    {
        PackEditorNode? environment = FindContextNode(context, PackEditorNodeKind.Environment);
        JObject environmentJson;
        if (environment is not null)
        {
            environmentJson = environment.JsonObject;
        }
        else if (context.Kind == PackEditorNodeKind.Pack && EnumerateObjects(_root, "environments").FirstOrDefault() is JObject rootEnvironment)
        {
            environmentJson = rootEnvironment;
        }
        else
        {
            throw new InvalidOperationException("Sélectionne le pack, une bibliothèque ou l'un de ses enfants avant d'ajouter une bibliothèque.");
        }
        JArray libraries = EnsureArray(environmentJson, "libraries");
        string name = FindAvailableName(libraries, "NewLibrary");
        var json = new JObject
        {
            ["name"] = name,
            ["categories"] = new JArray(),
        };
        libraries.Add(json);
        IsDirty = true;
        RebuildTree();
        return FindNode(json) ?? throw new InvalidOperationException("La bibliothèque ajoutée n'a pas pu être retrouvée.");
    }

    public PackEditorNode AddCategory(PackEditorNode context)
    {
        PackEditorNode library = FindContextNode(context, PackEditorNodeKind.Library)
            ?? throw new InvalidOperationException("Sélectionne une bibliothèque ou l'un de ses enfants avant d'ajouter une catégorie.");
        JArray categories = EnsureArray(library.JsonObject, "categories");
        string name = FindAvailableName(categories, "NewCategory");
        var json = new JObject
        {
            ["name"] = name,
            ["functions"] = new JArray(),
            ["groups"] = new JArray(),
        };
        categories.Add(json);
        IsDirty = true;
        RebuildTree();
        return FindNode(json) ?? throw new InvalidOperationException("La catégorie ajoutée n'a pas pu être retrouvée.");
    }

    public PackEditorNode AddGroup(PackEditorNode context)
    {
        PackEditorNode owner = ResolveFunctionContainer(context)
            ?? throw new InvalidOperationException("Sélectionne une catégorie, un groupe ou un élément avant d'ajouter un groupe.");
        JArray groups = EnsureArray(owner.JsonObject, "groups");
        string name = FindAvailableName(groups, "NewGroup");
        var json = new JObject
        {
            ["name"] = name,
            ["functions"] = new JArray(),
            ["groups"] = new JArray(),
        };
        groups.Add(json);
        IsDirty = true;
        RebuildTree();
        return FindNode(json) ?? throw new InvalidOperationException("Le groupe ajouté n'a pas pu être retrouvé.");
    }

    public PackEditorNode AddElement(PackEditorNode context)
    {
        PackEditorNode parentNode = ResolveFunctionContainer(context)
            ?? throw new InvalidOperationException("Sélectionne une catégorie, un groupe ou un élément existant avant d'ajouter un élément.");

        JArray functions = EnsureArray(parentNode.JsonObject, "functions");
        string newName = FindAvailableName(functions, "NewElement");
        string environment = FindAncestorName(parentNode, PackEditorNodeKind.Environment);
        string library = FindAncestorName(parentNode, PackEditorNodeKind.Library);
        string category = FindAncestorName(parentNode, PackEditorNodeKind.Category);

        var json = new JObject
        {
            ["name"] = newName,
            ["symbolKind"] = "snippet",
            ["returnType"] = string.Empty,
            ["signature"] = string.Empty,
            ["declaration"] = string.Empty,
            ["insertText"] = "// TODO: compléter le snippet",
            ["description"] = "Nouvel élément créé depuis le Visual Pack Editor.",
            ["longDescription"] = string.Empty,
            ["header"] = string.Empty,
            ["environment"] = environment,
            ["library"] = library,
            ["category"] = category,
            ["parameters"] = new JArray(),
        };
        functions.Add(json);
        IsDirty = true;
        RebuildTree();
        return FindNode(json) ?? throw new InvalidOperationException("L'élément ajouté n'a pas pu être retrouvé dans l'arborescence.");
    }

    public void DeleteNode(PackEditorNode node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (node.Kind == PackEditorNodeKind.Pack || node.ParentArray is null)
        {
            throw new InvalidOperationException("Le nœud sélectionné ne peut pas être supprimé.");
        }

        node.ParentArray.Remove(node.JsonObject);
        IsDirty = true;
        RebuildTree();
    }

    public IReadOnlyList<PackEditorParameter> GetParameters(PackEditorNode element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));
        if (element.Kind != PackEditorNodeKind.Element) return Array.Empty<PackEditorParameter>();
        return EnumerateObjects(element.JsonObject, "parameters")
            .Select(value => new PackEditorParameter(value))
            .ToArray();
    }

    public PackEditorParameter AddParameter(PackEditorNode element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));
        if (element.Kind != PackEditorNodeKind.Element) throw new InvalidOperationException("Sélectionne un élément avant d'ajouter un paramètre.");
        JArray parameters = EnsureArray(element.JsonObject, "parameters");
        string name = FindAvailableName(parameters, "param");
        var json = new JObject
        {
            ["name"] = name,
            ["type"] = "int",
            ["description"] = string.Empty,
            ["editorType"] = "text",
            ["defaultValue"] = string.Empty,
            ["placeholder"] = string.Empty,
            ["optional"] = false,
            ["presets"] = new JArray(),
            ["options"] = new JArray(),
        };
        parameters.Add(json);
        IsDirty = true;
        return new PackEditorParameter(json);
    }

    public void DeleteParameter(PackEditorNode element, PackEditorParameter parameter)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        JArray? parameters = element.JsonObject["parameters"] as JArray;
        if (parameters is null || !parameters.Remove(parameter.JsonObject))
        {
            throw new InvalidOperationException("Le paramètre sélectionné est introuvable.");
        }
        IsDirty = true;
    }

    public void SetParameterProperty(PackEditorParameter parameter, string propertyName, string value)
    {
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Le nom de propriété est vide.", nameof(propertyName));
        SetString(parameter.JsonObject, propertyName, value, trim: false);
        parameter.NotifyChanged();
        IsDirty = true;
    }

    public void SetParameterChoiceListProperty(PackEditorParameter parameter, string propertyName, string text)
    {
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Le nom de propriété est vide.", nameof(propertyName));
        var values = new JArray();
        foreach (string rawLine in (text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            string[] parts = line.Split(new[] { " | " }, StringSplitOptions.None).Select(value => value.Trim()).ToArray();
            if (parts.Length == 1)
            {
                values.Add(parts[0]);
                continue;
            }
            var choice = new JObject { ["value"] = parts[0] };
            if (parts.Length > 1 && parts[1].Length > 0) choice["label"] = parts[1];
            if (parts.Length > 2 && parts[2].Length > 0) choice["description"] = parts[2];
            if (parts.Length > 3 && parts[3].Length > 0) choice["detail"] = parts[3];
            values.Add(choice);
        }
        parameter.JsonObject[propertyName] = values;
        parameter.NotifyChanged();
        IsDirty = true;
    }

    public void SetParameterBooleanProperty(PackEditorParameter parameter, string propertyName, bool value)
    {
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Le nom de propriété est vide.", nameof(propertyName));
        parameter.JsonObject[propertyName] = value;
        parameter.NotifyChanged();
        IsDirty = true;
    }

    public void SetParameterObjectProperty(PackEditorParameter parameter, string propertyName, string json)
    {
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Le nom de propriété est vide.", nameof(propertyName));
        if (string.IsNullOrWhiteSpace(json)) parameter.JsonObject.Remove(propertyName);
        else parameter.JsonObject[propertyName] = JObject.Parse(json);
        parameter.NotifyChanged();
        IsDirty = true;
    }

    public IReadOnlyList<PackEditorNode> GetBatchSelectedElements()
    {
        return EnumerateTreeNodes()
            .Where(node => node.Kind == PackEditorNodeKind.Element && node.IsBatchSelected)
            .ToArray();
    }

    public int BatchSelectedElementCount => GetBatchSelectedElements().Count;

    public void ClearBatchSelection()
    {
        _batchSelectedObjects.Clear();
        foreach (PackEditorNode node in EnumerateTreeNodes())
        {
            if (node.Kind == PackEditorNodeKind.Element) node.IsBatchSelected = false;
        }
    }

    public IReadOnlyList<PackEditorNode> GetFunctionContainers()
    {
        return EnumerateTreeNodes()
            .Where(node => node.Kind == PackEditorNodeKind.Category || node.Kind == PackEditorNodeKind.Group)
            .ToArray();
    }

    public int DuplicateElements(IEnumerable<PackEditorNode> elements)
    {
        IReadOnlyList<PackEditorNode> sources = NormalizeElementSelection(elements);
        if (sources.Count == 0) return 0;

        CaptureBatchSelection();
        _batchSelectedObjects.Clear();
        int duplicated = 0;
        foreach (PackEditorNode source in sources)
        {
            JArray? parent = source.ParentArray;
            if (parent is null) continue;

            var clone = (JObject)source.JsonObject.DeepClone();
            string baseName = Fallback(ReadString(source.JsonObject, "name"), "Element") + " Copy";
            clone["name"] = FindAvailableName(parent, baseName);
            parent.Add(clone);
            _batchSelectedObjects.Add(clone);
            duplicated++;
        }

        if (duplicated > 0)
        {
            IsDirty = true;
            SynchronizeElementMetadata();
            RebuildTree(captureSelection: false);
        }
        return duplicated;
    }

    public int DeleteElements(IEnumerable<PackEditorNode> elements)
    {
        IReadOnlyList<PackEditorNode> selected = NormalizeElementSelection(elements);
        if (selected.Count == 0) return 0;

        CaptureBatchSelection();
        int deleted = 0;
        foreach (PackEditorNode node in selected)
        {
            if (node.ParentArray is null) continue;
            if (node.ParentArray.Remove(node.JsonObject))
            {
                _batchSelectedObjects.Remove(node.JsonObject);
                deleted++;
            }
        }

        if (deleted > 0)
        {
            IsDirty = true;
            RebuildTree(captureSelection: false);
        }
        return deleted;
    }

    public bool MoveNodeRelative(PackEditorNode node, int direction)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (direction != -1 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction));
        if (node.Kind == PackEditorNodeKind.Pack || node.ParentArray is null) return false;

        CaptureBatchSelection();
        int index = node.ParentArray.IndexOf(node.JsonObject);
        int target = index + direction;
        if (index < 0 || target < 0 || target >= node.ParentArray.Count) return false;

        JToken token = node.ParentArray[index];
        node.ParentArray.RemoveAt(index);
        node.ParentArray.Insert(target, token);
        IsDirty = true;
        RebuildTree(captureSelection: false);
        return true;
    }

    public int MoveElementsRelative(IEnumerable<PackEditorNode> elements, int direction)
    {
        if (direction != -1 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction));
        IReadOnlyList<PackEditorNode> selected = NormalizeElementSelection(elements);
        if (selected.Count == 0) return 0;

        CaptureBatchSelection();
        int moved = 0;
        foreach (IGrouping<JArray, PackEditorNode> group in selected.GroupBy(node => node.ParentArray!, ReferenceEqualityComparer<JArray>.Instance))
        {
            JArray? array = group.Key;
            if (array is null) continue;
            var objects = new HashSet<JObject>(group.Select(node => node.JsonObject), ReferenceEqualityComparer<JObject>.Instance);

            if (direction < 0)
            {
                for (int index = 1; index < array.Count; index++)
                {
                    if (array[index] is JObject current && objects.Contains(current) &&
                        array[index - 1] is JObject previous && !objects.Contains(previous))
                    {
                        array.RemoveAt(index);
                        array.Insert(index - 1, current);
                        moved++;
                    }
                }
            }
            else
            {
                for (int index = array.Count - 2; index >= 0; index--)
                {
                    if (array[index] is JObject current && objects.Contains(current) &&
                        array[index + 1] is JObject next && !objects.Contains(next))
                    {
                        array.RemoveAt(index);
                        array.Insert(index + 1, current);
                        moved++;
                    }
                }
            }
        }

        if (moved > 0)
        {
            IsDirty = true;
            RebuildTree(captureSelection: false);
        }
        return moved;
    }

    public int MoveElementsToContainer(IEnumerable<PackEditorNode> elements, PackEditorNode destination)
    {
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (destination.Kind != PackEditorNodeKind.Category && destination.Kind != PackEditorNodeKind.Group)
        {
            throw new InvalidOperationException("La destination doit être une catégorie ou un groupe.");
        }

        IReadOnlyList<PackEditorNode> selected = NormalizeElementSelection(elements);
        if (selected.Count == 0) return 0;

        CaptureBatchSelection();
        JArray target = EnsureArray(destination.JsonObject, "functions");
        int moved = 0;
        foreach (PackEditorNode node in selected)
        {
            if (node.ParentArray is null || ReferenceEquals(node.ParentArray, target)) continue;
            if (node.ParentArray.Remove(node.JsonObject))
            {
                target.Add(node.JsonObject);
                moved++;
            }
        }

        if (moved > 0)
        {
            IsDirty = true;
            SynchronizeElementMetadata();
            RebuildTree(captureSelection: false);
        }
        return moved;
    }


    /// <summary>
    /// Retourne les parents compatibles pour déplacer un sous-arbre structurel.
    /// Les descendants du nœud source sont exclus afin d'empêcher la création
    /// d'une boucle dans la hiérarchie JSON.
    /// </summary>
    public IReadOnlyList<PackEditorNode> GetStructureMoveTargets(PackEditorNode node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        IEnumerable<PackEditorNode> targets = node.Kind switch
        {
            PackEditorNodeKind.Library => EnumerateTreeNodes().Where(value => value.Kind == PackEditorNodeKind.Environment),
            PackEditorNodeKind.Category => EnumerateTreeNodes().Where(value => value.Kind == PackEditorNodeKind.Library),
            PackEditorNodeKind.Group => EnumerateTreeNodes().Where(value => value.Kind == PackEditorNodeKind.Category || value.Kind == PackEditorNodeKind.Group),
            _ => Enumerable.Empty<PackEditorNode>(),
        };

        return targets
            .Where(value => !ReferenceEquals(value.JsonObject, node.JsonObject))
            .Where(value => !IsDescendantOf(value, node))
            .ToArray();
    }

    /// <summary>
    /// Déplace une bibliothèque, une catégorie ou un groupe avec l'intégralité
    /// de son contenu vers un parent compatible.
    /// </summary>
    public bool MoveSubtreeToParent(PackEditorNode node, PackEditorNode destination)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (node.ParentArray is null) throw new InvalidOperationException("Le nœud sélectionné ne peut pas être déplacé.");
        if (ReferenceEquals(node.JsonObject, destination.JsonObject) || IsDescendantOf(destination, node))
        {
            throw new InvalidOperationException("Un nœud ne peut pas être déplacé dans lui-même ou dans l'un de ses descendants.");
        }

        JArray target = ResolveSubtreeDestination(node.Kind, destination);
        if (ReferenceEquals(node.ParentArray, target)) return false;

        CaptureBatchSelection();
        if (!node.ParentArray.Remove(node.JsonObject)) return false;
        target.Add(node.JsonObject);
        IsDirty = true;
        SynchronizeElementMetadata();
        RebuildTree(captureSelection: false);
        return true;
    }

    /// <summary>
    /// Duplique un environnement, une bibliothèque, une catégorie ou un groupe
    /// par clonage profond. Les fonctions et les paramètres structurés restent
    /// donc strictement conservés.
    /// </summary>
    public PackEditorNode DuplicateSubtree(PackEditorNode node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (node.Kind == PackEditorNodeKind.Pack || node.Kind == PackEditorNodeKind.Element || node.ParentArray is null)
        {
            throw new InvalidOperationException("Sélectionne un environnement, une bibliothèque, une catégorie ou un groupe à dupliquer.");
        }

        CaptureBatchSelection();
        var clone = (JObject)node.JsonObject.DeepClone();
        string baseName = Fallback(ReadString(node.JsonObject, "name"), node.Kind.ToString()) + " Copy";
        clone["name"] = FindAvailableName(node.ParentArray, baseName);
        node.ParentArray.Add(clone);
        IsDirty = true;
        SynchronizeElementMetadata();
        RebuildTree(captureSelection: false);
        return FindNode(clone) ?? throw new InvalidOperationException("Le sous-arbre dupliqué n'a pas pu être retrouvé.");
    }

    public static bool CanExportPartialNode(PackEditorNode? node)
    {
        return node is not null &&
            (node.Kind == PackEditorNodeKind.Library || node.Kind == PackEditorNodeKind.Category || node.Kind == PackEditorNodeKind.Group);
    }

    public static bool CanImportPartialNode(PackEditorNode? node)
    {
        return node is not null &&
            (node.Kind == PackEditorNodeKind.Pack || node.Kind == PackEditorNodeKind.Environment || node.Kind == PackEditorNodeKind.Library ||
             node.Kind == PackEditorNodeKind.Category || node.Kind == PackEditorNodeKind.Group);
    }

    /// <summary>
    /// Exporte une bibliothèque, une catégorie ou un groupe dans un fragment
    /// JSON JC Lib réutilisable par un autre pack.
    /// </summary>
    public void ExportPartialNode(PackEditorNode node, string filePath)
    {
        if (!CanExportPartialNode(node))
        {
            throw new InvalidOperationException("Sélectionne une bibliothèque, une catégorie ou un groupe à exporter.");
        }
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Le chemin d'export est vide.", nameof(filePath));

        string fullPath = Path.GetFullPath(filePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Le dossier d'export est introuvable.");
        Directory.CreateDirectory(directory);

        var fragment = new JObject
        {
            ["format"] = "jclib.partial.v1",
            ["kind"] = ToPartialKind(node.Kind),
            ["exportedFromPack"] = PackId,
            ["exportedAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["node"] = node.JsonObject.DeepClone(),
        };
        File.WriteAllText(fullPath, fragment.ToString(Formatting.Indented), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Importe un fragment partiel sous le parent actuellement sélectionné.
    /// Le nom du nœud importé est rendu unique sans écraser le contenu existant.
    /// </summary>
    public PackEditorNode ImportPartialNode(PackEditorNode destination, string filePath)
    {
        if (!CanImportPartialNode(destination))
        {
            throw new InvalidOperationException("Sélectionne un environnement, une bibliothèque, une catégorie ou un groupe cible.");
        }
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Le chemin d'import est vide.", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Le fragment JC Lib est introuvable.", filePath);

        JObject fragment = JObject.Parse(File.ReadAllText(filePath, Encoding.UTF8));
        string format = ReadString(fragment, "format");
        if (!string.Equals(format, "jclib.partial.v1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Format de fragment non pris en charge. Format attendu : jclib.partial.v1.");
        }

        string kind = ReadString(fragment, "kind");
        if (fragment["node"] is not JObject exportedNode)
        {
            throw new InvalidOperationException("Le fragment ne contient aucun nœud JSON exploitable.");
        }

        JArray target = ResolvePartialImportDestination(destination, kind);
        var clone = (JObject)exportedNode.DeepClone();
        string initialName = Fallback(ReadString(clone, "name"), kind);
        clone["name"] = FindAvailableName(target, initialName);
        target.Add(clone);
        IsDirty = true;
        SynchronizeElementMetadata();
        RebuildTree(captureSelection: false);
        return FindNode(clone) ?? throw new InvalidOperationException("Le fragment importé n'a pas pu être retrouvé.");
    }


    private static JArray ResolveSubtreeDestination(PackEditorNodeKind kind, PackEditorNode destination)
    {
        return kind switch
        {
            PackEditorNodeKind.Library when destination.Kind == PackEditorNodeKind.Environment => EnsureArray(destination.JsonObject, "libraries"),
            PackEditorNodeKind.Library when destination.Kind == PackEditorNodeKind.Pack => EnsureArray(EnumerateObjects(destination.JsonObject, "environments").First(), "libraries"),
            PackEditorNodeKind.Category when destination.Kind == PackEditorNodeKind.Library => EnsureArray(destination.JsonObject, "categories"),
            PackEditorNodeKind.Group when destination.Kind == PackEditorNodeKind.Category || destination.Kind == PackEditorNodeKind.Group => EnsureArray(destination.JsonObject, "groups"),
            _ => throw new InvalidOperationException("Le parent sélectionné n'est pas compatible avec ce type de sous-arbre."),
        };
    }

    private static JArray ResolvePartialImportDestination(PackEditorNode destination, string kind)
    {
        return kind switch
        {
            "library" when destination.Kind == PackEditorNodeKind.Environment => EnsureArray(destination.JsonObject, "libraries"),
            "library" when destination.Kind == PackEditorNodeKind.Pack => EnsureArray(EnumerateObjects(destination.JsonObject, "environments").First(), "libraries"),
            "category" when destination.Kind == PackEditorNodeKind.Library => EnsureArray(destination.JsonObject, "categories"),
            "group" when destination.Kind == PackEditorNodeKind.Category || destination.Kind == PackEditorNodeKind.Group => EnsureArray(destination.JsonObject, "groups"),
            _ => throw new InvalidOperationException($"Le fragment « {kind} » ne peut pas être importé sous « {destination.Kind} »."),
        };
    }

    private static string ToPartialKind(PackEditorNodeKind kind)
    {
        return kind switch
        {
            PackEditorNodeKind.Library => "library",
            PackEditorNodeKind.Category => "category",
            PackEditorNodeKind.Group => "group",
            _ => throw new InvalidOperationException("Ce type de nœud ne peut pas être exporté partiellement."),
        };
    }

    private static bool IsDescendantOf(PackEditorNode candidate, PackEditorNode possibleAncestor)
    {
        PackEditorNode? current = candidate.Parent;
        while (current is not null)
        {
            if (ReferenceEquals(current.JsonObject, possibleAncestor.JsonObject)) return true;
            current = current.Parent;
        }
        return false;
    }

    public IReadOnlyList<PackEditorValidationIssue> Validate()
    {
        var issues = new List<PackEditorValidationIssue>();
        ValidateRequiredMetadata(issues, "id", "L'identifiant du pack est obligatoire.");
        ValidateRequiredMetadata(issues, "name", "Le nom du pack est obligatoire.");
        ValidateRequiredMetadata(issues, "version", "La version du pack est obligatoire.");

        JArray? environments = _root["environments"] as JArray;
        if (environments is null || environments.Count == 0)
        {
            issues.Add(new PackEditorValidationIssue("pack", "Le pack doit contenir au moins un environnement."));
            return issues;
        }

        ValidateNamedChildren(_root, "environments", "environnement", "pack", issues);
        foreach (JObject environment in environments.OfType<JObject>())
        {
            string environmentName = ValidateName(environment, "Environnement", issues);
            ValidateNamedChildren(environment, "libraries", "bibliothèque", environmentName, issues);
            foreach (JObject library in EnumerateObjects(environment, "libraries"))
            {
                string libraryName = ValidateName(library, environmentName, issues);
                string libraryPath = $"{environmentName} / {libraryName}";
                ValidateNamedChildren(library, "categories", "catégorie", libraryPath, issues);
                foreach (JObject category in EnumerateObjects(library, "categories"))
                {
                    string categoryName = ValidateName(category, libraryPath, issues);
                    string categoryPath = $"{libraryPath} / {categoryName}";
                    ValidateFunctions(category, categoryPath, issues);
                    ValidateGroups(category, categoryPath, issues);
                }
            }
        }

        return issues;
    }

    public void Save()
    {
        NormalizeNames();
        SynchronizeElementMetadata();
        IReadOnlyList<PackEditorValidationIssue> issues = Validate();
        if (issues.Count > 0)
        {
            throw new InvalidOperationException("La sauvegarde est bloquée tant que des erreurs de validation subsistent.");
        }

        string temporaryPath = FilePath + ".jclib.tmp";
        JObject storageRoot = BuildLibraryFirstStorageRoot();
        File.WriteAllText(temporaryPath, storageRoot.ToString(Formatting.Indented), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Copy(temporaryPath, FilePath, overwrite: true);
        File.Delete(temporaryPath);
        IsDirty = false;
        RebuildTree();
    }

    private static bool IsSyntheticRootEnvironment(JObject environment)
    {
        string name = ReadString(environment, "name");
        return string.Equals(name, "General", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase);
    }

    private static void NormalizeLibraryFirstInputForEditing(JObject root)
    {
        if (root["environments"] is JArray existing && existing.Count > 0) return;
        JArray libraries = root["libraries"] as JArray ?? new JArray();
        root.Remove("libraries");
        root["environments"] = new JArray
        {
            new JObject
            {
                ["name"] = "General",
                ["libraries"] = libraries,
            },
        };
    }

    private JObject BuildLibraryFirstStorageRoot()
    {
        var clone = (JObject)_root.DeepClone();
        if (clone["environments"] is JArray environments && environments.Count == 1 && environments[0] is JObject environment)
        {
            string environmentName = ReadString(environment, "name");
            if (string.Equals(environmentName, "General", StringComparison.OrdinalIgnoreCase)
                || string.Equals(environmentName, "Default", StringComparison.OrdinalIgnoreCase))
            {
                clone.Remove("environments");
                clone["libraries"] = environment["libraries"]?.DeepClone() ?? new JArray();
            }
        }
        return clone;
    }

    public PackEditorNode? FindNode(JObject jsonObject)
    {
        foreach (PackEditorNode root in RootNodes)
        {
            PackEditorNode? node = FindNodeRecursive(root, jsonObject);
            if (node is not null) return node;
        }
        return null;
    }

    public void RebuildTree(bool captureSelection = true)
    {
        bool isInitialBuild = RootNodes.Count == 0;
        if (captureSelection) CaptureBatchSelection();
        CaptureTreeUiState();
        RootNodes.Clear();
        var packNode = new PackEditorNode(BuildPackNodeName(), PackEditorNodeKind.Pack, _root)
        {
            IsExpanded = isInitialBuild || _expandedObjects.Contains(_root),
            IsSelected = ReferenceEquals(_selectedObject, _root),
        };
        RootNodes.Add(packNode);

        JObject[] environments = EnumerateObjects(_root, "environments").ToArray();
        bool libraryFirstMode = environments.Length == 1 && IsSyntheticRootEnvironment(environments[0]);
        foreach (JObject environment in environments)
        {
            PackEditorNode hierarchyParent = libraryFirstMode
                ? packNode
                : CreateNode(environment, PackEditorNodeKind.Environment, packNode, _root["environments"] as JArray);
            foreach (JObject library in EnumerateObjects(environment, "libraries"))
            {
                var libraryNode = CreateNode(library, PackEditorNodeKind.Library, hierarchyParent, environment["libraries"] as JArray);
                foreach (JObject category in EnumerateObjects(library, "categories"))
                {
                    var categoryNode = CreateNode(category, PackEditorNodeKind.Category, libraryNode, library["categories"] as JArray);
                    AddFunctionNodes(category, categoryNode);
                    AddGroupNodes(category, categoryNode);
                }
            }
        }
    }

    private void AddGroupNodes(JObject owner, PackEditorNode ownerNode)
    {
        JArray? groups = owner["groups"] as JArray;
        if (groups is null) return;

        foreach (JObject group in groups.OfType<JObject>())
        {
            var groupNode = CreateNode(group, PackEditorNodeKind.Group, ownerNode, groups);
            AddFunctionNodes(group, groupNode);
            AddGroupNodes(group, groupNode);
        }
    }

    private void AddFunctionNodes(JObject owner, PackEditorNode ownerNode)
    {
        JArray? functions = owner["functions"] as JArray;
        if (functions is null) return;

        foreach (JObject function in functions.OfType<JObject>())
        {
            CreateNode(function, PackEditorNodeKind.Element, ownerNode, functions);
        }
    }

    private PackEditorNode CreateNode(JObject value, PackEditorNodeKind kind, PackEditorNode parent, JArray? parentArray)
    {
        var node = new PackEditorNode(ReadString(value, "name"), kind, value, parent, parentArray)
        {
            IsExpanded = _expandedObjects.Contains(value),
            IsSelected = ReferenceEquals(_selectedObject, value),
        };
        if (kind == PackEditorNodeKind.Element && _batchSelectedObjects.Contains(value))
        {
            node.IsBatchSelected = true;
        }
        parent.Children.Add(node);
        return node;
    }

    private static PackEditorNode? FindNodeRecursive(PackEditorNode node, JObject target)
    {
        if (ReferenceEquals(node.JsonObject, target)) return node;
        foreach (PackEditorNode child in node.Children)
        {
            PackEditorNode? found = FindNodeRecursive(child, target);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    /// Capture l'état visuel porté par les modèles avant de reconstruire les
    /// wrappers PackEditorNode. Les JObject restent les identités stables du
    /// document, y compris après un réordonnancement ou une sauvegarde.
    /// </summary>
    public void CaptureTreeUiState()
    {
        if (RootNodes.Count == 0) return;

        _expandedObjects.Clear();
        JObject? selected = null;
        foreach (PackEditorNode node in EnumerateTreeNodes())
        {
            if (node.IsExpanded) _expandedObjects.Add(node.JsonObject);
            if (node.IsSelected) selected = node.JsonObject;
        }
        _selectedObject = selected;
    }

    public PackEditorNode? GetRememberedSelectedNode()
    {
        return _selectedObject is null ? null : FindNode(_selectedObject);
    }

    private void CaptureBatchSelection()
    {
        if (RootNodes.Count == 0) return;
        _batchSelectedObjects.Clear();
        foreach (PackEditorNode node in EnumerateTreeNodes())
        {
            if (node.Kind == PackEditorNodeKind.Element && node.IsBatchSelected)
            {
                _batchSelectedObjects.Add(node.JsonObject);
            }
        }
    }

    private IEnumerable<PackEditorNode> EnumerateTreeNodes()
    {
        foreach (PackEditorNode root in RootNodes)
        {
            foreach (PackEditorNode node in EnumerateTreeNodes(root)) yield return node;
        }
    }

    private static IEnumerable<PackEditorNode> EnumerateTreeNodes(PackEditorNode node)
    {
        yield return node;
        foreach (PackEditorNode child in node.Children)
        {
            foreach (PackEditorNode descendant in EnumerateTreeNodes(child)) yield return descendant;
        }
    }

    private static IReadOnlyList<PackEditorNode> NormalizeElementSelection(IEnumerable<PackEditorNode> elements)
    {
        if (elements is null) return Array.Empty<PackEditorNode>();
        var seen = new HashSet<JObject>(ReferenceEqualityComparer<JObject>.Instance);
        var result = new List<PackEditorNode>();
        foreach (PackEditorNode node in elements)
        {
            if (node is null || node.Kind != PackEditorNodeKind.Element || node.ParentArray is null) continue;
            if (seen.Add(node.JsonObject)) result.Add(node);
        }
        return result;
    }

    private void ValidateRequiredMetadata(ICollection<PackEditorValidationIssue> issues, string propertyName, string message)
    {
        if (string.IsNullOrWhiteSpace(ReadString(_root, propertyName)))
        {
            issues.Add(new PackEditorValidationIssue("pack", message));
        }
    }

    private static string ValidateName(JObject value, string parentPath, ICollection<PackEditorValidationIssue> issues)
    {
        string name = ReadString(value, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add(new PackEditorValidationIssue(parentPath, "Nom vide."));
            return "<sans nom>";
        }
        return name.Trim();
    }

    private static void ValidateNamedChildren(JObject owner, string propertyName, string label, string ownerPath, ICollection<PackEditorValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JObject child in EnumerateObjects(owner, propertyName))
        {
            string normalized = ReadString(child, "name").Trim();
            if (normalized.Length == 0) continue;
            if (!seen.Add(normalized))
            {
                issues.Add(new PackEditorValidationIssue(ownerPath, $"Nom de {label} dupliqué : {normalized}."));
            }
        }
    }

    private static void ValidateGroups(JObject owner, string ownerPath, ICollection<PackEditorValidationIssue> issues)
    {
        ValidateNamedChildren(owner, "groups", "groupe", ownerPath, issues);
        foreach (JObject group in EnumerateObjects(owner, "groups"))
        {
            string name = ValidateName(group, ownerPath, issues);
            string path = $"{ownerPath} / {name}";
            ValidateFunctions(group, path, issues);
            ValidateGroups(group, path, issues);
        }
    }

    private static void ValidateFunctions(JObject owner, string ownerPath, ICollection<PackEditorValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JObject function in EnumerateObjects(owner, "functions"))
        {
            string normalized = ReadString(function, "name").Trim();
            if (normalized.Length == 0)
            {
                issues.Add(new PackEditorValidationIssue(ownerPath, "Nom d'élément vide."));
                continue;
            }

            if (!seen.Add(normalized))
            {
                issues.Add(new PackEditorValidationIssue(ownerPath, $"Nom d'élément dupliqué : {normalized}."));
            }

            ValidateParameters(function, $"{ownerPath} / {normalized}", issues);
        }
    }

    private static void ValidateParameters(JObject function, string ownerPath, ICollection<PackEditorValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JObject parameter in EnumerateObjects(function, "parameters"))
        {
            string normalized = ReadString(parameter, "name").Trim();
            if (normalized.Length == 0)
            {
                issues.Add(new PackEditorValidationIssue(ownerPath, "Nom de paramètre vide."));
                continue;
            }
            if (!seen.Add(normalized))
            {
                issues.Add(new PackEditorValidationIssue(ownerPath, $"Nom de paramètre dupliqué : {normalized}."));
            }
        }
    }

    private void NormalizeNames()
    {
        TrimProperty(_root, "id");
        TrimProperty(_root, "name");
        TrimProperty(_root, "version");
        foreach (JObject environment in EnumerateObjects(_root, "environments"))
        {
            NormalizeHierarchy(environment, "libraries");
        }
        UpdatePackNodeHeader();
    }

    private static void NormalizeHierarchy(JObject value, string childArrayName)
    {
        TrimProperty(value, "name");
        NormalizeFunctions(value);
        NormalizeGroups(value);

        foreach (JObject child in EnumerateObjects(value, childArrayName))
        {
            string next = childArrayName == "libraries" ? "categories" : string.Empty;
            if (next.Length > 0)
            {
                NormalizeHierarchy(child, next);
            }
            else
            {
                TrimProperty(child, "name");
                NormalizeFunctions(child);
                NormalizeGroups(child);
            }
        }
    }

    private static void NormalizeGroups(JObject owner)
    {
        foreach (JObject group in EnumerateObjects(owner, "groups"))
        {
            TrimProperty(group, "name");
            NormalizeFunctions(group);
            NormalizeGroups(group);
        }
    }

    private static void NormalizeFunctions(JObject owner)
    {
        foreach (JObject function in EnumerateObjects(owner, "functions"))
        {
            TrimProperty(function, "name");
            foreach (JObject parameter in EnumerateObjects(function, "parameters"))
            {
                TrimProperty(parameter, "name");
                TrimProperty(parameter, "type");
                TrimProperty(parameter, "editorType");
            }
        }
    }

    private void SynchronizeElementMetadata()
    {
        foreach (JObject environment in EnumerateObjects(_root, "environments"))
        {
            string environmentName = ReadString(environment, "name").Trim();
            foreach (JObject library in EnumerateObjects(environment, "libraries"))
            {
                string libraryName = ReadString(library, "name").Trim();
                foreach (JObject category in EnumerateObjects(library, "categories"))
                {
                    string categoryName = ReadString(category, "name").Trim();
                    SynchronizeFunctions(category, environmentName, libraryName, categoryName);
                }
            }
        }
    }

    private static void SynchronizeFunctions(JObject owner, string environmentName, string libraryName, string categoryName)
    {
        foreach (JObject function in EnumerateObjects(owner, "functions"))
        {
            function["environment"] = environmentName;
            function["library"] = libraryName;
            function["category"] = categoryName;
        }
        foreach (JObject group in EnumerateObjects(owner, "groups"))
        {
            SynchronizeFunctions(group, environmentName, libraryName, categoryName);
        }
    }

    private static void TrimProperty(JObject value, string propertyName)
    {
        JToken? token = value[propertyName];
        if (token?.Type == JTokenType.String)
        {
            value[propertyName] = token.Value<string>()?.Trim() ?? string.Empty;
        }
    }

    private void UpdatePackNodeHeader()
    {
        if (RootNodes.Count > 0)
        {
            RootNodes[0].Name = BuildPackNodeName();
        }
    }

    private string BuildPackNodeName() => $"{Fallback(PackName, "JC Lib pack")} — v{Fallback(PackVersion, "?")}";

    private static string Fallback(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string FindAvailableName(JArray values, string baseName)
    {
        var names = new HashSet<string>(
            values.OfType<JObject>().Select(value => ReadString(value, "name").Trim()),
            StringComparer.OrdinalIgnoreCase);
        string candidate = baseName;
        int suffix = 2;
        while (names.Contains(candidate))
        {
            candidate = baseName + suffix++;
        }
        return candidate;
    }

    private static PackEditorNode? FindContextNode(PackEditorNode context, PackEditorNodeKind expectedKind)
    {
        PackEditorNode? current = context;
        while (current is not null)
        {
            if (current.Kind == expectedKind) return current;
            current = current.Parent;
        }
        return null;
    }

    private static PackEditorNode? ResolveFunctionContainer(PackEditorNode context)
    {
        PackEditorNode? current = context.Kind == PackEditorNodeKind.Element ? context.Parent : context;
        while (current is not null)
        {
            if (current.Kind == PackEditorNodeKind.Category || current.Kind == PackEditorNodeKind.Group) return current;
            current = current.Parent;
        }
        return null;
    }

    private static string FindAncestorName(PackEditorNode node, PackEditorNodeKind kind)
    {
        PackEditorNode? current = node;
        while (current is not null)
        {
            if (current.Kind == kind) return current.Name;
            current = current.Parent;
        }
        return string.Empty;
    }

    private static IEnumerable<JObject> EnumerateObjects(JObject owner, string propertyName)
    {
        return (owner[propertyName] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>();
    }

    private static JArray EnsureArray(JObject owner, string propertyName)
    {
        if (owner[propertyName] is JArray array) return array;
        array = new JArray();
        owner[propertyName] = array;
        return array;
    }

    private static string ReadString(JObject value, string propertyName) => value[propertyName]?.Value<string>() ?? string.Empty;

    private static void SetString(JObject value, string propertyName, string? text, bool trim = true)
    {
        value[propertyName] = trim ? (text ?? string.Empty).Trim() : text ?? string.Empty;
    }

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
