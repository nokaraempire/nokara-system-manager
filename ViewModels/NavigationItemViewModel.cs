namespace NokaraSystemManager.ViewModels;

public sealed class NavigationItemViewModel : ViewModelBase
{
    public NavigationItemViewModel(string title, string shortLabel, string iconGlyph, string routeSegment, ViewModelBase viewModel)
    {
        Title = title;
        ShortLabel = shortLabel;
        IconGlyph = iconGlyph;
        RouteSegment = routeSegment;
        ViewModel = viewModel;
    }

    public string Title { get; }
    public string ShortLabel { get; }
    public string IconGlyph { get; }
    public string RouteSegment { get; }
    public string Address => $"nokara://{RouteSegment}";
    public ViewModelBase ViewModel { get; }
}
