using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class CustomSoundProgressWindow : Window
    {
        private bool allowClose;
        private bool cancellationRequested;

        public CustomSoundProgressWindow()
        {
            InitializeComponent();
        }

        public event EventHandler CancelRequested;

        public void CompleteAndClose()
        {
            allowClose = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs args)
        {
            RequestCancel();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                args.Handled = true;
                RequestCancel();
            }
        }

        private void OnClosing(object sender, CancelEventArgs args)
        {
            if (!allowClose)
            {
                args.Cancel = true;
                RequestCancel();
            }
        }

        private void RequestCancel()
        {
            if (cancellationRequested)
            {
                return;
            }

            cancellationRequested = true;
            CancelButton.IsEnabled = false;
            var handler = CancelRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
