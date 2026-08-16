using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.OverlayHost
{
    internal sealed class OverlayWindow : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x20;
        private const int WsExToolWindow = 0x80;
        private const int WsExNoActivate = 0x08000000;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);

        private readonly int parentProcessId;
        private readonly ProcessSuspensionLease suspensionLease;
        private readonly TextBlock titleText;
        private readonly TextBlock messageText;
        private readonly TextBlock instructionText;
        private readonly TextBlock pauseStatusText;
        private readonly Path controllerIcon;
        private readonly Path pauseStatusIcon;
        private readonly Border pauseStatusBadge;
        private readonly Border incidentCard;
        private readonly DispatcherTimer watchdog;
        private DateTime lastHeartbeatUtc = DateTime.UtcNow;
        private string currentSessionId;
        private Color presentationAccent = Color.FromRgb(91, 177, 255);
        private Color presentationWarning = Color.FromRgb(245, 181, 66);

        public OverlayWindow(int sourceParentProcessId, ProcessSuspensionLease sourceSuspensionLease)
        {
            parentProcessId = sourceParentProcessId;
            suspensionLease = sourceSuspensionLease;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));

            controllerIcon = new Path
            {
                Width = 30,
                Height = 30,
                Stretch = Stretch.Uniform,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            titleText = CreateText(30, FontWeights.SemiBold, Brushes.White, new Thickness(0, 0, 0, 12));
            messageText = CreateText(22, FontWeights.SemiBold, Brushes.White, new Thickness(0));
            instructionText = CreateText(19, FontWeights.SemiBold,
                new SolidColorBrush(Color.FromRgb(80, 170, 255)), new Thickness(0, 0, 0, 14));
            pauseStatusText = CreateText(15, FontWeights.Normal,
                new SolidColorBrush(Color.FromRgb(190, 195, 205)), new Thickness(0));
            pauseStatusIcon = new Path
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusContent = new StackPanel { Orientation = Orientation.Horizontal };
            statusContent.Children.Add(pauseStatusIcon);
            statusContent.Children.Add(pauseStatusText);
            pauseStatusBadge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 7, 12, 7),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = statusContent
            };

            var controllerContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
            controllerContent.Children.Add(controllerIcon);
            controllerContent.Children.Add(messageText);

            var content = new StackPanel { MaxWidth = 720 };
            content.Children.Add(titleText);
            content.Children.Add(controllerContent);
            content.Children.Add(instructionText);
            content.Children.Add(pauseStatusBadge);
            incidentCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 18, 20, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(35, 145, 255)),
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(42, 34, 42, 34),
                MinWidth = 540,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = content
            };
            Content = new Grid { Children = { incidentCard } };

            SourceInitialized += OnSourceInitialized;
            watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            watchdog.Tick += OnWatchdogTick;
            watchdog.Start();
        }

        public void ShowIncident(string sessionId, string incidentId, int gameProcessId, string title,
            string message, string instruction, string pauseStatus, string pauseStatusKind,
            string pauseStatusIconGeometry, string iconGeometry, bool forcePause, int pauseProcessId,
            string pauseFailureStatus, string pauseFailureKind, string pauseFailureIconGeometry,
            string presentationStyle)
        {
            currentSessionId = sessionId;
            lastHeartbeatUtc = DateTime.UtcNow;
            titleText.Text = title;
            messageText.Text = message;
            instructionText.Text = instruction;
            ApplyPresentationStyle(presentationStyle);
            var suspensionSucceeded = !forcePause || suspensionLease.TrySuspend(pauseProcessId, incidentId);
            pauseStatusText.Text = suspensionSucceeded ? pauseStatus : pauseFailureStatus;
            ApplyPauseStatusStyle(suspensionSucceeded ? pauseStatusKind : pauseFailureKind);
            try
            {
                controllerIcon.Data = string.IsNullOrWhiteSpace(iconGeometry) ? null : Geometry.Parse(iconGeometry);
                var statusGeometry = suspensionSucceeded ? pauseStatusIconGeometry : pauseFailureIconGeometry;
                pauseStatusIcon.Data = string.IsNullOrWhiteSpace(statusGeometry) ? null : Geometry.Parse(statusGeometry);
            }
            catch
            {
                controllerIcon.Data = null;
                pauseStatusIcon.Data = null;
            }

            var screen = GetTargetScreen(gameProcessId);
            if (!IsVisible)
            {
                Show();
            }
            ApplyWindowStylesAndBounds(screen);
        }

        private void ApplyPauseStatusStyle(string kind)
        {
            Color foreground;
            Color background;
            if (string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase))
            {
                foreground = presentationWarning;
                background = Color.FromArgb(42, presentationWarning.R, presentationWarning.G, presentationWarning.B);
            }
            else if (string.Equals(kind, "pause", StringComparison.OrdinalIgnoreCase))
            {
                foreground = presentationAccent;
                background = Color.FromArgb(38, presentationAccent.R, presentationAccent.G, presentationAccent.B);
            }
            else
            {
                foreground = Color.FromRgb(190, 195, 205);
                background = Color.FromArgb(28, 190, 195, 205);
            }

            var brush = new SolidColorBrush(foreground);
            pauseStatusText.Foreground = brush;
            pauseStatusIcon.Stroke = brush;
            pauseStatusBadge.Background = new SolidColorBrush(background);
        }

        private void ApplyPresentationStyle(string value)
        {
            var parts = (value ?? string.Empty).Split(';');
            int scalePercent;
            if (parts.Length == 0 || !int.TryParse(parts[0], out scalePercent)) scalePercent = 100;
            scalePercent = Math.Max(80, Math.Min(140, scalePercent));
            var scale = scalePercent / 100.0;
            var dim = ParseColor(parts.Length > 1 ? parts[1] : null, Color.FromArgb(150, 0, 0, 0));
            var card = ParseColor(parts.Length > 2 ? parts[2] : null, Color.FromArgb(235, 18, 20, 24));
            presentationAccent = ParseColor(parts.Length > 3 ? parts[3] : null, Color.FromRgb(35, 145, 255));
            var text = ParseColor(parts.Length > 4 ? parts[4] : null, Colors.White);
            presentationWarning = ParseColor(parts.Length > 5 ? parts[5] : null, Color.FromRgb(245, 181, 66));
            var titleSize = ParseInt(parts, 6, 30, 18, 64);
            var controllerSize = ParseInt(parts, 7, 22, 12, 48);
            var instructionSize = ParseInt(parts, 8, 19, 12, 40);
            var statusSize = ParseInt(parts, 9, 15, 10, 30);
            var controllerIconSize = ParseInt(parts, 10, 30, 16, 72);
            var statusIconSize = ParseInt(parts, 11, 18, 12, 48);
            var padding = ParseInt(parts, 12, 34, 12, 80);
            bool showBorder;
            if (parts.Length <= 13 || !bool.TryParse(parts[13], out showBorder)) showBorder = true;
            var borderThickness = ParseInt(parts, 14, 3, 0, 10);
            var cornerRadius = ParseInt(parts, 15, 13, 0, 40);

            Background = new SolidColorBrush(dim);
            incidentCard.Background = new SolidColorBrush(card);
            incidentCard.BorderBrush = new SolidColorBrush(presentationAccent);
            incidentCard.BorderThickness = showBorder ? new Thickness(borderThickness) : new Thickness(0);
            incidentCard.CornerRadius = new CornerRadius(cornerRadius);
            incidentCard.Padding = new Thickness(padding);
            incidentCard.MinWidth = 540 * scale;
            titleText.FontSize = titleSize;
            messageText.FontSize = controllerSize;
            instructionText.FontSize = instructionSize;
            pauseStatusText.FontSize = statusSize;
            controllerIcon.Width = controllerIconSize;
            controllerIcon.Height = controllerIconSize;
            pauseStatusIcon.Width = statusIconSize;
            pauseStatusIcon.Height = statusIconSize;
            titleText.Foreground = new SolidColorBrush(text);
            messageText.Foreground = new SolidColorBrush(text);
            controllerIcon.Stroke = new SolidColorBrush(text);
            instructionText.Foreground = new SolidColorBrush(presentationAccent);
        }

        private static int ParseInt(string[] parts, int index, int fallback, int minimum, int maximum)
        {
            int value;
            return parts.Length > index && int.TryParse(parts[index], out value)
                ? Math.Max(minimum, Math.Min(maximum, value))
                : fallback;
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(value); }
            catch { return fallback; }
        }

        public void HideSession(string sessionId)
        {
            if (string.Equals(currentSessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            {
                suspensionLease.Resume();
                Hide();
            }
            lastHeartbeatUtc = DateTime.UtcNow;
        }

        public void RecordHeartbeat(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(currentSessionId) ||
                string.Equals(currentSessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            {
                lastHeartbeatUtc = DateTime.UtcNow;
            }
        }

        private void OnSourceInitialized(object sender, EventArgs args)
        {
            ApplyWindowStylesAndBounds(Forms.Screen.PrimaryScreen);
        }

        private void ApplyWindowStylesAndBounds(Forms.Screen screen)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(handle, GwlExStyle);
            SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
            var bounds = (screen ?? Forms.Screen.PrimaryScreen).Bounds;
            SetWindowPos(handle, HwndTopmost, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                SwpNoActivate | SwpShowWindow);
        }

        private Forms.Screen GetTargetScreen(int processId)
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
            catch
            {
            }

            return Forms.Screen.PrimaryScreen;
        }

        private void OnWatchdogTick(object sender, EventArgs args)
        {
            try
            {
                using (var parent = Process.GetProcessById(parentProcessId))
                {
                    if (parent.HasExited)
                    {
                        suspensionLease.Resume();
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }
            catch
            {
                suspensionLease.Resume();
                Application.Current.Shutdown();
                return;
            }

            var elapsed = DateTime.UtcNow - lastHeartbeatUtc;
            if (IsVisible && elapsed > TimeSpan.FromSeconds(8))
            {
                suspensionLease.Resume();
                Hide();
            }
            if (elapsed > TimeSpan.FromSeconds(30))
            {
                suspensionLease.Resume();
                Application.Current.Shutdown();
            }
        }

        private static TextBlock CreateText(double size, FontWeight weight, Brush foreground, Thickness margin)
        {
            return new TextBlock
            {
                FontSize = size,
                FontWeight = weight,
                Foreground = foreground,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = margin
            };
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
