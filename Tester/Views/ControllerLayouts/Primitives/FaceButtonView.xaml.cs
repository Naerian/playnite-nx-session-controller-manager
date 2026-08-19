using System.Windows;
using System.Windows.Controls;

namespace ControllerLayoutPrimitives
{
    public partial class FaceButtonView : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(FaceButtonView), new PropertyMetadata(false));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(FaceButtonView), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ButtonSizeProperty =
            DependencyProperty.Register("ButtonSize", typeof(double), typeof(FaceButtonView), new PropertyMetadata(40d));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(FaceButtonView), new PropertyMetadata(2d));

        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register("LabelFontSize", typeof(double), typeof(FaceButtonView), new PropertyMetadata(14d));

        public static readonly DependencyProperty LabelOffsetYProperty =
            DependencyProperty.Register("LabelOffsetY", typeof(double), typeof(FaceButtonView), new PropertyMetadata(0d));

        public FaceButtonView()
        {
            InitializeComponent();
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public double ButtonSize
        {
            get { return (double)GetValue(ButtonSizeProperty); }
            set { SetValue(ButtonSizeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public double LabelFontSize
        {
            get { return (double)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        public double LabelOffsetY
        {
            get { return (double)GetValue(LabelOffsetYProperty); }
            set { SetValue(LabelOffsetYProperty, value); }
        }
    }
}
