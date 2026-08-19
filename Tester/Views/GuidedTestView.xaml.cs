using System.Windows;
using System.Windows.Controls;

namespace ControllerSessionManager.Tester.Views
{
    public partial class GuidedTestView : UserControl
    {
        public GuidedTestView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Close();
            }
        }
    }
}
