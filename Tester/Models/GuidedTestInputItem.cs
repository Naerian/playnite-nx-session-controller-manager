using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class GuidedTestInputItem : INotifyPropertyChanged
    {
        private string label;
        private bool isCovered;
        private bool isCurrent;

        public GuidedTestInputItem(string key)
        {
            Key = key;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Key { get; private set; }

        public string Label
        {
            get { return label; }
            set { SetValue(ref label, value); }
        }

        public bool IsCovered
        {
            get { return isCovered; }
            set { SetValue(ref isCovered, value); }
        }

        public bool IsCurrent
        {
            get { return isCurrent; }
            set { SetValue(ref isCurrent, value); }
        }

        private void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
