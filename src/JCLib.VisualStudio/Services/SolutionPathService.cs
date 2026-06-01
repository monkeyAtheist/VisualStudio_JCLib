using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace JCLib.VisualStudio.Services;

public static class SolutionPathService
{
    public static string? TryGetCurrentSolutionDirectory()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        if (solution is null)
        {
            return null;
        }

        int hr = solution.GetSolutionInfo(out string solutionDirectory, out _, out _);
        if (hr < 0 || string.IsNullOrWhiteSpace(solutionDirectory))
        {
            return null;
        }

        return solutionDirectory;
    }
}
