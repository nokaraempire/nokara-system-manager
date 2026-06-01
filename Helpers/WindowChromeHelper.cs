using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NokaraSystemManager.Helpers;

public static class WindowChromeHelper
{
    private const int DwmwaUseImmersiveDarkModeBefore20h1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    public static void TryUseImmersiveDarkMode(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20h1, ref enabled, sizeof(int));
            }

            SetColorAttribute(handle, DwmwaCaptionColor, Color.FromRgb(10, 11, 16));
            SetColorAttribute(handle, DwmwaBorderColor, Color.FromRgb(52, 54, 74));
            SetColorAttribute(handle, DwmwaTextColor, Color.FromRgb(247, 243, 255));
        }
        catch
        {
            // Native title-bar coloring is cosmetic. If DWM rejects it, the app should keep running normally.
        }
    }

    private static void SetColorAttribute(IntPtr handle, int attribute, Color color)
    {
        var colorRef = color.R | (color.G << 8) | (color.B << 16);
        DwmSetWindowAttribute(handle, attribute, ref colorRef, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
