using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Unispect
{
    public static class Utilities
    {
        private static readonly Dictionary<ulong, string> UnknownClassNameCache = new Dictionary<ulong, string>();

        private static Dictionary<string, int> _prefixIndexer;

        private static Dictionary<string, int> PrefixIndexer
        {
            get
            {
                if (_prefixIndexer != null)
                    return _prefixIndexer;

                _prefixIndexer = new Dictionary<string, int>();
                foreach (var e in Enum.GetNames(typeof(UnknownPrefix)))
                    _prefixIndexer.Add(e, 0);

                return _prefixIndexer;
            }
        }

        public static string ToUnknownClassString(this byte[] _, UnknownPrefix prefix, uint token)
        {
            var hash = (token- 0x2000000) * (uint)prefix;
            if (UnknownClassNameCache.ContainsKey(hash))
                return UnknownClassNameCache[hash];

            var prefixName = Enum.GetName(typeof(UnknownPrefix), prefix);
            //var str = $"{prefixName}{PrefixIndexer[prefixName ?? throw new InvalidOperationException()]++:0000}";
            var str = $"{prefixName}{hash:X4}";
            UnknownClassNameCache.Add(hash, str);

            return str;
        }

        public static IEnumerable<int> Step(int fromInclusive, int toExclusive, int step)
        {
            for (var i = fromInclusive; i < toExclusive; i += step)
            {
                yield return i;
            }
        }

        public static string ToAsciiString(this byte[] buffer, int start = 0)
        {
            var length = 0;
            for (var i = start; i < buffer.Length; i++)
            {
                if (buffer[i] != 0) continue;

                length = i - start;
                break;
            }

            return Encoding.ASCII.GetString(buffer, start, length);
        }

        public static string LowerChar(this string str, int index = 0)
        {
            if (index < str.Length && index > -1) // instead of casting from uint, just check if it's zero or greater
            {
                if (index == 0)
                    return char.ToLower(str[index]) + str.Substring(index + 1);

                return str.Substring(0, index - 1) + char.ToLower(str[index]) + str.Substring(index + 1);
            }

            return str;
        }

        public static string FormatFieldText(this string text)
        {
            var ret = text.Replace("[]", "Array");
            var lessThanIndex = ret.IndexOf('<');
            if (lessThanIndex > -1)
            {
                // The type name _should_ always end at the following index, so we don't need to splice.
                //var greaterThanIndex = ret.IndexOf('>'); 
                ret = ret.Substring(0, lessThanIndex);
            }

            return ret;
        }

        public static int ToInt32(this byte[] buffer, int start = 0) => BitConverter.ToInt32(buffer, start);

        public static string CurrentVersion
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersionInfo.ProductVersion;
            }
        }

        public static string GithubLink => "http://www.github.com/Razchek/Unispect";

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
            int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out IntRect rect);

        public struct IntRect
        {
            public int Left, Top, Right, Bottom;
        }

        public static void ShowSystemMenu(Window window)
        {
            var hWnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            GetWindowRect(hWnd, out var pos);
            var hMenu = GetSystemMenu(hWnd, false);
            var cmd = TrackPopupMenu(hMenu, 0x100, pos.Left + 20, pos.Top + 20, 0, hWnd, IntPtr.Zero);
            if (cmd > 0) SendMessage(hWnd, 0x112, (IntPtr)cmd, IntPtr.Zero);
        }


        public static async void LaunchUrl(string url)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.AppStarting;
                Process.Start(url);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                var nl = Environment.NewLine;
                await MessageBox(
                    $"Couldn't open: {url}.{nl}{nl}Exception:{nl}{ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        public static async Task<MessageDialogResult> MessageBox(string msg, string title = "",
            MessageDialogStyle messageDialogStyle = MessageDialogStyle.Affirmative,
            MetroDialogSettings metroDialogSettings = null)
        {
            if (string.IsNullOrEmpty(title))
                title = Application.Current.MainWindow?.Title;

            var mw = (Application.Current.MainWindow as MetroWindow);
            return await mw.ShowMessageAsync(title, msg, messageDialogStyle, metroDialogSettings);
        }

        public static void FadeFromTo(this UIElement uiElement, double fromOpacity, double toOpacity,
            int durationInMilliseconds, bool showOnStart, bool collapseOnFinish)
        {
            var timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);
            var doubleAnimation =
                new DoubleAnimation(fromOpacity, toOpacity,
                    new Duration(timeSpan));

            uiElement.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
            if (showOnStart)
            {
                uiElement.ApplyAnimationClock(UIElement.VisibilityProperty, null);
                uiElement.Visibility = Visibility.Visible;
            }
            if (collapseOnFinish)
            {
                var keyAnimation = new ObjectAnimationUsingKeyFrames { Duration = new Duration(timeSpan) };
                keyAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(timeSpan)));
                uiElement.BeginAnimation(UIElement.VisibilityProperty, keyAnimation);
            }
        }

        public static void FadeIn(this UIElement uiElement, int durationInMilliseconds = 100)
        {
            uiElement.FadeFromTo(0, 1, durationInMilliseconds, true, false);
        }

        public static void FadeOut(this UIElement uiElement, int durationInMilliseconds = 100)
        {
            uiElement.FadeFromTo(1, 0, durationInMilliseconds, false, true);
        }

        public static void ResizeFromTo(this FrameworkElement uiElement, Size fromSize, Size toSize, int durationInMilliseconds)
        {
            var timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);

            //var sizeAnimationWidth = new DoubleAnimation(fromSize.Width, toSize.Width, new Duration(timeSpan));
            var sizeAnimationHeight = new DoubleAnimation(fromSize.Height, toSize.Height, new Duration(timeSpan));

            //uiElement.BeginAnimation(FrameworkElement.WidthProperty, sizeAnimationWidth);
            uiElement.BeginAnimation(FrameworkElement.HeightProperty, sizeAnimationHeight);
        }
    }
}