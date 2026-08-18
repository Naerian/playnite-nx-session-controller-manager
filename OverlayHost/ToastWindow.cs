using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.OverlayHost
{
    internal sealed class ToastWindow : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x20;
        private const int WsExToolWindow = 0x80;
        private const int WsExNoActivate = 0x08000000;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);

        private readonly Queue<ToastRequest> pending = new Queue<ToastRequest>();
        private readonly TextBlock titleText;
        private readonly TextBlock messageText;
        private readonly Path icon;
        private readonly Grid contentLayout;
        private readonly StackPanel textPanel;
        private readonly Border card;
        private readonly DispatcherTimer holdTimer;
        private ToastRequest current;

        public ToastWindow()
        {
            Width = 430;
            Height = 100;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Opacity = 0;

            icon = new Path
            {
                Width = 28,
                Height = 28,
                Stretch = Stretch.Uniform,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent,
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleText = new TextBlock
            {
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            messageText = new TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 203, 212)),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            textPanel = new StackPanel();
            textPanel.Children.Add(titleText);
            textPanel.Children.Add(messageText);
            contentLayout = new Grid();
            card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(244, 18, 20, 24)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 14, 18, 14),
                Child = contentLayout
            };
            ConfigureContentLayout("Left", 14);
            Content = card;
            SourceInitialized += OnSourceInitialized;
            holdTimer = new DispatcherTimer();
            holdTimer.Tick += OnHoldElapsed;
        }

        public void Enqueue(string id, int processId, int durationMilliseconds, string kind,
            string title, string message, string iconGeometry, string presentationStyle)
        {
            pending.Enqueue(new ToastRequest
            {
                Id = id,
                ProcessId = processId,
                DurationMilliseconds = Math.Max(2000, Math.Min(15000, durationMilliseconds)),
                Kind = kind,
                Title = title,
                Message = message,
                IconGeometry = iconGeometry,
                PresentationStyle = presentationStyle
            });
            if (current == null)
            {
                ShowNext();
            }
        }

        public void ReplaceWith(string id, int processId, int durationMilliseconds, string kind,
            string title, string message, string iconGeometry, string presentationStyle)
        {
            pending.Clear();
            holdTimer.Stop();
            current = null;
            BeginAnimation(OpacityProperty, null);
            Opacity = 0;
            Enqueue(id, processId, durationMilliseconds, kind, title, message, iconGeometry,
                presentationStyle);
        }

        private void ShowNext()
        {
            if (pending.Count == 0)
            {
                current = null;
                Hide();
                return;
            }

            current = pending.Dequeue();
            titleText.Text = current.Title;
            messageText.Text = current.Message;
            messageText.Visibility = string.IsNullOrWhiteSpace(current.Message) ? Visibility.Collapsed : Visibility.Visible;
            var style = ToastStyle.Parse(current.PresentationStyle);
            var scale = style.ScalePercent / 100.0;
            Width = style.Width;
            var textHeight = style.TitleFontSize + (messageText.Visibility == Visibility.Visible
                ? style.MessageFontSize * 2 + 8 : 0);
            titleText.FontSize = style.TitleFontSize;
            messageText.FontSize = style.MessageFontSize;
            icon.Width = style.IconSize;
            icon.Height = style.IconSize;
            var iconGap = Math.Max(8, style.Padding * 0.75);
            var verticalIcon = string.Equals(style.IconPosition, "Top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(style.IconPosition, "Bottom", StringComparison.OrdinalIgnoreCase);
            var hiddenIcon = string.Equals(style.IconPosition, "Hidden", StringComparison.OrdinalIgnoreCase);
            var contentHeight = hiddenIcon
                ? textHeight
                : verticalIcon
                    ? style.IconSize + iconGap + textHeight
                    : Math.Max(style.IconSize, textHeight);
            Height = Math.Max(82, contentHeight + style.Padding * 2);
            ConfigureContentLayout(style.IconPosition, iconGap);
            card.Padding = new Thickness(style.Padding);
            card.CornerRadius = new CornerRadius(style.CornerRadius * scale);
            card.Background = Brush(style.Background, Color.FromArgb(244, 18, 20, 24));
            titleText.Foreground = Brush(style.PrimaryText, Colors.White);
            messageText.Foreground = Brush(style.SecondaryText, Color.FromRgb(198, 203, 212));
            try { icon.Data = Geometry.Parse(current.IconGeometry ?? string.Empty); }
            catch { icon.Data = null; }
            var accent = ParseColor(string.Equals(current.Kind, "warning", StringComparison.OrdinalIgnoreCase)
                ? style.WarningAccent
                : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                    ? style.ConnectedAccent
                    : style.DisconnectedAccent,
                string.Equals(current.Kind, "warning", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromRgb(245, 181, 66)
                    : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                        ? Color.FromRgb(79, 194, 126)
                        : Color.FromRgb(80, 170, 255));
            card.BorderBrush = new SolidColorBrush(accent);
            card.BorderThickness = style.ShowBorder
                ? CreateBorderThickness(style.BorderPosition, style.BorderThickness)
                : new Thickness(0);
            icon.Stroke = new SolidColorBrush(accent);
            card.Measure(new Size(Width, double.PositiveInfinity));
            Height = Math.Max(82, Math.Ceiling(card.DesiredSize.Height));
            if (!IsVisible)
            {
                Show();
            }
            ApplyBounds(GetTargetScreen(current.ProcessId), style.Position);
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            holdTimer.Interval = TimeSpan.FromMilliseconds(current.DurationMilliseconds);
            holdTimer.Start();
        }

        private void ConfigureContentLayout(string position, double gap)
        {
            var normalized = string.IsNullOrWhiteSpace(position) ? "Left" : position;
            contentLayout.Children.Clear();
            contentLayout.ColumnDefinitions.Clear();
            contentLayout.RowDefinitions.Clear();
            icon.Margin = new Thickness(0);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            textPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            textPanel.VerticalAlignment = VerticalAlignment.Center;

            if (string.Equals(normalized, "Hidden", StringComparison.OrdinalIgnoreCase))
            {
                icon.Visibility = Visibility.Collapsed;
                contentLayout.Children.Add(textPanel);
                return;
            }

            icon.Visibility = Visibility.Visible;
            if (string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                contentLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var iconFirst = string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase);
                Grid.SetRow(icon, iconFirst ? 0 : 1);
                Grid.SetRow(textPanel, iconFirst ? 1 : 0);
                icon.Margin = iconFirst ? new Thickness(0, 0, 0, gap) : new Thickness(0, gap, 0, 0);
            }
            else
            {
                contentLayout.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = string.Equals(normalized, "Right", StringComparison.OrdinalIgnoreCase)
                        ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
                contentLayout.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = string.Equals(normalized, "Right", StringComparison.OrdinalIgnoreCase)
                        ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });
                var iconFirst = !string.Equals(normalized, "Right", StringComparison.OrdinalIgnoreCase);
                Grid.SetColumn(icon, iconFirst ? 0 : 1);
                Grid.SetColumn(textPanel, iconFirst ? 1 : 0);
                icon.Margin = iconFirst ? new Thickness(0, 0, gap, 0) : new Thickness(gap, 0, 0, 0);
            }

            contentLayout.Children.Add(icon);
            contentLayout.Children.Add(textPanel);
        }

        private void OnHoldElapsed(object sender, EventArgs args)
        {
            holdTimer.Stop();
            var animation = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(220));
            animation.Completed += delegate { ShowNext(); };
            BeginAnimation(OpacityProperty, animation);
        }

        private void OnSourceInitialized(object sender, EventArgs args)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(handle, GwlExStyle);
            SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
        }

        private void ApplyBounds(Forms.Screen screen, string position)
        {
            var bounds = (screen ?? Forms.Screen.PrimaryScreen).WorkingArea;
            var dpiScaleX = 1.0;
            var dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            var pixelWidth = (int)Math.Ceiling(Width * dpiScaleX);
            var pixelHeight = (int)Math.Ceiling(Height * dpiScaleY);
            var left = string.Equals(position, "TopLeft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(position, "BottomLeft", StringComparison.OrdinalIgnoreCase)
                ? bounds.Left + 28
                : bounds.Right - pixelWidth - 28;
            var top = string.Equals(position, "BottomLeft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(position, "BottomRight", StringComparison.OrdinalIgnoreCase)
                ? bounds.Bottom - pixelHeight - 28
                : bounds.Top + 28;
            SetWindowPos(new WindowInteropHelper(this).Handle, HwndTopmost, left, top,
                pixelWidth, pixelHeight, SwpNoActivate | SwpShowWindow);
        }

        private static Forms.Screen GetTargetScreen(int processId)
        {
            try
            {
                if (processId > 0)
                {
                    using (var process = Process.GetProcessById(processId))
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return Forms.Screen.FromHandle(process.MainWindowHandle);
                        }
                    }
                }
            }
            catch { }
            return Forms.Screen.PrimaryScreen;
        }

        private sealed class ToastRequest
        {
            public string Id { get; set; }
            public int ProcessId { get; set; }
            public int DurationMilliseconds { get; set; }
            public string Kind { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string IconGeometry { get; set; }
            public string PresentationStyle { get; set; }
        }

        private sealed class ToastStyle
        {
            public int Width = 520;
            public int ScalePercent = 110;
            public string Position = "TopRight";
            public string Background = "#F4121418";
            public string PrimaryText = "#FFFFFFFF";
            public string SecondaryText = "#FFC6CBD4";
            public string ConnectedAccent = "#FF4FC27E";
            public string DisconnectedAccent = "#FF50AAFF";
            public string WarningAccent = "#FFF5B542";
            public int TitleFontSize = 19;
            public int MessageFontSize = 15;
            public int IconSize = 32;
            public int Padding = 18;
            public bool ShowBorder = true;
            public string BorderPosition = "Bottom";
            public int BorderThickness = 3;
            public int CornerRadius = 10;
            public string IconPosition = "Left";

            public static ToastStyle Parse(string value)
            {
                var style = new ToastStyle();
                var parts = (value ?? string.Empty).Split(';');
                int parsed;
                if (parts.Length > 0 && int.TryParse(parts[0], out parsed)) style.Width = Math.Max(300, Math.Min(900, parsed));
                if (parts.Length > 1 && int.TryParse(parts[1], out parsed)) style.ScalePercent = Math.Max(80, Math.Min(160, parsed));
                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) style.Position = parts[2];
                if (parts.Length > 3) style.Background = parts[3];
                if (parts.Length > 4) style.PrimaryText = parts[4];
                if (parts.Length > 5) style.SecondaryText = parts[5];
                if (parts.Length > 6) style.ConnectedAccent = parts[6];
                if (parts.Length > 7) style.DisconnectedAccent = parts[7];
                if (parts.Length > 8) style.WarningAccent = parts[8];
                if (parts.Length > 9 && int.TryParse(parts[9], out parsed)) style.TitleFontSize = Math.Max(12, Math.Min(36, parsed));
                if (parts.Length > 10 && int.TryParse(parts[10], out parsed)) style.MessageFontSize = Math.Max(10, Math.Min(30, parsed));
                if (parts.Length > 11 && int.TryParse(parts[11], out parsed)) style.IconSize = Math.Max(16, Math.Min(72, parsed));
                if (parts.Length > 12 && int.TryParse(parts[12], out parsed)) style.Padding = Math.Max(6, Math.Min(40, parsed));
                bool parsedBool;
                if (parts.Length > 13 && bool.TryParse(parts[13], out parsedBool)) style.ShowBorder = parsedBool;
                if (parts.Length > 14 && !string.IsNullOrWhiteSpace(parts[14])) style.BorderPosition = parts[14];
                if (parts.Length > 15 && int.TryParse(parts[15], out parsed)) style.BorderThickness = Math.Max(0, Math.Min(10, parsed));
                if (parts.Length > 16 && int.TryParse(parts[16], out parsed)) style.CornerRadius = Math.Max(0, Math.Min(40, parsed));
                if (parts.Length > 17 && IsIconPosition(parts[17])) style.IconPosition = parts[17];
                return style;
            }

            private static bool IsIconPosition(string value)
            {
                return value == "Left" || value == "Right" || value == "Top" ||
                    value == "Bottom" || value == "Hidden";
            }
        }

        private static Thickness CreateBorderThickness(string position, double value)
        {
            if (string.Equals(position, "Left", StringComparison.OrdinalIgnoreCase)) return new Thickness(value, 0, 0, 0);
            if (string.Equals(position, "Top", StringComparison.OrdinalIgnoreCase)) return new Thickness(0, value, 0, 0);
            if (string.Equals(position, "Right", StringComparison.OrdinalIgnoreCase)) return new Thickness(0, 0, value, 0);
            if (string.Equals(position, "Full", StringComparison.OrdinalIgnoreCase)) return new Thickness(value);
            return new Thickness(0, 0, 0, value);
        }

        private static Brush Brush(string value, Color fallback)
        {
            return new SolidColorBrush(ParseColor(value, fallback));
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(value); }
            catch { return fallback; }
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y,
            int width, int height, uint flags);
    }
}
