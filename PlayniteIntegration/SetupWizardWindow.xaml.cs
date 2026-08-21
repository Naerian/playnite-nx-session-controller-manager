using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class SetupWizardWindow : Window
    {
        private const int StepCount = 5;
        private readonly ControllerSessionManagerPlugin plugin;
        private readonly ControllerSessionManagerSettings draft;
        private int step;
        private bool suppressRecenter;
        private bool userMovedWindow;

        public SetupWizardWindow(ControllerSessionManagerPlugin sourcePlugin, ControllerSessionManagerSettings workingCopy)
        {
            if (sourcePlugin == null)
            {
                throw new ArgumentNullException("sourcePlugin");
            }

            if (workingCopy == null)
            {
                throw new ArgumentNullException("workingCopy");
            }

            plugin = sourcePlugin;
            draft = workingCopy;
            InitializeComponent();
            Title = Loc("LOCCSM_SetupWizardTitle");
            LoadLabels();
            LoadDraftIntoControls();
            ShowStep(0);
        }

        public ControllerSessionManagerSettings Draft
        {
            get { return draft; }
        }

        private string Loc(string key)
        {
            return plugin.Loc(key);
        }

        private void LoadLabels()
        {
            SkipButton.Content = Loc("LOCCSM_SetupWizardSkip");
            BackButton.Content = Loc("LOCCSM_SetupWizardBack");
            WelcomeBody.Text = Loc("LOCCSM_SetupWizardWelcomeBody");
            PauseNone.Content = Loc("LOCCSM_AutoPauseModeNone");
            PauseNoneHelp.Text = Loc("LOCCSM_AutoPauseModeNoneHelp");
            PauseOffline.Content = Loc("LOCCSM_ForcePauseOfflineGames");
            PauseOfflineHelp.Text = Loc("LOCCSM_ForcePauseOfflineGamesHelp");
            PauseAlways.Content = Loc("LOCCSM_PauseGameOnDisconnect");
            PauseAlwaysHelp.Text = Loc("LOCCSM_PauseGameOnDisconnectHelp");
            PauseOfflineWarning.Text = Loc("LOCCSM_ForcePauseOfflineGamesWarning");
            PauseAlwaysWarning.Text = Loc("LOCCSM_PauseGameOnDisconnectWarning");
            OverlayCheck.Content = Loc("LOCCSM_EnableDisconnectOverlay");
            OverlayHelp.Text = Loc("LOCCSM_SetupWizardOverlayHelp");
            FullscreenToastCheck.Content = Loc("LOCCSM_ShowFullscreenControllerNotifications");
            DesktopToastCheck.Content = Loc("LOCCSM_ShowDesktopControllerNotifications");
            TopPanelLabel.Text = Loc("LOCCSM_TopPanelSectionTitle");
            TopHidden.Content = Loc("LOCCSM_TopPanelModeHidden");
            TopDefault.Content = Loc("LOCCSM_TopPanelModeDefault");
            TopPrimary.Content = Loc("LOCCSM_TopPanelModePrimary");
            SidebarCheck.Content = Loc("LOCCSM_Tester_SettingsShowSidebar");
            SidebarHelp.Text = Loc("LOCCSM_SetupWizardSidebarHelp");
            GuideCheck.Content = Loc("LOCCSM_LaunchFullscreenOnGuide");
            GuideHelp.Text = Loc("LOCCSM_LaunchFullscreenOnGuideHelp");
            SummaryHelp.Text = Loc("LOCCSM_SetupWizardSummaryHelp");
        }

        private void LoadDraftIntoControls()
        {
            PauseNone.IsChecked = draft.IsAutoPauseModeNone;
            PauseOffline.IsChecked = draft.IsAutoPauseModeOfflineOnly;
            PauseAlways.IsChecked = draft.IsAutoPauseModeAlways;
            OverlayCheck.IsChecked = draft.ShowDisconnectOverlay;
            FullscreenToastCheck.IsChecked = draft.ShowFullscreenControllerNotifications;
            DesktopToastCheck.IsChecked = draft.ShowDesktopControllerNotifications;
            TopHidden.IsChecked = string.Equals(
                draft.TopPanelControllerMode, ControllerSessionManagerSettings.TopPanelControllerModeHidden,
                StringComparison.OrdinalIgnoreCase);
            TopDefault.IsChecked = string.Equals(
                draft.TopPanelControllerMode, ControllerSessionManagerSettings.TopPanelControllerModeDefault,
                StringComparison.OrdinalIgnoreCase);
            TopPrimary.IsChecked = string.Equals(
                draft.TopPanelControllerMode, ControllerSessionManagerSettings.TopPanelControllerModePrimary,
                StringComparison.OrdinalIgnoreCase);
            SidebarCheck.IsChecked = draft.Tester != null && draft.Tester.ShowSidebarItem;
            GuideCheck.IsChecked = draft.LaunchFullscreenOnGuideButton;
            UpdatePauseWarnings();
        }

        private void CommitControlsToDraft()
        {
            if (PauseAlways.IsChecked == true)
            {
                draft.AutoPauseMode = "Always";
            }
            else if (PauseOffline.IsChecked == true)
            {
                draft.AutoPauseMode = "OfflineOnly";
            }
            else
            {
                draft.AutoPauseMode = "None";
            }

            draft.ShowDisconnectOverlay = OverlayCheck.IsChecked == true;
            draft.ShowFullscreenControllerNotifications = FullscreenToastCheck.IsChecked == true;
            draft.ShowDesktopControllerNotifications = DesktopToastCheck.IsChecked == true;

            if (TopPrimary.IsChecked == true)
            {
                draft.TopPanelControllerMode = ControllerSessionManagerSettings.TopPanelControllerModePrimary;
            }
            else if (TopDefault.IsChecked == true)
            {
                draft.TopPanelControllerMode = ControllerSessionManagerSettings.TopPanelControllerModeDefault;
            }
            else
            {
                draft.TopPanelControllerMode = ControllerSessionManagerSettings.TopPanelControllerModeHidden;
            }

            if (draft.Tester != null)
            {
                draft.Tester.ShowSidebarItem = SidebarCheck.IsChecked == true;
            }

            draft.LaunchFullscreenOnGuideButton = GuideCheck.IsChecked == true;
            draft.EnableMonitoring = true;
            draft.EnableSessionTracking = true;
        }

        private void ShowStep(int index)
        {
            step = Math.Max(0, Math.Min(StepCount - 1, index));
            StepWelcome.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
            StepPause.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            StepFeedback.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            StepAccess.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            StepSummary.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;

            StepLabel.Text = string.Format(Loc("LOCCSM_SetupWizardStep"), step + 1, StepCount);
            BackButton.Visibility = step == 0 ? Visibility.Collapsed : Visibility.Visible;
            NextButton.Content = step == StepCount - 1
                ? Loc("LOCCSM_SetupWizardFinish")
                : Loc("LOCCSM_SetupWizardNext");

            switch (step)
            {
                case 0:
                    StepTitle.Text = Loc("LOCCSM_SetupWizardWelcomeTitle");
                    StepHelp.Text = Loc("LOCCSM_SetupWizardWelcomeHelp");
                    break;
                case 1:
                    StepTitle.Text = Loc("LOCCSM_SetupWizardPauseTitle");
                    StepHelp.Text = Loc("LOCCSM_SetupWizardPauseHelp");
                    break;
                case 2:
                    StepTitle.Text = Loc("LOCCSM_SetupWizardFeedbackTitle");
                    StepHelp.Text = Loc("LOCCSM_SetupWizardFeedbackHelp");
                    break;
                case 3:
                    StepTitle.Text = Loc("LOCCSM_SetupWizardAccessTitle");
                    StepHelp.Text = Loc("LOCCSM_SetupWizardAccessHelp");
                    break;
                default:
                    CommitControlsToDraft();
                    StepTitle.Text = Loc("LOCCSM_SetupWizardSummaryTitle");
                    StepHelp.Text = string.Empty;
                    RebuildSummaryRows();
                    break;
            }

            Dispatcher.BeginInvoke(new Action(CenterInOwnerOrScreen), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs args)
        {
            CenterInOwnerOrScreen();
            // SizeToContent may finish measuring after Loaded; recenter once layout is idle.
            Dispatcher.BeginInvoke(
                new Action(CenterInOwnerOrScreen),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void RebuildSummaryRows()
        {
            SummaryRows.Children.Clear();

            var pause = draft.IsAutoPauseModeAlways
                ? Loc("LOCCSM_PauseGameOnDisconnect")
                : (draft.IsAutoPauseModeOfflineOnly
                    ? Loc("LOCCSM_ForcePauseOfflineGames")
                    : Loc("LOCCSM_AutoPauseModeNone"));
            var top = string.Equals(draft.TopPanelControllerMode,
                ControllerSessionManagerSettings.TopPanelControllerModePrimary, StringComparison.OrdinalIgnoreCase)
                ? Loc("LOCCSM_TopPanelModePrimary")
                : (string.Equals(draft.TopPanelControllerMode,
                    ControllerSessionManagerSettings.TopPanelControllerModeDefault, StringComparison.OrdinalIgnoreCase)
                    ? Loc("LOCCSM_TopPanelModeDefault")
                    : Loc("LOCCSM_TopPanelModeHidden"));

            AddSummaryRow(Loc("LOCCSM_SetupWizardSummaryLabelPause"), pause);
            AddSummaryRow(
                Loc("LOCCSM_SetupWizardSummaryLabelOverlay"),
                draft.ShowDisconnectOverlay ? Loc("LOCCSM_SetupWizardSummaryOn") : Loc("LOCCSM_SetupWizardSummaryOff"));
            AddSummaryRow(
                Loc("LOCCSM_SetupWizardSummaryLabelFullscreenToasts"),
                draft.ShowFullscreenControllerNotifications ? Loc("LOCCSM_SetupWizardSummaryOn") : Loc("LOCCSM_SetupWizardSummaryOff"));
            AddSummaryRow(
                Loc("LOCCSM_SetupWizardSummaryLabelDesktopToasts"),
                draft.ShowDesktopControllerNotifications ? Loc("LOCCSM_SetupWizardSummaryOn") : Loc("LOCCSM_SetupWizardSummaryOff"));
            AddSummaryRow(Loc("LOCCSM_SetupWizardSummaryLabelTopPanel"), top);
            AddSummaryRow(
                Loc("LOCCSM_SetupWizardSummaryLabelSidebar"),
                draft.Tester != null && draft.Tester.ShowSidebarItem
                    ? Loc("LOCCSM_SetupWizardSummaryOn")
                    : Loc("LOCCSM_SetupWizardSummaryOff"));
            AddSummaryRow(
                Loc("LOCCSM_SetupWizardSummaryLabelGuide"),
                draft.LaunchFullscreenOnGuideButton
                    ? Loc("LOCCSM_SetupWizardSummaryOn")
                    : Loc("LOCCSM_SetupWizardSummaryOff"));
        }

        private void AddSummaryRow(string label, string value)
        {
            var border = new Border { Style = (Style)FindResource("WizardSummaryRow") };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = label,
                Style = (Style)FindResource("WizardSummaryLabel")
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                Style = (Style)FindResource("WizardSummaryValue")
            });
            border.Child = stack;
            SummaryRows.Children.Add(border);
        }

        private void PauseModeChanged(object sender, RoutedEventArgs args)
        {
            UpdatePauseWarnings();
        }

        private void UpdatePauseWarnings()
        {
            PauseOfflineWarning.Visibility = PauseOffline.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            PauseAlwaysWarning.Visibility = PauseAlways.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NextClick(object sender, RoutedEventArgs args)
        {
            if (step < StepCount - 1)
            {
                if (step == 1 || step == 2 || step == 3)
                {
                    CommitControlsToDraft();
                }

                ShowStep(step + 1);
                return;
            }

            CommitControlsToDraft();
            draft.SetupWizardCompleted = true;
            DialogResult = true;
        }

        private void BackClick(object sender, RoutedEventArgs args)
        {
            if (step > 0)
            {
                ShowStep(step - 1);
            }
        }

        private void SkipClick(object sender, RoutedEventArgs args)
        {
            draft.SetupWizardCompleted = true;
            DialogResult = false;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                args.Handled = true;
                SkipClick(sender, args);
            }
        }

        private void OnDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Left)
            {
                try
                {
                    userMovedWindow = true;
                    DragMove();
                }
                catch
                {
                }
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (suppressRecenter || userMovedWindow || !IsLoaded)
            {
                return;
            }

            if (!args.HeightChanged && !args.WidthChanged)
            {
                return;
            }

            CenterInOwnerOrScreen();
        }

        private void CenterInOwnerOrScreen()
        {
            if (userMovedWindow)
            {
                return;
            }

            suppressRecenter = true;
            try
            {
                UpdateLayout();
                var width = ActualWidth;
                var height = ActualHeight;
                // Avoid centering on a half-measured SizeToContent size (anchors top-left
                // at the visual center, then the window grows into the bottom-right).
                if (width < 100 || height < 100 || double.IsNaN(width) || double.IsNaN(height))
                {
                    return;
                }

                // Prefer Playnite's main window so a small/offset settings dialog does not
                // pull the wizard off-center. Owner stays as the settings window for modality.
                var anchor = GetCenteringAnchor();
                Point? centerDip = null;
                if (anchor == null || anchor.WindowState != WindowState.Maximized)
                {
                    centerDip = TryGetWindowCenterDip(anchor);
                }

                double left;
                double top;

                if (centerDip.HasValue)
                {
                    left = centerDip.Value.X - (width / 2.0);
                    top = centerDip.Value.Y - (height / 2.0);
                }
                else
                {
                    var workArea = GetWorkAreaDip(anchor);
                    left = workArea.Left + ((workArea.Width - width) / 2.0);
                    top = workArea.Top + ((workArea.Height - height) / 2.0);
                }

                var clampArea = GetWorkAreaDip(anchor);
                if (width <= clampArea.Width)
                {
                    left = Math.Min(Math.Max(left, clampArea.Left), clampArea.Right - width);
                }
                else
                {
                    left = clampArea.Left;
                }

                if (height <= clampArea.Height)
                {
                    top = Math.Min(Math.Max(top, clampArea.Top), clampArea.Bottom - height);
                }
                else
                {
                    top = clampArea.Top;
                }

                if (!double.IsNaN(left) && !double.IsNaN(top) &&
                    !double.IsInfinity(left) && !double.IsInfinity(top))
                {
                    Left = left;
                    Top = top;
                }
            }
            finally
            {
                suppressRecenter = false;
            }
        }

        private Window GetCenteringAnchor()
        {
            try
            {
                var main = Application.Current != null ? Application.Current.MainWindow : null;
                if (main != null &&
                    main.IsVisible &&
                    main.WindowState != WindowState.Minimized &&
                    main.ActualWidth > 0 &&
                    main.ActualHeight > 0)
                {
                    return main;
                }
            }
            catch
            {
            }

            return Owner;
        }

        /// <summary>
        /// True on-screen center of a window in WPF DIPs.
        /// Uses PointToScreen so maximized windows work (Left/Top are restore bounds).
        /// </summary>
        private Point? TryGetWindowCenterDip(Window window)
        {
            if (window == null ||
                !window.IsVisible ||
                window.WindowState == WindowState.Minimized ||
                window.ActualWidth <= 0 ||
                window.ActualHeight <= 0)
            {
                return null;
            }

            try
            {
                var centerPx = window.PointToScreen(new Point(
                    window.ActualWidth / 2.0,
                    window.ActualHeight / 2.0));
                var fromDevice = GetTransformFromDevice(this) ?? GetTransformFromDevice(window);
                if (fromDevice == null)
                {
                    return new Point(centerPx.X, centerPx.Y);
                }

                return fromDevice.Value.Transform(new Point(centerPx.X, centerPx.Y));
            }
            catch
            {
                return null;
            }
        }

        private Rect GetWorkAreaDip(Window anchor)
        {
            try
            {
                var screen = GetScreenForWindow(anchor) ?? GetScreenForWindow(this) ?? Forms.Screen.PrimaryScreen;
                if (screen == null)
                {
                    return SystemParameters.WorkArea;
                }

                var pixel = screen.WorkingArea;
                var fromDevice = GetTransformFromDevice(anchor) ?? GetTransformFromDevice(this);
                if (fromDevice == null)
                {
                    // Fallback: assume 96 DPI (pixel == DIP).
                    return new Rect(pixel.Left, pixel.Top, pixel.Width, pixel.Height);
                }

                var topLeft = fromDevice.Value.Transform(new Point(pixel.Left, pixel.Top));
                var bottomRight = fromDevice.Value.Transform(new Point(pixel.Right, pixel.Bottom));
                return new Rect(topLeft, bottomRight);
            }
            catch
            {
                return SystemParameters.WorkArea;
            }
        }

        private static Forms.Screen GetScreenForWindow(Window window)
        {
            if (window == null)
            {
                return null;
            }

            try
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                {
                    return Forms.Screen.FromHandle(handle);
                }

                if (!double.IsNaN(window.Left) && !double.IsNaN(window.Top))
                {
                    var px = GetTransformToDevice(window);
                    if (px != null)
                    {
                        var point = px.Value.Transform(new Point(window.Left + 8, window.Top + 8));
                        return Forms.Screen.FromPoint(new System.Drawing.Point(
                            (int)Math.Round(point.X),
                            (int)Math.Round(point.Y)));
                    }

                    return Forms.Screen.FromPoint(new System.Drawing.Point(
                        (int)Math.Round(window.Left + 8),
                        (int)Math.Round(window.Top + 8)));
                }
            }
            catch
            {
            }

            return null;
        }

        private static Matrix? GetTransformFromDevice(Window window)
        {
            var source = GetPresentationSource(window);
            if (source == null || source.CompositionTarget == null)
            {
                return null;
            }

            return source.CompositionTarget.TransformFromDevice;
        }

        private static Matrix? GetTransformToDevice(Window window)
        {
            var source = GetPresentationSource(window);
            if (source == null || source.CompositionTarget == null)
            {
                return null;
            }

            return source.CompositionTarget.TransformToDevice;
        }

        private static PresentationSource GetPresentationSource(Window window)
        {
            if (window == null)
            {
                return null;
            }

            var source = PresentationSource.FromVisual(window);
            if (source != null)
            {
                return source;
            }

            try
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                {
                    return HwndSource.FromHwnd(handle);
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
