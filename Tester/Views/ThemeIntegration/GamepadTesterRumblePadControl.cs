using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterRumblePadControl : GamepadTesterThemeControlBase
    {
        private Button firstButton;
        private Button captureButton;

        public GamepadTesterRumblePadControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new StackPanel();

            var header = Text(L("LOCCSM_Tester_RumbleTests", "Rumble tests"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 12);
            root.Children.Add(header);

            var buttons = new UniformGrid
            {
                Columns = 4
            };
            firstButton = CreateButton(L("LOCCSM_Tester_RumbleLight", "Light"), "LightRumbleCommand");
            buttons.Children.Add(firstButton);
            buttons.Children.Add(CreateButton(L("LOCCSM_Tester_RumbleMedium", "Medium"), "MediumRumbleCommand"));
            buttons.Children.Add(CreateButton(L("LOCCSM_Tester_RumbleHeavy", "Heavy"), "HeavyRumbleCommand"));
            buttons.Children.Add(CreateButton(L("LOCCSM_Tester_RumblePulse", "Pulse"), "PulseRumbleCommand"));
            root.Children.Add(buttons);

            var status = Text(string.Empty, 12, FontWeights.Normal);
            status.Margin = new Thickness(0, 12, 0, 0);
            status.Opacity = 0.72;
            status.SetBinding(TextBlock.TextProperty, Bind("RumbleStatusLabel"));
            root.Children.Add(status);

            panel.Child = root;
            Content = panel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewLostKeyboardFocus += OnPreviewLostKeyboardFocus;
            Loaded += (sender, args) => FocusFirstButton();
            IsVisibleChanged += (sender, args) => FocusFirstButton();
            Unloaded += OnControlUnloaded;
        }

        private Button CreateButton(string text, string commandPath)
        {
            var button = new Button
            {
                Content = text,
                MinHeight = 44,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 8, 12, 8),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1)
            };
            SetThemeResource(button, Control.ForegroundProperty, TextBrushKey);
            SetThemeResource(button, Control.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(button, Control.BorderBrushProperty, ControlBorderBrushKey);
            button.SetBinding(Button.CommandProperty, new Binding(commandPath));
            return button;
        }

        private void FocusFirstButton()
        {
            if (!IsVisible || firstButton == null)
            {
                return;
            }

            var current = Keyboard.FocusedElement as DependencyObject;
            while (current != null)
            {
                if (current is GamepadTesterThemeControlBase)
                {
                    return;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsVisible && firstButton.IsEnabled)
                {
                    firstButton.Focus();
                    Keyboard.Focus(firstButton);
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != "IsRumbleRunning")
            {
                return;
            }

            if (ViewModel.IsRumbleRunning)
            {
                captureButton = Keyboard.FocusedElement as Button ?? firstButton;
                FocusCaptureButton();
            }
            else
            {
                var completedButton = captureButton;
                captureButton = null;
                if (completedButton != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (IsVisible && completedButton.IsEnabled)
                        {
                            completedButton.Focus();
                            Keyboard.Focus(completedButton);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (!ViewModel.IsRumbleRunning || !IsCaptureKey(args.Key))
            {
                return;
            }

            args.Handled = true;
            FocusCaptureButton();
        }

        private void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
        {
            if (!ViewModel.IsRumbleRunning || IsInsideControl(args.NewFocus as DependencyObject))
            {
                return;
            }

            args.Handled = true;
            FocusCaptureButton();
        }

        private void FocusCaptureButton()
        {
            var target = captureButton ?? firstButton;
            if (target == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ViewModel.IsRumbleRunning)
                {
                    target.Focus();
                    Keyboard.Focus(target);
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private bool IsInsideControl(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, this))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static bool IsCaptureKey(Key key)
        {
            return key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right ||
                key == Key.Tab || key == Key.PageUp || key == Key.PageDown ||
                key == Key.Escape || key == Key.BrowserBack || key == Key.Back;
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            PreviewKeyDown -= OnPreviewKeyDown;
            PreviewLostKeyboardFocus -= OnPreviewLostKeyboardFocus;
            Unloaded -= OnControlUnloaded;
        }
    }
}
