using System;

namespace JCLib.VisualStudio;

internal static class PackageGuids
{
    public const string PackageString = "6f22038e-dc48-46db-9f71-b3553a5ec9be";
    public const string CommandSetString = "6371953d-2183-42f5-91d4-3086ff77c11f";
    public const string ToolWindowString = "e9466b16-12f8-4c46-99e3-4e793ad2525b";

    public static readonly Guid CommandSet = new Guid(CommandSetString);
}
