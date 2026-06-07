using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JCLib.VisualStudio.Models;
using JCLib.VisualStudio.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;

namespace JCLib.VisualStudio;

public partial class PackEditorWindow : Window
{
    private readonly PackEditorDocument _document;
    private bool _loadingFields;
    private PackEditorNode? _selectedNode;
    private PackEditorParameter? _selectedParameter;

    public PackEditorWindow(string packPath)
    {
        InitializeComponent();
        ThemeService.ApplyTheme(this, UserPreferencesStore.Load().Theme);
        _document = PackEditorDocument.Load(packPath);
        PackFilePathText.Text = _document.FilePath;
        LoadPackMetadata();
        RefreshTree();
        RefreshValidation("Pack chargé. Sélectionne un nœud pour le modifier.");
        Closing += OnWindowClosing;
    }

    public bool WasSaved { get; private set; }

    private void LoadPackMetadata()
    {
        _loadingFields = true;
        try
        {
            PackIdTextBox.Text = _document.PackId;
            PackNameTextBox.Text = _document.PackName;
            PackVersionTextBox.Text = _document.PackVersion;
        }
        finally
        {
            _loadingFields = false;
        }
    }

    private void RefreshTree(PackEditorNode? preferredNode = null)
    {
        JObject? selectedObject = preferredNode?.JsonObject ?? _selectedNode?.JsonObject;
        _document.CaptureTreeUiState();

        EditorTreeView.ItemsSource = null;
        EditorTreeView.ItemsSource = _document.RootNodes;

        PackEditorNode? restoredNode = selectedObject is null
            ? _document.GetRememberedSelectedNode()
            : _document.FindNode(selectedObject);

        _selectedNode = null;
        _selectedParameter = null;
        if (restoredNode is null)
        {
            ClearNodeFields();
            ClearElementFields();
            RefreshActionButtons();
            return;
        }

        ExpandAncestors(restoredNode);
        restoredNode.IsSelected = true;
        ShowNode(restoredNode);
    }

    private void RefreshValidation(string? status = null)
    {
        IReadOnlyList<PackEditorValidationIssue> issues = _document.Validate();
        ValidationIssuesListBox.ItemsSource = issues;
        ValidationExpander.Header = issues.Count == 0
            ? "Validation — aucune erreur"
            : $"Validation — {issues.Count:N0} erreur(s)";
        SaveButton.IsEnabled = issues.Count == 0 && _document.IsDirty;
        SaveAndCloseButton.IsEnabled = issues.Count == 0;

        if (!string.IsNullOrWhiteSpace(status))
        {
            EditorStatusText.Text = status;
        }
        else if (issues.Count > 0)
        {
            EditorStatusText.Text = "Sauvegarde désactivée tant que les erreurs de validation subsistent.";
        }
        else
        {
            EditorStatusText.Text = _document.IsDirty
                ? "Modifications non sauvegardées."
                : "Pack valide. Aucune modification en attente.";
        }
    }

    private void RefreshActionButtons()
    {
        PackEditorNode? node = _selectedNode;
        bool hasNode = node is not null;
        AddEnvironmentButton.IsEnabled = true;
        AddLibraryButton.IsEnabled = hasNode && FindAncestor(node!, PackEditorNodeKind.Environment) is not null;
        AddCategoryButton.IsEnabled = hasNode && FindAncestor(node!, PackEditorNodeKind.Library) is not null;
        AddGroupButton.IsEnabled = hasNode && ResolveContainer(node!) is not null;
        AddElementButton.IsEnabled = hasNode && ResolveContainer(node!) is not null;
        DeleteStructureButton.IsEnabled = node is not null &&
            node.Kind != PackEditorNodeKind.Pack &&
            node.Kind != PackEditorNodeKind.Element;
        DeleteElementButton.IsEnabled = node?.Kind == PackEditorNodeKind.Element;
        AddParameterButton.IsEnabled = node?.Kind == PackEditorNodeKind.Element;
        DeleteParameterButton.IsEnabled = _selectedParameter is not null;

        IReadOnlyList<PackEditorNode> batch = _document.GetBatchSelectedElements();
        int batchCount = batch.Count;
        bool hasElementOperation = batchCount > 0 || node?.Kind == PackEditorNodeKind.Element;
        bool canMoveCurrentNode = node is not null && node.Kind != PackEditorNodeKind.Pack && node.ParentArray is not null;
        bool isStructureNode = node is not null &&
            (node.Kind == PackEditorNodeKind.Environment || node.Kind == PackEditorNodeKind.Library ||
             node.Kind == PackEditorNodeKind.Category || node.Kind == PackEditorNodeKind.Group);
        bool canMoveSubtree = node is not null &&
            (node.Kind == PackEditorNodeKind.Library || node.Kind == PackEditorNodeKind.Category || node.Kind == PackEditorNodeKind.Group);

        BatchSelectionSummaryText.Text = batchCount == 0
            ? "Sélection groupée : 0 élément"
            : $"Sélection groupée : {batchCount:N0} élément(s)";
        DuplicateSelectionButton.IsEnabled = hasElementOperation;
        MoveUpButton.IsEnabled = batchCount > 0 || canMoveCurrentNode;
        MoveDownButton.IsEnabled = batchCount > 0 || canMoveCurrentNode;
        ChangeParentButton.IsEnabled = hasElementOperation || canMoveSubtree;
        DeleteSelectionButton.IsEnabled = batchCount > 0;
        ClearBatchSelectionButton.IsEnabled = batchCount > 0;
        DuplicateSubtreeButton.IsEnabled = isStructureNode;
        ExportPartialButton.IsEnabled = PackEditorDocument.CanExportPartialNode(node);
        ImportPartialButton.IsEnabled = PackEditorDocument.CanImportPartialNode(node);
    }

    private void ShowNode(PackEditorNode? node)
    {
        _selectedNode = node;
        _selectedParameter = null;
        RefreshActionButtons();

        if (node is null)
        {
            ClearNodeFields();
            ClearElementFields();
            SelectedElementPathText.Text = "Sélectionne un nœud dans l'arborescence.";
            return;
        }

        SelectedElementPathText.Text = node.Path;
        if (node.Kind == PackEditorNodeKind.Element)
        {
            ClearNodeFields();
            ShowElementFields(node);
            return;
        }

        ClearElementFields();
        if (node.Kind == PackEditorNodeKind.Pack)
        {
            ClearNodeFields();
            SelectedElementPathText.Text = node.Path + "\nLes métadonnées du pack se modifient dans la barre supérieure.";
            return;
        }

        NodeFieldsPanel.Visibility = Visibility.Visible;
        _loadingFields = true;
        try
        {
            NodeNameTextBox.Text = Read(node.JsonObject, "name");
        }
        finally
        {
            _loadingFields = false;
        }
    }

    private void ShowElementFields(PackEditorNode node)
    {
        ElementFieldsPanel.Visibility = Visibility.Visible;
        _loadingFields = true;
        try
        {
            ElementNameTextBox.Text = Read(node.JsonObject, "name");
            SymbolKindTextBox.Text = Read(node.JsonObject, "symbolKind");
            ReturnTypeTextBox.Text = Read(node.JsonObject, "returnType");
            HeaderTextBox.Text = Read(node.JsonObject, "header");
            SignatureTextBox.Text = Read(node.JsonObject, "signature");
            DeclarationTextBox.Text = Read(node.JsonObject, "declaration");
            InsertTextTextBox.Text = Read(node.JsonObject, "insertText");
            DescriptionTextBox.Text = Read(node.JsonObject, "description");
            LongDescriptionTextBox.Text = Read(node.JsonObject, "longDescription");
        }
        finally
        {
            _loadingFields = false;
        }
        RefreshParameterList();
    }

    private void ClearNodeFields()
    {
        NodeFieldsPanel.Visibility = Visibility.Collapsed;
        _loadingFields = true;
        try { NodeNameTextBox.Text = string.Empty; }
        finally { _loadingFields = false; }
    }

    private void ClearElementFields()
    {
        ElementFieldsPanel.Visibility = Visibility.Collapsed;
        _loadingFields = true;
        try
        {
            ElementNameTextBox.Text = string.Empty;
            SymbolKindTextBox.Text = string.Empty;
            ReturnTypeTextBox.Text = string.Empty;
            HeaderTextBox.Text = string.Empty;
            SignatureTextBox.Text = string.Empty;
            DeclarationTextBox.Text = string.Empty;
            InsertTextTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            LongDescriptionTextBox.Text = string.Empty;
            ParametersListBox.ItemsSource = null;
            ClearParameterFields();
        }
        finally
        {
            _loadingFields = false;
        }
    }

    private void RefreshParameterList(PackEditorParameter? selected = null)
    {
        if (_selectedNode?.Kind != PackEditorNodeKind.Element)
        {
            ParametersListBox.ItemsSource = null;
            ClearParameterFields();
            return;
        }

        IReadOnlyList<PackEditorParameter> parameters = _document.GetParameters(_selectedNode);
        ParametersListBox.ItemsSource = parameters;
        _selectedParameter = selected is null
            ? null
            : parameters.FirstOrDefault(item => ReferenceEquals(item.JsonObject, selected.JsonObject));
        ParametersListBox.SelectedItem = _selectedParameter;
        if (_selectedParameter is null) ClearParameterFields();
        RefreshActionButtons();
    }

    private void ShowParameter(PackEditorParameter? parameter)
    {
        _selectedParameter = parameter;
        RefreshActionButtons();
        if (parameter is null)
        {
            ClearParameterFields();
            return;
        }

        ParameterFieldsPanel.Visibility = Visibility.Visible;
        _loadingFields = true;
        try
        {
            ParameterNameTextBox.Text = Read(parameter.JsonObject, "name");
            ParameterTypeTextBox.Text = Read(parameter.JsonObject, "type");
            ParameterDescriptionTextBox.Text = Read(parameter.JsonObject, "description");
            ParameterEditorTypeTextBox.Text = Read(parameter.JsonObject, "editorType");
            ParameterDefaultValueTextBox.Text = Read(parameter.JsonObject, "defaultValue");
            ParameterPlaceholderTextBox.Text = Read(parameter.JsonObject, "placeholder");
            ParameterOptionalCheckBox.IsChecked = parameter.JsonObject["optional"]?.Value<bool?>() ?? false;
            ParameterPresetsTextBox.Text = ReadChoiceArray(parameter.JsonObject, "presets");
            ParameterOptionsTextBox.Text = ReadChoiceArray(parameter.JsonObject, "options");
            ParameterPickerConfigTextBox.Text = ReadObject(parameter.JsonObject, "pickerConfig");
        }
        finally
        {
            _loadingFields = false;
        }
    }

    private void ClearParameterFields()
    {
        ParameterFieldsPanel.Visibility = Visibility.Collapsed;
        _selectedParameter = null;
        _loadingFields = true;
        try
        {
            ParameterNameTextBox.Text = string.Empty;
            ParameterTypeTextBox.Text = string.Empty;
            ParameterDescriptionTextBox.Text = string.Empty;
            ParameterEditorTypeTextBox.Text = string.Empty;
            ParameterDefaultValueTextBox.Text = string.Empty;
            ParameterPlaceholderTextBox.Text = string.Empty;
            ParameterOptionalCheckBox.IsChecked = false;
            ParameterPresetsTextBox.Text = string.Empty;
            ParameterOptionsTextBox.Text = string.Empty;
            ParameterPickerConfigTextBox.Text = string.Empty;
        }
        finally
        {
            _loadingFields = false;
        }
        RefreshActionButtons();
    }

    private void OnPackMetadataChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingFields) return;
        _document.SetPackMetadata(PackIdTextBox.Text, PackNameTextBox.Text, PackVersionTextBox.Text);
        RefreshValidation();
    }

    private void OnEditorTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        PackEditorNode? next = e.NewValue as PackEditorNode;
        if (_selectedNode is not null && !ReferenceEquals(_selectedNode, next))
        {
            _selectedNode.IsSelected = false;
        }
        if (next is not null) next.IsSelected = true;
        ShowNode(next);
    }

    private void OnNodeNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingFields || _selectedNode is null || _selectedNode.Kind == PackEditorNodeKind.Pack || _selectedNode.Kind == PackEditorNodeKind.Element) return;
        _document.SetNodeName(_selectedNode, NodeNameTextBox.Text);
        SelectedElementPathText.Text = _selectedNode.Path;
        RefreshValidation();
    }

    private void OnElementFieldChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingFields || _selectedNode?.Kind != PackEditorNodeKind.Element) return;
        if (sender is not TextBox textBox || textBox.Tag is not string propertyName) return;

        _document.SetElementProperty(_selectedNode, propertyName, textBox.Text);
        if (string.Equals(propertyName, "name", StringComparison.Ordinal)) SelectedElementPathText.Text = _selectedNode.Path;
        RefreshValidation();
    }

    private void OnParameterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowParameter(ParametersListBox.SelectedItem as PackEditorParameter);
    }

    private void OnParameterFieldChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingFields || _selectedParameter is null) return;
        if (sender is not TextBox textBox || textBox.Tag is not string propertyName) return;
        _document.SetParameterProperty(_selectedParameter, propertyName, textBox.Text);
        RefreshValidation();
    }

    private void OnParameterListFieldChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingFields || _selectedParameter is null) return;
        if (sender is not TextBox textBox || textBox.Tag is not string propertyName) return;
        _document.SetParameterChoiceListProperty(_selectedParameter, propertyName, textBox.Text);
        RefreshValidation();
    }

    private void OnParameterOptionalChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingFields || _selectedParameter is null) return;
        _document.SetParameterBooleanProperty(_selectedParameter, "optional", ParameterOptionalCheckBox.IsChecked == true);
        RefreshValidation();
    }

    private void OnParameterPickerConfigLostFocus(object sender, RoutedEventArgs e)
    {
        if (_loadingFields || _selectedParameter is null) return;
        try
        {
            _document.SetParameterObjectProperty(_selectedParameter, "pickerConfig", ParameterPickerConfigTextBox.Text);
            RefreshValidation("Configuration de sélection structurée mise à jour.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"pickerConfig invalide : {ex.Message}";
        }
    }

    private void OnBatchSelectionCheckBoxClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(RefreshActionButtons), DispatcherPriority.Background);
    }

    private IReadOnlyList<PackEditorNode> GetElementOperationSelection()
    {
        IReadOnlyList<PackEditorNode> batch = _document.GetBatchSelectedElements();
        if (batch.Count > 0) return batch;
        if (_selectedNode?.Kind == PackEditorNodeKind.Element) return new[] { _selectedNode };
        return Array.Empty<PackEditorNode>();
    }

    private void OnDuplicateSelectionClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<PackEditorNode> selection = GetElementOperationSelection();
        if (selection.Count == 0) return;
        try
        {
            int duplicated = _document.DuplicateElements(selection);
            RefreshTree();
            RefreshValidation(duplicated == 0
                ? "Aucun élément n'a été dupliqué."
                : $"{duplicated:N0} élément(s) dupliqué(s). Les copies restent cochées pour faciliter leur déplacement.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Duplication impossible : {ex.Message}";
        }
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e) => MoveSelectionRelative(-1, "vers le haut");

    private void OnMoveDownClick(object sender, RoutedEventArgs e) => MoveSelectionRelative(1, "vers le bas");

    private void MoveSelectionRelative(int direction, string label)
    {
        try
        {
            IReadOnlyList<PackEditorNode> batch = _document.GetBatchSelectedElements();
            int moved;
            if (batch.Count > 0)
            {
                moved = _document.MoveElementsRelative(batch, direction);
            }
            else if (_selectedNode is not null)
            {
                moved = _document.MoveNodeRelative(_selectedNode, direction) ? 1 : 0;
            }
            else
            {
                return;
            }

            RefreshTree();
            RefreshValidation(moved == 0
                ? "Aucun déplacement possible : la sélection est déjà en limite de liste."
                : $"{moved:N0} élément(s) déplacé(s) {label}.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Réordonnancement impossible : {ex.Message}";
        }
    }

    private void OnChangeParentClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<PackEditorNode> selection = GetElementOperationSelection();
        if (selection.Count > 0)
        {
            var dialog = new MoveTargetDialog(
                _document.GetFunctionContainers(),
                "Changer le parent des éléments",
                "Sélectionne la catégorie ou le groupe qui recevra les éléments cochés. Les éléments conservent leurs paramètres et leurs propriétés JSON.",
                "Sélectionne une catégorie ou un groupe cible.") { Owner = this };
            if (dialog.ShowDialog() != true || dialog.SelectedTarget is null) return;

            try
            {
                int moved = _document.MoveElementsToContainer(selection, dialog.SelectedTarget);
                RefreshTree();
                RefreshValidation(moved == 0
                    ? "Aucun changement : les éléments se trouvent déjà dans ce parent."
                    : $"{moved:N0} élément(s) déplacé(s) vers « {dialog.SelectedTarget.Path} »." );
            }
            catch (Exception ex)
            {
                EditorStatusText.Text = $"Changement de parent impossible : {ex.Message}";
            }
            return;
        }

        PackEditorNode? node = _selectedNode;
        if (node is null) return;
        IReadOnlyList<PackEditorNode> targets = _document.GetStructureMoveTargets(node);
        if (targets.Count == 0)
        {
            EditorStatusText.Text = "Aucun parent compatible disponible pour ce sous-arbre.";
            return;
        }

        var subtreeDialog = new MoveTargetDialog(
            targets,
            "Déplacer un sous-arbre",
            "Sélectionne le nouveau parent. La bibliothèque, la catégorie ou le groupe sera déplacé avec tous ses descendants.",
            "Sélectionne un parent compatible.") { Owner = this };
        if (subtreeDialog.ShowDialog() != true || subtreeDialog.SelectedTarget is null) return;

        try
        {
            bool moved = _document.MoveSubtreeToParent(node, subtreeDialog.SelectedTarget);
            RefreshTree(node);
            RefreshValidation(moved
                ? $"Sous-arbre « {node.Name} » déplacé vers « {subtreeDialog.SelectedTarget.Path} »."
                : "Aucun changement : ce sous-arbre se trouve déjà dans ce parent.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Déplacement du sous-arbre impossible : {ex.Message}";
        }
    }

    private void OnDuplicateSubtreeClick(object sender, RoutedEventArgs e)
    {
        PackEditorNode? node = _selectedNode;
        if (node is null) return;
        try
        {
            PackEditorNode clone = _document.DuplicateSubtree(node);
            RefreshTree(clone);
            RefreshValidation($"Sous-arbre « {node.Name} » dupliqué sous « {clone.Name} » avec tout son contenu.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Duplication du sous-arbre impossible : {ex.Message}";
        }
    }

    private void OnExportPartialClick(object sender, RoutedEventArgs e)
    {
        PackEditorNode? node = _selectedNode;
        if (!PackEditorDocument.CanExportPartialNode(node)) return;

        var dialog = new SaveFileDialog
        {
            Title = "Exporter un fragment JC Lib",
            Filter = "Fragment JC Lib (*.jclib-fragment.json)|*.jclib-fragment.json|Fichier JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
            FileName = BuildSafeFileName(node!.Name) + ".jclib-fragment.json",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _document.ExportPartialNode(node, dialog.FileName);
            EditorStatusText.Text = $"Fragment exporté : {dialog.FileName}";
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Export partiel impossible : {ex.Message}";
        }
    }

    private void OnImportPartialClick(object sender, RoutedEventArgs e)
    {
        PackEditorNode? destination = _selectedNode;
        if (!PackEditorDocument.CanImportPartialNode(destination)) return;

        var dialog = new OpenFileDialog
        {
            Title = "Importer un fragment JC Lib",
            Filter = "Fragment JC Lib (*.jclib-fragment.json;*.json)|*.jclib-fragment.json;*.json|Tous les fichiers (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            PackEditorNode imported = _document.ImportPartialNode(destination!, dialog.FileName);
            RefreshTree(imported);
            RefreshValidation($"Fragment importé sous « {destination!.Path} » : « {imported.Name} ».");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Import partiel impossible : {ex.Message}";
        }
    }

    private static string BuildSafeFileName(string value)
    {
        string result = string.IsNullOrWhiteSpace(value) ? "jclib-fragment" : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars()) result = result.Replace(invalid, '_');
        return result.Replace(' ', '_');
    }

    private void OnDeleteSelectionClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<PackEditorNode> batch = _document.GetBatchSelectedElements();
        if (batch.Count == 0) return;

        MessageBoxResult answer = MessageBox.Show(
            $"Supprimer définitivement les {batch.Count:N0} éléments cochés ?",
            "JC Lib — suppression groupée",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            int deleted = _document.DeleteElements(batch);
            RefreshTree();
            RefreshValidation($"{deleted:N0} élément(s) supprimé(s).");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Suppression groupée impossible : {ex.Message}";
        }
    }

    private void OnClearBatchSelectionClick(object sender, RoutedEventArgs e)
    {
        _document.ClearBatchSelection();
        RefreshActionButtons();
        EditorStatusText.Text = "Sélection groupée effacée.";
    }

    private void OnAddEnvironmentClick(object sender, RoutedEventArgs e) => AddStructure(() => _document.AddEnvironment(), "Environnement");

    private void OnAddLibraryClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is null) return;
        AddStructure(() => _document.AddLibrary(_selectedNode), "Bibliothèque");
    }

    private void OnAddCategoryClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is null) return;
        AddStructure(() => _document.AddCategory(_selectedNode), "Catégorie");
    }

    private void OnAddGroupClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is null) return;
        AddStructure(() => _document.AddGroup(_selectedNode), "Groupe");
    }

    private void AddStructure(Func<PackEditorNode> action, string label)
    {
        try
        {
            PackEditorNode added = action();
            RefreshTree(added);
            RefreshValidation($"{label} « {added.Name} » ajouté. Sélectionne-le pour le renommer.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Ajout impossible : {ex.Message}";
        }
    }

    private void OnAddElementClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is null) return;
        AddStructure(() => _document.AddElement(_selectedNode), "Élément");
    }

    private void OnDeleteStructureClick(object sender, RoutedEventArgs e)
    {
        PackEditorNode? node = _selectedNode;
        if (node is null || node.Kind == PackEditorNodeKind.Pack || node.Kind == PackEditorNodeKind.Element) return;
        DeleteSelectedNode(node, "nœud et tout son contenu");
    }

    private void OnDeleteElementClick(object sender, RoutedEventArgs e)
    {
        PackEditorNode? node = _selectedNode;
        if (node?.Kind != PackEditorNodeKind.Element) return;
        DeleteSelectedNode(node, "élément");
    }

    private void DeleteSelectedNode(PackEditorNode node, string label)
    {
        MessageBoxResult answer = MessageBox.Show(
            $"Supprimer définitivement ce {label} ?\n\n{node.Path}",
            "JC Lib — suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            string deletedName = node.Name;
            _document.DeleteNode(node);
            RefreshTree();
            RefreshValidation($"« {deletedName} » supprimé.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Suppression impossible : {ex.Message}";
        }
    }

    private void OnAddParameterClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode?.Kind != PackEditorNodeKind.Element) return;
        try
        {
            PackEditorParameter parameter = _document.AddParameter(_selectedNode);
            RefreshParameterList(parameter);
            ShowParameter(parameter);
            RefreshValidation($"Paramètre « {parameter.Name} » ajouté.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Ajout du paramètre impossible : {ex.Message}";
        }
    }

    private void OnDeleteParameterClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode?.Kind != PackEditorNodeKind.Element || _selectedParameter is null) return;
        MessageBoxResult answer = MessageBox.Show(
            $"Supprimer le paramètre « {_selectedParameter.Name} » ?",
            "JC Lib — suppression de paramètre",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            string name = _selectedParameter.Name;
            _document.DeleteParameter(_selectedNode, _selectedParameter);
            RefreshParameterList();
            RefreshValidation($"Paramètre « {name} » supprimé.");
        }
        catch (Exception ex)
        {
            EditorStatusText.Text = $"Suppression du paramètre impossible : {ex.Message}";
        }
    }

    private void OnValidateClick(object sender, RoutedEventArgs e) => RefreshValidation();

    private void OnSaveClick(object sender, RoutedEventArgs e) => SaveDocument(closeAfterSave: false);

    private void OnSaveAndCloseClick(object sender, RoutedEventArgs e)
    {
        if (SaveDocument(closeAfterSave: true)) Close();
    }

    private bool SaveDocument(bool closeAfterSave)
    {
        try
        {
            if (_document.IsDirty)
            {
                _document.Save();
                WasSaved = true;
                LoadPackMetadata();
                RefreshTree();
            }

            RefreshValidation(closeAfterSave
                ? "Pack sauvegardé. Fermeture de l'éditeur..."
                : "Pack sauvegardé. Le navigateur principal sera rechargé à la fermeture de l'éditeur.");
            return true;
        }
        catch (Exception ex)
        {
            RefreshValidation($"Sauvegarde impossible : {ex.Message}");
            return false;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_document.IsDirty) return;

        MessageBoxResult answer = MessageBox.Show(
            "Des modifications ne sont pas sauvegardées. Fermer l'éditeur sans enregistrer ?",
            "JC Lib — modifications non sauvegardées",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) e.Cancel = true;
    }

    private static void ExpandAncestors(PackEditorNode node)
    {
        PackEditorNode? current = node.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private static PackEditorNode? FindAncestor(PackEditorNode node, PackEditorNodeKind kind)
    {
        PackEditorNode? current = node;
        while (current is not null)
        {
            if (current.Kind == kind) return current;
            current = current.Parent;
        }
        return null;
    }

    private static PackEditorNode? ResolveContainer(PackEditorNode node)
    {
        PackEditorNode? current = node.Kind == PackEditorNodeKind.Element ? node.Parent : node;
        while (current is not null)
        {
            if (current.Kind == PackEditorNodeKind.Category || current.Kind == PackEditorNodeKind.Group) return current;
            current = current.Parent;
        }
        return null;
    }

    private static string Read(JObject value, string propertyName) => value[propertyName]?.Value<string>() ?? string.Empty;

    private static string ReadChoiceArray(JObject value, string propertyName)
    {
        JArray? array = value[propertyName] as JArray;
        if (array is null) return string.Empty;
        return string.Join(Environment.NewLine, array.Select(item =>
        {
            if (item.Type == JTokenType.String) return item.Value<string>() ?? string.Empty;
            if (item is not JObject choice) return item.ToString(Formatting.None);
            string optionValue = choice["value"]?.Value<string>() ?? choice["constant"]?.Value<string>() ?? string.Empty;
            string label = choice["label"]?.Value<string>() ?? string.Empty;
            string description = choice["description"]?.Value<string>() ?? string.Empty;
            string detail = choice["detail"]?.Value<string>() ?? string.Empty;
            string[] fields = { optionValue, label, description, detail };
            int lastPopulatedField = fields.Length - 1;
            while (lastPopulatedField >= 0 && string.IsNullOrWhiteSpace(fields[lastPopulatedField]))
            {
                lastPopulatedField--;
            }
            return string.Join(" | ", fields.Take(lastPopulatedField + 1));
        }));
    }

    private static string ReadObject(JObject value, string propertyName)
    {
        return value[propertyName] is JObject obj ? obj.ToString(Formatting.Indented) : string.Empty;
    }
}
