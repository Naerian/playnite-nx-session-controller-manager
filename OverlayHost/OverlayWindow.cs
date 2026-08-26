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
        private readonly StackPanel metadataBadges;
        private readonly Border connectionBadge;
        private readonly Border batteryBadge;
        private readonly Path connectionIcon;
        private readonly Path batteryIcon;
        private readonly TextBlock connectionText;
        private readonly TextBlock batteryText;
        private readonly Border incidentCard;
        private readonly StackPanel content;
        private readonly Grid compositionRoot;
        private readonly Border overlayImageLayer;
        private readonly Border overlayTintLayer;
        private readonly Border overlayContentHost;
        private readonly Grid controllerHost;
        private readonly Border controllerContainer;
        private readonly DispatcherTimer watchdog;
        private DateTime lastHeartbeatUtc = DateTime.UtcNow;
        private string currentSessionId;
        private string currentIncidentId;
        private string currentAnimation = "FadeScale";
        private string currentBatteryState;
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
                Fill = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleText = CreateText(30, FontWeights.SemiBold, Brushes.White, new Thickness(0));
            titleText.HorizontalAlignment = HorizontalAlignment.Center;
            titleText.TextAlignment = TextAlignment.Center;
            messageText = CreateText(22, FontWeights.SemiBold, Brushes.White, new Thickness(0));
            messageText.HorizontalAlignment = HorizontalAlignment.Center;
            messageText.TextAlignment = TextAlignment.Center;
            messageText.TextWrapping = TextWrapping.Wrap;
            instructionText = CreateText(19, FontWeights.SemiBold,
                new SolidColorBrush(Color.FromRgb(80, 170, 255)), new Thickness(0));
            instructionText.HorizontalAlignment = HorizontalAlignment.Center;
            instructionText.TextAlignment = TextAlignment.Center;
            instructionText.TextWrapping = TextWrapping.Wrap;
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

            connectionIcon = CreateBadgeIcon();
            batteryIcon = CreateBadgeIcon();
            connectionText = CreateText(13, FontWeights.Medium, Brushes.White, new Thickness(0));
            batteryText = CreateText(13, FontWeights.Medium, Brushes.White, new Thickness(0));
            connectionBadge = CreateMetadataBadge(connectionIcon, connectionText);
            batteryBadge = CreateMetadataBadge(batteryIcon, batteryText);
            metadataBadges = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            metadataBadges.Children.Add(connectionBadge);
            metadataBadges.Children.Add(batteryBadge);

            controllerHost = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ConfigureControllerLayout("Left", 10, true, true);
            controllerContainer = new Border
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = controllerHost
            };

            content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(titleText);
            content.Children.Add(controllerContainer);
            content.Children.Add(metadataBadges);
            content.Children.Add(instructionText);
            content.Children.Add(pauseStatusBadge);
            compositionRoot = new Grid();
            compositionRoot.Children.Add(content);
            overlayImageLayer = new Border { IsHitTestVisible = false };
            overlayTintLayer = new Border { IsHitTestVisible = false };
            overlayContentHost = new Border
            {
                Padding = new Thickness(42, 34, 42, 34),
                Child = compositionRoot
            };
            var cardLayers = new Grid();
            cardLayers.Children.Add(overlayImageLayer);
            cardLayers.Children.Add(overlayTintLayer);
            cardLayers.Children.Add(overlayContentHost);
            incidentCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 18, 20, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(35, 145, 255)),
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(0),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = cardLayers
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
            string presentationStyle, string connectionLabel, string batteryLabel,
            string connectionIconGeometry, string batteryIconGeometry, string batteryState)
        {
            var animateEntry = !IsVisible ||
                !string.Equals(currentIncidentId, incidentId, StringComparison.OrdinalIgnoreCase);
            currentSessionId = sessionId;
            currentIncidentId = incidentId;
            lastHeartbeatUtc = DateTime.UtcNow;
            titleText.Text = title;
            messageText.Text = message;
            instructionText.Text = instruction;
            connectionText.Text = connectionLabel;
            batteryText.Text = batteryLabel;
            currentBatteryState = batteryState;
            SetPathData(connectionIcon, connectionIconGeometry);
            SetPathData(batteryIcon, batteryIconGeometry);
            try
            {
                controllerIcon.Data = string.IsNullOrWhiteSpace(iconGeometry) ? null : Geometry.Parse(iconGeometry);
            }
            catch
            {
                controllerIcon.Data = null;
            }

            ApplyPresentationStyle(presentationStyle);
            var suspensionSucceeded = !forcePause || suspensionLease.TrySuspend(pauseProcessId, incidentId);
            pauseStatusText.Text = suspensionSucceeded ? pauseStatus : pauseFailureStatus;
            ApplyPauseStatusStyle(suspensionSucceeded ? pauseStatusKind : pauseFailureKind);
            try
            {
                var statusGeometry = suspensionSucceeded ? pauseStatusIconGeometry : pauseFailureIconGeometry;
                pauseStatusIcon.Data = string.IsNullOrWhiteSpace(statusGeometry) ? null : Geometry.Parse(statusGeometry);
            }
            catch
            {
                pauseStatusIcon.Data = null;
            }

            var screen = GetTargetScreen(gameProcessId);
            if (!IsVisible)
            {
                Show();
            }
            ApplyWindowStylesAndBounds(screen);
            if (animateEntry)
            {
                BeginEntryAnimation();
            }
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
            var controllerIconSize = ParseInt(parts, 10, 30, 16, 128);
            var statusIconSize = ParseInt(parts, 11, 18, 12, 48);
            var padding = ParseInt(parts, 12, 34, 12, 80);
            bool showBorder;
            if (parts.Length <= 13 || !bool.TryParse(parts[13], out showBorder)) showBorder = true;
            var borderThickness = ParseInt(parts, 14, 3, 0, 10);
            var cornerRadius = ParseInt(parts, 15, 13, 0, 40);
            var showControllerIcon = ParseBool(parts, 16, true);
            var showStatusIcon = ParseBool(parts, 17, true);
            var elementSpacing = ParseInt(parts, 18, 14, 0, 48);
            var iconPosition = parts.Length > 19 && IsIconPosition(parts[19]) ? parts[19] : "Left";
            var showControllerName = ParseBool(parts, 20, true);
            var showConnectionBadge = ParseBool(parts, 23, true);
            var showBatteryBadge = ParseBool(parts, 24, true);
            var showTitle = ParseBool(parts, 25, true);
            var showInstruction = ParseBool(parts, 26, true);
            var showPauseStatus = ParseBool(parts, 27, true);
            var cardPosition = parts.Length > 28 ? parts[28] : "Center";
            currentAnimation = parts.Length > 29 ? parts[29] : "FadeScale";
            var borderPosition = parts.Length > 30 ? parts[30] : "Full";
            var cardWidth = ParseInt(parts, 31, 620, 320, 1000);
            var showShadow = ParseBool(parts, 32, true);
            var titleFontFamily = parts.Length > 33 ? parts[33] : parts.Length > 21 ? parts[21] : null;
            var titleFontWeight = parts.Length > 34 ? parts[34] : parts.Length > 22 ? parts[22] : null;
            var controllerFontFamily = parts.Length > 35 ? parts[35] : titleFontFamily;
            var controllerFontWeight = parts.Length > 36 ? parts[36] : titleFontWeight;
            var instructionFontFamily = parts.Length > 37 ? parts[37] : titleFontFamily;
            var instructionFontWeight = parts.Length > 38 ? parts[38] : titleFontWeight;
            var statusFontFamily = parts.Length > 39 ? parts[39] : titleFontFamily;
            var statusFontWeight = parts.Length > 40 ? parts[40] : titleFontWeight;
            var connectionTextColor = ParseColor(parts.Length > 41 ? parts[41] : null, text);
            var connectionIconColor = ParseColor(parts.Length > 42 ? parts[42] : null, text);
            var connectionBackground = ParseColor(parts.Length > 43 ? parts[43] : null,
                Color.FromArgb(30, presentationAccent.R, presentationAccent.G, presentationAccent.B));
            var connectionBorder = ParseColor(parts.Length > 44 ? parts[44] : null, presentationAccent);
            var connectionBorderThickness = ParseInt(parts, 45, 0, 0, 8);
            var connectionCornerRadius = ParseInt(parts, 46, 5, 0, 24);
            var connectionIconSize = ParseInt(parts, 47, 14, 10, 40);
            var connectionTextSize = ParseInt(parts, 48, 13, 9, 30);
            var batteryTextColor = ParseColor(parts.Length > 49 ? parts[49] : null, presentationWarning);
            var batteryIconColor = ParseColor(parts.Length > 50 ? parts[50] : null, presentationWarning);
            var batteryBackground = ParseColor(parts.Length > 51 ? parts[51] : null,
                Color.FromArgb(30, presentationWarning.R, presentationWarning.G, presentationWarning.B));
            var batteryBorder = ParseColor(parts.Length > 52 ? parts[52] : null, presentationWarning);
            var batteryBorderThickness = ParseInt(parts, 53, 0, 0, 8);
            var batteryCornerRadius = ParseInt(parts, 54, 5, 0, 24);
            var batteryIconSize = ParseInt(parts, 55, 14, 10, 40);
            var batteryTextSize = ParseInt(parts, 56, 13, 9, 30);
            var useBatteryStateColors = ParseBool(parts, 57, true);
            var contentAlignment = parts.Length > 62 ? parts[62] : "Center";
            var screenMargin = ParseInt(parts, 63, 42, 0, 160);
            var useGradient = ParseBool(parts, 64, false);
            var gradientColor = ParseColor(parts.Length > 65 ? parts[65] : null, card);
            var gradientAngle = ParseInt(parts, 66, 0, 0, 359);
            var uppercaseTitle = ParseBool(parts, 67, false);
            var layoutMode = parts.Length > 68 ? parts[68] : "Standard";
            var useBackgroundImage = ParseBool(parts, 69, false);
            var backgroundImagePath = parts.Length > 70 ? DecodeStyleValue(parts[70]) : string.Empty;
            var backgroundImageStretch = parts.Length > 71 ? parts[71] : "UniformToFill";
            var backgroundImageHorizontal = parts.Length > 72 ? parts[72] : "Center";
            var backgroundImageVertical = parts.Length > 73 ? parts[73] : "Center";
            var backgroundImageOpacity = ParseInt(parts, 74, 70, 0, 100);
            var backgroundImageTintOpacity = ParseInt(parts, 75, 45, 0, 100);
            var showControllerContainer = ParseBool(parts, 76, false);
            var controllerContainerColor = ParseColor(parts.Length > 77 ? parts[77] : null,
                Color.FromArgb(32, 0, 0, 0));
            var controllerContainerBorderColor = ParseColor(parts.Length > 78 ? parts[78] : null,
                Colors.Transparent);
            var controllerContainerBorderThickness = ParseInt(parts, 79, 0, 0, 8);
            var controllerContainerCornerRadius = ParseInt(parts, 80, 12, 0, 40);
            var controllerContainerPadding = ParseInt(parts, 81, 12, 0, 32);
            var blockOrder = parts.Length > 82 ? parts[82] : "Title,Controller,Metadata,Instruction,Status";
            var metadataOrientation = parts.Length > 83 ? parts[83] : "Horizontal";
            var useIndependentBorders = ParseBool(parts, 84, false);
            var borderLeftThickness = ParseInt(parts, 85, borderThickness, 0, 12);
            var borderTopThickness = ParseInt(parts, 86, borderThickness, 0, 12);
            var borderRightThickness = ParseInt(parts, 87, borderThickness, 0, 12);
            var borderBottomThickness = ParseInt(parts, 88, borderThickness, 0, 12);
            var useBorderGradient = ParseBool(parts, 89, false);
            var borderGradientStart = ParseColor(parts.Length > 90 ? parts[90] : null, presentationAccent);
            var borderGradientEnd = ParseColor(parts.Length > 91 ? parts[91] : null, presentationAccent);
            var borderGradientAngle = ParseInt(parts, 92, 45, 0, 359);
            var showBorderGlow = ParseBool(parts, 93, false);
            var borderGlowColor = ParseColor(parts.Length > 94 ? parts[94] : null, presentationAccent);
            var borderGlowBlur = ParseInt(parts, 95, 16, 0, 48);
            var borderGlowOpacity = ParseInt(parts, 96, 30, 0, 100);
            if (useBatteryStateColors)
            {
                var stateColor = GetBatteryStateColor(parts, currentBatteryState, batteryTextColor);
                batteryTextColor = stateColor;
                batteryIconColor = stateColor;
            }

            Background = new SolidColorBrush(dim);
            incidentCard.Background = useGradient
                ? (Brush)new LinearGradientBrush(card, gradientColor, gradientAngle)
                : new SolidColorBrush(card);
            incidentCard.BorderBrush = useBorderGradient
                ? (Brush)new LinearGradientBrush(borderGradientStart, borderGradientEnd, borderGradientAngle)
                : new SolidColorBrush(presentationAccent);
            incidentCard.BorderThickness = showBorder
                ? useIndependentBorders
                    ? new Thickness(borderLeftThickness, borderTopThickness,
                        borderRightThickness, borderBottomThickness)
                    : CreateBorderThickness(borderPosition, borderThickness)
                : new Thickness(0);
            incidentCard.CornerRadius = new CornerRadius(cornerRadius);
            overlayContentHost.Padding = new Thickness(padding);
            incidentCard.ClearValue(FrameworkElement.MinWidthProperty);
            incidentCard.MaxWidth = cardWidth;
            compositionRoot.MaxWidth = Math.Max(220, cardWidth - padding * 2);
            titleText.FontSize = titleSize;
            messageText.FontSize = controllerSize;
            instructionText.FontSize = instructionSize;
            pauseStatusText.FontSize = statusSize;
            ApplyTypeface(titleText, titleFontFamily, titleFontWeight);
            ApplyTypeface(messageText, controllerFontFamily, controllerFontWeight);
            ApplyTypeface(instructionText, instructionFontFamily, instructionFontWeight);
            ApplyTypeface(pauseStatusText, statusFontFamily, statusFontWeight);
            ApplyTypeface(connectionText, statusFontFamily, statusFontWeight);
            ApplyTypeface(batteryText, statusFontFamily, statusFontWeight);
            connectionText.FontSize = connectionTextSize;
            batteryText.FontSize = batteryTextSize;
            PathAspectSizer.FitToMaxSize(controllerIcon, controllerIconSize);
            controllerIcon.Visibility = showControllerIcon ? Visibility.Visible : Visibility.Collapsed;
            messageText.Visibility = showControllerName && !string.IsNullOrWhiteSpace(messageText.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            pauseStatusIcon.Width = statusIconSize;
            pauseStatusIcon.Height = statusIconSize;
            pauseStatusIcon.Visibility = showStatusIcon ? Visibility.Visible : Visibility.Collapsed;
            titleText.Visibility = showTitle && !string.IsNullOrWhiteSpace(titleText.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            if (uppercaseTitle && titleText.Visibility == Visibility.Visible)
            {
                titleText.Text = titleText.Text.ToUpperInvariant();
            }
            instructionText.Visibility = showInstruction && !string.IsNullOrWhiteSpace(instructionText.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            pauseStatusBadge.Visibility = showPauseStatus ? Visibility.Visible : Visibility.Collapsed;
            connectionBadge.Visibility = showConnectionBadge &&
                !string.IsNullOrWhiteSpace(connectionText.Text) ? Visibility.Visible : Visibility.Collapsed;
            batteryBadge.Visibility = showBatteryBadge &&
                !string.IsNullOrWhiteSpace(batteryText.Text) ? Visibility.Visible : Visibility.Collapsed;
            metadataBadges.Visibility = connectionBadge.Visibility == Visibility.Visible ||
                batteryBadge.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            metadataBadges.Orientation = string.Equals(metadataOrientation, "Vertical",
                StringComparison.OrdinalIgnoreCase) ? Orientation.Vertical : Orientation.Horizontal;
            titleText.Foreground = new SolidColorBrush(text);
            messageText.Foreground = new SolidColorBrush(text);
            var textBrush = new SolidColorBrush(text);
            controllerIcon.Fill = textBrush;
            instructionText.Foreground = new SolidColorBrush(presentationAccent);
            connectionText.Foreground = new SolidColorBrush(connectionTextColor);
            connectionIcon.Stroke = new SolidColorBrush(connectionIconColor);
            connectionIcon.Width = connectionIconSize;
            connectionIcon.Height = connectionIconSize;
            connectionBadge.Background = new SolidColorBrush(connectionBackground);
            connectionBadge.BorderBrush = new SolidColorBrush(connectionBorder);
            connectionBadge.BorderThickness = new Thickness(connectionBorderThickness);
            connectionBadge.CornerRadius = new CornerRadius(connectionCornerRadius);
            batteryText.Foreground = new SolidColorBrush(batteryTextColor);
            batteryIcon.Stroke = new SolidColorBrush(batteryIconColor);
            batteryIcon.Width = batteryIconSize;
            batteryIcon.Height = batteryIconSize;
            batteryBadge.Background = new SolidColorBrush(batteryBackground);
            batteryBadge.BorderBrush = new SolidColorBrush(batteryBorder);
            batteryBadge.BorderThickness = new Thickness(batteryBorderThickness);
            batteryBadge.CornerRadius = new CornerRadius(batteryCornerRadius);

            controllerContainer.Background = showControllerContainer
                ? new SolidColorBrush(controllerContainerColor) : Brushes.Transparent;
            controllerContainer.BorderBrush = new SolidColorBrush(controllerContainerBorderColor);
            controllerContainer.BorderThickness = showControllerContainer
                ? new Thickness(controllerContainerBorderThickness) : new Thickness(0);
            controllerContainer.CornerRadius = new CornerRadius(controllerContainerCornerRadius);
            controllerContainer.Padding = showControllerContainer
                ? new Thickness(controllerContainerPadding) : new Thickness(0);
            ApplyOverlayBackgroundImage(useBackgroundImage, backgroundImagePath,
                backgroundImageStretch, backgroundImageHorizontal, backgroundImageVertical,
                backgroundImageOpacity, backgroundImageTintOpacity, card,
                new CornerRadius(cornerRadius));

            ApplyContentAlignment(contentAlignment);

            var gap = Math.Max(0, elementSpacing);
            ConfigureControllerLayout(iconPosition, gap, showControllerIcon, showControllerName);
            ConfigureComposition(layoutMode, gap, blockOrder);
            var heroLayout = string.Equals(layoutMode, "Hero", StringComparison.OrdinalIgnoreCase);
            var splitLayout = string.Equals(layoutMode, "Split", StringComparison.OrdinalIgnoreCase);
            titleText.Margin = heroLayout && titleText.Visibility == Visibility.Visible
                ? new Thickness(0, gap, 0, 0) : new Thickness(0);
            controllerContainer.Visibility = controllerHost.Visibility;
            controllerContainer.Margin = controllerContainer.Visibility == Visibility.Visible
                ? (splitLayout || heroLayout ? new Thickness(0) : new Thickness(0, gap, 0, 0))
                : new Thickness(0);
            metadataBadges.Margin = metadataBadges.Visibility == Visibility.Visible
                ? new Thickness(0, gap, 0, 0) : new Thickness(0);
            instructionText.Margin = instructionText.Visibility == Visibility.Visible
                ? new Thickness(0, gap, 0, 0) : new Thickness(0);
            pauseStatusBadge.Margin = pauseStatusBadge.Visibility == Visibility.Visible
                ? new Thickness(0, gap, 0, 0) : new Thickness(0);
            ApplyCardPosition(cardPosition, screenMargin);
            incidentCard.Effect = showBorder && showBorderGlow ? new DropShadowEffect
            {
                BlurRadius = borderGlowBlur,
                ShadowDepth = 0,
                Direction = 0,
                Opacity = borderGlowOpacity / 100.0,
                Color = borderGlowColor
            } : showShadow ? new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 3,
                Direction = 270,
                Opacity = 0.55,
                Color = Colors.Black
            } : null;

            // Grow the card around its content; padding must not shrink the interior.
            compositionRoot.Measure(new Size(Math.Max(220, cardWidth - padding * 2), double.PositiveInfinity));
            var contentWidth = Math.Ceiling(compositionRoot.DesiredSize.Width);
            var contentHeight = Math.Ceiling(compositionRoot.DesiredSize.Height);
            incidentCard.MinWidth = Math.Min(cardWidth, contentWidth + padding * 2);
            incidentCard.MinHeight = contentHeight + padding * 2;
            incidentCard.LayoutTransform = new ScaleTransform(scale, scale);
        }

        private void ApplyCardPosition(string position, double margin)
        {
            incidentCard.Margin = new Thickness(margin);
            incidentCard.HorizontalAlignment = position != null && position.EndsWith("Left",
                StringComparison.OrdinalIgnoreCase) ? HorizontalAlignment.Left :
                position != null && position.EndsWith("Right", StringComparison.OrdinalIgnoreCase)
                    ? HorizontalAlignment.Right : HorizontalAlignment.Center;
            incidentCard.VerticalAlignment = position != null && position.StartsWith("Top",
                StringComparison.OrdinalIgnoreCase) ? VerticalAlignment.Top :
                position != null && position.StartsWith("Bottom", StringComparison.OrdinalIgnoreCase)
                    ? VerticalAlignment.Bottom : VerticalAlignment.Center;
        }

        private void ConfigureComposition(string mode, double gap, string blockOrder)
        {
            Detach(content);
            Detach(titleText);
            Detach(controllerContainer);
            Detach(metadataBadges);
            Detach(instructionText);
            Detach(pauseStatusBadge);
            content.Children.Clear();
            compositionRoot.Children.Clear();
            compositionRoot.ColumnDefinitions.Clear();
            compositionRoot.RowDefinitions.Clear();

            if (string.Equals(mode, "Split", StringComparison.OrdinalIgnoreCase))
            {
                compositionRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                compositionRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap * 2) });
                compositionRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(controllerContainer, 0);
                compositionRoot.Children.Add(controllerContainer);
                var details = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                details.Children.Add(titleText);
                details.Children.Add(metadataBadges);
                details.Children.Add(instructionText);
                details.Children.Add(pauseStatusBadge);
                Grid.SetColumn(details, 2);
                compositionRoot.Children.Add(details);
                return;
            }

            Grid.SetColumn(controllerContainer, 0);
            AddOrderedBlocks(content, blockOrder);
            compositionRoot.Children.Add(content);
        }

        private void AddOrderedBlocks(Panel panel, string order)
        {
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in (order ?? string.Empty).Split(','))
            {
                var key = token.Trim();
                if (!added.Add(key)) continue;
                if (key.Equals("Title", StringComparison.OrdinalIgnoreCase)) panel.Children.Add(titleText);
                else if (key.Equals("Controller", StringComparison.OrdinalIgnoreCase)) panel.Children.Add(controllerContainer);
                else if (key.Equals("Metadata", StringComparison.OrdinalIgnoreCase)) panel.Children.Add(metadataBadges);
                else if (key.Equals("Instruction", StringComparison.OrdinalIgnoreCase)) panel.Children.Add(instructionText);
                else if (key.Equals("Status", StringComparison.OrdinalIgnoreCase)) panel.Children.Add(pauseStatusBadge);
            }
            if (!added.Contains("Title")) panel.Children.Add(titleText);
            if (!added.Contains("Controller")) panel.Children.Add(controllerContainer);
            if (!added.Contains("Metadata")) panel.Children.Add(metadataBadges);
            if (!added.Contains("Instruction")) panel.Children.Add(instructionText);
            if (!added.Contains("Status")) panel.Children.Add(pauseStatusBadge);
        }

        private static void Detach(UIElement element)
        {
            var parent = VisualTreeHelper.GetParent(element) as Panel;
            if (parent != null)
            {
                parent.Children.Remove(element);
            }
        }

        private void ApplyOverlayBackgroundImage(bool enabled, string path, string stretch,
            string horizontal, string vertical, int opacity, int tintOpacity, Color cardColor,
            CornerRadius cornerRadius)
        {
            overlayImageLayer.Background = Brushes.Transparent;
            overlayTintLayer.Background = Brushes.Transparent;
            overlayImageLayer.CornerRadius = cornerRadius;
            overlayTintLayer.CornerRadius = cornerRadius;
            if (!enabled || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                overlayImageLayer.Background = new ImageBrush(bitmap)
                {
                    Stretch = ParseImageStretch(stretch),
                    AlignmentX = ParseAlignmentX(horizontal),
                    AlignmentY = ParseAlignmentY(vertical),
                    Opacity = opacity / 100.0
                };
                overlayTintLayer.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)Math.Round(255 * tintOpacity / 100.0),
                    cardColor.R, cardColor.G, cardColor.B));
            }
            catch
            {
                overlayImageLayer.Background = Brushes.Transparent;
                overlayTintLayer.Background = Brushes.Transparent;
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

        private static string DecodeStyleValue(string value)
        {
            try
            {
                return string.IsNullOrWhiteSpace(value) ? string.Empty
                    : System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch { return string.Empty; }
        }

        private void ApplyContentAlignment(string value)
        {
            var left = string.Equals(value, "Left", StringComparison.OrdinalIgnoreCase);
            var right = string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase);
            var horizontal = left ? HorizontalAlignment.Left : right
                ? HorizontalAlignment.Right : HorizontalAlignment.Center;
            var textAlignment = left ? TextAlignment.Left : right
                ? TextAlignment.Right : TextAlignment.Center;
            content.HorizontalAlignment = horizontal;
            titleText.HorizontalAlignment = horizontal;
            titleText.TextAlignment = textAlignment;
            controllerHost.HorizontalAlignment = horizontal;
            metadataBadges.HorizontalAlignment = horizontal;
            instructionText.HorizontalAlignment = horizontal;
            instructionText.TextAlignment = textAlignment;
            pauseStatusBadge.HorizontalAlignment = horizontal;
        }

        private void BeginEntryAnimation()
        {
            BeginAnimation(OpacityProperty, null);
            incidentCard.BeginAnimation(OpacityProperty, null);
            incidentCard.RenderTransformOrigin = new Point(0.5, 0.5);
            incidentCard.RenderTransform = Transform.Identity;
            if (string.Equals(currentAnimation, "None", StringComparison.OrdinalIgnoreCase))
            {
                Opacity = 1;
                return;
            }

            Opacity = 0;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1,
                TimeSpan.FromMilliseconds(220)) { EasingFunction = new QuadraticEase() });
            if (string.Equals(currentAnimation, "FadeScale", StringComparison.OrdinalIgnoreCase))
            {
                var transform = new ScaleTransform(0.94, 0.94);
                incidentCard.RenderTransform = transform;
                var animation = new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(240))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
            else if (string.Equals(currentAnimation, "Slide", StringComparison.OrdinalIgnoreCase))
            {
                var transform = new TranslateTransform(0, 28);
                incidentCard.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(28, 0, TimeSpan.FromMilliseconds(240))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }
        }

        private static Thickness CreateBorderThickness(string position, double thickness)
        {
            if (string.Equals(position, "Left", StringComparison.OrdinalIgnoreCase)) return new Thickness(thickness, 0, 0, 0);
            if (string.Equals(position, "Top", StringComparison.OrdinalIgnoreCase)) return new Thickness(0, thickness, 0, 0);
            if (string.Equals(position, "Right", StringComparison.OrdinalIgnoreCase)) return new Thickness(0, 0, thickness, 0);
            if (string.Equals(position, "Bottom", StringComparison.OrdinalIgnoreCase)) return new Thickness(0, 0, 0, thickness);
            return new Thickness(thickness);
        }

        private static Path CreateBadgeIcon()
        {
            return new Path
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static Border CreateMetadataBadge(Path icon, TextBlock text)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(icon);
            row.Children.Add(text);
            return new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 5, 9, 5),
                Margin = new Thickness(4, 0, 4, 0),
                Child = row
            };
        }

        private static void SetPathData(Path path, string data)
        {
            try { path.Data = string.IsNullOrWhiteSpace(data) ? null : Geometry.Parse(data); }
            catch { path.Data = null; }
        }

        private void ConfigureControllerLayout(string position, double gap, bool showIcon, bool showName)
        {
            controllerHost.Children.Clear();
            controllerHost.RowDefinitions.Clear();
            controllerHost.ColumnDefinitions.Clear();
            controllerIcon.Margin = new Thickness(0);
            messageText.Margin = new Thickness(0);
            Grid.SetRow(controllerIcon, 0);
            Grid.SetColumn(controllerIcon, 0);
            Grid.SetRow(messageText, 0);
            Grid.SetColumn(messageText, 0);

            if (!showIcon && !showName)
            {
                controllerHost.Visibility = Visibility.Collapsed;
                return;
            }

            controllerHost.Visibility = Visibility.Visible;
            var both = showIcon && showName;
            var normalized = string.IsNullOrWhiteSpace(position) ? "Left" : position;
            if (both &&
                (string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(normalized, "Bottom", StringComparison.OrdinalIgnoreCase)))
            {
                controllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                controllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var iconFirst = string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase);
                Grid.SetRow(controllerIcon, iconFirst ? 0 : 1);
                Grid.SetRow(messageText, iconFirst ? 1 : 0);
                // Only space icon↔name when both are visible; otherwise the gap stacks with the
                // instruction margin and looks larger than title↔controller.
                controllerIcon.Margin = iconFirst ? new Thickness(0, 0, 0, gap) : new Thickness(0, gap, 0, 0);
            }
            else if (both)
            {
                controllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                controllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var iconFirst = !string.Equals(normalized, "Right", StringComparison.OrdinalIgnoreCase);
                Grid.SetColumn(controllerIcon, iconFirst ? 0 : 1);
                Grid.SetColumn(messageText, iconFirst ? 1 : 0);
                controllerIcon.Margin = iconFirst ? new Thickness(0, 0, gap, 0) : new Thickness(gap, 0, 0, 0);
            }
            else
            {
                controllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                controllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            if (showIcon)
            {
                controllerHost.Children.Add(controllerIcon);
            }

            if (showName)
            {
                controllerHost.Children.Add(messageText);
            }
        }

        private static bool IsIconPosition(string value)
        {
            return value == "Left" || value == "Right" || value == "Top" || value == "Bottom";
        }

        private static int ParseInt(string[] parts, int index, int fallback, int minimum, int maximum)
        {
            int value;
            return parts.Length > index && int.TryParse(parts[index], out value)
                ? Math.Max(minimum, Math.Min(maximum, value))
                : fallback;
        }

        private static bool ParseBool(string[] parts, int index, bool fallback)
        {
            bool value;
            return parts.Length > index && bool.TryParse(parts[index], out value) ? value : fallback;
        }

        private static void ApplyTypeface(TextBlock textBlock, string family, string weight)
        {
            textBlock.FontFamily = NotificationFontCatalog.Resolve(family, weight);
            textBlock.FontWeight = NotificationFontCatalog.ResolveEffectiveWeight(family, weight);
        }

        private static Color GetBatteryStateColor(string[] parts, string state, Color fallback)
        {
            var index = string.Equals(state, "Full", StringComparison.OrdinalIgnoreCase) ? 58
                : string.Equals(state, "Medium", StringComparison.OrdinalIgnoreCase) ? 59
                : string.Equals(state, "Low", StringComparison.OrdinalIgnoreCase) ? 60
                : string.Equals(state, "Empty", StringComparison.OrdinalIgnoreCase) ? 61
                : -1;
            return index < 0 || parts.Length <= index ? fallback : ParseColor(parts[index], fallback);
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

        private static TextBlock CreateText(double size, FontWeight weight, Brush brush, Thickness margin)
        {
            return new TextBlock
            {
                FontSize = size,
                FontWeight = weight,
                Foreground = brush,
                Margin = margin,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
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
