using ControllerSessionManager.Tester.Converters;
using ControllerSessionManager.Tester.Services;
using ControllerSessionManager.Tester.ViewModels;
using Playnite.SDK.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public abstract class GamepadTesterThemeControlBase : PluginUserControl
    {
        public const string ControlBackgroundBrushKey = "GamepadTesterControlBackgroundBrush";
        public const string ButtonBackgroundBrushKey = "GamepadTesterButtonBackgroundBrush";
        public const string ControlBorderBrushKey = "GamepadTesterControlBorderBrush";
        public const string StickGuideBrushKey = "GamepadTesterStickGuideBrush";
        public const string TextBrushKey = "GamepadTesterTextBrush";

        public static readonly DependencyProperty IsInputCaptureActiveProperty =
            DependencyProperty.Register(
                "IsInputCaptureActive",
                typeof(bool),
                typeof(GamepadTesterThemeControlBase),
                new PropertyMetadata(false, OnInputCaptureActiveChanged));

        public static readonly DependencyProperty IsControllerConnectedProperty =
            DependencyProperty.Register(
                "IsControllerConnected",
                typeof(bool),
                typeof(GamepadTesterThemeControlBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ActiveTestKindProperty =
            DependencyProperty.Register(
                "ActiveTestKind",
                typeof(string),
                typeof(GamepadTesterThemeControlBase),
                new PropertyMetadata("None"));

        public static readonly DependencyProperty CanNavigateBackProperty =
            DependencyProperty.Register(
                "CanNavigateBack",
                typeof(bool),
                typeof(GamepadTesterThemeControlBase),
                new PropertyMetadata(true));

        private readonly GamepadTesterThemeRuntimeHandle runtimeHandle;

        protected GamepadTesterThemeControlBase(GamepadTesterSettings settings, Func<string, string> localizer)
        {
            EnsureThemeBrushFallbacks();
            runtimeHandle = GamepadTesterThemeRuntime.Acquire(settings, localizer);
            DataContext = runtimeHandle.ViewModel;
            SetBinding(IsInputCaptureActiveProperty, Bind("IsFullscreenInputCaptureActive"));
            SetBinding(IsControllerConnectedProperty, Bind("HasController"));
            SetBinding(ActiveTestKindProperty, Bind("ActiveTestKind"));
            SetBinding(CanNavigateBackProperty, Bind("CanNavigateBack"));
            Unloaded += OnUnloaded;
        }

        public bool IsInputCaptureActive
        {
            get { return (bool)GetValue(IsInputCaptureActiveProperty); }
        }

        public bool IsControllerConnected
        {
            get { return (bool)GetValue(IsControllerConnectedProperty); }
        }

        public string ActiveTestKind
        {
            get { return (string)GetValue(ActiveTestKindProperty); }
        }

        public bool CanNavigateBack
        {
            get { return (bool)GetValue(CanNavigateBackProperty); }
        }

        private static void OnInputCaptureActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var control = dependencyObject as GamepadTesterThemeControlBase;
            if (control != null)
            {
                GamepadTesterThemeHost.UpdateCaptureGuardState(control);
            }
        }

        public string ThemeContractVersion
        {
            get { return GamepadTesterThemeContract.Version; }
        }

        protected GamepadTesterViewModel ViewModel
        {
            get { return runtimeHandle.ViewModel; }
        }

        protected string L(string key, string fallback)
        {
            return runtimeHandle.Localize(key, fallback);
        }

        protected static Binding Bind(string path)
        {
            return new Binding(path);
        }

        protected static Binding Bind(string path, IValueConverter converter)
        {
            return new Binding(path) { Converter = converter };
        }

        protected static TextBlock Text(string text, double size, FontWeight weight)
        {
            var block = new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = weight,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetThemeResource(block, TextBlock.ForegroundProperty, TextBrushKey);
            return block;
        }

        protected static Border Panel()
        {
            var panel = new Border
            {
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1)
            };
            SetThemeResource(panel, Border.BackgroundProperty, ControlBackgroundBrushKey);
            SetThemeResource(panel, Border.BorderBrushProperty, ControlBorderBrushKey);
            return panel;
        }

        protected static void SetThemeResource(FrameworkElement element, DependencyProperty property, string key)
        {
            if (element != null)
            {
                element.SetResourceReference(property, key);
            }
        }

        protected static IValueConverter BoolToVisibility()
        {
            return new BoolToVisibilityConverter();
        }

        protected static IValueConverter InverseBoolToVisibility()
        {
            return new InverseBoolToVisibilityConverter();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            runtimeHandle.Dispose();
        }

        private static void EnsureThemeBrushFallbacks()
        {
            EnsureThemeBrushFallback(
                ControlBackgroundBrushKey,
                "ControlBackgroundBrush",
                new SolidColorBrush(Color.FromRgb(20, 24, 31)));
            EnsureThemeBrushFallback(
                ButtonBackgroundBrushKey,
                "ButtonBackgroundBrush",
                new SolidColorBrush(Color.FromRgb(31, 36, 47)));
            EnsureThemeBrushFallback(
                ControlBorderBrushKey,
                "ControlBorderBrush",
                new SolidColorBrush(Color.FromRgb(63, 72, 88)));
            EnsureThemeBrushFallback(
                StickGuideBrushKey,
                "ControlBorderBrush",
                new SolidColorBrush(Color.FromRgb(63, 72, 88)));
            EnsureThemeBrushFallback(TextBrushKey, "TextBrush", Brushes.White);
        }

        private static void EnsureThemeBrushFallback(string key, string fallbackKey, Brush hardFallback)
        {
            var application = Application.Current;
            if (application == null || application.TryFindResource(key) is Brush)
            {
                return;
            }

            var fallback = application.TryFindResource(fallbackKey) as Brush ?? hardFallback;
            application.Resources[key] = fallback;
        }
    }

    internal sealed class GamepadTesterThemeRuntimeHandle : IDisposable
    {
        private readonly Action release;
        private bool disposed;

        public GamepadTesterThemeRuntimeHandle(GamepadTesterViewModel viewModel, Func<string, string> localizer, Action release)
        {
            ViewModel = viewModel;
            Localizer = localizer;
            this.release = release;
        }

        public GamepadTesterViewModel ViewModel { get; private set; }
        public Func<string, string> Localizer { get; private set; }

        public string Localize(string key, string fallback)
        {
            if (Localizer == null)
            {
                return fallback;
            }

            var value = Localizer(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            release();
        }
    }

    internal static class GamepadTesterThemeRuntime
    {
        private static readonly object sync = new object();
        private static GamepadTesterViewModel viewModel;
        private static Func<string, string> localizer;
        private static Func<IGamepadInputProvider> inputProviderFactory = () => new HostedGamepadInputProvider();
        private static int referenceCount;

        internal static void SetInputProviderFactoryForTests(Func<IGamepadInputProvider> factory)
        {
            lock (sync)
            {
                if (viewModel != null)
                {
                    throw new InvalidOperationException("The theme runtime is already active.");
                }

                inputProviderFactory = factory ?? (() => new HostedGamepadInputProvider());
            }
        }

        public static GamepadTesterThemeRuntimeHandle Acquire(GamepadTesterSettings settings, Func<string, string> localize)
        {
            lock (sync)
            {
                if (viewModel == null)
                {
                    localizer = localize;
                    viewModel = new GamepadTesterViewModel(
                        new GamepadPollingService(inputProviderFactory()),
                        settings,
                        localize);
                    viewModel.IsFullscreenSimplifiedMode = true;
                    viewModel.IsInputLogEnabled = false;
                    viewModel.Start();
                }

                referenceCount++;
                return new GamepadTesterThemeRuntimeHandle(viewModel, localizer, Release);
            }
        }

        private static void Release()
        {
            lock (sync)
            {
                if (referenceCount > 0)
                {
                    referenceCount--;
                }

                if (referenceCount != 0 || viewModel == null)
                {
                    return;
                }

                viewModel.Dispose();
                viewModel = null;
                localizer = null;
            }
        }
    }

    internal sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
