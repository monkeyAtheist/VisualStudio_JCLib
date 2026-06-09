using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace JCLib.VisualStudio.Services;

public sealed class UserPreferences
{
    public string SearchText { get; set; } = string.Empty;
    public string SelectedEntryKey { get; set; } = string.Empty;
    public string FilterEnvironment { get; set; } = UserPreferencesStore.AllFilterLabel;
    public string FilterLibrary { get; set; } = UserPreferencesStore.AllFilterLabel;
    public bool FavoritesOnly { get; set; }
    public bool IncludeBundledPack { get; set; }
    public List<string> Favorites { get; set; } = new List<string>();
    public List<string> RecentEntries { get; set; } = new List<string>();
    public ThemePreferences Theme { get; set; } = ThemePreferences.CreateAccessibleDark();
}

public sealed class ThemePreferences
{
    public string Background { get; set; } = "#252526";
    public string Panel { get; set; } = "#2D2D30";
    public string Input { get; set; } = "#1E1E1E";
    public string DropdownBackground { get; set; } = "#1E1E1E";
    public string DropdownText { get; set; } = "#F1F1F1";
    public string Text { get; set; } = "#F1F1F1";
    public string SecondaryText { get; set; } = "#C8C8C8";
    public string Accent { get; set; } = "#8FC7FF";
    public string Border { get; set; } = "#666666";
    public string ButtonText { get; set; } = "#111111";

    // Hierarchy palette used by the catalog browser and the Visual Pack Editor.
    // The values intentionally mirror the VS Code explorer: warm environments,
    // cyan libraries, blue categories, purple groups and green symbols.
    public string TreeRoot { get; set; } = "#8FC7FF";
    public string TreePack { get; set; } = "#E5C07B";
    public string TreeEnvironment { get; set; } = "#D7BA7D";
    public string TreeLibrary { get; set; } = "#4FC1FF";
    public string TreeCategory { get; set; } = "#569CD6";
    public string TreeGroup { get; set; } = "#C586C0";
    public string TreeElement { get; set; } = "#B5CEA8";
    public string TreeBadge { get; set; } = "#3E3E42";
    public string TreeIconText { get; set; } = "#111111";

    public static ThemePreferences CreateAccessibleDark() => new ThemePreferences();

    public ThemePreferences Clone() => new ThemePreferences
    {
        Background = Background,
        Panel = Panel,
        Input = Input,
        DropdownBackground = DropdownBackground,
        DropdownText = DropdownText,
        Text = Text,
        SecondaryText = SecondaryText,
        Accent = Accent,
        Border = Border,
        ButtonText = ButtonText,
        TreeRoot = TreeRoot,
        TreePack = TreePack,
        TreeEnvironment = TreeEnvironment,
        TreeLibrary = TreeLibrary,
        TreeCategory = TreeCategory,
        TreeGroup = TreeGroup,
        TreeElement = TreeElement,
        TreeBadge = TreeBadge,
        TreeIconText = TreeIconText,
    };
}

public static class UserPreferencesStore
{
    public const string AllFilterLabel = "<Tous>";
    private const int RecentLimit = 18;

    public static string GetStateFilePath(bool createDirectory = true)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JCLib",
            "VisualStudio");
        if (createDirectory) Directory.CreateDirectory(directory);
        return Path.Combine(directory, "user-preferences.json");
    }

    public static UserPreferences Load()
    {
        string path = GetStateFilePath(createDirectory: true);
        try
        {
            if (!File.Exists(path)) return Normalize(new UserPreferences());
            string json = File.ReadAllText(path, Encoding.UTF8);
            return Normalize(JsonConvert.DeserializeObject<UserPreferences>(json) ?? new UserPreferences());
        }
        catch
        {
            return Normalize(new UserPreferences());
        }
    }

    public static void Save(UserPreferences preferences)
    {
        if (preferences is null) throw new ArgumentNullException(nameof(preferences));
        UserPreferences normalized = Normalize(preferences);
        string path = GetStateFilePath(createDirectory: true);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonConvert.SerializeObject(normalized, Formatting.Indented), new UTF8Encoding(false));
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }

    public static void AddRecent(UserPreferences preferences, string key)
    {
        if (preferences is null || string.IsNullOrWhiteSpace(key)) return;
        preferences.RecentEntries.RemoveAll(value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
        preferences.RecentEntries.Insert(0, key);
        if (preferences.RecentEntries.Count > RecentLimit)
        {
            preferences.RecentEntries.RemoveRange(RecentLimit, preferences.RecentEntries.Count - RecentLimit);
        }
    }

    public static bool ToggleFavorite(UserPreferences preferences, string key)
    {
        if (preferences is null || string.IsNullOrWhiteSpace(key)) return false;
        int index = preferences.Favorites.FindIndex(value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            preferences.Favorites.RemoveAt(index);
            return false;
        }
        preferences.Favorites.Add(key);
        return true;
    }

    public static bool IsFavorite(UserPreferences preferences, string key) =>
        preferences?.Favorites.Any(value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase)) == true;

    private static UserPreferences Normalize(UserPreferences preferences)
    {
        preferences.SearchText ??= string.Empty;
        preferences.SelectedEntryKey ??= string.Empty;
        preferences.FilterEnvironment = string.IsNullOrWhiteSpace(preferences.FilterEnvironment) ? AllFilterLabel : preferences.FilterEnvironment.Trim();
        preferences.FilterLibrary = string.IsNullOrWhiteSpace(preferences.FilterLibrary) ? AllFilterLabel : preferences.FilterLibrary.Trim();
        preferences.Favorites = NormalizeKeys(preferences.Favorites);
        preferences.RecentEntries = NormalizeKeys(preferences.RecentEntries).Take(RecentLimit).ToList();
        preferences.Theme ??= ThemePreferences.CreateAccessibleDark();
        if (string.IsNullOrWhiteSpace(preferences.Theme.DropdownBackground)) preferences.Theme.DropdownBackground = preferences.Theme.Input;
        if (string.IsNullOrWhiteSpace(preferences.Theme.DropdownText)) preferences.Theme.DropdownText = preferences.Theme.Text;
        var hierarchyDefaults = ThemePreferences.CreateAccessibleDark();
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeRoot)) preferences.Theme.TreeRoot = hierarchyDefaults.TreeRoot;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreePack)) preferences.Theme.TreePack = hierarchyDefaults.TreePack;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeEnvironment)) preferences.Theme.TreeEnvironment = hierarchyDefaults.TreeEnvironment;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeLibrary)) preferences.Theme.TreeLibrary = hierarchyDefaults.TreeLibrary;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeCategory)) preferences.Theme.TreeCategory = hierarchyDefaults.TreeCategory;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeGroup)) preferences.Theme.TreeGroup = hierarchyDefaults.TreeGroup;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeElement)) preferences.Theme.TreeElement = hierarchyDefaults.TreeElement;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeBadge)) preferences.Theme.TreeBadge = hierarchyDefaults.TreeBadge;
        if (string.IsNullOrWhiteSpace(preferences.Theme.TreeIconText)) preferences.Theme.TreeIconText = hierarchyDefaults.TreeIconText;
        return preferences;
    }

    private static List<string> NormalizeKeys(IEnumerable<string>? values) =>
        (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
