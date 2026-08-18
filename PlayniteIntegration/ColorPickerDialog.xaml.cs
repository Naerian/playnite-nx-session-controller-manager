using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ColorPickerDialog : Window
    {
        private readonly Func<string, string> loc;
        private double hue;
        private double saturation = 1;
        private double value = 1;
        private byte alpha = 255;
        private bool updatingUi;
        private bool draggingSv;
        private bool draggingHue;

        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color initial, Func<string, string> locLookup)
        {
            loc = locLookup ?? new Func<string, string>(delegate(string key) { return key; });
            InitializeComponent();
            TitleText.Text = Loc("LOCCSM_SelectColor");
            PreviewLabel.Text = Loc("LOCCSM_ColorPreview");
            OpacityLabel.Text = Loc("LOCCSM_ColorOpacity");
            HexLabel.Text = Loc("LOCCSM_ColorHex");
            ApplyButton.Content = Loc("LOCCSM_ColorPickerApply");
            CancelButton.Content = Loc("LOCCSM_ColorPickerCancel");
            Title = TitleText.Text;
            SelectedColor = initial;
            ColorPickerMath.RgbToHsv(initial.R, initial.G, initial.B, out hue, out saturation, out value);
            alpha = initial.A;
            Loaded += OnLoaded;
        }

        private string Loc(string key)
        {
            var value = loc(key);
            return string.IsNullOrWhiteSpace(value) ? key : value;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            SvSurface.SizeChanged += OnPickerSizeChanged;
            HueBar.SizeChanged += OnPickerSizeChanged;
            RefreshFromHsv(true);
        }

        private void OnPickerSizeChanged(object sender, SizeChangedEventArgs args)
        {
            PositionThumbs();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                args.Handled = true;
                DialogResult = false;
            }
            else if (args.Key == Key.Enter)
            {
                args.Handled = true;
                ApplyClick(sender, args);
            }
        }

        private void ApplyClick(object sender, RoutedEventArgs args)
        {
            CommitCurrentColor();
            DialogResult = true;
        }

        private void CancelClick(object sender, RoutedEventArgs args)
        {
            DialogResult = false;
        }

        private void OpacitySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            if (updatingUi)
            {
                return;
            }

            alpha = ColorPickerMath.PercentToAlpha((int)Math.Round(OpacitySlider.Value));
            RefreshFromHsv(false);
        }

        private void HexBoxTextChanged(object sender, TextChangedEventArgs args)
        {
            if (updatingUi)
            {
                return;
            }

            byte parsedAlpha;
            byte red;
            byte green;
            byte blue;
            if (!ColorPickerMath.TryParseHex(HexBox.Text, out parsedAlpha, out red, out green, out blue))
            {
                return;
            }

            ColorPickerMath.RgbToHsv(red, green, blue, out hue, out saturation, out value);
            alpha = parsedAlpha;
            RefreshFromHsv(false);
        }

        private void SvSurfaceMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            draggingSv = true;
            SvSurface.CaptureMouse();
            ApplySvPoint(args.GetPosition(SvSurface));
        }

        private void SvSurfaceMouseMove(object sender, MouseEventArgs args)
        {
            if (!draggingSv)
            {
                return;
            }

            ApplySvPoint(args.GetPosition(SvSurface));
        }

        private void SvSurfaceMouseLeftButtonUp(object sender, MouseButtonEventArgs args)
        {
            draggingSv = false;
            if (SvSurface.IsMouseCaptured)
            {
                SvSurface.ReleaseMouseCapture();
            }
        }

        private void HueBarMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            draggingHue = true;
            HueBar.CaptureMouse();
            ApplyHuePoint(args.GetPosition(HueBar));
        }

        private void HueBarMouseMove(object sender, MouseEventArgs args)
        {
            if (!draggingHue)
            {
                return;
            }

            ApplyHuePoint(args.GetPosition(HueBar));
        }

        private void HueBarMouseLeftButtonUp(object sender, MouseButtonEventArgs args)
        {
            draggingHue = false;
            if (HueBar.IsMouseCaptured)
            {
                HueBar.ReleaseMouseCapture();
            }
        }

        private void ApplySvPoint(Point point)
        {
            var width = SvSurface.ActualWidth;
            var height = SvSurface.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            saturation = Clamp01(point.X / width);
            value = 1.0 - Clamp01(point.Y / height);
            RefreshFromHsv(false);
        }

        private void ApplyHuePoint(Point point)
        {
            var height = HueBar.ActualHeight;
            if (height <= 0)
            {
                return;
            }

            hue = Clamp01(point.Y / height) * 360.0;
            if (hue >= 360.0)
            {
                hue = 0;
            }

            RefreshFromHsv(false);
        }

        private void RefreshFromHsv(bool updateHex)
        {
            updatingUi = true;
            try
            {
                byte red;
                byte green;
                byte blue;
                ColorPickerMath.HsvToRgb(hue, saturation, value, out red, out green, out blue);
                SelectedColor = Color.FromArgb(alpha, red, green, blue);
                byte hueRed;
                byte hueGreen;
                byte hueBlue;
                ColorPickerMath.HsvToRgb(hue, 1, 1, out hueRed, out hueGreen, out hueBlue);
                HueFill.Background = new SolidColorBrush(Color.FromRgb(hueRed, hueGreen, hueBlue));
                PreviewSwatch.Background = new SolidColorBrush(SelectedColor);

                var percent = ColorPickerMath.AlphaToPercent(alpha);
                OpacitySlider.Value = percent;
                OpacityValueText.Text = string.Format(Loc("LOCCSM_ColorOpacityValue"), percent);

                if (updateHex || !HexBox.IsFocused)
                {
                    HexBox.Text = ColorPickerMath.ToHex(alpha, red, green, blue);
                }

                PositionThumbs();
            }
            finally
            {
                updatingUi = false;
            }
        }

        private void PositionThumbs()
        {
            var svWidth = SvSurface.ActualWidth;
            var svHeight = SvSurface.ActualHeight;
            if (svWidth > 0 && svHeight > 0)
            {
                Canvas.SetLeft(SvThumb, saturation * svWidth - (SvThumb.Width / 2.0));
                Canvas.SetTop(SvThumb, (1.0 - value) * svHeight - (SvThumb.Height / 2.0));
            }

            var hueHeight = HueBar.ActualHeight;
            if (hueHeight > 0)
            {
                Canvas.SetTop(HueThumb, (hue / 360.0) * hueHeight - (HueThumb.Height / 2.0));
            }
        }

        private void CommitCurrentColor()
        {
            byte red;
            byte green;
            byte blue;
            ColorPickerMath.HsvToRgb(hue, saturation, value, out red, out green, out blue);
            SelectedColor = Color.FromArgb(alpha, red, green, blue);
        }

        private static double Clamp01(double value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 1 ? 1 : value;
        }
    }
}
