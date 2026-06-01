using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace JCLib.VisualStudio;

[Guid(PackageGuids.ToolWindowString)]
public sealed class JCLibToolWindow : ToolWindowPane
{
    public JCLibToolWindow() : base(null)
    {
        Caption = "JC Lib";
        Content = new JCLibToolWindowControl();
    }
}
