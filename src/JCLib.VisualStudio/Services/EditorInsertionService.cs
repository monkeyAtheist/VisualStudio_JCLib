using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace JCLib.VisualStudio.Services;

internal sealed class EditorInsertionResult
{
    public EditorInsertionResult(bool success, string message, bool replacedSelection = false)
    {
        Success = success;
        Message = message;
        ReplacedSelection = replacedSelection;
    }

    public bool Success { get; }

    public string Message { get; }

    public bool ReplacedSelection { get; }
}

internal static class EditorInsertionService
{
    public static EditorInsertionResult InsertSnippet(string snippet)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrWhiteSpace(snippet))
        {
            return new EditorInsertionResult(false, "Aucun snippet n'est défini pour cet élément.");
        }

        var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
        if (textManager is null)
        {
            return new EditorInsertionResult(false, "Le service éditeur Visual Studio est indisponible.");
        }

        // The Tool Window owns the keyboard focus when the user clicks the button.
        // fMustHaveFocus = 0 intentionally retrieves the previously active code view.
        int hr = textManager.GetActiveView(0, null, out IVsTextView activeView);
        if (ErrorHandler.Failed(hr) || activeView is null)
        {
            return new EditorInsertionResult(false, "Aucun éditeur texte actif. Ouvre un fichier source, place le curseur puis réessaie.");
        }

        ErrorHandler.ThrowOnFailure(activeView.GetBuffer(out IVsTextLines textLines));
        ErrorHandler.ThrowOnFailure(activeView.GetSelection(
            out int anchorLine,
            out int anchorColumn,
            out int endLine,
            out int endColumn));

        NormalizeSelection(
            anchorLine,
            anchorColumn,
            endLine,
            endColumn,
            out int startLine,
            out int startColumn,
            out int normalizedEndLine,
            out int normalizedEndColumn);

        startColumn = ClampColumnToLineLength(textLines, startLine, startColumn);
        normalizedEndColumn = ClampColumnToLineLength(textLines, normalizedEndLine, normalizedEndColumn);

        bool replacesSelection = startLine != normalizedEndLine || startColumn != normalizedEndColumn;
        string indentation = ReadLeadingWhitespace(textLines, startLine);
        string insertionText = ApplyBaseIndentation(snippet, indentation);

        var changedSpan = new TextSpan[1];
        IntPtr nativeText = Marshal.StringToCoTaskMemUni(insertionText);

        try
        {
            ErrorHandler.ThrowOnFailure(textLines.ReplaceLines(
                startLine,
                startColumn,
                normalizedEndLine,
                normalizedEndColumn,
                nativeText,
                insertionText.Length,
                changedSpan));
        }
        finally
        {
            Marshal.FreeCoTaskMem(nativeText);
        }

        TextSpan insertedSpan = changedSpan[0];
        ErrorHandler.ThrowOnFailure(activeView.SetSelection(
            insertedSpan.iEndLine,
            insertedSpan.iEndIndex,
            insertedSpan.iEndLine,
            insertedSpan.iEndIndex));
        ErrorHandler.ThrowOnFailure(activeView.SetCaretPos(insertedSpan.iEndLine, insertedSpan.iEndIndex));
        ErrorHandler.ThrowOnFailure(activeView.EnsureSpanVisible(insertedSpan));

        return replacesSelection
            ? new EditorInsertionResult(true, "Snippet inséré : la sélection active a été remplacée.", replacedSelection: true)
            : new EditorInsertionResult(true, "Snippet inséré à la position du curseur.");
    }

    private static void NormalizeSelection(
        int anchorLine,
        int anchorColumn,
        int endLine,
        int endColumn,
        out int startLine,
        out int startColumn,
        out int normalizedEndLine,
        out int normalizedEndColumn)
    {
        bool anchorComesFirst = anchorLine < endLine ||
            (anchorLine == endLine && anchorColumn <= endColumn);

        if (anchorComesFirst)
        {
            startLine = anchorLine;
            startColumn = anchorColumn;
            normalizedEndLine = endLine;
            normalizedEndColumn = endColumn;
            return;
        }

        startLine = endLine;
        startColumn = endColumn;
        normalizedEndLine = anchorLine;
        normalizedEndColumn = anchorColumn;
    }

    private static int ClampColumnToLineLength(IVsTextLines textLines, int line, int column)
    {
        ErrorHandler.ThrowOnFailure(textLines.GetLengthOfLine(line, out int length));
        return Math.Max(0, Math.Min(column, length));
    }

    private static string ReadLeadingWhitespace(IVsTextLines textLines, int line)
    {
        ErrorHandler.ThrowOnFailure(textLines.GetLengthOfLine(line, out int length));
        ErrorHandler.ThrowOnFailure(textLines.GetLineText(line, 0, line, length, out string lineText));

        int index = 0;
        while (index < lineText.Length && (lineText[index] == ' ' || lineText[index] == '\t'))
        {
            index++;
        }

        return lineText.Substring(0, index);
    }

    private static string ApplyBaseIndentation(string snippet, string indentation)
    {
        string normalized = snippet
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        bool endsWithNewLine = normalized.EndsWith("\n", StringComparison.Ordinal);
        string[] lines = normalized.Split('\n');

        int lineCount = endsWithNewLine ? lines.Length - 1 : lines.Length;
        string body = string.Join("\r\n" + indentation, lines, 0, lineCount);

        return endsWithNewLine ? body + "\r\n" : body;
    }
}
