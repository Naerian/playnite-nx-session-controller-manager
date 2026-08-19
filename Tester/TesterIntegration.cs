using ControllerSessionManager.Tester.Services;
using ControllerSessionManager.Tester.ViewModels;
using ControllerSessionManager.Tester.Views;
using ControllerSessionManager.Tester.Views.ThemeIntegration;
using ControllerSessionManager.Tester.Models;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;
using System.Windows.Media;

namespace ControllerSessionManager.Tester
{
    internal sealed class TesterIntegration
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly ILogger logger;
        private readonly Func<string, string> loc;
        private readonly Action openDesktopSettings;
        private GamepadTesterSettings settings;
        private GamepadTesterViewModel sidebarViewModel;
        private GamepadTesterThemeIntegration themeIntegration;
        private Commands.RelayCommand openTesterCommand;
        private Commands.RelayCommand openButtonTestCommand;
        private Commands.RelayCommand openSticksCommand;
        private Commands.RelayCommand openRumbleCommand;
        private Commands.RelayCommand openLatencyCommand;
        private Window testerWindow;
        private GamepadTesterViewModel testerWindowViewModel;
        private bool testerBackButtonHeld;
        private bool testerCaptureLeftShoulderHeld;
        private bool testerCaptureRightShoulderHeld;
        private System.Windows.Threading.DispatcherTimer testerCaptureExitTimer;
        private bool themeCaptureLeftShoulderHeld;
        private bool themeCaptureRightShoulderHeld;
        private System.Windows.Threading.DispatcherTimer themeCaptureExitTimer;
        private GamepadTesterThemeControlBase themeCaptureOwner;

        public GamepadTesterThemeIntegration ThemeIntegration
        {
            get { return themeIntegration; }
        }

        public TesterIntegration(IPlayniteAPI api, ILogger sourceLogger, GamepadTesterSettings testerSettings,
            Func<string, string> localizer, Action openDesktopSettings)
        {
            playniteApi = api;
            logger = sourceLogger;
            loc = localizer;
            this.openDesktopSettings = openDesktopSettings;
            settings = testerSettings ?? new GamepadTesterSettings();
            openTesterCommand = new Commands.RelayCommand(() => OpenTester(0, false));
            openButtonTestCommand = new Commands.RelayCommand(() => OpenTester(0, true));
            openRumbleCommand = new Commands.RelayCommand(() => OpenTester(0, true));
            openSticksCommand = new Commands.RelayCommand(() => OpenTester(1, true));
            openLatencyCommand = new Commands.RelayCommand(() => OpenTester(2, true));
            themeIntegration = new GamepadTesterThemeIntegration(settings, openTesterCommand, openButtonTestCommand,
                openSticksCommand, openRumbleCommand, openLatencyCommand);
            GamepadTesterThemeHost.Configure(
                settings,
                Loc,
                () => OpenTester(0, true),
                message => logger.Info(message));
        }

        public void UpdateSettings(GamepadTesterSettings testerSettings)
        {
            settings = testerSettings ?? new GamepadTesterSettings();
            GamepadTesterThemeHost.Configure(
                settings,
                Loc,
                () => OpenTester(0, true),
                message => logger.Info(message));
        }

        public void Shutdown()
        {
            DisposeSidebarView();
            if (testerWindow != null)
            {
                try
                {
                    testerWindow.Close();
                }
                catch
                {
                }
            }

            testerWindowViewModel = null;
            testerWindow = null;
            TesterHostClient.ForceStopShared();
        }

        public IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (!settings.ShowSidebarItem)
            {
                yield break;
            }

            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = Loc("LOCCSM_Tester_PluginName"),
                Visible = true,
                Icon = CreateSidebarIcon(),
                Opened = () =>
                {
                    DisposeSidebarView();
                    GamepadTesterViewModel viewModel;
                    var view = CreateTesterView(out viewModel, false);
                    sidebarViewModel = viewModel;
                    return view;
                },
                Closed = DisposeSidebarView
            };
        }

        public IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield break;
        }

        public Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null)
            {
                return null;
            }

            if (IsThemeControlName(args.Name, "GamepadTesterLauncher"))
            {
                return new GamepadTesterThemeLauncherControl(() => OpenTester(0, true), Loc);
            }

            if (IsThemeControlName(args.Name, "StatusBadge"))
            {
                return new GamepadTesterStatusBadgeControl(settings, Loc);
            }

            if (IsThemeControlName(args.Name, "ButtonMap"))
            {
                return new GamepadTesterButtonMapControl(settings, Loc);
            }

            if (IsThemeControlName(args.Name, "StickCheck"))
            {
                return new GamepadTesterStickCheckControl(settings, Loc);
            }

            if (IsThemeControlName(args.Name, "TriggerCheck"))
            {
                return new GamepadTesterTriggerCheckControl(settings, Loc);
            }

            if (IsThemeControlName(args.Name, "RumblePad"))
            {
                return new GamepadTesterRumblePadControl(settings, Loc);
            }

            if (IsThemeControlName(args.Name, "LatencyMini"))
            {
                return new GamepadTesterLatencyMiniControl(settings, Loc);
            }

            return null;
        }

        private static bool IsThemeControlName(string actualName, string logicalName)
        {
            return string.Equals(actualName, logicalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualName, "GamepadTester" + logicalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualName, "GamepadTester_" + logicalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualName, "Tester" + logicalName, StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(logicalName, "GamepadTesterLauncher", StringComparison.OrdinalIgnoreCase) &&
                 (string.Equals(actualName, "TesterLauncher", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(actualName, "Launcher", StringComparison.OrdinalIgnoreCase)));
        }

        public void HandleControllerInput(OnControllerButtonStateChangedArgs args)
        {
            if (args == null)
            {
                return;
            }

            HandleThemeControllerInput(args.Button, args.State);

            if (testerWindow == null || testerWindowViewModel == null)
            {
                return;
            }

            testerWindow.Dispatcher.BeginInvoke(new Action(() => HandleTesterControllerInput(args.Button, args.State)));
        }

        private void HandleThemeControllerInput(ControllerInput button, ControllerInputState state)
        {
            var dispatcher = Application.Current == null ? null : Application.Current.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.BeginInvoke(new Action(() => HandleThemeControllerInputOnUiThread(button, state)));
        }

        private void HandleThemeControllerInputOnUiThread(ControllerInput button, ControllerInputState state)
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            var focusedThemeControl = FindThemeControlFromFocus(focused);
            var themeControl = GamepadTesterThemeHost.FindActiveCaptureControl() ?? focusedThemeControl;
            var buttonMap = themeControl as GamepadTesterButtonMapControl;
            var latencyControl = themeControl as GamepadTesterLatencyMiniControl;
            var stickControl = themeControl as GamepadTesterStickCheckControl;
            var captureActive = (buttonMap != null && buttonMap.IsTestRunning) ||
                (latencyControl != null && latencyControl.IsTestRunning) ||
                (stickControl != null && stickControl.IsTestRunning);

            if (captureActive)
            {
                themeCaptureOwner = themeControl;
                if (button == ControllerInput.LeftShoulder)
                {
                    themeCaptureLeftShoulderHeld = state == ControllerInputState.Pressed;
                }
                else if (button == ControllerInput.RightShoulder)
                {
                    themeCaptureRightShoulderHeld = state == ControllerInputState.Pressed;
                }

                UpdateThemeCaptureExitChord();
                return;
            }

            ResetThemeCaptureExitChord();
            if (state == ControllerInputState.Pressed && button == ControllerInput.A)
            {
                ActivateFocusedThemeControl();
            }
        }

        private void UpdateThemeCaptureExitChord()
        {
            if (!themeCaptureLeftShoulderHeld || !themeCaptureRightShoulderHeld)
            {
                if (themeCaptureExitTimer != null)
                {
                    themeCaptureExitTimer.Stop();
                }

                return;
            }

            if (themeCaptureExitTimer == null)
            {
                themeCaptureExitTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                themeCaptureExitTimer.Tick += OnThemeCaptureExitTimerTick;
            }

            themeCaptureExitTimer.Stop();
            themeCaptureExitTimer.Start();
        }

        private void OnThemeCaptureExitTimerTick(object sender, EventArgs args)
        {
            themeCaptureExitTimer.Stop();
            if (!themeCaptureLeftShoulderHeld || !themeCaptureRightShoulderHeld)
            {
                return;
            }

            var completedCaptureOwner = themeCaptureOwner;
            var buttonMap = completedCaptureOwner as GamepadTesterButtonMapControl;
            if (buttonMap != null)
            {
                buttonMap.StopButtonCapture();
            }

            var latencyControl = completedCaptureOwner as GamepadTesterLatencyMiniControl;
            if (latencyControl != null)
            {
                latencyControl.StopLatencyTest();
            }

            var stickControl = completedCaptureOwner as GamepadTesterStickCheckControl;
            if (stickControl != null)
            {
                stickControl.StopStickCapture();
            }

            ResetThemeCaptureExitChord();
            FocusThemeBackButton(completedCaptureOwner);
        }

        private static void FocusThemeBackButton(FrameworkElement captureOwner)
        {
            var window = captureOwner == null ? null : Window.GetWindow(captureOwner);
            if (window == null)
            {
                return;
            }

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                var backButton = FindNamedButton(window, "GamepadTester_BackButton");
                if (backButton != null && backButton.IsVisible && backButton.IsEnabled)
                {
                    backButton.Focus();
                    Keyboard.Focus(backButton);
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private static ButtonBase FindNamedButton(DependencyObject root, string name)
        {
            if (root == null)
            {
                return null;
            }

            var element = root as FrameworkElement;
            var button = root as ButtonBase;
            if (button != null && element != null && element.Name == name)
            {
                return button;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var match = FindNamedButton(VisualTreeHelper.GetChild(root, index), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void ResetThemeCaptureExitChord()
        {
            themeCaptureLeftShoulderHeld = false;
            themeCaptureRightShoulderHeld = false;
            themeCaptureOwner = null;
            if (themeCaptureExitTimer != null)
            {
                themeCaptureExitTimer.Stop();
            }
        }

        private void OpenTester(int selectedTabIndex, bool fullscreenSimplified)
        {
            if (playniteApi.ApplicationInfo.Mode != ApplicationMode.Fullscreen && openDesktopSettings != null)
            {
                PendingTabIndex = selectedTabIndex;
                PendingOpenSettingsTab = true;
                openDesktopSettings();
                return;
            }

            OpenTesterWindow(selectedTabIndex, fullscreenSimplified);
        }

        public static int PendingTabIndex;
        public static ushort PendingVendorId;
        public static ushort PendingProductId;
        public static string PendingControllerName;
        public static bool PendingOpenSettingsTab;

        public static void RequestController(ushort vendorId, ushort productId, string name = null)
        {
            PendingVendorId = vendorId;
            PendingProductId = productId;
            PendingControllerName = name;
        }

        private void OpenTesterWindow(int selectedTabIndex, bool fullscreenSimplified)
        {
            try
            {
                if (testerWindow != null)
                {
                    if (testerWindowViewModel != null)
                    {
                        testerWindowViewModel.SelectedTabIndex = selectedTabIndex;
                        testerWindowViewModel.IsFullscreenSimplifiedMode = fullscreenSimplified;
                    }

                    testerWindow.Activate();
                    return;
                }

                GamepadTesterViewModel viewModel;
                var view = CreateTesterView(out viewModel);
                viewModel.SelectedTabIndex = selectedTabIndex;
                viewModel.IsFullscreenSimplifiedMode = fullscreenSimplified && playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
                var fullscreenFriendly = ShouldUseFullscreenFriendlyWindow();
                var window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = !fullscreenFriendly,
                    ShowMaximizeButton = !fullscreenFriendly,
                    ShowCloseButton = true
                });

                window.Title = Loc("LOCCSM_Tester_PluginName");
                window.Content = view;
                window.Owner = playniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ApplyTesterWindowSize(window, fullscreenFriendly, 1280, 820, 1180, 760);
                testerWindow = window;
                testerWindowViewModel = viewModel;
                window.Closed += (sender, eventArgs) =>
                {
                    ResetTesterCaptureExitChord();
                    viewModel.Dispose();
                    if (ReferenceEquals(testerWindow, window))
                    {
                        testerWindow = null;
                        testerWindowViewModel = null;
                        testerBackButtonHeld = false;
                    }
                };
                window.Closing += OnTesterWindowClosing;
                window.PreviewKeyDown += CloseWindowOnEscape;

                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester.");
                playniteApi.Dialogs.ShowErrorMessage(exception.Message, Loc("LOCCSM_Tester_PluginName"));
            }
        }

        private void HandleTesterControllerInput(ControllerInput button, ControllerInputState state)
        {
            if (testerWindow == null || testerWindowViewModel == null || !testerWindow.IsVisible)
            {
                return;
            }

            if (!testerWindowViewModel.CanNavigateBack)
            {
                if (button == ControllerInput.LeftShoulder)
                {
                    testerCaptureLeftShoulderHeld = state == ControllerInputState.Pressed;
                }
                else if (button == ControllerInput.RightShoulder)
                {
                    testerCaptureRightShoulderHeld = state == ControllerInputState.Pressed;
                }

                UpdateTesterCaptureExitChord();
                return;
            }

            ResetTesterCaptureExitChord();

            if (button == ControllerInput.Back)
            {
                testerBackButtonHeld = state == ControllerInputState.Pressed;
                return;
            }

            if (state != ControllerInputState.Pressed)
            {
                return;
            }

            if (testerBackButtonHeld && button == ControllerInput.A && testerWindowViewModel.IsFullscreenSimplifiedMode && testerWindowViewModel.SelectedTabIndex == GamepadTesterViewModel.TabLatency)
            {
                if (testerWindowViewModel.StartLatencyTestCommand.CanExecute(null))
                {
                    testerWindowViewModel.StartLatencyTestCommand.Execute(null);
                }

                return;
            }

            if (button == ControllerInput.B)
            {
                testerWindow.Close();
                return;
            }

            if (button == ControllerInput.LeftShoulder)
            {
                testerWindowViewModel.MoveSelectedTab(-1);
                FocusFirstTesterControl();
                return;
            }

            if (button == ControllerInput.RightShoulder)
            {
                testerWindowViewModel.MoveSelectedTab(1);
                FocusFirstTesterControl();
                return;
            }

            if (button == ControllerInput.A)
            {
                ActivateFocusedControl();
                return;
            }

            if (button == ControllerInput.DPadUp)
            {
                MoveFocus(FocusNavigationDirection.Up);
                return;
            }

            if (button == ControllerInput.DPadDown)
            {
                MoveFocus(FocusNavigationDirection.Down);
                return;
            }

            if (button == ControllerInput.DPadLeft)
            {
                MoveFocus(FocusNavigationDirection.Left);
                return;
            }

            if (button == ControllerInput.DPadRight)
            {
                MoveFocus(FocusNavigationDirection.Right);
            }
        }

        private void ActivateFocusedControl()
        {
            var button = FindButtonFromFocus(Keyboard.FocusedElement as DependencyObject);
            if (button == null || !button.IsEnabled)
            {
                return;
            }

            ActivateButton(button);
        }

        private void ActivateFocusedThemeControl()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            var button = FindButtonFromFocus(focused);
            if (button != null && button.IsEnabled && IsInsideGamepadTesterThemeControl(button))
            {
                ActivateButton(button);
                return;
            }

            var themeControl = FindThemeControlFromFocus(focused);
            if (themeControl == null)
            {
                return;
            }

            var firstButton = FindFirstEnabledButton(themeControl);
            if (firstButton != null)
            {
                ActivateButton(firstButton);
            }
        }

        private static ButtonBase FindButtonFromFocus(DependencyObject focused)
        {
            var current = focused;
            while (current != null)
            {
                var button = current as ButtonBase;
                if (button != null)
                {
                    return button;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static void ActivateButton(ButtonBase button)
        {
            if (button.Command != null)
            {
                var parameter = button.CommandParameter;
                if (button.Command.CanExecute(parameter))
                {
                    button.Command.Execute(parameter);
                }

                return;
            }

            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private static bool IsInsideGamepadTesterThemeControl(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is GamepadTesterThemeControlBase || current is GamepadTesterThemeLauncherControl)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static GamepadTesterThemeControlBase FindThemeControlFromFocus(DependencyObject focused)
        {
            var current = focused;
            while (current != null)
            {
                var themeControl = current as GamepadTesterThemeControlBase;
                if (themeControl != null)
                {
                    return themeControl;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return FindDescendant<GamepadTesterThemeControlBase>(focused);
        }

        private static ButtonBase FindFirstEnabledButton(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            var rootButton = root as ButtonBase;
            if (rootButton != null && rootButton.IsEnabled)
            {
                return rootButton;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var button = FindFirstEnabledButton(VisualTreeHelper.GetChild(root, i));
                if (button != null)
                {
                    return button;
                }
            }

            var contentControl = root as ContentControl;
            if (contentControl != null)
            {
                var content = contentControl.Content as DependencyObject;
                if (content != null)
                {
                    return FindFirstEnabledButton(content);
                }
            }

            return null;
        }

        private static T FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var match = child as T;
                if (match != null)
                {
                    return match;
                }

                match = FindDescendant<T>(child);
                if (match != null)
                {
                    return match;
                }
            }

            var contentControl = root as ContentControl;
            if (contentControl != null)
            {
                var content = contentControl.Content as DependencyObject;
                if (content != null)
                {
                    return FindDescendant<T>(content);
                }
            }

            return null;
        }

        private void FocusFirstTesterControl()
        {
            MoveFocus(FocusNavigationDirection.First);
        }

        private void MoveFocus(FocusNavigationDirection direction)
        {
            if (testerWindow == null)
            {
                return;
            }

            var focused = Keyboard.FocusedElement as UIElement;
            if (focused == null)
            {
                focused = testerWindow;
            }

            focused.MoveFocus(new TraversalRequest(direction));
        }

        private static FrameworkElement CreateSidebarIcon()
        {
            var iconPath = new System.Windows.Shapes.Path
            {
                Data = LoadSidebarIconGeometry(),
                Fill = Brushes.White
            };

            var themeForegroundBinding = new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.FindAncestor,
                    typeof(Control),
                    1),
                FallbackValue = Brushes.White
            };

            iconPath.SetBinding(System.Windows.Shapes.Shape.FillProperty, themeForegroundBinding);

            var canvas = new Canvas
            {
                Width = 511.983,
                Height = 511.983
            };
            canvas.Children.Add(iconPath);

            return new Viewbox
            {
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                Child = canvas
            };
        }

        private static Geometry LoadSidebarIconGeometry()
        {
            const string resourceName = "ControllerSessionManager.Icons.gamepad-tester.svg";

            try
            {
                using (var stream = typeof(TesterIntegration).Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException("Embedded sidebar icon was not found.");
                    }

                    var document = System.Xml.Linq.XDocument.Load(stream);
                    var geometry = new GeometryGroup { FillRule = FillRule.Nonzero };
                    foreach (var pathElement in document.Descendants().Where(element => element.Name.LocalName == "path"))
                    {
                        var data = pathElement.Attribute("d");
                        if (data != null && !string.IsNullOrWhiteSpace(data.Value))
                        {
                            geometry.Children.Add(Geometry.Parse(data.Value));
                        }
                    }

                    if (geometry.Children.Count == 0)
                    {
                        throw new InvalidOperationException("Embedded sidebar icon has no path geometry.");
                    }

                    geometry.Freeze();
                    return geometry;
                }
            }
            catch (Exception)
            {
                var fallback = Geometry.Parse("M17.32 5H6.68A4 4 0 0 0 2.702 8.59C2.604 9.416 2 14.456 2 16A3 3 0 0 0 5 19C6 19 6.5 18.5 7 18L8.414 16.586A2 2 0 0 1 9.828 16H14.172A2 2 0 0 1 15.586 16.586L17 18C17.5 18.5 18 19 19 19A3 3 0 0 0 22 16C22 14.455 21.396 9.416 21.298 8.591A4 4 0 0 0 17.32 5Z").Clone();
                fallback.Transform = new ScaleTransform(21.332625, 21.332625);
                fallback.Freeze();
                return fallback;
            }
        }

        private void UpdateTesterCaptureExitChord()
        {
            if (!testerCaptureLeftShoulderHeld || !testerCaptureRightShoulderHeld)
            {
                if (testerCaptureExitTimer != null)
                {
                    testerCaptureExitTimer.Stop();
                }

                return;
            }

            if (testerCaptureExitTimer == null)
            {
                testerCaptureExitTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                testerCaptureExitTimer.Tick += OnTesterCaptureExitTimerTick;
            }

            testerCaptureExitTimer.Stop();
            testerCaptureExitTimer.Start();
        }

        private void OnTesterCaptureExitTimerTick(object sender, EventArgs args)
        {
            testerCaptureExitTimer.Stop();
            if (!testerCaptureLeftShoulderHeld || !testerCaptureRightShoulderHeld || testerWindowViewModel == null)
            {
                return;
            }

            if (testerWindowViewModel.IsButtonCaptureRunning && testerWindowViewModel.StartButtonCaptureCommand.CanExecute(null))
            {
                testerWindowViewModel.StartButtonCaptureCommand.Execute(null);
            }
            else if (testerWindowViewModel.IsStickCaptureRunning && testerWindowViewModel.StartStickCaptureCommand.CanExecute(null))
            {
                testerWindowViewModel.StartStickCaptureCommand.Execute(null);
            }
            else if (testerWindowViewModel.IsLatencyTestRunning && testerWindowViewModel.StartLatencyTestCommand.CanExecute(null))
            {
                testerWindowViewModel.StartLatencyTestCommand.Execute(null);
            }

            ResetTesterCaptureExitChord();
            FocusFirstTesterControl();
        }

        private void ResetTesterCaptureExitChord()
        {
            testerCaptureLeftShoulderHeld = false;
            testerCaptureRightShoulderHeld = false;
            if (testerCaptureExitTimer != null)
            {
                testerCaptureExitTimer.Stop();
            }
        }

        private void OnTesterWindowClosing(object sender, CancelEventArgs args)
        {
            if (ShouldBlockFullscreenClose(testerWindowViewModel))
            {
                args.Cancel = true;
            }
        }

        private static bool ShouldBlockFullscreenClose(GamepadTesterViewModel viewModel)
        {
            return viewModel != null && viewModel.IsFullscreenSimplifiedMode && !viewModel.CanNavigateBack;
        }

        public string Loc(string key)
        {
            if (loc != null)
            {
                return loc(key);
            }

            return key;
        }

        public GamepadTesterView CreateEmbeddedView(out GamepadTesterViewModel viewModel)
        {
            return CreateTesterView(out viewModel, true);
        }

        public bool TryStandardRumble(ushort vendorId, ushort productId)
        {
            if (settings != null && !settings.EnableRumbleTests)
            {
                return false;
            }

            var provider = new HostedGamepadInputProvider(logger);
            try
            {
                GamepadControllerInfo match = null;
                for (var attempt = 0; attempt < 25; attempt++)
                {
                    provider.ReadState();
                    match = FindHostController(provider.GetControllers(), vendorId, productId);
                    if (match != null)
                    {
                        break;
                    }

                    Thread.Sleep(40);
                }

                if (match == null)
                {
                    return false;
                }

                provider.SelectController(match.InstanceId);
                Thread.Sleep(40);
                return provider.TryRumble(42000, 52000, 350);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Standard rumble through TesterHost failed.");
                return false;
            }
            finally
            {
                provider.Dispose();
            }
        }

        private static GamepadControllerInfo FindHostController(
            IReadOnlyList<GamepadControllerInfo> controllers, ushort vendorId, ushort productId)
        {
            if (controllers == null || controllers.Count == 0)
            {
                return null;
            }

            if (vendorId != 0 || productId != 0)
            {
                foreach (var controller in controllers)
                {
                    if (controller.VendorId == vendorId && controller.ProductId == productId)
                    {
                        return controller;
                    }
                }
            }

            return controllers.Count == 1 ? controllers[0] : null;
        }

        private GamepadTesterView CreateTesterView(out GamepadTesterViewModel viewModel, bool showOptionsTab = true)
        {
            var pollingService = new GamepadPollingService(new HostedGamepadInputProvider(logger));
            viewModel = new GamepadTesterViewModel(pollingService, settings, Loc, OpenGuidedTestWindow);
            viewModel.IsOptionsTabVisible = showOptionsTab;
            var view = new GamepadTesterView
            {
                DataContext = viewModel,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            viewModel.Start();
            return view;
        }

        private void OpenGuidedTestWindow(GamepadTesterViewModel viewModel)
        {
            try
            {
                if (viewModel == null || !viewModel.State.IsConnected)
                {
                    return;
                }

                viewModel.StartGuidedTestCommand.Execute(null);
                var view = new GuidedTestView
                {
                    DataContext = viewModel
                };

                var fullscreenFriendly = ShouldUseFullscreenFriendlyWindow();
                var window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = !fullscreenFriendly,
                    ShowCloseButton = true
                });

                window.Title = Loc("LOCCSM_Tester_GuidedTest");
                window.Content = view;
                window.Owner = playniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ApplyTesterWindowSize(window, fullscreenFriendly, 1080, 820, 960, 720);
                window.PreviewKeyDown += CloseWindowOnEscape;
                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester guided test.");
                playniteApi.Dialogs.ShowErrorMessage(exception.Message, Loc("LOCCSM_Tester_GuidedTest"));
            }
        }

        private void DisposeSidebarView()
        {
            if (sidebarViewModel == null)
            {
                return;
            }

            sidebarViewModel.Dispose();
            sidebarViewModel = null;
        }

        private bool ShouldUseFullscreenFriendlyWindow()
        {
            return settings.UseFullscreenFriendlyWindow &&
                playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
        }

        private static void ApplyTesterWindowSize(Window window, bool fullscreenFriendly, double width, double height, double minWidth, double minHeight)
        {
            window.MinWidth = minWidth;
            window.MinHeight = minHeight;

            if (fullscreenFriendly)
            {
                window.WindowState = WindowState.Maximized;
                return;
            }

            window.Width = width;
            window.Height = height;
        }

        private static void CloseWindowOnEscape(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key != Key.Escape)
            {
                return;
            }

            var window = sender as Window;
            if (window != null)
            {
                var content = window.Content as FrameworkElement;
                var viewModel = content == null ? null : content.DataContext as GamepadTesterViewModel;
                if (ShouldBlockFullscreenClose(viewModel))
                {
                    eventArgs.Handled = true;
                    return;
                }

                window.Close();
                eventArgs.Handled = true;
            }
        }
    }

    public class GamepadTesterThemeIntegration
    {
        private readonly GamepadTesterSettings settings;

        public ICommand OpenTesterCommand { get; private set; }
        public ICommand OpenButtonTestCommand { get; private set; }
        public ICommand OpenSticksCommand { get; private set; }
        public ICommand OpenRumbleCommand { get; private set; }
        public ICommand OpenLatencyCommand { get; private set; }
        public ICommand RefreshThemeBlocksCommand { get; private set; }

        public string ContractVersion
        {
            get { return GamepadTesterThemeContract.Version; }
        }

        public string SupportedBlocks
        {
            get { return string.Join(", ", GamepadTesterThemeContract.BlockNames); }
        }

        public bool ShowTopPanelItem
        {
            get { return settings.ShowTopPanelItem; }
        }

        public bool UseFullscreenFriendlyWindow
        {
            get { return settings.UseFullscreenFriendlyWindow; }
        }

        public GamepadTesterThemeIntegration(
            GamepadTesterSettings settings,
            ICommand openTesterCommand,
            ICommand openButtonTestCommand,
            ICommand openSticksCommand,
            ICommand openRumbleCommand,
            ICommand openLatencyCommand)
        {
            this.settings = settings;
            OpenTesterCommand = openTesterCommand;
            OpenButtonTestCommand = openButtonTestCommand;
            OpenSticksCommand = openSticksCommand;
            OpenRumbleCommand = openRumbleCommand;
            OpenLatencyCommand = openLatencyCommand;
            RefreshThemeBlocksCommand = new global::ControllerSessionManager.Tester.Commands.RelayCommand(
                () => GamepadTesterThemeHost.RefreshOpenWindows());
        }
    }
}
