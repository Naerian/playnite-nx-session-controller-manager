using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Models
{
    public enum GamepadCompatibilitySeverity
    {
        Ready,
        Info,
        Warning,
        Limited
    }

    public enum GamepadInputMode
    {
        Unknown,
        XInput,
        DirectInput,
        NativeHid
    }

    public sealed class GamepadCompatibilityFinding
    {
        public string Code { get; set; }
        public GamepadCompatibilitySeverity Severity { get; set; }
        public string Evidence { get; set; }
    }

    public sealed class GamepadCompatibilityAssessment
    {
        public GamepadCompatibilitySeverity Severity { get; set; }
        public GamepadInputMode InputMode { get; set; }
        public bool HasMapping { get; set; }
        public int MappingCoveragePercent { get; set; }
        public List<string> MissingBindings { get; private set; }
        public List<GamepadCompatibilityFinding> Findings { get; private set; }

        public GamepadCompatibilityAssessment()
        {
            MissingBindings = new List<string>();
            Findings = new List<GamepadCompatibilityFinding>();
        }
    }

    public sealed class GamepadCompatibilityFindingView
    {
        public string Severity { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
    }
}
