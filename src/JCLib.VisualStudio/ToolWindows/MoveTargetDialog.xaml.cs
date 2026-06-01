using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JCLib.VisualStudio.Models;
using JCLib.VisualStudio.Services;

namespace JCLib.VisualStudio;

public partial class MoveTargetDialog : Window
{
    private readonly string _missingTargetMessage;

    public MoveTargetDialog(
        IEnumerable<PackEditorNode> targets,
        string heading = "Choisir le nouveau parent",
        string instructions = "Sélectionne le nouveau parent compatible.",
        string missingTargetMessage = "Sélectionne un parent compatible.")
    {
        InitializeComponent();
        ThemeService.ApplyTheme(this, UserPreferencesStore.Load().Theme);
        HeadingText.Text = heading;
        InstructionsText.Text = instructions;
        _missingTargetMessage = missingTargetMessage;
        PackEditorNode[] values = (targets ?? throw new ArgumentNullException(nameof(targets)))
            .OrderBy(target => target.Path, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        TargetsListBox.ItemsSource = values;
        if (values.Length > 0) TargetsListBox.SelectedIndex = 0;
    }

    public PackEditorNode? SelectedTarget => TargetsListBox.SelectedItem as PackEditorNode;

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (SelectedTarget is null)
        {
            MessageBox.Show(
                _missingTargetMessage,
                "JC Lib — destination manquante",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
