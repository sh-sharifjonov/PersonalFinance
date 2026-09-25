using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Formatting;

// Category.Icon stores a sprite id (see the <symbol id="i-…"> sprite in index.html) and
// Category.Color a design-system tone name; both are optional so older rows still render.
public static class Visuals
{
    public record IconOption(string Icon, string Tone);

    public static readonly IReadOnlyList<IconOption> CategoryIcons = new[]
    {
        new IconOption("cart", "orange"),
        new IconOption("bus", "info"),
        new IconOption("cup", "pink"),
        new IconOption("home", "primary"),
        new IconOption("wifi", "teal"),
        new IconOption("heart", "danger"),
        new IconOption("receipt", "purple"),
        new IconOption("case", "success"),
        new IconOption("laptop", "success"),
        new IconOption("piggy", "purple"),
        new IconOption("card", "primary"),
        new IconOption("dots", "muted")
    };

    private static readonly string[] AccountIcons = { "card", "cash", "piggy", "wallet" };
    private static readonly string[] AccountTones = { "primary", "success", "purple", "info" };

    public static string ToneFor(string icon) =>
        CategoryIcons.FirstOrDefault(o => o.Icon == icon)?.Tone ?? "muted";

    public static string CategoryIcon(Category? category) =>
        string.IsNullOrEmpty(category?.Icon) ? "tag" : category.Icon;

    public static string CategoryTone(Category? category) =>
        category is null ? "muted"
        : !string.IsNullOrEmpty(category.Color) ? category.Color
        : category.Type == CategoryType.Income ? "success" : "muted";

    /// <summary>Inline style for a tinted icon tile: soft fill + full-strength stroke.</summary>
    public static string ToneStyle(string tone) => tone == "muted"
        ? "background:var(--bg-muted);color:var(--t-muted)"
        : $"background:var(--{tone}-soft);color:var(--{tone})";

    public static string ToneColor(string tone) => tone == "muted" ? "var(--t-light)" : $"var(--{tone})";

    public static string AccountIcon(int index) => AccountIcons[index % AccountIcons.Length];

    public static string AccountTone(int index) => AccountTones[index % AccountTones.Length];
}
