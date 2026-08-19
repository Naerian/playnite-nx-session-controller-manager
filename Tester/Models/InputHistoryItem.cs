using System;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class InputHistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string InputName { get; set; }
        public string State { get; set; }

        public string TimeLabel
        {
            get { return Timestamp.ToString("HH:mm:ss.fff"); }
        }

        public string DetailLabel
        {
            get { return string.Format("{0} at {1}", State, TimeLabel); }
        }

        public string DisplayText
        {
            get { return string.Format("{0:HH:mm:ss.fff}  {1}  {2}", Timestamp, InputName, State); }
        }
    }
}
