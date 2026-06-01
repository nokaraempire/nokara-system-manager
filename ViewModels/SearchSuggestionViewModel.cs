namespace NokaraSystemManager.ViewModels;

public sealed class SearchSuggestionViewModel
{
    public SearchSuggestionViewModel(string title, string description, string iconGlyph, NavigationItemViewModel target, params string[] keywords)
    {
        Title = title;
        Description = description;
        IconGlyph = iconGlyph;
        Target = target;
        Keywords = keywords;
    }

    public string Title { get; }
    public string Description { get; }
    public string IconGlyph { get; }
    public NavigationItemViewModel Target { get; }
    public IReadOnlyList<string> Keywords { get; }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Description.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Target.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Keywords.Any(keyword => keyword.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
