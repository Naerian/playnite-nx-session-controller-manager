using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterStickCheckControl : GamepadTesterThemeControlBase
    {
        private readonly Button actionButton;

        public GamepadTesterStickCheckControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new StackPanel();

            var headerRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition());
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var header = Text(L("LOCCSM_Tester_Sticks", "Sticks"), 14, FontWeights.SemiBold);
            headerRow.Children.Add(header);
            root.Children.Add(headerRow);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var left = CreateStickBlock(
                L("LOCCSM_Tester_LeftStick", "Left stick"),
                new SolidColorBrush(Color.FromRgb(76, 201, 240)),
                "LeftStickVector",
                "LeftStickDriftStatus",
                "LeftStickCurrentMagnitudeLabel",
                "LeftStickDiagnosticsDotX",
                "LeftStickDiagnosticsDotY",
                "LeftStickCircularCoverageGeometry",
                "LeftStickPathGeometry",
                "LeftStickCircularCoverageLabel",
                "LeftStickMaxReachLabel",
                "LeftStickPathSampleLabel");
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var right = (FrameworkElement)CreateStickBlock(
                L("LOCCSM_Tester_RightStick", "Right stick"),
                new SolidColorBrush(Color.FromRgb(247, 185, 85)),
                "RightStickVector",
                "RightStickDriftStatus",
                "RightStickCurrentMagnitudeLabel",
                "RightStickDiagnosticsDotX",
                "RightStickDiagnosticsDotY",
                "RightStickCircularCoverageGeometry",
                "RightStickPathGeometry",
                "RightStickCircularCoverageLabel",
                "RightStickMaxReachLabel",
                "RightStickPathSampleLabel");
            right.Margin = new Thickness(18, 0, 0, 0);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            root.Children.Add(grid);

            var actionArea = new Grid
            {
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            actionButton = new Button
            {
                Name = "GamepadTester_StickTestAction",
                MinWidth = 160,
                MinHeight = 40,
                Padding = new Thickness(14, 6, 14, 6),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            SetThemeResource(actionButton, Control.ForegroundProperty, TextBrushKey);
            SetThemeResource(actionButton, Control.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(actionButton, Control.BorderBrushProperty, ControlBorderBrushKey);
            actionButton.SetBinding(Button.CommandProperty, Bind("StartStickCaptureCommand"));
            actionButton.SetBinding(ContentControl.ContentProperty, Bind("StickCaptureButtonLabel"));
            actionButton.SetBinding(VisibilityProperty, Bind("IsStickCaptureRunning", InverseBoolToVisibility()));
            actionArea.Children.Add(actionButton);

            var exitHint = new Border
            {
                MinWidth = 270,
                MinHeight = 40,
                Padding = new Thickness(14, 6, 14, 6),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            SetThemeResource(exitHint, Border.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(exitHint, Border.BorderBrushProperty, ControlBorderBrushKey);
            exitHint.SetBinding(VisibilityProperty, Bind("IsStickCaptureRunning", BoolToVisibility()));
            var exitHintText = Text(string.Empty, 13, FontWeights.SemiBold);
            exitHintText.HorizontalAlignment = HorizontalAlignment.Center;
            exitHintText.TextAlignment = TextAlignment.Center;
            exitHintText.SetBinding(TextBlock.TextProperty, Bind("CaptureExitHintLabel"));
            exitHint.Child = exitHintText;
            actionArea.Children.Add(exitHint);
            Grid.SetColumn(actionArea, 1);
            headerRow.Children.Add(actionArea);

            panel.Child = root;
            Content = panel;

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Unloaded += OnControlUnloaded;
        }

        public bool IsTestRunning
        {
            get { return ViewModel.IsStickCaptureRunning; }
        }

        public void StopStickCapture()
        {
            if (IsTestRunning && ViewModel.StartStickCaptureCommand.CanExecute(null))
            {
                ViewModel.StartStickCaptureCommand.Execute(null);
            }
        }

        public void FocusStartButton()
        {
            FocusActionButton();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "IsStickCaptureRunning" && !IsTestRunning)
            {
                FocusActionButton();
            }
        }

        private void FocusActionButton()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsVisible && actionButton.IsEnabled)
                {
                    actionButton.Focus();
                    Keyboard.Focus(actionButton);
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Unloaded -= OnControlUnloaded;
        }

        private UIElement CreateStickBlock(
            string title,
            Brush dotBrush,
            string vectorPath,
            string driftPath,
            string magnitudePath,
            string diagnosticsXPath,
            string diagnosticsYPath,
            string coverageGeometryPath,
            string pathGeometryPath,
            string coverageLabelPath,
            string maxReachPath,
            string sampleLabelPath)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            var diagnostics = CreateDiagnosticsPlot(
                dotBrush,
                diagnosticsXPath,
                diagnosticsYPath,
                coverageGeometryPath,
                pathGeometryPath);
            Grid.SetColumn(diagnostics, 0);
            row.Children.Add(diagnostics);

            var info = new StackPanel
            {
                Margin = new Thickness(14, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            info.Children.Add(Text(title, 13, FontWeights.SemiBold));
            info.Children.Add(BoundText(vectorPath, 12, 0.72));
            info.Children.Add(BoundText(driftPath, 12, 0.72));
            info.Children.Add(BoundText(magnitudePath, 12, 0.72));
            info.Children.Add(BoundText(coverageLabelPath, 12, 0.72));
            info.Children.Add(BoundText(maxReachPath, 12, 0.72));
            info.Children.Add(BoundText(sampleLabelPath, 12, 0.72));
            Grid.SetColumn(info, 1);
            row.Children.Add(info);

            return row;
        }

        private FrameworkElement CreateDiagnosticsPlot(
            Brush accent,
            string dotXPath,
            string dotYPath,
            string coverageGeometryPath,
            string pathGeometryPath)
        {
            var plot = new Grid { Width = 280, Height = 280 };

            var surface = new Ellipse { StrokeThickness = 2 };
            SetThemeResource(surface, Shape.FillProperty, ControlBackgroundBrushKey);
            SetThemeResource(surface, Shape.StrokeProperty, StickGuideBrushKey);
            plot.Children.Add(surface);

            var range = new Ellipse
            {
                Width = 232,
                Height = 232,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 4 },
                Opacity = 0.72
            };
            SetThemeResource(range, Shape.StrokeProperty, StickGuideBrushKey);
            plot.Children.Add(range);

            var verticalAxis = new Line { X1 = 140, X2 = 140, Y1 = 18, Y2 = 262, Opacity = 0.55 };
            SetThemeResource(verticalAxis, Shape.StrokeProperty, StickGuideBrushKey);
            plot.Children.Add(verticalAxis);

            var horizontalAxis = new Line { X1 = 18, X2 = 262, Y1 = 140, Y2 = 140, Opacity = 0.55 };
            SetThemeResource(horizontalAxis, Shape.StrokeProperty, StickGuideBrushKey);
            plot.Children.Add(horizontalAxis);

            var coverage = new Path { Fill = accent, Opacity = 0.24 };
            coverage.SetBinding(Path.DataProperty, Bind(coverageGeometryPath));
            plot.Children.Add(coverage);

            var path = new Path
            {
                Stroke = accent,
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = 0.95
            };
            path.SetBinding(Path.DataProperty, Bind(pathGeometryPath));
            plot.Children.Add(path);

            var dot = new Ellipse
            {
                Width = 22,
                Height = 22,
                Fill = accent,
                StrokeThickness = 1.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetThemeResource(dot, Shape.StrokeProperty, TextBrushKey);
            var transform = new TranslateTransform();
            BindingOperations.SetBinding(transform, TranslateTransform.XProperty, Bind(dotXPath));
            BindingOperations.SetBinding(transform, TranslateTransform.YProperty, Bind(dotYPath));
            dot.RenderTransform = transform;
            plot.Children.Add(dot);

            return new Viewbox
            {
                Width = 120,
                Height = 120,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Child = plot
            };
        }

        private static TextBlock BoundText(string path, double size, double opacity)
        {
            var text = Text(string.Empty, size, FontWeights.Normal);
            text.Opacity = opacity;
            text.SetBinding(TextBlock.TextProperty, Bind(path));
            return text;
        }
    }
}
