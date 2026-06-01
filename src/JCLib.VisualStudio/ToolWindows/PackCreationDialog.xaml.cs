using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using JCLib.VisualStudio.Models;
using JCLib.VisualStudio.Services;

namespace JCLib.VisualStudio;

public sealed class PackCreationRequest
{
    public PackCreationRequest(CatalogPackSourceKind targetKind, string id, string name, string version, string fileName)
    {
        TargetKind = targetKind;
        Id = id;
        Name = name;
        Version = version;
        FileName = fileName;
    }

    public CatalogPackSourceKind TargetKind { get; }
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string FileName { get; }
}

public partial class PackCreationDialog : Window
{
    private sealed class DestinationOption
    {
        public DestinationOption(CatalogPackSourceKind kind, string label)
        {
            Kind = kind;
            Label = label;
        }
        public CatalogPackSourceKind Kind { get; }
        public string Label { get; }
    }

    public PackCreationDialog(string title, string description, string defaultId, string defaultName, string defaultVersion, string defaultFileName)
    {
        InitializeComponent();
        ThemeService.ApplyTheme(this, UserPreferencesStore.Load().Theme);
        Title = title;
        DialogTitleText.Text = title;
        DialogDescriptionText.Text = description;
        PackIdTextBox.Text = defaultId;
        PackNameTextBox.Text = defaultName;
        PackVersionTextBox.Text = defaultVersion;
        FileNameTextBox.Text = defaultFileName;

        var options = new List<DestinationOption>
        {
            new DestinationOption(CatalogPackSourceKind.GlobalUser, "Global utilisateur — %LOCALAPPDATA%\\JCLib\\VisualStudio\\Packs"),
        };
        if (!string.IsNullOrWhiteSpace(CatalogLoader.GetSolutionPacksDirectory(create: false)))
        {
            options.Add(new DestinationOption(CatalogPackSourceKind.Solution, "Solution ouverte — .jclib\\packs"));
        }
        DestinationComboBox.ItemsSource = options;
        DestinationComboBox.SelectedIndex = 0;
    }

    public PackCreationRequest? Request { get; private set; }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        string id = PackIdTextBox.Text.Trim();
        string name = PackNameTextBox.Text.Trim();
        string version = PackVersionTextBox.Text.Trim();
        string fileName = FileNameTextBox.Text.Trim();
        if (DestinationComboBox.SelectedItem is not DestinationOption destination)
        {
            ValidationText.Text = "Sélectionne une destination.";
            return;
        }
        if (id.Length == 0 || name.Length == 0 || version.Length == 0)
        {
            ValidationText.Text = "L'identifiant, le nom et la version sont obligatoires.";
            return;
        }
        if (fileName.Length == 0)
        {
            ValidationText.Text = "Le nom du fichier est obligatoire.";
            return;
        }
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) fileName += ".json";
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            ValidationText.Text = "Saisis uniquement un nom de fichier, sans chemin de dossier.";
            return;
        }
        if (Path.GetInvalidFileNameChars().Any(ch => fileName.IndexOf(ch) >= 0))
        {
            ValidationText.Text = "Le nom de fichier contient un caractère interdit.";
            return;
        }

        Request = new PackCreationRequest(destination.Kind, id, name, version, fileName);
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
