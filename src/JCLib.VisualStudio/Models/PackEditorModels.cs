using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace JCLib.VisualStudio.Models;

public enum PackEditorNodeKind
{
    Pack,
    Environment,
    Library,
    Category,
    Group,
    Element,
}

public sealed class PackEditorNode : INotifyPropertyChanged
{
    private string _name;
    private bool _isBatchSelected;
    private bool _isExpanded;
    private bool _isSelected;

    public PackEditorNode(
        string name,
        PackEditorNodeKind kind,
        JObject jsonObject,
        PackEditorNode? parent = null,
        JArray? parentArray = null)
    {
        _name = string.IsNullOrWhiteSpace(name) ? "<sans nom>" : name.Trim();
        Kind = kind;
        JsonObject = jsonObject ?? throw new ArgumentNullException(nameof(jsonObject));
        Parent = parent;
        ParentArray = parentArray;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "<sans nom>" : value.Trim();
            if (string.Equals(_name, normalized, StringComparison.Ordinal)) return;
            _name = normalized;
            Notify(nameof(Name));
            Notify(nameof(Header));
            Notify(nameof(Path));
        }
    }

    public PackEditorNodeKind Kind { get; }

    public JObject JsonObject { get; }

    public PackEditorNode? Parent { get; }

    public JArray? ParentArray { get; }

    public ObservableCollection<PackEditorNode> Children { get; } = new ObservableCollection<PackEditorNode>();

    public bool IsBatchSelectable => Kind == PackEditorNodeKind.Element;

    /// <summary>
    /// État développé du nœud dans le TreeView. Il est mémorisé avant chaque
    /// reconstruction afin d'éviter de refermer les parents pendant l'édition.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            Notify(nameof(IsExpanded));
        }
    }

    /// <summary>
    /// Sélection courante dans le TreeView. La liaison TwoWay permet de la
    /// restaurer après un ajout, une sauvegarde ou un déplacement.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            Notify(nameof(IsSelected));
        }
    }

    public bool IsBatchSelected
    {
        get => _isBatchSelected;
        set
        {
            if (_isBatchSelected == value) return;
            _isBatchSelected = value;
            Notify(nameof(IsBatchSelected));
        }
    }

    public string Header => Kind == PackEditorNodeKind.Element
        ? Name
        : $"{Name} ({Children.Count})";

    public string Path => Parent is null ? Name : $"{Parent.Path} > {Name}";

    public override string ToString() => Header;

    private void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class PackEditorParameter : INotifyPropertyChanged
{
    public PackEditorParameter(JObject jsonObject)
    {
        JsonObject = jsonObject ?? throw new ArgumentNullException(nameof(jsonObject));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public JObject JsonObject { get; }

    public string Name => Read("name", "param");

    public string Type => Read("type", "int");

    public string EditorType => Read("editorType", "text");

    public string Display => $"{Name} : {Type} [{EditorType}]";

    public void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditorType)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
    }

    private string Read(string propertyName, string fallback)
    {
        string value = JsonObject[propertyName]?.Value<string>() ?? string.Empty;
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

public sealed class PackEditorValidationIssue
{
    public PackEditorValidationIssue(string path, string message)
    {
        Path = path;
        Message = message;
    }

    public string Path { get; }

    public string Message { get; }

    public string Display => string.IsNullOrWhiteSpace(Path) ? Message : $"[{Path}] {Message}";

    public override string ToString() => Display;
}
