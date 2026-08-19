using System;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class StickState
    {
        public float X { get; set; }
        public float Y { get; set; }

        public float Magnitude
        {
            get { return (float)Math.Sqrt((X * X) + (Y * Y)); }
        }
    }
}
