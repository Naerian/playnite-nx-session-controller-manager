using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using ControllerSessionManager.PlayniteIntegration;

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
        private readonly Border iconContainer;
        private readonly Path connectionIcon;
        private readonly Grid rootLayout;
        private readonly Grid contentLayout;
        private readonly Grid cardShell;
        private readonly StackPanel textPanel;
        private readonly Border card;
        private readonly Border imageLayer;
        private readonly Border tintLayer;
        private readonly Border borderOverlay;
        private readonly Border contentHost;
        private readonly DispatcherTimer holdTimer;
        private double currentShadowInset;
        private ToastRequest current;
        private ToastStyle currentStyle;

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
            iconContainer = new Border
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = icon
            };
            connectionIcon = new Path
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.75,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
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
            rootLayout = new Grid();
            rootLayout.Children.Add(contentLayout);
            rootLayout.Children.Add(connectionIcon);
            var paddedContent = new Border
            {
                Padding = new Thickness(18, 14, 18, 14),
                Child = rootLayout
            };
            contentHost = paddedContent;
            card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(244, 18, 20, 24)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0)
            };
            imageLayer = new Border { IsHitTestVisible = false };
            tintLayer = new Border { IsHitTestVisible = false };
            borderOverlay = new Border { Background = Brushes.Transparent, IsHitTestVisible = false };
            cardShell = new Grid();
            cardShell.Children.Add(card);
            cardShell.Children.Add(imageLayer);
            cardShell.Children.Add(tintLayer);
            cardShell.Children.Add(borderOverlay);
            cardShell.Children.Add(contentHost);
            ConfigureContentLayout("Left", 14);
            Content = cardShell;
            SourceInitialized += OnSourceInitialized;
            holdTimer = new DispatcherTimer();
            holdTimer.Tick += OnHoldElapsed;
        }

        public void Enqueue(string id, int processId, int durationMilliseconds, string kind,
            string title, string message, string iconGeometry, string presentationStyle,
            string connectionIconGeometry = null)
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
                PresentationStyle = presentationStyle,
                ConnectionIconGeometry = connectionIconGeometry
            });
            if (current == null)
            {
                ShowNext();
            }
        }

        public void ReplaceWith(string id, int processId, int durationMilliseconds, string kind,
            string title, string message, string iconGeometry, string presentationStyle,
            string connectionIconGeometry = null)
        {
            pending.Clear();
            holdTimer.Stop();
            current = null;
            currentStyle = null;
            BeginAnimation(OpacityProperty, null);
            cardShell.RenderTransform = Transform.Identity;
            Opacity = 0;
            Enqueue(id, processId, durationMilliseconds, kind, title, message, iconGeometry,
                presentationStyle, connectionIconGeometry);
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
            currentStyle = style;
            textPanel.Children.Clear();
            if (string.Equals(style.TextOrder, "MessageFirst", StringComparison.OrdinalIgnoreCase))
            {
                textPanel.Children.Add(messageText);
                textPanel.Children.Add(titleText);
            }
            else
            {
                textPanel.Children.Add(titleText);
                textPanel.Children.Add(messageText);
            }
            titleText.Text = style.UppercaseTitle
                ? (current.Title ?? string.Empty).ToUpperInvariant()
                : current.Title;
            var scale = style.ScalePercent / 100.0;
            var iconSize = Math.Max(16, style.IconSize * scale);
            var titleSize = Math.Max(10, style.TitleFontSize * scale);
            var messageSize = Math.Max(9, style.MessageFontSize * scale);
            var padding = Math.Max(0, style.Padding * scale);
            var elementSpacing = Math.Max(0, style.ElementSpacing * scale);
            var cardWidth = Math.Max(280, style.Width * scale);
            currentShadowInset = style.ShowShadow || style.ShowBorderGlow
                ? Math.Ceiling(Math.Max(18, style.BorderGlowBlur + 4) * scale)
                : 0;
            cardShell.Margin = new Thickness(currentShadowInset);
            Width = cardWidth + currentShadowInset * 2;
            titleText.FontSize = titleSize;
            messageText.FontSize = messageSize;
            titleText.FontFamily = NotificationFontCatalog.Resolve(style.TitleFontFamily, style.TitleFontWeight);
            titleText.FontWeight = NotificationFontCatalog.ResolveEffectiveWeight(style.TitleFontFamily, style.TitleFontWeight);
            messageText.FontFamily = NotificationFontCatalog.Resolve(style.MessageFontFamily, style.MessageFontWeight);
            messageText.FontWeight = NotificationFontCatalog.ResolveEffectiveWeight(style.MessageFontFamily, style.MessageFontWeight);
            messageText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            messageText.LineHeight = Math.Ceiling(messageSize * 1.28);
            messageText.MaxHeight = messageText.LineHeight * style.MessageMaxLines;
            messageText.TextTrimming = TextTrimming.CharacterEllipsis;
            titleText.TextAlignment = NotificationFontCatalog.ResolveAlignment(style.TextAlignment);
            messageText.TextAlignment = titleText.TextAlignment;
            titleText.Visibility = style.ShowTitle && !string.IsNullOrWhiteSpace(current.Title)
                ? Visibility.Visible : Visibility.Collapsed;
            titleText.Margin = new Thickness(0);
            messageText.Margin = new Thickness(0);
            if (titleText.Visibility == Visibility.Visible && messageText.Visibility == Visibility.Visible)
            {
                if (string.Equals(style.TextOrder, "MessageFirst", StringComparison.OrdinalIgnoreCase))
                    titleText.Margin = new Thickness(0, elementSpacing, 0, 0);
                else messageText.Margin = new Thickness(0, elementSpacing, 0, 0);
            }
            icon.Width = iconSize;
            icon.Height = iconSize;
            try { icon.Data = Geometry.Parse(current.IconGeometry ?? string.Empty); }
            catch { icon.Data = null; }
            PathAspectSizer.FitToMaxSize(icon, iconSize);
            var iconContainerPadding = style.ShowIconContainer
                ? Math.Max(0, style.IconContainerPadding * scale) : 0;
            iconContainer.Padding = new Thickness(iconContainerPadding);
            iconContainer.Background = style.ShowIconContainer
                ? Brush(style.IconContainerColor, Color.FromArgb(32, 0, 0, 0))
                : Brushes.Transparent;
            iconContainer.BorderBrush = Brush(style.IconContainerBorderColor, Colors.Transparent);
            iconContainer.BorderThickness = style.ShowIconContainer
                ? new Thickness(Math.Max(0, style.IconContainerBorderThickness * scale))
                : new Thickness(0);
            iconContainer.CornerRadius = new CornerRadius(
                Math.Max(0, style.IconContainerCornerRadius * scale));
            var iconGap = Math.Max(0, style.IconSpacing * scale);
            var verticalIcon = string.Equals(style.IconPosition, "Top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(style.IconPosition, "Bottom", StringComparison.OrdinalIgnoreCase);
            var hiddenIcon = string.Equals(style.IconPosition, "Hidden", StringComparison.OrdinalIgnoreCase);
            var textHeight = (titleText.Visibility == Visibility.Visible ? titleSize : 0) +
                (messageText.Visibility == Visibility.Visible
                    ? messageSize * 2 + (titleText.Visibility == Visibility.Visible ? elementSpacing : 0)
                    : 0);
            var contentHeight = hiddenIcon
                ? textHeight
                : verticalIcon
                    ? icon.Height + iconContainerPadding * 2 + iconGap + textHeight
                    : Math.Max(icon.Height + iconContainerPadding * 2, textHeight);
            Height = Math.Max(1, contentHeight + padding * 2);
            ConfigureContentLayout(style.IconPosition, iconGap);
            contentHost.Padding = new Thickness(padding);
            card.CornerRadius = new CornerRadius(style.CornerRadius * scale);
            imageLayer.CornerRadius = card.CornerRadius;
            tintLayer.CornerRadius = card.CornerRadius;
            borderOverlay.CornerRadius = card.CornerRadius;
            var primaryTextBrush = Brush(style.PrimaryText, Colors.White);
            titleText.Foreground = primaryTextBrush;
            messageText.Foreground = Brush(style.SecondaryText, Color.FromRgb(198, 203, 212));
            var secondaryBrush = Brush(style.SecondaryText, Color.FromRgb(198, 203, 212));
            var isWarning = string.Equals(current.Kind, "warning", StringComparison.OrdinalIgnoreCase);
            var isLowBattery = string.Equals(current.Kind, "lowbattery", StringComparison.OrdinalIgnoreCase);
            var accent = ParseColor(
                isLowBattery
                    ? style.LowBatteryAccent
                    : isWarning
                        ? style.WarningAccent
                        : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                            ? style.ConnectedAccent
                            : style.DisconnectedAccent,
                isLowBattery
                    ? Color.FromRgb(224, 82, 82)
                    : isWarning
                        ? Color.FromRgb(245, 181, 66)
                        : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                            ? Color.FromRgb(79, 194, 126)
                            : Color.FromRgb(80, 170, 255));
            var accentBrush = new SolidColorBrush(accent);
            var stateBorderColor = isLowBattery ? style.LowBatteryBorderColor : isWarning
                ? style.WarningBorderColor : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                    ? style.ConnectedBorderColor : style.DisconnectedBorderColor;
            var borderAccent = style.UseStateBorderColors
                ? ParseColor(stateBorderColor, accent)
                : accent;
            var borderAccentBrush = new SolidColorBrush(borderAccent);
            var stateBackground = isLowBattery ? style.LowBatteryBackground : isWarning
                ? style.WarningBackground : string.Equals(current.Kind, "connected", StringComparison.OrdinalIgnoreCase)
                    ? style.ConnectedBackground : style.DisconnectedBackground;
            var background = ParseColor(style.UseStateBackgroundColors ? stateBackground : style.Background,
                Color.FromArgb(244, 18, 20, 24));
            if (string.Equals(style.AccentMode, "TintedBackground", StringComparison.OrdinalIgnoreCase))
            {
                background = Blend(background, accent, 0.12);
            }
            else if (string.Equals(style.AccentMode, "SolidBackground", StringComparison.OrdinalIgnoreCase))
            {
                background = Color.FromArgb(Math.Max((byte)220, background.A), accent.R, accent.G, accent.B);
            }
            var gradient = ParseColor(style.GradientColor, background);
            card.Background = style.UseGradient
                ? (Brush)new LinearGradientBrush(background, gradient, style.GradientAngle)
                : new SolidColorBrush(background);
            ApplyBackgroundImage(style, background);
            card.BorderThickness = new Thickness(0);
            borderOverlay.BorderBrush = style.UseBorderGradient
                ? (Brush)new LinearGradientBrush(
                    ParseColor(style.BorderGradientStartColor, accent),
                    style.UseStateBorderColors
                        ? borderAccent
                        : ParseColor(style.BorderGradientEndColor, accent),
                    style.BorderGradientAngle)
                : borderAccentBrush;
            borderOverlay.BorderThickness = style.ShowBorder
                ? style.UseIndependentBorders
                    ? new Thickness(style.BorderLeftThickness, style.BorderTopThickness,
                        style.BorderRightThickness, style.BorderBottomThickness)
                    : CreateBorderThickness(style.BorderPosition, style.BorderThickness)
                : new Thickness(0);
            borderOverlay.Effect = style.ShowBorder && style.ShowBorderGlow
                ? new DropShadowEffect
                {
                    BlurRadius = Math.Max(0, style.BorderGlowBlur * scale),
                    ShadowDepth = 0,
                    Opacity = style.BorderGlowOpacity / 100.0,
                    Color = ParseColor(style.BorderGlowColor, accent),
                    Direction = 0
                }
                : null;
            icon.Fill = string.Equals(style.AccentMode, "SolidBackground", StringComparison.OrdinalIgnoreCase)
                ? primaryTextBrush
                : accentBrush;
            icon.Stroke = Brushes.Transparent;
            icon.StrokeThickness = 0;

            ApplyConnectionIcon(
                style.ShowConnectionBadge ? current.ConnectionIconGeometry : null,
                secondaryBrush,
                scale,
                style.BadgePosition);
            // Match PlayniteAchievements' layer model: only the rounded surface casts the shadow;
            // text and icons live in the separate crisp layer above. The outer inset prevents the
            // transparent topmost window from clipping the blur into a displaced hard edge.
            card.Effect = style.ShowShadow
                ? new DropShadowEffect
                {
                    BlurRadius = Math.Max(8, 12 * scale),
                    ShadowDepth = Math.Max(1, 3 * scale),
                    Opacity = 0.5,
                    Color = Colors.Black,
                    Direction = 300
                }
                : null;
            cardShell.Measure(new Size(Width, double.PositiveInfinity));
            Height = Math.Max(1, Math.Ceiling(cardShell.DesiredSize.Height));
            if (!IsVisible)
            {
                Show();
            }
            ApplyBounds(GetTargetScreen(current.ProcessId), style.Position, style.ScreenMargin);
            BeginEntryAnimation(style);
            holdTimer.Interval = TimeSpan.FromMilliseconds(current.DurationMilliseconds);
            holdTimer.Start();
        }

        private void BeginEntryAnimation(ToastStyle style)
        {
            BeginAnimation(OpacityProperty, null);
            cardShell.RenderTransformOrigin = new Point(0.5, 0.5);
            cardShell.RenderTransform = Transform.Identity;
            var duration = TimeSpan.FromMilliseconds(190);
            if (string.Equals(style.Animation, "None", StringComparison.OrdinalIgnoreCase))
            {
                Opacity = 1;
                return;
            }

            if (string.Equals(style.Animation, "Slide", StringComparison.OrdinalIgnoreCase))
            {
                var from = style.Position.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 ? -42 : 42;
                var transform = new TranslateTransform(from, 0);
                cardShell.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(from, 0, duration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            }
            else if (string.Equals(style.Animation, "Scale", StringComparison.OrdinalIgnoreCase))
            {
                var transform = new ScaleTransform(0.86, 0.86);
                cardShell.RenderTransform = transform;
                var easing = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.86, 1, duration) { EasingFunction = easing });
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.86, 1, duration) { EasingFunction = easing });
            }

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
        }

        private void ApplyBackgroundImage(ToastStyle style, Color background)
        {
            imageLayer.Background = Brushes.Transparent;
            tintLayer.Background = Brushes.Transparent;
            if (!style.UseBackgroundImage || string.IsNullOrWhiteSpace(style.BackgroundImagePath) ||
                !System.IO.File.Exists(style.BackgroundImagePath))
            {
                return;
            }

            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(style.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                var brush = new ImageBrush(bitmap)
                {
                    Stretch = ParseImageStretch(style.BackgroundImageStretch),
                    AlignmentX = ParseAlignmentX(style.BackgroundImageHorizontalAlignment),
                    AlignmentY = ParseAlignmentY(style.BackgroundImageVerticalAlignment),
                    Opacity = style.BackgroundImageOpacity / 100.0
                };
                imageLayer.Background = brush;
                tintLayer.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)Math.Round(255 * style.BackgroundImageTintOpacity / 100.0),
                    background.R, background.G, background.B));
            }
            catch
            {
                imageLayer.Background = Brushes.Transparent;
                tintLayer.Background = Brushes.Transparent;
            }
        }

        private static Stretch ParseImageStretch(string value)
        {
            if (string.Equals(value, "Uniform", StringComparison.OrdinalIgnoreCase)) return Stretch.Uniform;
            if (string.Equals(value, "Fill", StringComparison.OrdinalIgnoreCase)) return Stretch.Fill;
            return Stretch.UniformToFill;
        }

        private static AlignmentX ParseAlignmentX(string value)
        {
            if (string.Equals(value, "Left", StringComparison.OrdinalIgnoreCase)) return AlignmentX.Left;
            if (string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase)) return AlignmentX.Right;
            return AlignmentX.Center;
        }

        private static AlignmentY ParseAlignmentY(string value)
        {
            if (string.Equals(value, "Top", StringComparison.OrdinalIgnoreCase)) return AlignmentY.Top;
            if (string.Equals(value, "Bottom", StringComparison.OrdinalIgnoreCase)) return AlignmentY.Bottom;
            return AlignmentY.Center;
        }

        private void ApplyConnectionIcon(string geometry, Brush stroke, double scale, string position)
        {
            if (string.IsNullOrWhiteSpace(geometry))
            {
                connectionIcon.Data = null;
                connectionIcon.Visibility = Visibility.Collapsed;
                titleText.Margin = new Thickness(0);
                messageText.Margin = new Thickness(0, messageText.Margin.Top, 0, 0);
                return;
            }

            try
            {
                connectionIcon.Data = Geometry.Parse(geometry);
            }
            catch
            {
                connectionIcon.Data = null;
            }

            if (connectionIcon.Data == null)
            {
                connectionIcon.Visibility = Visibility.Collapsed;
                titleText.Margin = new Thickness(0);
                messageText.Margin = new Thickness(0, messageText.Margin.Top, 0, 0);
                return;
            }

            var size = Math.Max(11, Math.Min(16, 13 * scale));
            var reserve = size + Math.Max(8, 10 * scale);
            connectionIcon.Width = size;
            connectionIcon.Height = size;
            connectionIcon.Stroke = stroke;
            connectionIcon.StrokeThickness = Math.Max(1.4, 1.75 * scale);
            var left = string.Equals(position, "TopLeft", StringComparison.OrdinalIgnoreCase);
            connectionIcon.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            connectionIcon.VerticalAlignment = VerticalAlignment.Top;
            connectionIcon.Margin = new Thickness(0);
            connectionIcon.Visibility = Visibility.Visible;
            // Keep the title from running under the badge; long names ellipsis instead.
            titleText.Margin = left ? new Thickness(reserve, 0, 0, 0) : new Thickness(0, 0, reserve, 0);
            if (titleText.Visibility != Visibility.Visible)
            {
                messageText.Margin = left
                    ? new Thickness(reserve, messageText.Margin.Top, 0, 0)
                    : new Thickness(0, messageText.Margin.Top, reserve, 0);
            }
        }

        private void ConfigureContentLayout(string position, double gap)
        {
            var normalized = string.IsNullOrWhiteSpace(position) ? "Left" : position;
            contentLayout.Children.Clear();
            contentLayout.ColumnDefinitions.Clear();
            contentLayout.RowDefinitions.Clear();
            icon.Margin = new Thickness(0);
            iconContainer.Margin = new Thickness(0);
            iconContainer.HorizontalAlignment = HorizontalAlignment.Center;
            iconContainer.VerticalAlignment = VerticalAlignment.Center;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            textPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            textPanel.VerticalAlignment = VerticalAlignment.Center;

            if (string.Equals(normalized, "Hidden", StringComparison.OrdinalIgnoreCase))
            {
                iconContainer.Visibility = Visibility.Collapsed;
                contentLayout.Children.Add(textPanel);
                return;
            }

            iconContainer.Visibility = Visibility.Visible;
            if (string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                contentLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var iconFirst = string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase);
                Grid.SetRow(iconContainer, iconFirst ? 0 : 1);
                Grid.SetRow(textPanel, iconFirst ? 1 : 0);
                iconContainer.Margin = iconFirst ? new Thickness(0, 0, 0, gap) : new Thickness(0, gap, 0, 0);
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
                Grid.SetColumn(iconContainer, iconFirst ? 0 : 1);
                Grid.SetColumn(textPanel, iconFirst ? 1 : 0);
                iconContainer.Margin = iconFirst ? new Thickness(0, 0, gap, 0) : new Thickness(gap, 0, 0, 0);
            }

            contentLayout.Children.Add(iconContainer);
            contentLayout.Children.Add(textPanel);
        }

        private void OnHoldElapsed(object sender, EventArgs args)
        {
            holdTimer.Stop();
            BeginExitAnimation();
        }

        private void BeginExitAnimation()
        {
            var style = currentStyle ?? new ToastStyle();
            if (string.Equals(style.Animation, "None", StringComparison.OrdinalIgnoreCase))
            {
                ShowNext();
                return;
            }

            var duration = TimeSpan.FromMilliseconds(180);
            if (string.Equals(style.Animation, "Slide", StringComparison.OrdinalIgnoreCase))
            {
                var to = style.Position.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 ? -34 : 34;
                var transform = new TranslateTransform(0, 0);
                cardShell.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, to, duration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            }
            else if (string.Equals(style.Animation, "Scale", StringComparison.OrdinalIgnoreCase))
            {
                var transform = new ScaleTransform(1, 1);
                cardShell.RenderTransform = transform;
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.90, duration));
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.90, duration));
            }

            var fade = new DoubleAnimation(Opacity, 0, duration);
            fade.Completed += delegate { ShowNext(); };
            BeginAnimation(OpacityProperty, fade);
        }

        private void OnSourceInitialized(object sender, EventArgs args)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(handle, GwlExStyle);
            SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
        }

        private void ApplyBounds(Forms.Screen screen, string position, int screenMargin)
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
            var shadowInsetX = (int)Math.Ceiling(currentShadowInset * dpiScaleX);
            var shadowInsetY = (int)Math.Ceiling(currentShadowInset * dpiScaleY);
            var margin = Math.Max(8, Math.Min(64, screenMargin));
            var left = string.Equals(position, "TopLeft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(position, "BottomLeft", StringComparison.OrdinalIgnoreCase)
                ? bounds.Left + margin - shadowInsetX
                : bounds.Right - pixelWidth - margin + shadowInsetX;
            var top = string.Equals(position, "BottomLeft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(position, "BottomRight", StringComparison.OrdinalIgnoreCase)
                ? bounds.Bottom - pixelHeight - margin + shadowInsetY
                : bounds.Top + margin - shadowInsetY;
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
            public string ConnectionIconGeometry { get; set; }
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
            public string LowBatteryAccent = "#FFE05252";
            public int TitleFontSize = 19;
            public int MessageFontSize = 15;
            public int IconSize = 32;
            public int Padding = 18;
            public bool ShowBorder = true;
            public string BorderPosition = "Bottom";
            public int BorderThickness = 3;
            public int CornerRadius = 10;
            public string IconPosition = "Left";
            public int ElementSpacing = 8;
            public int IconSpacing = 14;
            public bool ShowConnectionBadge = true;
            public int ScreenMargin = 28;
            public bool ShowShadow = true;
            public string FontFamily = NotificationFontCatalog.SystemDefault;
            public string FontWeight = "SemiBold";
            public string TitleFontFamily = NotificationFontCatalog.SystemDefault;
            public string TitleFontWeight = "SemiBold";
            public string MessageFontFamily = NotificationFontCatalog.SystemDefault;
            public string MessageFontWeight = "SemiBold";
            public int MessageMaxLines = 2;
            public string BadgePosition = "TopRight";
            public string TextAlignment = "Left";
            public string AccentMode = "IconAndBorder";
            public string Animation = "Fade";
            public bool ShowTitle = true;
            public bool UseBackgroundImage;
            public string BackgroundImagePath = string.Empty;
            public string BackgroundImageStretch = "UniformToFill";
            public string BackgroundImageHorizontalAlignment = "Center";
            public string BackgroundImageVerticalAlignment = "Center";
            public int BackgroundImageOpacity = 45;
            public int BackgroundImageTintOpacity = 45;
            public bool UseGradient;
            public string GradientColor = "#F4121418";
            public int GradientAngle;
            public bool UppercaseTitle;
            public bool ShowIconContainer;
            public string IconContainerColor = "#20000000";
            public string IconContainerBorderColor = "#00000000";
            public int IconContainerBorderThickness;
            public int IconContainerCornerRadius = 10;
            public int IconContainerPadding = 8;
            public string TextOrder = "TitleFirst";
            public bool UseIndependentBorders;
            public int BorderLeftThickness;
            public int BorderTopThickness;
            public int BorderRightThickness;
            public int BorderBottomThickness = 3;
            public bool UseStateBackgroundColors;
            public string ConnectedBackground = "#F4121418";
            public string DisconnectedBackground = "#F4121418";
            public string WarningBackground = "#F4121418";
            public string LowBatteryBackground = "#F4121418";
            public bool UseBorderGradient;
            public bool UseStateBorderColors;
            public string ConnectedBorderColor = "#FF4FC27E";
            public string DisconnectedBorderColor = "#FF50AAFF";
            public string WarningBorderColor = "#FFF5B542";
            public string LowBatteryBorderColor = "#FFE05252";
            public string BorderGradientStartColor = "#FFFFFFFF";
            public string BorderGradientEndColor = "#FF50AAFF";
            public int BorderGradientAngle = 45;
            public bool ShowBorderGlow;
            public string BorderGlowColor = "#8050AAFF";
            public int BorderGlowBlur = 12;
            public int BorderGlowOpacity = 30;

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
                if (parts.Length > 11 && int.TryParse(parts[11], out parsed)) style.IconSize = Math.Max(16, Math.Min(128, parsed));
                if (parts.Length > 12 && int.TryParse(parts[12], out parsed)) style.Padding = Math.Max(0, Math.Min(40, parsed));
                bool parsedBool;
                if (parts.Length > 13 && bool.TryParse(parts[13], out parsedBool)) style.ShowBorder = parsedBool;
                if (parts.Length > 14 && !string.IsNullOrWhiteSpace(parts[14])) style.BorderPosition = parts[14];
                if (parts.Length > 15 && int.TryParse(parts[15], out parsed)) style.BorderThickness = Math.Max(0, Math.Min(10, parsed));
                if (parts.Length > 16 && int.TryParse(parts[16], out parsed)) style.CornerRadius = Math.Max(0, Math.Min(40, parsed));
                if (parts.Length > 17 && IsIconPosition(parts[17])) style.IconPosition = parts[17];
                if (parts.Length > 18 && int.TryParse(parts[18], out parsed)) style.ElementSpacing = Math.Max(0, Math.Min(40, parsed));
                if (parts.Length > 19 && !string.IsNullOrWhiteSpace(parts[19])) style.LowBatteryAccent = parts[19];
                if (parts.Length > 20 && bool.TryParse(parts[20], out parsedBool)) style.ShowConnectionBadge = parsedBool;
                if (parts.Length > 21 && int.TryParse(parts[21], out parsed)) style.ScreenMargin = Math.Max(8, Math.Min(64, parsed));
                if (parts.Length > 22 && bool.TryParse(parts[22], out parsedBool)) style.ShowShadow = parsedBool;
                if (parts.Length > 23) style.FontFamily = NotificationFontCatalog.Normalize(parts[23]);
                if (parts.Length > 24) style.FontWeight = NotificationFontCatalog.NormalizeWeight(parts[24]);
                if (parts.Length > 25) style.TextAlignment = NotificationFontCatalog.NormalizeAlignment(parts[25]);
                if (parts.Length > 26) style.AccentMode = NotificationFontCatalog.NormalizeAccentMode(parts[26]);
                if (parts.Length > 27) style.Animation = NotificationFontCatalog.NormalizeAnimation(parts[27]);
                if (parts.Length > 28 && bool.TryParse(parts[28], out parsedBool)) style.ShowTitle = parsedBool;
                if (parts.Length > 29 && bool.TryParse(parts[29], out parsedBool)) style.UseBackgroundImage = parsedBool;
                if (parts.Length > 30) style.BackgroundImagePath = DecodeStyleValue(parts[30]);
                if (parts.Length > 31 && !string.IsNullOrWhiteSpace(parts[31])) style.BackgroundImageStretch = parts[31];
                if (parts.Length > 32 && !string.IsNullOrWhiteSpace(parts[32])) style.BackgroundImageHorizontalAlignment = parts[32];
                if (parts.Length > 33 && !string.IsNullOrWhiteSpace(parts[33])) style.BackgroundImageVerticalAlignment = parts[33];
                if (parts.Length > 34 && int.TryParse(parts[34], out parsed)) style.BackgroundImageOpacity = Math.Max(0, Math.Min(100, parsed));
                if (parts.Length > 35 && int.TryParse(parts[35], out parsed)) style.BackgroundImageTintOpacity = Math.Max(0, Math.Min(100, parsed));
                if (parts.Length > 36 && int.TryParse(parts[36], out parsed)) style.IconSpacing = Math.Max(0, Math.Min(40, parsed));
                style.TitleFontFamily = parts.Length > 37
                    ? NotificationFontCatalog.Normalize(parts[37]) : style.FontFamily;
                style.TitleFontWeight = parts.Length > 38
                    ? NotificationFontCatalog.NormalizeWeight(parts[38]) : style.FontWeight;
                style.MessageFontFamily = parts.Length > 39
                    ? NotificationFontCatalog.Normalize(parts[39]) : style.FontFamily;
                style.MessageFontWeight = parts.Length > 40
                    ? NotificationFontCatalog.NormalizeWeight(parts[40]) : style.FontWeight;
                if (parts.Length > 41 && int.TryParse(parts[41], out parsed))
                    style.MessageMaxLines = Math.Max(1, Math.Min(6, parsed));
                if (parts.Length > 42 && string.Equals(parts[42], "TopLeft", StringComparison.OrdinalIgnoreCase))
                    style.BadgePosition = "TopLeft";
                if (parts.Length > 43 && bool.TryParse(parts[43], out parsedBool)) style.UseGradient = parsedBool;
                if (parts.Length > 44 && !string.IsNullOrWhiteSpace(parts[44])) style.GradientColor = parts[44];
                if (parts.Length > 45 && int.TryParse(parts[45], out parsed)) style.GradientAngle = ((parsed % 360) + 360) % 360;
                if (parts.Length > 46 && bool.TryParse(parts[46], out parsedBool)) style.UppercaseTitle = parsedBool;
                if (parts.Length > 47 && bool.TryParse(parts[47], out parsedBool)) style.ShowIconContainer = parsedBool;
                if (parts.Length > 48) style.IconContainerColor = parts[48];
                if (parts.Length > 49) style.IconContainerBorderColor = parts[49];
                if (parts.Length > 50 && int.TryParse(parts[50], out parsed)) style.IconContainerBorderThickness = Math.Max(0, Math.Min(8, parsed));
                if (parts.Length > 51 && int.TryParse(parts[51], out parsed)) style.IconContainerCornerRadius = Math.Max(0, Math.Min(40, parsed));
                if (parts.Length > 52 && int.TryParse(parts[52], out parsed)) style.IconContainerPadding = Math.Max(0, Math.Min(24, parsed));
                if (parts.Length > 53 && string.Equals(parts[53], "MessageFirst", StringComparison.OrdinalIgnoreCase)) style.TextOrder = "MessageFirst";
                if (parts.Length > 54 && bool.TryParse(parts[54], out parsedBool)) style.UseIndependentBorders = parsedBool;
                if (parts.Length > 55 && int.TryParse(parts[55], out parsed)) style.BorderLeftThickness = Math.Max(0, Math.Min(12, parsed));
                if (parts.Length > 56 && int.TryParse(parts[56], out parsed)) style.BorderTopThickness = Math.Max(0, Math.Min(12, parsed));
                if (parts.Length > 57 && int.TryParse(parts[57], out parsed)) style.BorderRightThickness = Math.Max(0, Math.Min(12, parsed));
                if (parts.Length > 58 && int.TryParse(parts[58], out parsed)) style.BorderBottomThickness = Math.Max(0, Math.Min(12, parsed));
                if (parts.Length > 59 && bool.TryParse(parts[59], out parsedBool)) style.UseStateBackgroundColors = parsedBool;
                if (parts.Length > 60) style.ConnectedBackground = parts[60];
                if (parts.Length > 61) style.DisconnectedBackground = parts[61];
                if (parts.Length > 62) style.WarningBackground = parts[62];
                if (parts.Length > 63) style.LowBatteryBackground = parts[63];
                if (parts.Length > 64 && bool.TryParse(parts[64], out parsedBool)) style.UseBorderGradient = parsedBool;
                if (parts.Length > 65) style.BorderGradientStartColor = parts[65];
                if (parts.Length > 66) style.BorderGradientEndColor = parts[66];
                if (parts.Length > 67 && int.TryParse(parts[67], out parsed)) style.BorderGradientAngle = ((parsed % 360) + 360) % 360;
                if (parts.Length > 68 && bool.TryParse(parts[68], out parsedBool)) style.ShowBorderGlow = parsedBool;
                if (parts.Length > 69) style.BorderGlowColor = parts[69];
                if (parts.Length > 70 && int.TryParse(parts[70], out parsed)) style.BorderGlowBlur = Math.Max(0, Math.Min(40, parsed));
                if (parts.Length > 71 && int.TryParse(parts[71], out parsed)) style.BorderGlowOpacity = Math.Max(0, Math.Min(100, parsed));
                if (parts.Length > 72 && bool.TryParse(parts[72], out parsedBool)) style.UseStateBorderColors = parsedBool;
                if (parts.Length > 73) style.ConnectedBorderColor = parts[73];
                if (parts.Length > 74) style.DisconnectedBorderColor = parts[74];
                if (parts.Length > 75) style.WarningBorderColor = parts[75];
                if (parts.Length > 76) style.LowBatteryBorderColor = parts[76];
                return style;
            }

            private static string DecodeStyleValue(string value)
            {
                try
                {
                    return string.IsNullOrWhiteSpace(value)
                        ? string.Empty
                        : System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
                catch
                {
                    return string.Empty;
                }
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

        private static Color Blend(Color background, Color accent, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                background.A,
                (byte)Math.Round(background.R * (1 - amount) + accent.R * amount),
                (byte)Math.Round(background.G * (1 - amount) + accent.G * amount),
                (byte)Math.Round(background.B * (1 - amount) + accent.B * amount));
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
