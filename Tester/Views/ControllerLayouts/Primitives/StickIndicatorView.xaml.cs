using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ControllerLayoutPrimitives
{
    public partial class StickIndicatorView : UserControl
    {
        public static readonly DependencyProperty OuterSizeProperty =
            DependencyProperty.Register("OuterSize", typeof(double), typeof(StickIndicatorView), new PropertyMetadata(72d));

        public static readonly DependencyProperty DotSizeProperty =
            DependencyProperty.Register("DotSize", typeof(double), typeof(StickIndicatorView), new PropertyMetadata(34d));

        public static readonly DependencyProperty DotXProperty =
            DependencyProperty.Register("DotX", typeof(double), typeof(StickIndicatorView), new PropertyMetadata(0d));

        public static readonly DependencyProperty DotYProperty =
            DependencyProperty.Register("DotY", typeof(double), typeof(StickIndicatorView), new PropertyMetadata(0d));

        public static readonly DependencyProperty DotFillProperty =
            DependencyProperty.Register("DotFill", typeof(Brush), typeof(StickIndicatorView), new PropertyMetadata(Brushes.DeepSkyBlue));

        public static readonly DependencyProperty OuterFillProperty =
            DependencyProperty.Register(
                "OuterFill",
                typeof(Brush),
                typeof(StickIndicatorView),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(20, 24, 31))));

        public static readonly DependencyProperty OuterStrokeProperty =
            DependencyProperty.Register(
                "OuterStroke",
                typeof(Brush),
                typeof(StickIndicatorView),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(82, 92, 110))));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(StickIndicatorView), new PropertyMetadata(2d));

        public StickIndicatorView()
        {
            InitializeComponent();
            SetResourceReference(OuterFillProperty, "SurfaceFill");
            SetResourceReference(OuterStrokeProperty, "MutedLineBrush");
        }

        public double OuterSize
        {
            get { return (double)GetValue(OuterSizeProperty); }
            set { SetValue(OuterSizeProperty, value); }
        }

        public double DotSize
        {
            get { return (double)GetValue(DotSizeProperty); }
            set { SetValue(DotSizeProperty, value); }
        }

        public double DotX
        {
            get { return (double)GetValue(DotXProperty); }
            set { SetValue(DotXProperty, value); }
        }

        public double DotY
        {
            get { return (double)GetValue(DotYProperty); }
            set { SetValue(DotYProperty, value); }
        }

        public Brush DotFill
        {
            get { return (Brush)GetValue(DotFillProperty); }
            set { SetValue(DotFillProperty, value); }
        }

        public Brush OuterFill
        {
            get { return (Brush)GetValue(OuterFillProperty); }
            set { SetValue(OuterFillProperty, value); }
        }

        public Brush OuterStroke
        {
            get { return (Brush)GetValue(OuterStrokeProperty); }
            set { SetValue(OuterStrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
    }
}
