using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JCLib.VisualStudio.Models;

namespace JCLib.VisualStudio.Services;

internal sealed class SnippetParameterValue
{
    public SnippetParameterValue(
        CatalogParameter parameter,
        string editorType,
        string placeholder,
        string value,
        string effectiveDefaultValue,
        IReadOnlyList<CatalogChoice> suggestedChoices,
        CatalogPickerConfig? pickerConfig)
    {
        Parameter = parameter;
        EditorType = editorType;
        Placeholder = placeholder;
        Value = value;
        EffectiveDefaultValue = effectiveDefaultValue;
        SuggestedChoices = suggestedChoices;
        PickerConfig = pickerConfig;
    }

    public CatalogParameter Parameter { get; }

    public string EditorType { get; }

    public string Placeholder { get; }

    public string Value { get; set; }

    public string EffectiveDefaultValue { get; }

    public IReadOnlyList<CatalogChoice> SuggestedChoices { get; }

    public IReadOnlyList<string> SuggestedValues => SuggestedChoices.Select(choice => choice.Value).ToArray();

    public CatalogPickerConfig? PickerConfig { get; }
}

internal static class SnippetParameterService
{
    private static readonly Regex ParameterTemplateRegex = new Regex(@"\{\{([A-Za-z_][A-Za-z0-9_]*)\}\}", RegexOptions.Compiled);

    public static IReadOnlyList<SnippetParameterValue> CreateEditorState(CatalogEntry entry)
    {
        var resolvedParameters = entry.Parameters
            .Select(ResolveParameter)
            .ToList();
        IReadOnlyList<string> defaultsFromSnippet = HasParameterizedInsertTemplate(entry)
            ? resolvedParameters.Select(parameter => parameter.DefaultValue).ToArray()
            : ExtractDefaultArguments(entry.InsertText, resolvedParameters);

        return resolvedParameters
            .Select((parameter, index) => new SnippetParameterValue(
                parameter.Parameter,
                parameter.EditorType,
                parameter.Placeholder,
                index < defaultsFromSnippet.Count && !string.IsNullOrWhiteSpace(defaultsFromSnippet[index])
                    ? defaultsFromSnippet[index]
                    : parameter.DefaultValue,
                parameter.DefaultValue,
                parameter.SuggestedChoices,
                parameter.PickerConfig))
            .ToArray();
    }

    public static string BuildInsertText(
        CatalogEntry entry,
        IReadOnlyList<SnippetParameterValue> values,
        string returnTarget)
    {
        if (HasParameterizedInsertTemplate(entry))
        {
            string rendered = ApplyParameterizedInsertTemplate(entry, values);
            if (!entry.IsCallable || !IsSimpleCallableTemplateText(rendered))
            {
                return rendered;
            }

            string statement = EnsureStatementTerminator(rendered, entry);
            return entry.HasReturnValue && !string.IsNullOrWhiteSpace(returnTarget)
                ? $"{returnTarget.Trim()} = {statement}"
                : statement;
        }

        if (!entry.IsCallable)
        {
            return FirstNonEmpty(entry.InsertText, entry.Declaration, entry.Signature, entry.Name);
        }

        string call = values.Count == 0
            ? $"{entry.Name}()"
            : $"{entry.Name}({string.Join(", ", values.Select(value =>
                string.IsNullOrWhiteSpace(value.Value)
                    ? value.EffectiveDefaultValue
                    : value.Value.Trim()))})";

        string callableStatement = EnsureStatementTerminator(call, entry);
        if (entry.HasReturnValue && !string.IsNullOrWhiteSpace(returnTarget))
        {
            return $"{returnTarget.Trim()} = {callableStatement}";
        }

        return callableStatement;
    }

    public static string QuoteCStringPath(string path)
    {
        string escaped = EscapeCString(path);
        return $"\"{escaped}\"";
    }

    public static string FormatPathForTemplate(CatalogEntry? entry, CatalogParameter parameter, string path)
    {
        string escaped = EscapeCString(path);
        if (entry is null || string.IsNullOrWhiteSpace(entry.InsertText))
        {
            return $"\"{escaped}\"";
        }

        string name = Regex.Escape(parameter.Name ?? string.Empty);
        var wrappedPlaceholder = new Regex($"[\\\"']\\s*\\{{\\{{{name}\\}}\\}}\\s*[\\\"']");
        return wrappedPlaceholder.IsMatch(entry.InsertText)
            ? escaped
            : $"\"{escaped}\"";
    }

    public static CatalogPickerConfig? CreateEffectivePickerConfig(SnippetParameterValue value)
    {
        if (value.PickerConfig is not null && value.PickerConfig.FlattenChoices().Count > 0)
        {
            return value.PickerConfig;
        }

        if (value.SuggestedChoices.Count == 0)
        {
            return null;
        }

        return new CatalogPickerConfig
        {
            Title = $"Choisir une valeur pour {value.Parameter.Name}",
            SelectionLabel = "Valeur sélectionnée",
            Subtitle = value.Parameter.Description,
            ApplyDefaultIfEmpty = false,
            MultiSelect = false,
            ValueSeparator = " | ",
            EmptyValue = string.Empty,
            Sections = new[]
            {
                new CatalogPickerSection
                {
                    Label = "Choix disponibles",
                    Groups = new[]
                    {
                        new CatalogPickerGroup
                        {
                            Label = value.Parameter.Name,
                            Description = value.Parameter.Description,
                            Items = value.SuggestedChoices,
                        },
                    },
                },
            },
        };
    }

    private static ResolvedParameter ResolveParameter(CatalogParameter parameter)
    {
        string editorType = InferEditorType(parameter);
        string defaultValue = parameter.HasExplicitDefaultValue
            ? parameter.DefaultValue.Trim()
            : InferDefaultValue(parameter.Name, parameter.Type, editorType);
        string placeholder = string.IsNullOrWhiteSpace(parameter.Placeholder)
            ? InferPlaceholder(parameter, editorType)
            : parameter.Placeholder.Trim();

        var suggestions = new List<CatalogChoice>();
        suggestions.AddRange(parameter.Options);
        suggestions.AddRange(parameter.Presets);
        suggestions.AddRange(InferSuggestions(parameter, editorType));
        if (parameter.PickerConfig is not null)
        {
            suggestions.AddRange(parameter.PickerConfig.FlattenChoices());
        }

        return new ResolvedParameter(
            parameter,
            editorType,
            defaultValue,
            placeholder,
            suggestions
                .Where(choice => !string.IsNullOrWhiteSpace(choice.Value))
                .GroupBy(choice => choice.Value.Trim(), StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray(),
            parameter.PickerConfig);
    }

    private static string InferEditorType(CatalogParameter parameter)
    {
        if (!string.IsNullOrWhiteSpace(parameter.EditorType))
        {
            return parameter.EditorType.Trim();
        }

        string name = parameter.Name.ToLowerInvariant();
        string type = parameter.Type.ToLowerInvariant();
        string description = parameter.Description.ToLowerInvariant();

        if (name.Contains("file") || name.Contains("filename") || name.Contains("uir")) return "pathFile";
        if (name.Contains("folder") || name.Contains("directory") || name.Contains("dir")) return "pathFolder";
        if (name.Contains("handle") || type == "initext" || name.Contains("panel")) return "handle";
        if (name.Contains("bool") || description.Contains("boolean") || name.StartsWith("is", StringComparison.Ordinal) || name.StartsWith("enable", StringComparison.Ordinal) || name.EndsWith("flag", StringComparison.Ordinal)) return "boolean";
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

    private static IEnumerable<CatalogChoice> InferSuggestions(CatalogParameter parameter, string editorType)
    {
        string lowerName = parameter.Name.ToLowerInvariant();
        string lowerType = parameter.Type.ToLowerInvariant();
        if (editorType == "boolean")
        {
            yield return Choice("0", "Désactivé / faux");
            yield return Choice("1", "Activé / vrai");
        }
        if (editorType == "pathFile" || editorType == "pathFolder") yield return Choice("\"\"");
        if (editorType == "handle")
        {
            if (lowerName.Contains("parent")) yield return Choice("0", "Aucun parent");
            yield return Choice(parameter.Name);
        }
        if (lowerName.Contains("section"))
        {
            yield return Choice("\"GENERAL\"");
            yield return Choice("\"SECTION\"");
        }
        if (lowerName.Contains("key"))
        {
            yield return Choice("\"debug\"");
            yield return Choice("\"KEY\"");
        }
        if (lowerName.Contains("value") && lowerType.Contains("*"))
        {
            yield return Choice("&value");
            yield return Choice("&result");
        }
        if (lowerName.EndsWith("id", StringComparison.Ordinal)) yield return Choice(parameter.Name.ToUpperInvariant());
    }

    private static CatalogChoice Choice(string value, string description = "") => new CatalogChoice
    {
        Value = value,
        Label = value,
        Description = description,
    };

    private static bool HasParameterizedInsertTemplate(CatalogEntry entry) =>
        entry.Parameters.Count > 0 && ParameterTemplateRegex.IsMatch(entry.InsertText ?? string.Empty);

    private static string ApplyParameterizedInsertTemplate(CatalogEntry entry, IReadOnlyList<SnippetParameterValue> values)
    {
        string output = FirstNonEmpty(entry.InsertText, entry.Signature, entry.Name);
        for (int index = 0; index < entry.Parameters.Count; index++)
        {
            CatalogParameter parameter = entry.Parameters[index];
            string replacement = index < values.Count && !string.IsNullOrWhiteSpace(values[index].Value)
                ? values[index].Value.Trim()
                : (index < values.Count ? values[index].EffectiveDefaultValue : (parameter.HasExplicitDefaultValue ? parameter.DefaultValue : InferDefaultValue(parameter.Name, parameter.Type, InferEditorType(parameter))));
            output = Regex.Replace(output, $@"\{{\{{{Regex.Escape(parameter.Name)}\}}\}}", _ => replacement);
        }
        return output;
    }

    private static bool IsSimpleCallableTemplateText(string text)
    {
        string trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Contains("\n") || trimmed.Contains("\r")) return false;
        return Regex.IsMatch(trimmed, @"^[A-Za-z_~][A-Za-z0-9_:.>\-]*\s*\(") ||
               Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_]*\s*->\s*[A-Za-z_~][A-Za-z0-9_]*\s*\(") ||
               Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_]*\s*\.\s*[A-Za-z_~][A-Za-z0-9_]*\s*\(");
    }

    private static string EnsureStatementTerminator(string text, CatalogEntry entry)
    {
        string trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0 || UsesPythonStatementStyle(entry) || string.Equals(entry.SymbolKind, "command", StringComparison.OrdinalIgnoreCase)) return trimmed.TrimEnd(';');
        return trimmed.EndsWith(";", StringComparison.Ordinal) || trimmed.EndsWith("}", StringComparison.Ordinal) ? trimmed : trimmed + ";";
    }

    private static bool UsesPythonStatementStyle(CatalogEntry entry) =>
        $"{entry.Environment} {entry.Library}".IndexOf("python", StringComparison.OrdinalIgnoreCase) >= 0;

    private static string EscapeCString(string path) => (path ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static IReadOnlyList<string> ExtractDefaultArguments(string insertText, IReadOnlyList<ResolvedParameter> parameters)
    {
        int start = (insertText ?? string.Empty).IndexOf('(');
        int end = (insertText ?? string.Empty).LastIndexOf(')');
        if (start < 0 || end <= start) return parameters.Select(parameter => parameter.DefaultValue).ToArray();
        string rawArguments = insertText.Substring(start + 1, end - start - 1).Trim();
        if (rawArguments.Length == 0) return Array.Empty<string>();
        var parsed = SplitCallArguments(rawArguments).ToList();
        while (parsed.Count < parameters.Count) parsed.Add(parameters[parsed.Count].DefaultValue);
        return parsed;
    }

    private static IEnumerable<string> SplitCallArguments(string text)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        int parentheses = 0, brackets = 0, braces = 0;
        char quote = '\0';
        bool escaped = false;
        foreach (char character in text)
        {
            if (quote != '\0')
            {
                current.Append(character);
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == quote) quote = '\0';
                continue;
            }
            if (character == '\'' || character == '\"') { quote = character; current.Append(character); continue; }
            switch (character)
            {
                case '(': parentheses++; break;
                case ')': parentheses = Math.Max(0, parentheses - 1); break;
                case '[': brackets++; break;
                case ']': brackets = Math.Max(0, brackets - 1); break;
                case '{': braces++; break;
                case '}': braces = Math.Max(0, braces - 1); break;
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

    private static string FirstNonEmpty(params string[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private sealed class ResolvedParameter
    {
        public ResolvedParameter(CatalogParameter parameter, string editorType, string defaultValue, string placeholder, IReadOnlyList<CatalogChoice> suggestedChoices, CatalogPickerConfig? pickerConfig)
        {
            Parameter = parameter;
            EditorType = editorType;
            DefaultValue = defaultValue;
            Placeholder = placeholder;
            SuggestedChoices = suggestedChoices;
            PickerConfig = pickerConfig;
        }
        public CatalogParameter Parameter { get; }
        public string EditorType { get; }
        public string DefaultValue { get; }
        public string Placeholder { get; }
        public IReadOnlyList<CatalogChoice> SuggestedChoices { get; }
        public CatalogPickerConfig? PickerConfig { get; }
    }
}
