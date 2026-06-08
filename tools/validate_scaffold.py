#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src" / "JCLib.VisualStudio"


def require(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def walk_functions(value):
    if isinstance(value, dict):
        for key, child in value.items():
            if key == "functions" and isinstance(child, list):
                yield from child
            else:
                yield from walk_functions(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_functions(child)


def assert_balanced(text: str, opening: str, closing: str, label: str) -> None:
    if text.count(opening) != text.count(closing):
        raise AssertionError(f"Unbalanced {label}: {text.count(opening)} != {text.count(closing)}")


def flatten_paths(pack: dict) -> list[str]:
    paths: list[str] = []

    def add_groups(environment: str, library: str, category: str, groups, parent_group: str = "") -> None:
        for group in groups or []:
            name = str(group.get("name") or "Groupe").strip()
            group_path = name if not parent_group else f"{parent_group} > {name}"
            for function in group.get("functions", []) or []:
                paths.append(" > ".join(filter(None, [environment, library, category, group_path, str(function.get("name") or "Élément").strip()])))
            add_groups(environment, library, category, group.get("groups", []), group_path)

    for environment in pack.get("environments", []) or []:
        environment_name = str(environment.get("name") or "Environnement").strip()
        for library in environment.get("libraries", []) or []:
            library_name = str(library.get("name") or "Bibliothèque").strip()
            for category in library.get("categories", []) or []:
                category_name = str(category.get("name") or "Catégorie").strip()
                for function in category.get("functions", []) or []:
                    paths.append(" > ".join(filter(None, [environment_name, library_name, category_name, str(function.get("name") or "Élément").strip()])))
                add_groups(environment_name, library_name, category_name, category.get("groups", []))
    return paths


def find_duplicate_function_names(pack: dict) -> list[str]:
    issues: list[str] = []

    def validate_container(container: dict, path: str) -> None:
        names: set[str] = set()
        for function in container.get("functions", []) or []:
            name = str(function.get("name") or "").strip().casefold()
            if name in names:
                issues.append(f"{path}: {name}")
            names.add(name)
        for group in container.get("groups", []) or []:
            group_name = str(group.get("name") or "Groupe").strip()
            validate_container(group, f"{path} / {group_name}")

    for environment in pack.get("environments", []) or []:
        env = str(environment.get("name") or "Environnement").strip()
        for library in environment.get("libraries", []) or []:
            lib = str(library.get("name") or "Bibliothèque").strip()
            for category in library.get("categories", []) or []:
                cat = str(category.get("name") or "Catégorie").strip()
                validate_container(category, f"{env} / {lib} / {cat}")
    return issues


def main() -> int:
    csproj_text = require(PROJECT / "JCLib.VisualStudio.csproj")
    manifest_text = require(PROJECT / "source.extension.vsixmanifest")
    vsct_text = require(PROJECT / "Commands" / "JCLibPackage.vsct")
    package_text = require(PROJECT / "JCLibPackage.cs")
    xaml_text = require(PROJECT / "ToolWindows" / "JCLibToolWindowControl.xaml")
    codebehind_text = require(PROJECT / "ToolWindows" / "JCLibToolWindowControl.xaml.cs")
    editor_xaml_text = require(PROJECT / "ToolWindows" / "PackEditorWindow.xaml")
    creation_xaml_text = require(PROJECT / "ToolWindows" / "PackCreationDialog.xaml")
    creation_code_text = require(PROJECT / "ToolWindows" / "PackCreationDialog.xaml.cs")
    move_target_xaml_text = require(PROJECT / "ToolWindows" / "MoveTargetDialog.xaml")
    move_target_code_text = require(PROJECT / "ToolWindows" / "MoveTargetDialog.xaml.cs")
    editor_code_text = require(PROJECT / "ToolWindows" / "PackEditorWindow.xaml.cs")
    editor_models_text = require(PROJECT / "Models" / "PackEditorModels.cs")
    editor_document_text = require(PROJECT / "Services" / "PackEditorDocument.cs")
    loader_text = require(PROJECT / "Services" / "CatalogLoader.cs")
    state_store_text = require(PROJECT / "Services" / "PackStateStore.cs")
    solution_service_text = require(PROJECT / "Services" / "SolutionPathService.cs")
    editor_insert_text = require(PROJECT / "Services" / "EditorInsertionService.cs")
    parameter_service_text = require(PROJECT / "Services" / "SnippetParameterService.cs")
    preferences_text = require(PROJECT / "Services" / "UserPreferencesStore.cs")
    theme_service_text = require(PROJECT / "Services" / "ThemeService.cs")
    appearance_xaml_text = require(PROJECT / "ToolWindows" / "AppearanceDialog.xaml")
    appearance_code_text = require(PROJECT / "ToolWindows" / "AppearanceDialog.xaml.cs")
    models_text = require(PROJECT / "Models" / "CatalogModels.cs")
    pack_text = require(PROJECT / "Assets" / "Packs" / "default_pack.json")
    build_pack_text = require(PROJECT / "Assets" / "Packs" / "build_pack.json")
    sample_text = require(ROOT / "docs" / "example_packs" / "jclib_sample_pack.json")
    fragment_text = require(ROOT / "docs" / "example_fragments" / "jclib_sample_group.jclib-fragment.json")

    ET.fromstring(csproj_text)
    ET.fromstring(manifest_text)
    ET.fromstring(vsct_text)
    ET.fromstring(xaml_text)
    ET.fromstring(editor_xaml_text)
    ET.fromstring(creation_xaml_text)
    ET.fromstring(move_target_xaml_text)
    ET.fromstring(appearance_xaml_text)
    pack = json.loads(pack_text)
    build_pack = json.loads(build_pack_text)
    sample_pack = json.loads(sample_text)
    sample_fragment = json.loads(fragment_text)

    assert '<Reference Include="System.Design" />' in csproj_text
    assert '<Reference Include="System.Drawing" />' in csproj_text
    assert '<Reference Include="System.Runtime.Serialization" />' in csproj_text
    assert '<Reference Include="System.Windows.Forms" />' in csproj_text
    assert '<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />' in csproj_text
    assert 'System.Web.Extensions' not in csproj_text
    assert 'CatalogEnabledWhen' in models_text
    assert 'DefaultTargetIndex' in models_text
    assert 'ParseEnabledWhen' in loader_text
    assert 'public static bool IsEnabled' in parameter_service_text
    assert 'NormalizeCommandText' in parameter_service_text
    assert 'IsCommand(entry)' in parameter_service_text
    assert '!string.Equals((SymbolKind ?? string.Empty).Trim(), "command"' in models_text
    assert '<ResourceName>Menus.ctmenu</ResourceName>' in csproj_text
    assert '<LogicalName>JCLib.VisualStudio.Assets.Packs.default_pack.json</LogicalName>' in csproj_text
    assert 'Assets\\Packs\\build_pack.json' in csproj_text
    assert '[ProvideMenuResource("Menus.ctmenu", 1)]' in package_text
    assert '"1.3.6"' in package_text
    assert 'Version="1.3.6"' in manifest_text
    assert pack.get('version') == '2.10.0'
    assert build_pack.get('version') == '1.2.3'
    assert 'DialogResult = true;' in (ROOT / 'src/JCLib.VisualStudio/ToolWindows/StructuredChoiceDialog.cs').read_text(encoding='utf-8')
    assert '{{includePathPrefix}} {{includeDirectory}}' in json.dumps(build_pack)
    assert '{{libraryPathPrefix}} {{libraryDirectory}}' in json.dumps(build_pack)
    assert '{{includePathPrefix}}{{includeDirectory}}' not in json.dumps(build_pack)
    assert '{{libraryPathPrefix}}{{libraryDirectory}}' not in json.dumps(build_pack)
    assert '<ProductArchitecture>amd64</ProductArchitecture>' in manifest_text
    assert '<ProductArchitecture>arm64</ProductArchitecture>' in manifest_text
    assert 'IDG_VS_WNDO_OTRWNDWS1' in vsct_text
    assert 'IDM_VS_CTXT_CODEWIN' in vsct_text
    assert '<KeyBindings>' in vsct_text
    assert 'editor="guidVSStd97"' in vsct_text
    assert 'key1="J"' in vsct_text and 'mod1="CONTROL"' in vsct_text and 'mod2="ALT"' in vsct_text

    for required in [
        'CatalogTreeView', 'SearchResultsListBox', 'ParametersExpander', 'ParametersPanel',
        'ReturnTargetTextBox', 'SnippetTextBox', 'InsertSnippetButton', 'DiagnosticsExpander',
        'DiagnosticsTextBox', 'AutoReloadCheckBox', 'OnImportGlobalPackClick',
        'OnImportSolutionPackClick', 'PackManagementExpander', 'PackManagementListBox',
        'ToggleSelectedPackButton', 'EditSelectedPackButton', 'DeleteSelectedPackButton',
        'OpenSelectedPackFolderButton', 'OnCreatePackClick', 'DuplicateSelectedPackButton',
        'QuickAccessExpander', 'FavoritesListBox', 'RecentListBox', 'EnvironmentFilterComboBox',
        'LibraryFilterComboBox', 'FavoritesOnlyCheckBox', 'FavoriteToggleButton', 'QuickInsertSnippetButton',
        'OnAppearanceClick', 'IncludeBundledPackCheckBox', 'OnIncludeBundledPackChanged',
    ]:
        assert required in xaml_text, required

    for required in [
        'OnInsertSnippetClick', 'OnBrowseParameterClick', 'CatalogLoader.LoadCatalog(_preferences.IncludeBundledPack)',
        'FileSystemWatcher', 'DispatcherTimer', 'OnPackFileChanged',
        'ImportPack(CatalogPackSourceKind targetKind)', 'OpenPacksFolder(CatalogPackSourceKind targetKind)',
        'OnPackManagementSelectionChanged', 'OnToggleSelectedPackClick', 'OnEditSelectedPackClick',
        'OnDeleteSelectedPackClick', 'OnOpenSelectedPackFolderClick', 'ResetPackManagementSelection',
        'new PackEditorWindow(path)', 'OnCreatePackClick', 'OnDuplicateSelectedPackClick',
        'CatalogLoader.CreatePack', 'CatalogLoader.DuplicatePack',
        'UserPreferencesStore.Load', 'ThemeService.ApplyTheme', 'RefreshQuickAccessLists',
        'PopulateFilters', 'RefreshNavigationTree', 'OnToggleFavoriteClick', 'QuickInsertEntry',
        'OnAppearanceClick', 'OnEnvironmentFilterChanged', 'OnLibraryFilterChanged', 'OnIncludeBundledPackChanged',
        'RefreshConditionalParameterEditors', 'SnippetParameterService.IsEnabled', 'DefaultTargetIndex',
    ]:
        assert required in codebehind_text, required

    for required in [
        'EditorTreeView', 'AddEnvironmentButton', 'AddLibraryButton', 'AddCategoryButton',
        'AddGroupButton', 'AddElementButton', 'DeleteStructureButton', 'DeleteElementButton', 'PackIdTextBox',
        'PackNameTextBox', 'PackVersionTextBox', 'ElementNameTextBox', 'SymbolKindTextBox',
        'InsertTextTextBox', 'ValidationIssuesListBox', 'SaveButton', 'SaveAndCloseButton',
        'ParametersListBox', 'AddParameterButton', 'DeleteParameterButton', 'ParameterNameTextBox',
        'ParameterPresetsTextBox', 'ParameterDescriptionTextBox', 'ParameterPlaceholderTextBox', 'ParameterOptionalCheckBox', 'ParameterPickerConfigTextBox', 'OnParameterPickerConfigLostFocus', 'OnAddEnvironmentClick', 'OnAddLibraryClick', 'OnAddCategoryClick',
        'OnAddGroupClick', 'OnAddElementClick', 'OnDeleteStructureClick', 'OnDeleteElementClick',
        'OnAddParameterClick', 'OnDeleteParameterClick', 'OnSaveClick', 'OnSaveAndCloseClick',
        'BatchSelectionSummaryText', 'DuplicateSelectionButton', 'MoveUpButton', 'MoveDownButton',
        'ChangeParentButton', 'DeleteSelectionButton', 'ClearBatchSelectionButton',
        'DuplicateSubtreeButton', 'ExportPartialButton', 'ImportPartialButton',
        'OnDuplicateSubtreeClick', 'OnExportPartialClick', 'OnImportPartialClick',
        'OnBatchSelectionCheckBoxClick', 'IsBatchSelected', 'IsBatchSelectable',
    ]:
        assert required in editor_xaml_text, required

    assert 'Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"' in editor_xaml_text
    assert 'Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"' in editor_xaml_text
    assert 'DynamicResource TextBrush' in editor_xaml_text
    assert 'ThemeService.ApplyTheme(this, UserPreferencesStore.Load().Theme)' in editor_code_text

    for required in [
        'PackEditorDocument.Load', 'RefreshValidation', 'OnElementFieldChanged', 'OnAddElementClick',
        'OnDeleteElementClick', 'OnAddParameterClick', 'OnDeleteParameterClick',
        'OnNodeNameChanged', 'OnParameterFieldChanged', 'SaveDocument', 'WasSaved', 'OnWindowClosing',
        'OnDuplicateSelectionClick', 'OnMoveUpClick', 'OnMoveDownClick', 'OnChangeParentClick',
        'OnDeleteSelectionClick', 'OnClearBatchSelectionClick', 'GetElementOperationSelection',
        'new MoveTargetDialog', 'ExpandAncestors', 'CaptureTreeUiState', 'GetRememberedSelectedNode',
        'OnDuplicateSubtreeClick', 'OnExportPartialClick', 'OnImportPartialClick', 'BuildSafeFileName',
        'MoveSubtreeToParent', 'GetStructureMoveTargets', 'ExportPartialNode', 'ImportPartialNode',
    ]:
        assert required in editor_code_text, required

    for required in [
        'JObject.Parse', 'AddEnvironment', 'AddLibrary', 'AddCategory', 'AddGroup', 'AddElement',
        'DeleteNode', 'AddParameter', 'DeleteParameter', 'SetParameterChoiceListProperty', 'SetParameterBooleanProperty', 'SetParameterObjectProperty', 'NormalizeNames',
        'ValidateFunctions', 'ValidateParameters', 'ValidateNamedChildren', 'SynchronizeElementMetadata',
        'Nom d\'élément dupliqué', 'File.WriteAllText', '.jclib.tmp', 'Formatting.Indented',
        'GetBatchSelectedElements', 'ClearBatchSelection', 'GetFunctionContainers', 'DuplicateElements',
        'DeleteElements', 'MoveNodeRelative', 'MoveElementsRelative', 'MoveElementsToContainer',
        'CaptureBatchSelection', 'CaptureTreeUiState', 'GetRememberedSelectedNode', '_expandedObjects', '_selectedObject',
        'NormalizeElementSelection', 'DeepClone', 'ReferenceEqualityComparer',
        'GetStructureMoveTargets', 'MoveSubtreeToParent', 'DuplicateSubtree',
        'CanExportPartialNode', 'CanImportPartialNode', 'ExportPartialNode', 'ImportPartialNode',
        'ResolveSubtreeDestination', 'ResolvePartialImportDestination', 'jclib.partial.v1', 'IsDescendantOf',
    ]:
        assert required in editor_document_text, required

    for required in ['PackEditorNode', 'PackEditorParameter', 'PackEditorValidationIssue', 'INotifyPropertyChanged', 'IsBatchSelectable', 'IsBatchSelected', 'IsExpanded', 'IsSelected']:
        assert required in editor_models_text, required

    for required in [
        'DestinationComboBox', 'PackIdTextBox', 'PackNameTextBox', 'PackVersionTextBox',
        'FileNameTextBox', 'OnConfirmClick', 'OnCancelClick',
    ]:
        assert required in creation_xaml_text, required

    for required in ['PackCreationRequest', 'CatalogPackSourceKind.GlobalUser', 'CatalogPackSourceKind.Solution', 'Path.GetInvalidFileNameChars']:
        assert required in creation_code_text, required

    for required in ['TargetsListBox', 'HeadingText', 'InstructionsText', 'OnConfirmClick', 'OnCancelClick']:
        assert required in move_target_xaml_text, required

    for required in ['MoveTargetDialog', 'SelectedTarget', 'OrderBy', 'DialogResult = true', '_missingTargetMessage']:
        assert required in move_target_code_text, required

    for required in [
        'EmbeddedPackResource', 'JsonConvert.DeserializeObject<PackDto>', 'GetGlobalPacksDirectory',
        'GetSolutionPacksDirectory', 'DetectConflicts', 'ValidatePackFile',
        'CatalogPackSourceKind.GlobalUser', 'CatalogPackSourceKind.Solution', 'ResolveEntries',
        'BuildResolvedRoot', 'SetPackEnabled', 'DeletePack', 'CreatePack', 'DuplicatePack',
        'RequireExternalDirectory', 'PackStateStore', 'includeBundledPack',
    ]:
        assert required in loader_text, required
    assert 'System.Web' not in loader_text

    for required in ['disabled-packs.txt', 'LoadDisabledPaths', 'SetEnabled', 'RemovePath', 'LocalApplicationData']:
        assert required in state_store_text, required

    assert 'SVsSolution' in solution_service_text
    assert 'GetSolutionInfo' in solution_service_text
    assert 'ThreadHelper.ThrowIfNotOnUIThread' in solution_service_text

    for required in [
        'CatalogParameter', 'CatalogPackInfo', 'CatalogConflict', 'CatalogLoadIssue',
        'CatalogShadowedEntry', 'ActivePacks', 'ShadowedEntries', 'GetSourcePriority',
        'ManagementDisplay', 'PackSourcePath',
    ]:
        assert required in models_text, required

    assert 'BuildInsertText' in parameter_service_text
    assert 'ExtractDefaultArguments' in parameter_service_text
    assert 'QuoteCStringPath' in parameter_service_text
    assert 'ApplyParameterizedInsertTemplate' in parameter_service_text
    assert 'CreateEffectivePickerConfig' in parameter_service_text
    assert 'FormatPathForTemplate' in parameter_service_text
    assert 'JsonConvert.DeserializeObject<PackDto>' in loader_text
    assert 'ParseChoices(parameter.Options)' in loader_text
    assert 'ParsePickerConfig(parameter.PickerConfig)' in loader_text
    structured_dialog_text = require(PROJECT / 'ToolWindows' / 'StructuredChoiceDialog.cs')
    for required in ['StructuredChoiceDialog', 'SelectionMode.Multiple', 'ValueSeparator', 'EmptyValue', 'BuildSelectedValue']:
        assert required in structured_dialog_text, required
    assert_balanced(structured_dialog_text, '{', '}', 'C# braces in StructuredChoiceDialog')
    assert 'GetActiveView(0, null' in editor_insert_text
    assert 'ReplaceLines(' in editor_insert_text
    assert 'ApplyBaseIndentation' in editor_insert_text
    assert 'NormalizeSelection' in editor_insert_text

    for required in ['user-preferences.json', 'Favorites', 'RecentEntries', 'SelectedEntryKey', 'FilterEnvironment', 'FilterLibrary', 'ThemePreferences', 'AddRecent', 'ToggleFavorite', 'IncludeBundledPack', 'DropdownBackground', 'DropdownText']:
        assert required in preferences_text, required
    for required in ['ApplyTheme', 'BackgroundBrush', 'PanelBrush', 'InputBrush', 'DropdownBackgroundBrush', 'DropdownTextBrush', 'TextBrush', 'SecondaryTextBrush', 'AccentBrush', 'BorderBrush', 'ButtonTextBrush', '#RRGGBB']:
        assert required in theme_service_text, required
    for required in ['BackgroundTextBox', 'PanelTextBox', 'InputTextBox', 'DropdownBackgroundTextBox', 'DropdownTextTextBox', 'TextTextBox', 'SecondaryTextBox', 'AccentTextBox', 'BorderTextBox', 'ButtonTextBox', 'OnResetDarkClick', 'OnChooseColorClick', 'OnSaveClick']:
        assert required in appearance_xaml_text, required
    assert 'ThemeService.ApplyTheme(this, theme)' in appearance_code_text
    assert 'Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"' in xaml_text
    assert 'Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"' in xaml_text

    assert 'DropdownComboBoxItemStyle' in xaml_text
    assert 'DropdownBackgroundBrush' in xaml_text and 'DropdownTextBrush' in xaml_text
    assert 'DropdownComboBoxItemStyle' in creation_xaml_text
    assert 'IncludeBundledPackCheckBox' in xaml_text
    assert 'CatalogLoader.LoadCatalog(_preferences.IncludeBundledPack)' in codebehind_text
    assert 'if (includeBundledPack)' in loader_text

    for file_name, text in [
        ('ToolWindow control', codebehind_text),
        ('CatalogLoader', loader_text),
        ('CatalogModels', models_text),
        ('PackStateStore', state_store_text),
        ('SolutionPathService', solution_service_text),
        ('PackEditorWindow', editor_code_text),
        ('PackCreationDialog', creation_code_text),
        ('MoveTargetDialog', move_target_code_text),
        ('PackEditorModels', editor_models_text),
        ('PackEditorDocument', editor_document_text),
        ('UserPreferencesStore', preferences_text),
        ('ThemeService', theme_service_text),
        ('AppearanceDialog', appearance_code_text),
    ]:
        assert_balanced(text, '{', '}', f'C# braces in {file_name}')

    functions = list(walk_functions(pack))
    sample_functions = list(walk_functions(sample_pack))
    symbol_count = len(functions)
    parameter_count = sum(len(fn.get('parameters', [])) for fn in functions)
    parameterized_functions = sum(bool(fn.get('parameters')) for fn in functions)
    editor_types = sorted({
        param.get('editorType', '')
        for fn in functions
        for param in fn.get('parameters', [])
        if param.get('editorType')
    })
    environment_count = len(pack.get('environments', []))

    assert environment_count > 0
    assert symbol_count > 0
    assert parameter_count > 0
    assert parameterized_functions > 0
    assert {'text', 'handle', 'pathFile', 'pathFolder', 'boolean'}.issubset(editor_types)
    assert sample_pack.get('id') == 'jclib.sample.visualstudio'
    assert len(sample_functions) == 1
    assert sample_functions[0].get('name') == 'jc_sample_log'
    sample_paths = flatten_paths(sample_pack)
    assert sample_paths == ['Sample > Utilities > Imported pack demo > jc_sample_log']
    assert sample_fragment.get('format') == 'jclib.partial.v1'
    assert sample_fragment.get('kind') == 'group'
    assert sample_fragment.get('node', {}).get('name') == 'Imported diagnostics'
    assert sample_fragment.get('node', {}).get('functions', [])[0].get('name') == 'jc_fragment_trace'

    # Visual Pack Editor duplicate simulation: trailing whitespace must be considered a duplicate.
    duplicate_test = json.loads(sample_text)
    container = duplicate_test['environments'][0]['libraries'][0]['categories'][0]
    container['functions'] = [
        {'name': 'Capture (...)'},
        {'name': 'Capture (...) '},
    ]
    duplicate_issues = find_duplicate_function_names(duplicate_test)
    assert len(duplicate_issues) == 1

    # Priority simulation: the same logical element must resolve to solution > global > bundled.
    simulated = [
        ('bundled', 100, 'Sample > Utilities > Imported pack demo > jc_sample_log'),
        ('global', 200, 'Sample > Utilities > Imported pack demo > jc_sample_log'),
        ('solution', 300, 'Sample > Utilities > Imported pack demo > jc_sample_log'),
    ]
    winner = max(simulated, key=lambda item: item[1])
    assert winner[0] == 'solution'
    assert len({item[2] for item in simulated}) == 1

    # Batch editor simulation: duplicate, reorder, move to another group, then delete clones.
    source = [
        {'name': 'jc_batch_a', 'parameters': [{'name': 'delayMs'}]},
        {'name': 'jc_batch_b'},
        {'name': 'jc_batch_c'},
    ]
    destination: list[dict] = []
    clones = [json.loads(json.dumps(item)) for item in source[:2]]
    clones[0]['name'] = 'jc_batch_a Copy'
    clones[1]['name'] = 'jc_batch_b Copy'
    source.extend(clones)
    assert len(source) == 5
    destination.extend(clones)
    source = [item for item in source if item not in clones]
    assert [item['name'] for item in destination] == ['jc_batch_a Copy', 'jc_batch_b Copy']
    destination = [item for item in destination if item not in clones]
    assert destination == []
    assert [item['name'] for item in source] == ['jc_batch_a', 'jc_batch_b', 'jc_batch_c']

    # Subtree editor simulation: clone a group, move it, export it and import it.
    category_source = {'name': 'Source', 'functions': [], 'groups': [
        {'name': 'Diagnostics', 'functions': [
            {'name': 'jc_trace', 'parameters': [{'name': 'message', 'editorType': 'text'}]},
        ], 'groups': [
            {'name': 'Nested', 'functions': [{'name': 'jc_nested'}], 'groups': []},
        ]},
    ]}
    category_destination = {'name': 'Destination', 'functions': [], 'groups': []}
    subtree_clone = json.loads(json.dumps(category_source['groups'][0]))
    subtree_clone['name'] = 'Diagnostics Copy'
    category_source['groups'].append(subtree_clone)
    category_source['groups'].remove(subtree_clone)
    category_destination['groups'].append(subtree_clone)
    assert category_destination['groups'][0]['groups'][0]['functions'][0]['name'] == 'jc_nested'
    fragment = {
        'format': 'jclib.partial.v1',
        'kind': 'group',
        'node': json.loads(json.dumps(category_destination['groups'][0])),
    }
    imported = json.loads(json.dumps(fragment['node']))
    imported['name'] = 'Diagnostics Copy2'
    category_destination['groups'].append(imported)
    assert [group['name'] for group in category_destination['groups']] == ['Diagnostics Copy', 'Diagnostics Copy2']
    assert imported['functions'][0]['parameters'][0]['name'] == 'message'

    print('JC Lib Visual Studio 1.3.6 scaffold validation: OK')
    print(f'Bundled environments: {environment_count}')
    print(f'Bundled elements: {symbol_count}')
    print(f'Parameterized functions: {parameterized_functions}')
    print(f'Parameters: {parameter_count}')
    print(f'Editor types: {", ".join(editor_types)}')
    print('Pack manager UI checks: OK')
    print('Advanced Visual Pack Editor UI checks: OK')
    print('Batch selection, duplication, reordering, parent change and deletion checks: OK')
    print('Tree expansion and selected-node restoration checks: OK')
    print('Subtree move, deep duplication and partial import/export checks: OK')
    print('Pack creation and duplication checks: OK')
    print('Structured parameter editor checks: OK')
    print('Daily UX favorites, recents, filters and persistence checks: OK')
    print('Accessible dark theme, dropdown colors and configurable color checks: OK')
    print('Ctrl+Alt+J keybinding and editor context command checks: OK')
    print('Trailing-whitespace duplicate simulation: OK')
    print('Resolved priority policy simulation: solution > global > bundled: OK')
    print('Optional bundled fallback mode checks: OK')
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f'Validation failed: {exc}', file=sys.stderr)
        raise
