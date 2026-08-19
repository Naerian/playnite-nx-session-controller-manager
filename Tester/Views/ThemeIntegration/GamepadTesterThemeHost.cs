using ControllerSessionManager.Tester.Views;
using Playnite.SDK.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterThemeHost : DependencyObject
    {
        public static readonly DependencyProperty BlockProperty =
            DependencyProperty.RegisterAttached(
                "Block",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata(null, OnBlockChanged));

        private static readonly DependencyPropertyKey InitializationStatePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "InitializationState",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata("Unmarked"));

        public static readonly DependencyProperty InitializationStateProperty =
            InitializationStatePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey InitializationMessagePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "InitializationMessage",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty InitializationMessageProperty =
            InitializationMessagePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ResolvedBlockPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ResolvedBlock",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ResolvedBlockProperty =
            ResolvedBlockPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ContractVersionPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ContractVersion",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata(GamepadTesterThemeContract.Version));

        public static readonly DependencyProperty ContractVersionProperty =
            ContractVersionPropertyKey.DependencyProperty;

        private static readonly object sync = new object();
        private static bool classHandlerRegistered;
        private static readonly Dictionary<Window, int> windowScanAttempts = new Dictionary<Window, int>();
        private static readonly HashSet<Window> captureGuardWindows = new HashSet<Window>();
        private static readonly Dictionary<Window, Dictionary<ButtonBase, bool>> guardedBackButtons =
            new Dictionary<Window, Dictionary<ButtonBase, bool>>();
        private static DispatcherTimer windowScanner;
        private static EventInfo anikiButtonDownEvent;
        private static Delegate anikiButtonDownHandler;
        private static bool anikiCaptureInterceptorAttached;
        private static GamepadTesterSettings settings;
        private static Func<string, string> localizer;
        private static Action openTester;
        private static Action<string> log;

        private GamepadTesterThemeHost()
        {
        }

        public static void Configure(
            GamepadTesterSettings pluginSettings,
            Func<string, string> pluginLocalizer,
            Action openTesterAction,
            Action<string> logAction = null)
        {
            settings = pluginSettings;
            localizer = pluginLocalizer;
            openTester = openTesterAction;
            log = logAction;

            EnsureClassHandler();
            EnsureWindowScanner();
            EnsureAnikiCaptureInterceptor();
        }

        public static string GetBlock(DependencyObject element)
        {
            return element == null ? null : (string)element.GetValue(BlockProperty);
        }

        public static void SetBlock(DependencyObject element, string value)
        {
            if (element != null)
            {
                element.SetValue(BlockProperty, value);
            }
        }

        private static void OnBlockChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            var contentControl = element as ContentControl;
            if (contentControl == null)
            {
                return;
            }

            EnsureClassHandler();
            InitializeHost(contentControl);
        }

        private static void EnsureClassHandler()
        {
            lock (sync)
            {
                if (classHandlerRegistered)
                {
                    return;
                }

                EventManager.RegisterClassHandler(
                    typeof(ContentControl),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnContentControlLoaded),
                    true);
                classHandlerRegistered = true;
            }
        }

        private static void OnContentControlLoaded(object sender, RoutedEventArgs args)
        {
            var host = sender as ContentControl;
            var marker = host == null ? null : host.Tag as string;
            if (!string.IsNullOrWhiteSpace(marker) &&
                marker.StartsWith("GamepadTester", StringComparison.OrdinalIgnoreCase))
            {
                Log("Dynamic theme host loaded: " + marker);
            }

            InitializeHost(host);
        }

        private static void EnsureWindowScanner()
        {
            if (windowScanner != null || Application.Current == null)
            {
                return;
            }

            windowScanner = new DispatcherTimer(
                TimeSpan.FromMilliseconds(250),
                DispatcherPriority.Background,
                OnWindowScannerTick,
                Application.Current.Dispatcher);
            Application.Current.Activated += OnApplicationActivated;
            windowScanner.Start();
        }

        private static void OnApplicationActivated(object sender, EventArgs args)
        {
            if (Application.Current == null)
            {
                return;
            }

            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsActive)
                {
                    windowScanAttempts[window] = 0;
                }
            }
        }

        public static string GetInitializationState(DependencyObject element)
        {
            return element == null ? "Unmarked" : (string)element.GetValue(InitializationStateProperty);
        }

        public static string GetInitializationMessage(DependencyObject element)
        {
            return element == null ? string.Empty : (string)element.GetValue(InitializationMessageProperty);
        }

        public static string GetResolvedBlock(DependencyObject element)
        {
            return element == null ? string.Empty : (string)element.GetValue(ResolvedBlockProperty);
        }

        public static string GetContractVersion(DependencyObject element)
        {
            return element == null ? GamepadTesterThemeContract.Version : (string)element.GetValue(ContractVersionProperty);
        }

        private static void OnWindowScannerTick(object sender, EventArgs args)
        {
            if (Application.Current == null)
            {
                return;
            }

            EnsureAnikiCaptureInterceptor();

            var windows = Application.Current.Windows.Cast<Window>().ToArray();
            foreach (var stale in windowScanAttempts.Keys.Where(window => !windows.Contains(window)).ToArray())
            {
                windowScanAttempts.Remove(stale);
            }

            foreach (var window in windows)
            {
                int attempts;
                if (!windowScanAttempts.TryGetValue(window, out attempts))
                {
                    attempts = 0;
                }

                if (attempts >= 8 || !window.IsLoaded)
                {
                    windowScanAttempts[window] = attempts;
                    continue;
                }

                try
                {
                    window.ApplyTemplate();
                    var initialized = Refresh(window);
                    if (initialized > 0)
                    {
                        EnsureCaptureGuard(window);
                    }
                    windowScanAttempts[window] = initialized > 0 ? 8 : attempts + 1;
                }
                catch (Exception ex)
                {
                    windowScanAttempts[window] = attempts + 1;
                    Log("Dynamic theme host scan failed: " + ex.Message);
                }
            }
        }

        private static void EnsureAnikiCaptureInterceptor()
        {
            if (anikiCaptureInterceptorAttached)
            {
                return;
            }

            try
            {
                var inputType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("AnikiHelper.Services.Controller.AnikiControllerInput", false))
                    .FirstOrDefault(type => type != null);
                if (inputType == null)
                {
                    return;
                }

                var buttonDownEvent = inputType.GetEvent("ButtonDown", BindingFlags.Public | BindingFlags.Static);
                var handlerMethod = typeof(GamepadTesterThemeHost).GetMethod(
                    "OnAnikiControllerButtonDown",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (buttonDownEvent == null || buttonDownEvent.EventHandlerType == null || handlerMethod == null)
                {
                    return;
                }

                var handler = Delegate.CreateDelegate(buttonDownEvent.EventHandlerType, handlerMethod);
                buttonDownEvent.AddEventHandler(null, handler);
                anikiButtonDownEvent = buttonDownEvent;
                anikiButtonDownHandler = handler;
                anikiCaptureInterceptorAttached = true;
                Log("Aniki dynamic-window capture compatibility enabled.");
            }
            catch (Exception ex)
            {
                Log("Aniki capture compatibility initialization failed: " + ex.Message);
            }
        }

        private static void OnAnikiControllerButtonDown(object sender, ControllerInput button)
        {
            if (button != ControllerInput.B || Application.Current == null)
            {
                return;
            }

            var captureWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsVisible && HasActiveCapture(window));
            if (captureWindow == null || !captureWindow.IsActive)
            {
                return;
            }

            // Aniki closes its top custom window directly on B before WPF key handling.
            // Briefly activating the owner makes that close path skip this capture window.
            var owner = captureWindow.Owner ?? Application.Current.MainWindow;
            if (owner == null || ReferenceEquals(owner, captureWindow))
            {
                return;
            }

            owner.Activate();
            owner.Focus();
            Log("Aniki B close suppressed during input capture.");

            captureWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (captureWindow.IsVisible && HasActiveCapture(captureWindow))
                {
                    captureWindow.Activate();
                    captureWindow.Focus();
                }
            }), DispatcherPriority.Input);
        }

        public static int Refresh(DependencyObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var initialized = InitializeHost(root as ContentControl) ? 1 : 0;
            int childCount;
            try
            {
                childCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                return initialized;
            }

            for (var index = 0; index < childCount; index++)
            {
                initialized += Refresh(VisualTreeHelper.GetChild(root, index));
            }

            return initialized;
        }

        public static int RefreshOpenWindows()
        {
            if (Application.Current == null)
            {
                return 0;
            }

            var initialized = 0;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsLoaded)
                {
                    initialized += Refresh(window);
                }
            }

            Log("Manual theme host refresh completed: " + initialized + " block(s) ready.");
            return initialized;
        }

        private static void EnsureCaptureGuard(Window window)
        {
            if (window == null || ReferenceEquals(window, Application.Current.MainWindow) || captureGuardWindows.Contains(window))
            {
                return;
            }

            captureGuardWindows.Add(window);
            window.Closing += OnThemeWindowClosing;
            window.PreviewKeyDown += OnThemeWindowPreviewKeyDown;
            window.Closed += OnThemeWindowClosed;
            UpdateBackNavigationState(window);
        }

        internal static void UpdateCaptureGuardState(FrameworkElement source)
        {
            var window = source == null ? null : Window.GetWindow(source);
            if (window == null || ReferenceEquals(window, Application.Current == null ? null : Application.Current.MainWindow))
            {
                return;
            }

            EnsureCaptureGuard(window);
            UpdateBackNavigationState(window);
        }

        private static void UpdateBackNavigationState(Window window)
        {
            if (window == null)
            {
                return;
            }

            Dictionary<ButtonBase, bool> originalStates;
            if (!guardedBackButtons.TryGetValue(window, out originalStates))
            {
                originalStates = new Dictionary<ButtonBase, bool>();
                guardedBackButtons[window] = originalStates;
            }

            var captureActive = HasActiveCapture(window);
            var backButtons = FindNamedButtons(window, "GamepadTester_BackButton").ToArray();
            if (captureActive)
            {
                foreach (var button in backButtons)
                {
                    if (!originalStates.ContainsKey(button))
                    {
                        originalStates[button] = button.IsEnabled;
                    }

                    button.SetCurrentValue(UIElement.IsEnabledProperty, false);
                }

                return;
            }

            foreach (var item in originalStates.ToArray())
            {
                item.Key.SetCurrentValue(UIElement.IsEnabledProperty, item.Value);
                originalStates.Remove(item.Key);
            }
        }

        private static IEnumerable<ButtonBase> FindNamedButtons(DependencyObject root, string name)
        {
            if (root == null)
            {
                yield break;
            }

            var element = root as FrameworkElement;
            var button = root as ButtonBase;
            if (button != null && element != null && string.Equals(element.Name, name, StringComparison.Ordinal))
            {
                yield return button;
            }

            int childCount;
            try
            {
                childCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                childCount = 0;
            }

            for (var index = 0; index < childCount; index++)
            {
                foreach (var match in FindNamedButtons(VisualTreeHelper.GetChild(root, index), name))
                {
                    yield return match;
                }
            }

            var contentControl = root as ContentControl;
            var content = contentControl == null ? null : contentControl.Content as DependencyObject;
            if (content != null && childCount == 0)
            {
                foreach (var match in FindNamedButtons(content, name))
                {
                    yield return match;
                }
            }
        }

        private static void OnThemeWindowPreviewKeyDown(object sender, KeyEventArgs args)
        {
            var window = sender as Window;
            if (window != null && HasActiveCapture(window))
            {
                args.Handled = true;
            }
        }

        private static void OnThemeWindowClosing(object sender, CancelEventArgs args)
        {
            var window = sender as Window;
            if (window == null || window.Dispatcher.HasShutdownStarted)
            {
                return;
            }

            if (HasActiveCapture(window))
            {
                args.Cancel = true;
                Log("Dynamic theme window close blocked during input capture.");
            }
        }

        private static void OnThemeWindowClosed(object sender, EventArgs args)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }

            window.Closing -= OnThemeWindowClosing;
            window.PreviewKeyDown -= OnThemeWindowPreviewKeyDown;
            window.Closed -= OnThemeWindowClosed;
            captureGuardWindows.Remove(window);
            guardedBackButtons.Remove(window);
            windowScanAttempts.Remove(window);
        }

        private static bool HasActiveCapture(DependencyObject root)
        {
            if (root == null)
            {
                return false;
            }

            var themeControl = root as GamepadTesterThemeControlBase;
            if (themeControl != null && themeControl.IsInputCaptureActive)
            {
                return true;
            }

            int childCount;
            try
            {
                childCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                childCount = 0;
            }

            for (var index = 0; index < childCount; index++)
            {
                if (HasActiveCapture(VisualTreeHelper.GetChild(root, index)))
                {
                    return true;
                }
            }

            var contentControl = root as ContentControl;
            var content = contentControl == null ? null : contentControl.Content as DependencyObject;
            return content != null && HasActiveCapture(content);
        }

        public static GamepadTesterThemeControlBase FindActiveCaptureControl()
        {
            if (Application.Current == null)
            {
                return null;
            }

            foreach (var window in Application.Current.Windows.OfType<Window>().Where(item => item.IsVisible))
            {
                var active = FindActiveCaptureControl(window);
                if (active != null)
                {
                    return active;
                }
            }

            return null;
        }

        private static GamepadTesterThemeControlBase FindActiveCaptureControl(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            var themeControl = root as GamepadTesterThemeControlBase;
            var buttonMap = themeControl as GamepadTesterButtonMapControl;
            if (buttonMap != null && buttonMap.IsTestRunning)
            {
                return buttonMap;
            }

            var latency = themeControl as GamepadTesterLatencyMiniControl;
            if (latency != null && latency.IsTestRunning)
            {
                return latency;
            }

            var sticks = themeControl as GamepadTesterStickCheckControl;
            if (sticks != null && sticks.IsTestRunning)
            {
                return sticks;
            }

            int childCount;
            try
            {
                childCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                childCount = 0;
            }

            for (var index = 0; index < childCount; index++)
            {
                var active = FindActiveCaptureControl(VisualTreeHelper.GetChild(root, index));
                if (active != null)
                {
                    return active;
                }
            }

            var contentControl = root as ContentControl;
            var content = contentControl == null ? null : contentControl.Content as DependencyObject;
            return content == null ? null : FindActiveCaptureControl(content);
        }

        private static bool InitializeHost(ContentControl host)
        {
            if (host == null)
            {
                return false;
            }

            var block = ResolveBlock(host);
            if (string.IsNullOrWhiteSpace(block))
            {
                return false;
            }

            SetHostStatus(host, "Pending", block, "Waiting for Gamepad Tester to initialize this block.");
            if (settings == null)
            {
                SetHostStatus(host, "WaitingForPlugin", block, "Gamepad Tester has not configured its theme runtime yet.");
                return false;
            }

            if (!GamepadTesterThemeContract.SupportsBlock(block))
            {
                SetHostStatus(host, "UnknownBlock", block, "Unknown Gamepad Tester block: " + block);
                Log("Unknown dynamic theme block: " + block);
                return false;
            }

            if (host.Content is GamepadTesterThemeControlBase || host.Content is GamepadTesterThemeLauncherControl)
            {
                SetHostStatus(host, "Ready", block, "Gamepad Tester block initialized.");
                return true;
            }

            if (host.Content != null)
            {
                SetHostStatus(host, "Occupied", block, "The host already has content, so Gamepad Tester did not replace it.");
                return false;
            }

            Control content;
            try
            {
                content = CreateBlock(block);
            }
            catch (Exception ex)
            {
                SetHostStatus(host, "Error", block, ex.Message);
                Log("Dynamic theme host initialization failed for " + block + ": " + ex.Message);
                return false;
            }

            if (content != null)
            {
                host.Content = content;
                SetHostStatus(host, "Ready", block, "Gamepad Tester block initialized.");
                Log("Dynamic theme host initialized: " + block);
                return true;
            }

            SetHostStatus(host, "Error", block, "Gamepad Tester could not create the requested block.");
            return false;
        }

        private static void SetHostStatus(ContentControl host, string state, string block, string message)
        {
            host.SetValue(InitializationStatePropertyKey, state ?? string.Empty);
            host.SetValue(InitializationMessagePropertyKey, message ?? string.Empty);
            host.SetValue(ResolvedBlockPropertyKey, block ?? string.Empty);
            host.SetValue(ContractVersionPropertyKey, GamepadTesterThemeContract.Version);
        }

        private static string ResolveBlock(ContentControl host)
        {
            var block = NormalizeBlock(GetBlock(host));
            if (string.IsNullOrWhiteSpace(block))
            {
                block = NormalizeBlock(host.Tag as string);
            }

            if (string.IsNullOrWhiteSpace(block))
            {
                block = NormalizeHostName(host.Name);
            }

            return block;
        }

        private static void Log(string message)
        {
            if (log != null)
            {
                log(message);
            }
        }

        private static Control CreateBlock(string block)
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                return null;
            }

            if (IsBlock(block, "Launcher") || IsBlock(block, "GamepadTesterLauncher"))
            {
                return new GamepadTesterThemeLauncherControl(openTester, localizer);
            }

            if (IsBlock(block, "StatusBadge"))
            {
                return new GamepadTesterStatusBadgeControl(settings, localizer);
            }

            if (IsBlock(block, "ButtonMap"))
            {
                return new GamepadTesterButtonMapControl(settings, localizer);
            }

            if (IsBlock(block, "StickCheck"))
            {
                return new GamepadTesterStickCheckControl(settings, localizer);
            }

            if (IsBlock(block, "TriggerCheck"))
            {
                return new GamepadTesterTriggerCheckControl(settings, localizer);
            }

            if (IsBlock(block, "RumblePad"))
            {
                return new GamepadTesterRumblePadControl(settings, localizer);
            }

            if (IsBlock(block, "LatencyMini"))
            {
                return new GamepadTesterLatencyMiniControl(settings, localizer);
            }

            return null;
        }

        private static string NormalizeBlock(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith("GamepadTester_", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("GamepadTester_".Length);
            }
            else if (normalized.StartsWith("GamepadTester", StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > "GamepadTester".Length)
            {
                normalized = normalized.Substring("GamepadTester".Length);
            }

            return normalized;
        }

        private static string NormalizeHostName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith("GamepadTester_", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring("GamepadTester_".Length);
            }

            if (normalized.StartsWith("GamepadTester", StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > "GamepadTester".Length)
            {
                return normalized.Substring("GamepadTester".Length);
            }

            return null;
        }

        private static bool IsBlock(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
