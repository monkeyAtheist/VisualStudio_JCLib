using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JCLib.VisualStudio.Services;

public static class PackStateStore
{
    private const string StateFileName = "disabled-packs.txt";

    public static string GetStateFilePath(bool createDirectory)
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string directory = Path.Combine(localApplicationData, "JCLib", "VisualStudio");
        if (createDirectory)
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, StateFileName);
    }

    public static ISet<string> LoadDisabledPaths()
    {
        string path = GetStateFilePath(createDirectory: true);
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            File.ReadAllLines(path, Encoding.UTF8)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line)),
            StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsEnabled(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return true;
        }

        string normalized = NormalizePath(sourcePath);
        return !LoadDisabledPaths().Contains(normalized);
    }

    public static void SetEnabled(string sourcePath, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Le chemin du pack est vide.", nameof(sourcePath));
        }

        string normalized = NormalizePath(sourcePath);
        var disabled = new HashSet<string>(LoadDisabledPaths(), StringComparer.OrdinalIgnoreCase);
        if (isEnabled)
        {
            disabled.Remove(normalized);
        }
        else
        {
            disabled.Add(normalized);
        }

        Save(disabled);
    }

    public static void RemovePath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        string normalized = NormalizePath(sourcePath);
        var disabled = new HashSet<string>(LoadDisabledPaths(), StringComparer.OrdinalIgnoreCase);
        if (disabled.Remove(normalized))
        {
            Save(disabled);
        }
    }

    private static void Save(IEnumerable<string> disabledPaths)
    {
        string path = GetStateFilePath(createDirectory: true);
        string[] lines = disabledPaths
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string NormalizePath(string sourcePath)
    {
        return Path.GetFullPath(sourcePath.Trim());
    }
}
