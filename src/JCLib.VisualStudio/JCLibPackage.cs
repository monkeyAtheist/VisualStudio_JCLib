using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace JCLib.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("JC Lib - Visual Studio", "JC Lib browser with visual hierarchy badges, structured parameters, documented options, multi-select pickers, file browsers, external packs and WPF Visual Pack Editor", "1.3.11")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(JCLibToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids80.SolutionExplorer)]
[Guid(PackageGuids.PackageString)]
public sealed class JCLibPackage : AsyncPackage
{
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService is null)
        {
            throw new InvalidOperationException("Unable to access the Visual Studio command service.");
        }

        var commandId = new CommandID(PackageGuids.CommandSet, PackageIds.ShowJCLibToolWindow);
        commandService.AddCommand(new MenuCommand(ExecuteShowToolWindow, commandId));
    }

    private void ExecuteShowToolWindow(object? sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _ = JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                ToolWindowPane? window = await ShowToolWindowAsync(
                    typeof(JCLibToolWindow),
                    id: 0,
                    create: true,
                    cancellationToken: DisposalToken);

                if (window is null)
                {
                    throw new InvalidOperationException("The JC Lib Tool Window could not be created.");
                }
            }
            catch (Exception ex)
            {
                ActivityLog.LogError(nameof(JCLibPackage), ex.ToString());
            }
        });
    }
}
