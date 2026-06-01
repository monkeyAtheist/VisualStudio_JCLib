using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JCLib.VisualStudio.Models;

namespace JCLib.VisualStudio.Services;

internal sealed class SnippetParameterValue
{
    public SnippetParameterValue(
        CatalogParameter parameter,
        string editorType,
        string placeholder,
        string value,
        IReadOnlyList<string> suggestedValues)
    {
        Parameter = parameter;
        EditorType = editorType;
        Placeholder = placeholder;
        Value = value;
        SuggestedValues = suggestedValues;
    }

    public CatalogParameter Parameter { get; }

    public string EditorType { get; }

    public string Placeholder { get; }

    public string Value { get; set; }

    public IReadOnlyList<string> SuggestedValues { get; }
}

internal static class SnippetParameterService
{
    public static IReadOnlyList<SnippetParameterValue> CreateEditorState(CatalogEntry entry)
    {
        var resolvedParameters = entry.Parameters
            .Select(parameter => ResolveParameter(parameter))
            .ToList();
        IReadOnlyList<string> defaultsFromSnippet = ExtractDefaultArguments(entry.InsertText, resolvedParameters);

        return resolvedParameters
            .Select((parameter, index) => new SnippetParameterValue(
                parameter.Parameter,
                parameter.EditorType,
                parameter.Placeholder,
                index < defaultsFromSnippet.Count && !string.IsNullOrWhiteSpace(defaultsFromSnippet[index])
                    ? defaultsFromSnippet[index]
                    : parameter.DefaultValue,
                parameter.SuggestedValues))
            .ToArray();
    }

    public static string BuildInsertText(
        CatalogEntry entry,
        IReadOnlyList<SnippetParameterValue> values,
        string returnTarget)
    {
        if (!entry.IsFunction)
        {
            return FirstNonEmpty(entry.InsertText, entry.Declaration, entry.Signature, entry.Name);
        }

        string call = values.Count == 0
            ? $"{entry.Name}()"
            : $"{entry.Name}({string.Join(", ", values.Select(value =>
                string.IsNullOrWhiteSpace(value.Value)
                    ? InferDefaultValue(value.Parameter.Name, value.Parameter.Type, value.EditorType)
                    : value.Value.Trim()))})";

        if (entry.HasReturnValue && !string.IsNullOrWhiteSpace(returnTarget))
        {
            return $"{returnTarget.Trim()} = {call};";
        }

        return $"{call};";
    }

    public static string QuoteCStringPath(string path)
    {
        string escaped = (path ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static ResolvedParameter ResolveParameter(CatalogParameter parameter)
    {
        string editorType = InferEditorType(parameter);
        string defaultValue = string.IsNullOrWhiteSpace(parameter.DefaultValue)
            ? InferDefaultValue(parameter.Name, parameter.Type, editorType)
            : parameter.DefaultValue.Trim();
        string placeholder = InferPlaceholder(parameter, editorType);

        var suggestions = new List<string>();
        suggestions.AddRange(parameter.Options);
        suggestions.AddRange(parameter.Presets);
        suggestions.AddRange(InferSuggestions(parameter, editorType));

        return new ResolvedParameter(
            parameter,
            editorType,
            defaultValue,
            placeholder,
            suggestions
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    private static string InferEditorType(CatalogParameter parameter)
    {
        if (!string.IsNullOrWhiteSpace(parameter.EditorType))
        {
            return parameter.EditorType.Trim();
        }

        string name = parameter.Name.ToLowerInvariant();
        string type = parameter.Type.ToLowerInvariant();

        if (name.Contains("file") || name.Contains("filename") || name.Contains("uir"))
        {
            return "pathFile";
        }
        if (name.Contains("folder") || name.Contains("directory") || name.Contains("dir"))
        {
            return "pathFolder";
        }
        if (name.Contains("handle") || type == "initext" || name.Contains("panel"))
        {
            return "handle";
        }
        if (name.Contains("bool") || name.StartsWith("is", StringComparison.Ordinal) || name.StartsWith("enable", StringComparison.Ordinal) || name.EndsWith("flag", StringComparison.Ordinal))
        {
            return "boolean";
        }
        return "text";
    }

    private static string InferDefaultValue(string name, string type, string editorType)
    {
        string lowerType = (type ?? string.Empty).ToLowerInvariant();
        string lowerName = (name ?? string.Empty).ToLowerInvariant();

        if (editorType == "boolean") return "0";
        if (editorType == "pathFile" || editorType == "pathFolder") return "\"\"";

        if (editorType == "handle")
        {
            if (lowerType == "initext" || lowerName.Contains("ini")) return "iniHandle";
            if (lowerName.Contains("parent")) return "0";
            return name;
        }

        if (lowerType.Contains("char") && lowerType.Contains("*"))
        {
            if (lowerName.Contains("section")) return "\"SECTION\"";
            if (lowerName.Contains("key")) return "\"KEY\"";
            return "\"\"";
        }
        if (lowerType.Contains("double") || lowerType.Contains("float")) return "0.0";
        if (lowerType.Contains("int") && lowerType.Contains("*")) return lowerName.Contains("result") ? "&result" : $"&{name}";
        if (lowerType.Contains("*")) return lowerName.Contains("value") ? "&value" : name;
        if (lowerName.Contains("path") || lowerName.Contains("file")) return "\"\"";
        if (lowerName.EndsWith("id", StringComparison.Ordinal)) return name.ToUpperInvariant();
        return name;
    }

    private static string InferPlaceholder(CatalogParameter parameter, string editorType)
    {
        if (editorType == "pathFile") return "\"C:\\\\path\\\\file.uir\"";
        if (editorType == "pathFolder") return "\"C:\\\\path\\\\folder\"";
        if (editorType == "boolean") return "0 ou 1";
        return parameter.Name;
    }

    private static IEnumerable<string> InferSuggestions(CatalogParameter parameter, string editorType)
    {
        string lowerName = parameter.Name.ToLowerInvariant();
        string lowerType = parameter.Type.ToLowerInvariant();

        if (editorType == "boolean")
        {
            yield return "0";
            yield return "1";
        }
        if (editorType == "pathFile" || editorType == "pathFolder")
        {
            yield return "\"\"";
        }
        if (editorType == "handle")
        {
            if (lowerName.Contains("parent")) yield return "0";
            yield return parameter.Name;
        }
        if (lowerName.Contains("section"))
        {
            yield return "\"GENERAL\"";
            yield return "\"SECTION\"";
        }
        if (lowerName.Contains("key"))
        {
            yield return "\"debug\"";
            yield return "\"KEY\"";
        }
        if (lowerName.Contains("value") && lowerType.Contains("*"))
        {
            yield return "&value";
            yield return "&result";
        }
        if (lowerName.EndsWith("id", StringComparison.Ordinal))
        {
            yield return parameter.Name.ToUpperInvariant();
        }
    }

    private static IReadOnlyList<string> ExtractDefaultArguments(
        string insertText,
        IReadOnlyList<ResolvedParameter> parameters)
    {
        int start = (insertText ?? string.Empty).IndexOf('(');
        int end = (insertText ?? string.Empty).LastIndexOf(')');

        if (start < 0 || end <= start)
        {
            return parameters.Select(parameter => parameter.DefaultValue).ToArray();
        }

        string rawArguments = insertText.Substring(start + 1, end - start - 1).Trim();
        if (rawArguments.Length == 0)
        {
            return Array.Empty<string>();
        }

        var parsed = SplitCallArguments(rawArguments).ToList();
        while (parsed.Count < parameters.Count)
        {
            parsed.Add(parameters[parsed.Count].DefaultValue);
        }
        return parsed;
    }

    private static IEnumerable<string> SplitCallArguments(string text)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        int parentheses = 0;
        int brackets = 0;
        int braces = 0;
        char quote = '\0';
        bool escaped = false;

        foreach (char character in text)
        {
            if (quote != '\0')
            {
                current.Append(character);
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
                continue;
            }

            if (character == '\'' || character == '\"')
            {
                quote = character;
                current.Append(character);
                continue;
            }

            switch (character)
            {
                case '(':
                    parentheses++;
                    break;
                case ')':
                    parentheses = Math.Max(0, parentheses - 1);
                    break;
                case '[':
                    brackets++;
                    break;
                case ']':
                    brackets = Math.Max(0, brackets - 1);
                    break;
                case '{':
                    braces++;
                    break;
                case '}':
                    braces = Math.Max(0, braces - 1);
                    break;
                case ',':
                    if (parentheses == 0 && brackets == 0 && braces == 0)
                    {
                        values.Add(current.ToString().Trim());
                        current.Clear();
                        continue;
                    }
                    break;
            }
            current.Append(character);
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private sealed class ResolvedParameter
    {
        public ResolvedParameter(
            CatalogParameter parameter,
            string editorType,
            string defaultValue,
            string placeholder,
            IReadOnlyList<string> suggestedValues)
        {
            Parameter = parameter;
            EditorType = editorType;
            DefaultValue = defaultValue;
            Placeholder = placeholder;
            SuggestedValues = suggestedValues;
        }

        public CatalogParameter Parameter { get; }
        public string EditorType { get; }
        public string DefaultValue { get; }
        public string Placeholder { get; }
        public IReadOnlyList<string> SuggestedValues { get; }
    }
}
