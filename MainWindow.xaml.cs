using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Services;
using NokaraSystemManager.ViewModels;

namespace NokaraSystemManager;

public partial class MainWindow : Window
{
    private const double TabScrollStep = 220;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new AppServices());
        DataContext = _viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowChromeHelper.TryUseImmersiveDarkMode(this);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _ = Dispatcher.BeginInvoke(new Action(UpdateTabScrollButtons));
    }

    private void OnTabScrollLeft(object sender, RoutedEventArgs e)
    {
        TabStripScrollViewer.ScrollToHorizontalOffset(Math.Max(0, TabStripScrollViewer.HorizontalOffset - TabScrollStep));
        UpdateTabScrollButtons();
    }

    private void OnTabScrollRight(object sender, RoutedEventArgs e)
    {
        TabStripScrollViewer.ScrollToHorizontalOffset(Math.Min(TabStripScrollViewer.ScrollableWidth, TabStripScrollViewer.HorizontalOffset + TabScrollStep));
        UpdateTabScrollButtons();
    }

    private void OnTabStripScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateTabScrollButtons();
    }

    private void OnTabStripSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTabScrollButtons();
    }

    private void OnTabStripPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (TabStripScrollViewer.ScrollableWidth <= 0)
        {
            return;
        }

        var nextOffset = TabStripScrollViewer.HorizontalOffset - e.Delta;
        TabStripScrollViewer.ScrollToHorizontalOffset(Math.Clamp(nextOffset, 0, TabStripScrollViewer.ScrollableWidth));
        e.Handled = true;
        UpdateTabScrollButtons();
    }

    private void UpdateTabScrollButtons()
    {
        if (!IsInitialized)
        {
            return;
        }

        var canScroll = TabStripScrollViewer.ScrollableWidth > 0.5;
        TabScrollLeftButton.IsEnabled = canScroll && TabStripScrollViewer.HorizontalOffset > 0.5;
        TabScrollRightButton.IsEnabled = canScroll && TabStripScrollViewer.HorizontalOffset < TabStripScrollViewer.ScrollableWidth - 0.5;
    }
}
