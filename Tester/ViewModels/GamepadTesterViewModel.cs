using ControllerSessionManager.Controllers;
using ControllerSessionManager.Tester.Commands;
using ControllerSessionManager.Tester.Models;
using ControllerSessionManager.Tester.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace ControllerSessionManager.Tester.ViewModels
{
    public sealed class GamepadTesterViewModel : ObservableObject, IDisposable
    {
        private const double StickRadius = 34d;
        private const int LatencyGraphMaxSamples = 48;
        private readonly GamepadPollingService pollingService;
        private readonly GamepadTesterSettings settings;
        private readonly Func<string, string> localizer;
        private readonly Action<GamepadTesterViewModel> openGuidedTest;
        private readonly RelayCommand rumbleCommand;
        private readonly RelayCommand lightRumbleCommand;
        private readonly RelayCommand mediumRumbleCommand;
        private readonly RelayCommand heavyRumbleCommand;
        private readonly RelayCommand lowMotorRumbleCommand;
        private readonly RelayCommand highMotorRumbleCommand;
        private readonly RelayCommand pulseRumbleCommand;
        private readonly RelayCommand alternatingRumbleCommand;
        private readonly RelayCommand rampRumbleCommand;
        private readonly RelayCommand burstRumbleCommand;
        private readonly RelayCommand resetDiagnosticsCommand;
        private readonly RelayCommand startCenterCalibrationCommand;
        private readonly RelayCommand resetCalibrationCommand;
        private readonly RelayCommand resetStickRangeCommand;
        private readonly RelayCommand resetLatencyCommand;
        private readonly RelayCommand startLatencyTestCommand;
        private readonly RelayCommand startButtonCaptureCommand;
        private readonly RelayCommand startStickCaptureCommand;
        private readonly RelayCommand openGuidedTestCommand;
        private readonly RelayCommand startGuidedTestCommand;
        private readonly RelayCommand openSticksTabCommand;
        private readonly RelayCommand openLatencyTabCommand;
        private readonly RelayCommand openGuidedTabCommand;
        private readonly RelayCommand openGeneralTestTabCommand;
        private readonly RelayCommand exportReportCommand;
        private readonly RelayCommand exportInputLogCommand;
        private readonly RelayCommand exportLatencyCommand;
        private readonly RelayCommand exportSticksCommand;
        private readonly RelayCommand exportCompatibilityReportCommand;
        private readonly RelayCommand resetInputLogCommand;
        private readonly StickDiagnosticsTracker leftStickDiagnostics;
        private readonly StickDiagnosticsTracker rightStickDiagnostics;
        private readonly StickMotionTrailTracker leftStickTrail;
        private readonly StickMotionTrailTracker rightStickTrail;
        private readonly RestDriftTracker restDriftDiagnostics;
        private GamepadState state;
        private GamepadState latestInputState;
        private GamepadControllerInfo selectedController;
        private int controllerRefreshTick;
        private bool isRefreshingControllers;
        private GamepadButtonState previousButtons;
        private List<ExtraButtonState> previousExtraButtons;
        private float previousLeftTrigger;
        private float previousRightTrigger;
        private GamepadButtonState coveredButtons;
        private double maxLeftStickMagnitude;
        private double maxRightStickMagnitude;
        private float maxLeftTrigger;
        private float maxRightTrigger;
        private bool isControllerSelectorOpen;
        private bool isInputLogEnabled;
        private bool isGuidedTestRunning;
        private int guidedTestStepIndex;
        private bool hasGuidedTestReport;
        private bool isGuidedTestReportSuccess;
        private string guidedTestReportLabel;
        private ushort pendingSelectVendorId;
        private ushort pendingSelectProductId;
        private string pendingSelectName;
        private bool hasPendingControllerSelection;
        private bool isRumbleRunning;
        private string rumbleStatusLabel;
        private bool isCenterCalibrationRunning;
        private DateTime centerCalibrationEndsAt;
        private int centerCalibrationSamples;
        private double leftCenterXSum;
        private double leftCenterYSum;
        private double rightCenterXSum;
        private double rightCenterYSum;
        private double leftCenterMaxNoise;
        private double rightCenterMaxNoise;
        private double calibratedLeftCenterX;
        private double calibratedLeftCenterY;
        private double calibratedRightCenterX;
        private double calibratedRightCenterY;
        private double calibratedLeftCenterNoise;
        private double calibratedRightCenterNoise;
        private DateTime? lastStateSampleAt;
        private DateTime? lastInputEventAt;
        private double currentPollingIntervalMs;
        private double pollingIntervalSumMs;
        private double pollingIntervalMinMs;
        private double pollingIntervalMaxMs;
        private int pollingIntervalSamples;
        private double inputEventIntervalSumMs;
        private double inputEventIntervalMinMs;
        private double inputEventIntervalMaxMs;
        private int inputEventIntervalSamples;
        private double currentInputEventIntervalMs;
        private readonly Queue<double> latencyRateHistory = new Queue<double>();
        private string latencyStatusLabel;
        private bool hasLatencyTestStarted;
        private bool isLatencyTestRunning;
        private bool isButtonCaptureRunning;
        private bool isStickCaptureRunning;
        private bool stickCaptureCompletedAutomatically;
        private bool stickCaptureReachedLimit;
        private DateTime latencyTestStartedAt;
        private double latencyTestDurationSeconds;
        private double lastLatencyMs;
        private double bestLatencyMs;
        private double latencyTestSumMs;
        private int latencyTestSamples;
        private string exportReportStatusLabel;
        private string inputLogExportStatusLabel;
        private string compatibilityReportStatusLabel;
        private GamepadCompatibilityAssessment compatibilityAssessment;
        private bool compatibilityMetadataInitialized;
        private bool compatibilityConnected;
        private string compatibilityControllerName;
        private ushort compatibilityVendorId;
        private ushort compatibilityProductId;
        private GamepadLayout compatibilityLayout;
        private string compatibilitySdlGuid;
        private string compatibilitySdlMapping;
        private int compatibilityAxisCount;
        private int compatibilityButtonCount;
        private int compatibilityHatCount;
        private int compatibilityExtraButtonCount;
        private string selectedVisualSchemeKey;
        private bool isVisualSchemeManuallySelected;
        private int selectedTabIndex;
        private bool isFullscreenSimplifiedMode;
        private bool isOptionsTabVisible = true;
        private readonly bool sidebarItemAtOpen;

        public GamepadTesterViewModel(GamepadPollingService pollingService, GamepadTesterSettings settings = null, Func<string, string> localizer = null, Action<GamepadTesterViewModel> openGuidedTest = null)
        {
            this.pollingService = pollingService;
            this.settings = settings ?? new GamepadTesterSettings();
            this.localizer = localizer;
            this.openGuidedTest = openGuidedTest;
            sidebarItemAtOpen = this.settings.ShowSidebarItem;
            state = new GamepadState();
            latestInputState = state;
            Controllers = new ObservableCollection<GamepadControllerInfo>();
            InputHistory = new ObservableCollection<InputHistoryItem>();
            GuidedTestInputs = new ObservableCollection<GuidedTestInputItem>();
            GuidedTestReportItems = new ObservableCollection<GuidedTestReportItem>();
            VisualSchemeOptions = new ObservableCollection<ControllerVisualSchemeOption>();
            CompatibilityFindings = new ObservableCollection<GamepadCompatibilityFindingView>();
            InitializeVisualSchemeOptions();
            rumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_Standard", "Standard"), 42000, 52000, 350), CanRunRumble);
            lightRumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_Light", "Light"), 14000, 18000, 260), CanRunRumble);
            mediumRumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_Medium", "Medium"), 28000, 36000, 360), CanRunRumble);
            heavyRumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_Heavy", "Heavy"), 52000, 62000, 520), CanRunRumble);
            lowMotorRumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_LowMotor", "Low motor"), 52000, 0, 520), CanRunRumble);
            highMotorRumbleCommand = new RelayCommand(() => RunSimpleRumble(L("LOCCSM_Tester_HighMotor", "High motor"), 0, 56000, 520), CanRunRumble);
            pulseRumbleCommand = new RelayCommand(TestPulseRumble, CanRunRumble);
            alternatingRumbleCommand = new RelayCommand(TestAlternatingRumble, CanRunRumble);
            rampRumbleCommand = new RelayCommand(TestRampRumble, CanRunRumble);
            burstRumbleCommand = new RelayCommand(TestBurstRumble, CanRunRumble);
            resetDiagnosticsCommand = new RelayCommand(ResetDiagnostics);
            startCenterCalibrationCommand = new RelayCommand(StartCenterCalibration, () => State.IsConnected && !isCenterCalibrationRunning);
            resetCalibrationCommand = new RelayCommand(ResetCalibration, CanResetCalibration);
            resetStickRangeCommand = new RelayCommand(ResetStickRangeDiagnostics, CanResetStickRange);
            resetLatencyCommand = new RelayCommand(ResetLatency, () => State.IsConnected && !isLatencyTestRunning);
            startLatencyTestCommand = new RelayCommand(ToggleLatencyTest, () => isLatencyTestRunning || State.IsConnected);
            startButtonCaptureCommand = new RelayCommand(
                ToggleButtonCapture,
                () => isButtonCaptureRunning || (!isLatencyTestRunning && !isStickCaptureRunning));
            startStickCaptureCommand = new RelayCommand(
                ToggleStickCapture,
                () => isStickCaptureRunning || State.IsConnected);
            openGuidedTestCommand = new RelayCommand(OpenGuidedTest, () => State.IsConnected);
            startGuidedTestCommand = new RelayCommand(ToggleGuidedTest, () => State.IsConnected);
            openSticksTabCommand = new RelayCommand(() => SelectedTabIndex = TabSticks);
            openLatencyTabCommand = new RelayCommand(() => SelectedTabIndex = TabLatency);
            openGuidedTabCommand = new RelayCommand(OpenGuidedTest, () => State.IsConnected);
            openGeneralTestTabCommand = new RelayCommand(() => SelectedTabIndex = TabGeneral);
            exportReportCommand = new RelayCommand(ExportReport);
            exportInputLogCommand = new RelayCommand(ExportInputLog, () => InputHistory.Count > 0);
            exportLatencyCommand = new RelayCommand(ExportLatencyData, () => !isLatencyTestRunning && inputEventIntervalSamples > 0);
            exportSticksCommand = new RelayCommand(ExportStickData, () => State.IsConnected);
            exportCompatibilityReportCommand = new RelayCommand(ExportCompatibilityReport, () => State.IsConnected);
            resetInputLogCommand = new RelayCommand(ClearInputHistory, () => InputHistory.Count > 0);
            leftStickDiagnostics = new StickDiagnosticsTracker();
            rightStickDiagnostics = new StickDiagnosticsTracker();
            leftStickTrail = new StickMotionTrailTracker();
            rightStickTrail = new StickMotionTrailTracker();
            restDriftDiagnostics = new RestDriftTracker();
            coveredButtons = new GamepadButtonState();
            InitializeGuidedTestInputs();
            rumbleStatusLabel = L("LOCCSM_Tester_Ready", "Ready");
            latencyStatusLabel = L("LOCCSM_Tester_LatencyWaiting", "Waiting for input changes.");
            exportReportStatusLabel = L("LOCCSM_Tester_ReportReady", "Report ready to export.");
            inputLogExportStatusLabel = L("LOCCSM_Tester_InputLogExportReady", "Enable input log and press buttons to collect entries.");
            compatibilityReportStatusLabel = L("LOCCSM_Tester_CompatibilityReportReady", "Technical device report ready to export.");
            isInputLogEnabled = this.settings.EnableInputLogByDefault;
            pollingIntervalMinMs = double.MaxValue;
            inputEventIntervalMinMs = double.MaxValue;
            this.settings.PropertyChanged += OnSettingsPropertyChanged;
            pollingService.StateUpdated += OnStateUpdated;
            RefreshCompatibilityAssessment();
        }

        public ObservableCollection<GamepadControllerInfo> Controllers { get; private set; }
        public ObservableCollection<InputHistoryItem> InputHistory { get; private set; }
        public ObservableCollection<GuidedTestInputItem> GuidedTestInputs { get; private set; }
        public ObservableCollection<GuidedTestReportItem> GuidedTestReportItems { get; private set; }
        public ObservableCollection<ControllerVisualSchemeOption> VisualSchemeOptions { get; private set; }
        public ObservableCollection<GamepadCompatibilityFindingView> CompatibilityFindings { get; private set; }

        public GamepadState State
        {
            get { return state; }
            private set
            {
                state = value;
                RefreshCompatibilityAssessment();
                NotifyStateChanged();
            }
        }

        public ICommand RumbleCommand
        {
            get { return rumbleCommand; }
        }

        public ICommand LightRumbleCommand
        {
            get { return lightRumbleCommand; }
        }

        public ICommand MediumRumbleCommand
        {
            get { return mediumRumbleCommand; }
        }

        public ICommand HeavyRumbleCommand
        {
            get { return heavyRumbleCommand; }
        }

        public ICommand LowMotorRumbleCommand
        {
            get { return lowMotorRumbleCommand; }
        }

        public ICommand HighMotorRumbleCommand
        {
            get { return highMotorRumbleCommand; }
        }

        public ICommand PulseRumbleCommand
        {
            get { return pulseRumbleCommand; }
        }

        public ICommand AlternatingRumbleCommand
        {
            get { return alternatingRumbleCommand; }
        }

        public ICommand RampRumbleCommand
        {
            get { return rampRumbleCommand; }
        }

        public ICommand BurstRumbleCommand
        {
            get { return burstRumbleCommand; }
        }

        public ICommand ResetDiagnosticsCommand
        {
            get { return resetDiagnosticsCommand; }
        }

        public ICommand StartCenterCalibrationCommand
        {
            get { return startCenterCalibrationCommand; }
        }

        public ICommand ResetCalibrationCommand
        {
            get { return resetCalibrationCommand; }
        }

        public ICommand ResetStickRangeCommand
        {
            get { return resetStickRangeCommand; }
        }

        public ICommand ResetLatencyCommand
        {
            get { return resetLatencyCommand; }
        }

        public ICommand StartLatencyTestCommand
        {
            get { return startLatencyTestCommand; }
        }

        public ICommand StartButtonCaptureCommand
        {
            get { return startButtonCaptureCommand; }
        }

        public ICommand StartStickCaptureCommand
        {
            get { return startStickCaptureCommand; }
        }

        public const int TabGeneral = 0;
        public const int TabSticks = 1;
        public const int TabLatency = 2;
        public const int TabGuided = 3;
        public const int TabInputLog = 4;
        public const int TabDevice = 5;
        public const int TabDiagnostic = 6;
        public const int TabOptions = 7;

        public ICommand OpenGuidedTestCommand
        {
            get { return openGuidedTestCommand; }
        }

        public ICommand StartGuidedTestCommand
        {
            get { return startGuidedTestCommand; }
        }

        public ICommand OpenSticksTabCommand
        {
            get { return openSticksTabCommand; }
        }

        public ICommand OpenLatencyTabCommand
        {
            get { return openLatencyTabCommand; }
        }

        public ICommand OpenGuidedTabCommand
        {
            get { return openGuidedTabCommand; }
        }

        public ICommand OpenGeneralTestTabCommand
        {
            get { return openGeneralTestTabCommand; }
        }

        public ICommand ExportReportCommand
        {
            get { return exportReportCommand; }
        }

        public ICommand ExportInputLogCommand
        {
            get { return exportInputLogCommand; }
        }

        public ICommand ExportLatencyCommand
        {
            get { return exportLatencyCommand; }
        }

        public ICommand ExportSticksCommand
        {
            get { return exportSticksCommand; }
        }

        public ICommand ExportCompatibilityReportCommand
        {
            get { return exportCompatibilityReportCommand; }
        }

        public ICommand ResetInputLogCommand
        {
            get { return resetInputLogCommand; }
        }

        public GamepadTesterSettings Settings
        {
            get { return settings; }
        }

        /// <summary>
        /// Playnite rebuilds the Desktop sidebar only on startup, so toggling
        /// <see cref="GamepadTesterSettings.ShowSidebarItem"/> needs a restart.
        /// </summary>
        public bool IsSidebarRestartRequired
        {
            get { return settings.ShowSidebarItem != sidebarItemAtOpen; }
        }

        public GamepadControllerInfo SelectedController
        {
            get { return selectedController; }
            set
            {
                if (isRefreshingControllers && value == null)
                {
                    return;
                }

                var previousId = selectedController != null ? (int?)selectedController.InstanceId : null;
                var nextId = value != null ? (int?)value.InstanceId : null;
                if (previousId == nextId)
                {
                    if (!ReferenceEquals(selectedController, value))
                    {
                        selectedController = value;
                        OnPropertyChanged("SelectedController");
                    }

                    return;
                }

                selectedController = value;
                OnPropertyChanged("SelectedController");
                if (selectedController == null)
                {
                    return;
                }

                pollingService.SelectController(selectedController.InstanceId);
                if (previousId.HasValue)
                {
                    isVisualSchemeManuallySelected = false;
                    if (this.settings.AutoResetDiagnosticsOnControllerChange)
                    {
                        ResetDiagnostics();
                    }
                }
            }
        }

        public bool HasMultipleControllers
        {
            get { return Controllers.Count > 1; }
        }

        public bool IsControllerSelectorVisible
        {
            get
            {
                if (isFullscreenSimplifiedMode || Controllers.Count == 0)
                {
                    return false;
                }

                return Controllers.Count > 1 || settings.ShowDeviceSelectorWhenSingleController;
            }
        }

        public bool IsOptionsTabVisible
        {
            get { return isOptionsTabVisible && !isFullscreenSimplifiedMode; }
            set
            {
                if (isOptionsTabVisible == value)
                {
                    return;
                }

                isOptionsTabVisible = value;
                OnPropertyChanged("IsOptionsTabVisible");
                if (selectedTabIndex > GetMaxTabIndex())
                {
                    SelectedTabIndex = 0;
                }
                else
                {
                    OnPropertyChanged("IsNoControllerOverlayVisible");
                }
            }
        }

        public bool IsControllerSelectorOpen
        {
            get { return isControllerSelectorOpen; }
            set
            {
                if (isControllerSelectorOpen == value)
                {
                    return;
                }

                isControllerSelectorOpen = value;
                OnPropertyChanged("IsControllerSelectorOpen");
            }
        }

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                var next = Math.Max(0, Math.Min(GetMaxTabIndex(), value));
                if (selectedTabIndex == next)
                {
                    return;
                }

                selectedTabIndex = next;
                OnPropertyChanged("SelectedTabIndex");
                OnPropertyChanged("IsNoControllerOverlayVisible");
            }
        }

        public bool IsFullscreenSimplifiedMode
        {
            get { return isFullscreenSimplifiedMode; }
            set
            {
                if (isFullscreenSimplifiedMode == value)
                {
                    return;
                }

                isFullscreenSimplifiedMode = value;
                RefreshFullscreenDisplayState();
                OnPropertyChanged("IsFullscreenSimplifiedMode");
                OnPropertyChanged("IsControllerSelectorVisible");
                OnPropertyChanged("IsOptionsTabVisible");
                OnPropertyChanged("IsVisualSchemeSelectorVisible");
                OnPropertyChanged("IsFullTesterMode");
                if (isFullscreenSimplifiedMode && selectedTabIndex > 2)
                {
                    SelectedTabIndex = 0;
                }
            }
        }

        public bool IsVisualSchemeSelectorVisible
        {
            get { return !isFullscreenSimplifiedMode; }
        }

        public bool IsFullTesterMode
        {
            get { return !isFullscreenSimplifiedMode; }
        }

        public string FullscreenNavigationHint
        {
            get { return L("LOCCSM_Tester_FullscreenNavigationHint", "LB/RB sections  D-pad move  A select  Back+A latency  B close"); }
        }

        public void MoveSelectedTab(int direction)
        {
            var tabCount = GetMaxTabIndex() + 1;
            var next = SelectedTabIndex + direction;
            if (next < 0)
            {
                next = tabCount - 1;
            }
            else if (next >= tabCount)
            {
                next = 0;
            }

            SelectedTabIndex = next;
        }

        public bool HasController
        {
            get { return State.IsConnected; }
        }

        public bool IsNoControllerVisible
        {
            get { return !State.IsConnected; }
        }

        public bool IsNoControllerOverlayVisible
        {
            get { return IsNoControllerVisible && !(IsOptionsTabVisible && SelectedTabIndex == TabOptions); }
        }

        private int GetMaxTabIndex()
        {
            if (isFullscreenSimplifiedMode)
            {
                return TabLatency;
            }

            return isOptionsTabVisible ? TabOptions : TabDevice;
        }

        public bool IsInputLogEnabled
        {
            get { return isInputLogEnabled; }
            set
            {
                if (isInputLogEnabled == value)
                {
                    return;
                }

                isInputLogEnabled = value;
                if (!isInputLogEnabled)
                {
                    ClearInputHistory();
                }

                OnPropertyChanged("IsInputLogEnabled");
                OnPropertyChanged("IsInputLogDisabled");
                OnPropertyChanged("InputLogStatusLabel");
            }
        }

        public bool IsInputLogDisabled
        {
            get { return !isInputLogEnabled; }
        }

        public string InputLogStatusLabel
        {
            get
            {
                return isInputLogEnabled
                    ? L("LOCCSM_Tester_InputLogEnabledHelp", "Input history is recording button changes for this session.")
                    : L("LOCCSM_Tester_InputLogDisabledHelp", "Input history is paused. Enable it only when you need a detailed event log.");
            }
        }

        public string InputLogExportStatusLabel
        {
            get { return inputLogExportStatusLabel; }
        }

        public string BackendLabel
        {
            get { return L("LOCCSM_Tester_PlayniteSdlBackend", "Playnite SDL2 / SDL GameController"); }
        }

        public string MappingStatusLabel
        {
            get
            {
                if (State.IsConnected)
                {
                    return L("LOCCSM_Tester_MappingRecognized", "Mapped by SDL GameController");
                }

                return Controllers.Count == 0
                    ? L("LOCCSM_Tester_MappingNoController", "No mapped controller detected")
                    : L("LOCCSM_Tester_MappingWaiting", "Waiting for selected controller");
            }
        }

        public string RumbleStatusLabel
        {
            get { return rumbleStatusLabel; }
        }

        public double LeftStickDotX
        {
            get { return State.LeftStick.X * StickRadius; }
        }

        public double LeftStickDotY
        {
            get { return -State.LeftStick.Y * StickRadius; }
        }

        public double RightStickDotX
        {
            get { return State.RightStick.X * StickRadius; }
        }

        public double RightStickDotY
        {
            get { return -State.RightStick.Y * StickRadius; }
        }

        public double LeftStickDiagnosticsDotX
        {
            get { return State.LeftStick.X * 108d; }
        }

        public double LeftStickDiagnosticsDotY
        {
            get { return -State.LeftStick.Y * 108d; }
        }

        public double RightStickDiagnosticsDotX
        {
            get { return State.RightStick.X * 108d; }
        }

        public double RightStickDiagnosticsDotY
        {
            get { return -State.RightStick.Y * 108d; }
        }

        public double CompactLeftStickDotX
        {
            get { return State.LeftStick.X * 13d; }
        }

        public double CompactLeftStickDotY
        {
            get { return -State.LeftStick.Y * 13d; }
        }

        public double CompactRightStickDotX
        {
            get { return State.RightStick.X * 13d; }
        }

        public double CompactRightStickDotY
        {
            get { return -State.RightStick.Y * 13d; }
        }

        public int LeftTriggerPercent
        {
            get { return (int)Math.Round(State.LeftTrigger * 100); }
        }

        public int RightTriggerPercent
        {
            get { return (int)Math.Round(State.RightTrigger * 100); }
        }

        public int LiveLeftTriggerPercent
        {
            get { return latestInputState == null ? 0 : (int)Math.Round(latestInputState.LeftTrigger * 100); }
        }

        public int LiveRightTriggerPercent
        {
            get { return latestInputState == null ? 0 : (int)Math.Round(latestInputState.RightTrigger * 100); }
        }

        public string LiveLeftTriggerLabel
        {
            get { return string.Format("LT  {0}%", LiveLeftTriggerPercent); }
        }

        public string LiveRightTriggerLabel
        {
            get { return string.Format("RT  {0}%", LiveRightTriggerPercent); }
        }

        public bool IsLeftTriggerActive
        {
            get { return State.LeftTrigger > 0.02f; }
        }

        public bool IsRightTriggerActive
        {
            get { return State.RightTrigger > 0.02f; }
        }

        public int LeftStickDriftPercent
        {
            get { return (int)Math.Round(State.LeftStick.Magnitude * 100); }
        }

        public int RightStickDriftPercent
        {
            get { return (int)Math.Round(State.RightStick.Magnitude * 100); }
        }

        public bool IsDpadActive
        {
            get
            {
                return State.Buttons.DpadUp || State.Buttons.DpadDown || State.Buttons.DpadLeft || State.Buttons.DpadRight;
            }
        }

        public int ActiveButtonCount
        {
            get { return CountPressedButtons(State.Buttons) + ExtraActiveButtonCount + (IsLeftTriggerActive ? 1 : 0) + (IsRightTriggerActive ? 1 : 0); }
        }

        public int ExtraActiveButtonCount
        {
            get { return CountPressedExtraButtons(State.ExtraButtons); }
        }

        public bool HasExtraButtons
        {
            get { return State.ExtraButtons != null && State.ExtraButtons.Count > 0; }
        }

        public bool IsFavoriteButtonActive
        {
            get { return State.ExtraButtons != null && State.ExtraButtons.Count > 0 && State.ExtraButtons[0].IsPressed; }
        }

        public string ExtraButtonSummaryLabel
        {
            get
            {
                if (!HasExtraButtons)
                {
                    return L("LOCCSM_Tester_NoExtraButtons", "No additional buttons exposed by SDL.");
                }

                return string.Format(L("LOCCSM_Tester_ExtraButtonsFormat", "{0} additional controls exposed by SDL"), State.ExtraButtons.Count);
            }
        }

        public string LeftStickVector
        {
            get { return string.Format("X {0:0.000}  Y {1:0.000}", State.LeftStick.X, State.LeftStick.Y); }
        }

        public string RightStickVector
        {
            get { return string.Format("X {0:0.000}  Y {1:0.000}", State.RightStick.X, State.RightStick.Y); }
        }

        public string LeftStickDriftStatus
        {
            get { return GetDriftStatus(State.LeftStick.Magnitude); }
        }

        public string RightStickDriftStatus
        {
            get { return GetDriftStatus(State.RightStick.Magnitude); }
        }

        public string MaxDriftLabel
        {
            get { return string.Format("{0:0.000}", CurrentCenterDrift); }
        }

        public string SessionRestDriftLabel
        {
            get { return string.Format("{0:0.000}", restDriftDiagnostics.MaxDrift); }
        }

        private double CurrentCenterDrift
        {
            get { return Math.Max(State.LeftStick.Magnitude, State.RightStick.Magnitude); }
        }

        private double EvaluatedRestDrift
        {
            get { return restDriftDiagnostics.MaxDrift; }
        }

        private double HealthyDeadzoneThreshold
        {
            get { return Clamp(settings.HealthyDeadzone, 0.02d, 0.30d); }
        }

        private double MinorDriftThreshold
        {
            get { return Clamp(settings.MinorDriftThreshold, HealthyDeadzoneThreshold + 0.01d, 0.40d); }
        }

        private double AttentionDriftThreshold
        {
            get { return Clamp(settings.AttentionDriftThreshold, MinorDriftThreshold + 0.01d, 0.60d); }
        }

        private double StickEdgeThreshold
        {
            get { return Clamp(settings.StickEdgeThreshold, 0.50d, 1.00d); }
        }

        private float TriggerFullPressThreshold
        {
            get { return (float)Clamp(settings.TriggerFullPressThreshold, 0.50d, 1.00d); }
        }

        private int CenterCalibrationDurationMilliseconds
        {
            get { return (int)Clamp(settings.CenterCalibrationMilliseconds, 800d, 6000d); }
        }

        public PointCollection LeftStickPathPoints
        {
            get { return leftStickDiagnostics.PathPoints; }
        }

        public PointCollection RightStickPathPoints
        {
            get { return rightStickDiagnostics.PathPoints; }
        }

        public Geometry LeftStickPathGeometry
        {
            get { return leftStickDiagnostics.PathGeometry; }
        }

        public Geometry RightStickPathGeometry
        {
            get { return rightStickDiagnostics.PathGeometry; }
        }

        public Geometry LeftStickTrailRecentGeometry
        {
            get { return leftStickTrail.RecentGeometry; }
        }

        public Geometry LeftStickTrailMidGeometry
        {
            get { return leftStickTrail.MidGeometry; }
        }

        public Geometry LeftStickTrailFadeGeometry
        {
            get { return leftStickTrail.FadeGeometry; }
        }

        public Geometry RightStickTrailRecentGeometry
        {
            get { return rightStickTrail.RecentGeometry; }
        }

        public Geometry RightStickTrailMidGeometry
        {
            get { return rightStickTrail.MidGeometry; }
        }

        public Geometry RightStickTrailFadeGeometry
        {
            get { return rightStickTrail.FadeGeometry; }
        }

        public Geometry LeftStickCircularCoverageGeometry
        {
            get { return leftStickDiagnostics.CoverageGeometry; }
        }

        public Geometry RightStickCircularCoverageGeometry
        {
            get { return rightStickDiagnostics.CoverageGeometry; }
        }

        public int LeftStickCircularCoveragePercent
        {
            get { return leftStickDiagnostics.CoveragePercent; }
        }

        public int RightStickCircularCoveragePercent
        {
            get { return rightStickDiagnostics.CoveragePercent; }
        }

        public int LeftStickMaxReachPercent
        {
            get { return Math.Min(100, (int)Math.Round(leftStickDiagnostics.MaxMagnitude * 100d)); }
        }

        public int RightStickMaxReachPercent
        {
            get { return Math.Min(100, (int)Math.Round(rightStickDiagnostics.MaxMagnitude * 100d)); }
        }

        public int LeftStickCurrentMagnitudePercent
        {
            get { return Math.Min(100, (int)Math.Round(State.LeftStick.Magnitude * 100d)); }
        }

        public int RightStickCurrentMagnitudePercent
        {
            get { return Math.Min(100, (int)Math.Round(State.RightStick.Magnitude * 100d)); }
        }

        public string LeftStickCircularCoverageLabel
        {
            get { return GetCircularCoverageLabel(leftStickDiagnostics); }
        }

        public string RightStickCircularCoverageLabel
        {
            get { return GetCircularCoverageLabel(rightStickDiagnostics); }
        }

        public string LeftStickPathSampleLabel
        {
            get { return GetPathSampleLabel(leftStickDiagnostics); }
        }

        public string RightStickPathSampleLabel
        {
            get { return GetPathSampleLabel(rightStickDiagnostics); }
        }

        public string LeftStickMaxReachLabel
        {
            get { return string.Format("Max reach: {0}%", Math.Min(100, (int)Math.Round(leftStickDiagnostics.MaxMagnitude * 100d))); }
        }

        public string RightStickMaxReachLabel
        {
            get { return string.Format("Max reach: {0}%", Math.Min(100, (int)Math.Round(rightStickDiagnostics.MaxMagnitude * 100d))); }
        }

        public string LeftStickCurrentMagnitudeLabel
        {
            get { return string.Format("Current: {0}%", LeftStickCurrentMagnitudePercent); }
        }

        public string RightStickCurrentMagnitudeLabel
        {
            get { return string.Format("Current: {0}%", RightStickCurrentMagnitudePercent); }
        }

        public string LeftStickAngleLabel
        {
            get { return GetAngleLabel(State.LeftStick); }
        }

        public string RightStickAngleLabel
        {
            get { return GetAngleLabel(State.RightStick); }
        }

        public string LeftStickAxisRangeLabel
        {
            get { return GetAxisRangeLabel(leftStickDiagnostics); }
        }

        public string RightStickAxisRangeLabel
        {
            get { return GetAxisRangeLabel(rightStickDiagnostics); }
        }

        public string LeftStickAverageMagnitudeLabel
        {
            get { return GetAverageMagnitudeLabel(leftStickDiagnostics); }
        }

        public string RightStickAverageMagnitudeLabel
        {
            get { return GetAverageMagnitudeLabel(rightStickDiagnostics); }
        }

        public string CalibrationStatusLabel
        {
            get
            {
                if (isCenterCalibrationRunning)
                {
                    var remaining = Math.Max(0d, (centerCalibrationEndsAt - DateTime.UtcNow).TotalSeconds);
                    return string.Format(L("LOCCSM_Tester_CalibrationRunningFormat", "Keep sticks released. Capturing center for {0:0.0}s."), remaining);
                }

                if (centerCalibrationSamples <= 0)
                {
                    return L("LOCCSM_Tester_CalibrationNotRun", "Center calibration has not been captured yet.");
                }

                return string.Format(L("LOCCSM_Tester_CalibrationSamplesFormat", "Center captured from {0} samples."), centerCalibrationSamples);
            }
        }

        public int CalibrationProgress
        {
            get
            {
                if (!isCenterCalibrationRunning)
                {
                    return centerCalibrationSamples > 0 ? 100 : 0;
                }

                var remaining = Math.Max(0d, (centerCalibrationEndsAt - DateTime.UtcNow).TotalMilliseconds);
                return Math.Max(0, Math.Min(100, 100 - (int)Math.Round(remaining * 100d / CenterCalibrationDurationMilliseconds)));
            }
        }

        public string LeftCalibrationCenterLabel
        {
            get { return string.Format(L("LOCCSM_Tester_CenterFormat", "Center X {0:0.000}  Y {1:0.000}"), calibratedLeftCenterX, calibratedLeftCenterY); }
        }

        public string RightCalibrationCenterLabel
        {
            get { return string.Format(L("LOCCSM_Tester_CenterFormat", "Center X {0:0.000}  Y {1:0.000}"), calibratedRightCenterX, calibratedRightCenterY); }
        }

        public string LeftRecommendedDeadzoneLabel
        {
            get { return GetRecommendedDeadzoneLabel(calibratedLeftCenterNoise); }
        }

        public string RightRecommendedDeadzoneLabel
        {
            get { return GetRecommendedDeadzoneLabel(calibratedRightCenterNoise); }
        }

        public int LeftRecommendedDeadzonePercent
        {
            get { return GetRecommendedDeadzonePercent(calibratedLeftCenterNoise); }
        }

        public int RightRecommendedDeadzonePercent
        {
            get { return GetRecommendedDeadzonePercent(calibratedRightCenterNoise); }
        }

        public string LeftRangeQualityLabel
        {
            get { return GetRangeQualityLabel(leftStickDiagnostics); }
        }

        public string RightRangeQualityLabel
        {
            get { return GetRangeQualityLabel(rightStickDiagnostics); }
        }

        public int LeftRangeQualityPercent
        {
            get { return GetRangeQualityPercent(leftStickDiagnostics); }
        }

        public int RightRangeQualityPercent
        {
            get { return GetRangeQualityPercent(rightStickDiagnostics); }
        }

        public int LeftRangeDisplayProgress
        {
            get
            {
                var confidence = GetRangeConfidence(leftStickDiagnostics);
                return confidence.IsReady ? LeftRangeQualityPercent : confidence.ProgressPercent;
            }
        }

        public int RightRangeDisplayProgress
        {
            get
            {
                var confidence = GetRangeConfidence(rightStickDiagnostics);
                return confidence.IsReady ? RightRangeQualityPercent : confidence.ProgressPercent;
            }
        }

        public string LeftRangeConfidenceLabel
        {
            get { return GetConfidenceLabel(GetRangeConfidence(leftStickDiagnostics)); }
        }

        public string RightRangeConfidenceLabel
        {
            get { return GetConfidenceLabel(GetRangeConfidence(rightStickDiagnostics)); }
        }

        public string LatencyStatusLabel
        {
            get
            {
                return hasLatencyTestStarted
                    ? latencyStatusLabel
                    : "-";
            }
        }

        public string StartLatencyButtonLabel
        {
            get
            {
                return isLatencyTestRunning
                    ? L("LOCCSM_Tester_StopLatency", "Stop latency")
                    : L("LOCCSM_Tester_StartLatency", "Start latency");
            }
        }

        public string LatencyResultLabel
        {
            get
            {
                if (isLatencyTestRunning)
                {
                    return "- ms";
                }

                if (!hasLatencyTestStarted || latencyTestSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0} ms", lastLatencyMs);
            }
        }

        public string LatencyStatsLabel
        {
            get
            {
                if (!hasLatencyTestStarted || latencyTestSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_LatencyStatsFormat", "Best {0:0} ms  Average {1:0} ms  Samples {2}"),
                    bestLatencyMs,
                    latencyTestSumMs / latencyTestSamples,
                    latencyTestSamples);
            }
        }

        public string PollingLatencyAverageLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_PollingHintFormat", "Polling avg {0:0.0} ms"), inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string LatencySampleCountLabel
        {
            get
            {
                return hasLatencyTestStarted
                    ? string.Format(L("LOCCSM_Tester_LatencySamplesFormat", "{0} samples"), inputEventIntervalSamples)
                    : L("LOCCSM_Tester_LatencyNoSamples", "No samples");
            }
        }

        public string LatencyRangeLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0 || inputEventIntervalMinMs == double.MaxValue)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_LatencyRangeFormat", "{0:0.0} ms min / {1:0.0} ms max"),
                    inputEventIntervalMinMs,
                    inputEventIntervalMaxMs);
            }
        }

        public string LatencyTestDurationLabel
        {
            get
            {
                if (!hasLatencyTestStarted)
                {
                    return "-";
                }

                var seconds = isLatencyTestRunning
                    ? Math.Max(0d, (DateTime.UtcNow - latencyTestStartedAt).TotalSeconds)
                    : latencyTestDurationSeconds;
                return string.Format(L("LOCCSM_Tester_LatencyDurationFormat", "{0:0}s session"), seconds);
            }
        }

        public string PollingRateCurrentLabel
        {
            get
            {
                return hasLatencyTestStarted && inputEventIntervalSamples > 0
                    ? GetHzLabel(currentInputEventIntervalMs)
                    : "- Hz";
            }
        }

        public string PollingRateAverageValueLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- Hz";
                }

                return GetHzLabel(inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string PollingRateMaxValueLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalMinMs == double.MaxValue)
                {
                    return "- Hz";
                }

                return GetHzLabel(inputEventIntervalMinMs);
            }
        }

        public string PollingJitterLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0.0} ms", Math.Max(0d, inputEventIntervalMaxMs - inputEventIntervalMinMs));
            }
        }

        public string EstimatedDelayLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0.0} ms", inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string InputEventLatencyAverageLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_EventIntervalFormat", "Observed input event interval: {0:0.0} ms avg"), inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public bool IsLatencyTestRunning
        {
            get { return isLatencyTestRunning; }
        }

        public bool IsButtonCaptureRunning
        {
            get { return isButtonCaptureRunning; }
        }

        public bool IsStickCaptureRunning
        {
            get { return isStickCaptureRunning; }
        }

        public bool IsFullscreenInputCaptureActive
        {
            get { return isButtonCaptureRunning || isStickCaptureRunning || isLatencyTestRunning; }
        }

        public bool CanNavigateBack
        {
            get { return !IsFullscreenInputCaptureActive; }
        }

        public bool IsAnyTestRunning
        {
            get { return IsFullscreenInputCaptureActive || isRumbleRunning; }
        }

        public string ActiveTestKind
        {
            get
            {
                if (isButtonCaptureRunning) return "Buttons";
                if (isStickCaptureRunning) return "Sticks";
                if (isLatencyTestRunning) return "Latency";
                if (isRumbleRunning) return "Rumble";
                return "None";
            }
        }

        public string ThemeContractVersion
        {
            get { return Views.ThemeIntegration.GamepadTesterThemeContract.Version; }
        }

        public bool IsRumbleRunning
        {
            get { return isRumbleRunning; }
        }

        public string ButtonCaptureButtonLabel
        {
            get
            {
                return isButtonCaptureRunning
                    ? L("LOCCSM_Tester_ButtonCaptureRunning", "Testing buttons")
                    : L("LOCCSM_Tester_StartButtonCapture", "Test buttons");
            }
        }

        public string StickCaptureButtonLabel
        {
            get
            {
                return isStickCaptureRunning
                    ? L("LOCCSM_Tester_StickCaptureRunning", "Testing sticks")
                    : L("LOCCSM_Tester_StartStickCapture", "Test sticks");
            }
        }

        public string StickCaptureStatusLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_StickCaptureNeedsController", "Connect a controller to start the stick test.");
                }

                if (isStickCaptureRunning)
                {
                    return L("LOCCSM_Tester_StickCaptureRunningHelp", "Sampling sticks. Rotate both sticks slowly around their full outer edge.");
                }

                if (stickCaptureCompletedAutomatically)
                {
                    return L("LOCCSM_Tester_StickCaptureComplete", "Stick sampling stopped automatically after both sticks reached 100% circular coverage.");
                }

                if (stickCaptureReachedLimit)
                {
                    return L("LOCCSM_Tester_StickCaptureLimit", "Stick sampling reached its safety limit and stopped. Reset and retry any directions that are still missing.");
                }

                if (leftStickDiagnostics.SampleCount > 0 || rightStickDiagnostics.SampleCount > 0)
                {
                    return L("LOCCSM_Tester_StickCaptureStopped", "Stick sampling is stopped. The collected path and coverage remain available until reset.");
                }

                return L("LOCCSM_Tester_StickCaptureReady", "Start the stick test, then rotate both sticks around their full outer edge.");
            }
        }

        public string CaptureExitHintLabel
        {
            get { return L("LOCCSM_Tester_CaptureExitHint", "Hold LB + RB to finish the test."); }
        }

        public string LatencyConfidenceLabel
        {
            get { return GetConfidenceLabel(DiagnosticConfidenceEvaluator.ForLatency(hasLatencyTestStarted, inputEventIntervalSamples)); }
        }

        public PointCollection LatencyRateGraphPoints
        {
            get
            {
                var points = new PointCollection();
                if (!hasLatencyTestStarted || latencyRateHistory.Count == 0)
                {
                    return points;
                }

                const double width = 540d;
                const double height = 132d;
                var values = new List<double>(latencyRateHistory);
                var step = values.Count <= 1 ? width : width / (values.Count - 1);
                for (var index = 0; index < values.Count; index++)
                {
                    var normalized = Math.Max(0d, Math.Min(1d, values[index] / 1000d));
                    points.Add(new Point(index * step, height - (normalized * height)));
                }

                return points;
            }
        }

        public IList<double> LatencyRateGraphValues
        {
            get { return new List<double>(latencyRateHistory); }
        }

        public IList<double> DiagnosticRadarValues
        {
            get
            {
                var healthConfidence = HealthConfidence;
                var timingConfidence = DiagnosticConfidenceEvaluator.ForLatency(hasLatencyTestStarted, inputEventIntervalSamples);
                return new List<double>
                {
                    healthConfidence.IsReady ? HealthScore : 0d,
                    LeftStickCircularCoveragePercent,
                    RightStickCircularCoveragePercent,
                    Math.Round((maxLeftTrigger + maxRightTrigger) * 50d),
                    QuickTestProgress,
                    timingConfidence.IsReady ? 100d : timingConfidence.ProgressPercent
                };
            }
        }

        public IList<string> DiagnosticRadarLabels
        {
            get
            {
                return new List<string>
                {
                    L("LOCCSM_Tester_CenterDrift", "Center"),
                    L("LOCCSM_Tester_LeftStick", "Left stick"),
                    L("LOCCSM_Tester_RightStick", "Right stick"),
                    L("LOCCSM_Tester_Triggers", "Triggers"),
                    L("LOCCSM_Tester_SessionCoverage", "Controls"),
                    L("LOCCSM_Tester_InputEventLatency", "Timing")
                };
            }
        }

        public int QuickTestProgress
        {
            get
            {
                const int totalChecks = 19;
                var completed = CountPressedButtons(coveredButtons);

                if (maxLeftTrigger >= TriggerFullPressThreshold)
                {
                    completed++;
                }

                if (maxRightTrigger >= TriggerFullPressThreshold)
                {
                    completed++;
                }

                if (maxLeftStickMagnitude >= StickEdgeThreshold)
                {
                    completed++;
                }

                if (maxRightStickMagnitude >= StickEdgeThreshold)
                {
                    completed++;
                }

                return Math.Max(0, Math.Min(100, (int)Math.Round(completed * 100d / totalChecks)));
            }
        }

        public string QuickTestLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (QuickTestProgress == 100)
                {
                    return L("LOCCSM_Tester_AllControlsCovered", "All normalized controls covered.");
                }

                return string.Format(L("LOCCSM_Tester_PercentCompleteFormat", "{0}% complete"), QuickTestProgress);
            }
        }

        public string ButtonCoverageLabel
        {
            get { return string.Format(L("LOCCSM_Tester_ButtonsSeenFormat", "{0}/15 buttons seen"), CountPressedButtons(coveredButtons)); }
        }

        public string AnalogCoverageLabel
        {
            get
            {
                return string.Format("{0} {1}%  {2} {3}%  LS {4}%  RS {5}%",
                    LeftTriggerLabel,
                    (int)Math.Round(maxLeftTrigger * 100),
                    RightTriggerLabel,
                    (int)Math.Round(maxRightTrigger * 100),
                    (int)Math.Round(maxLeftStickMagnitude * 100),
                    (int)Math.Round(maxRightStickMagnitude * 100));
            }
        }

        public string QuickTestMissingLabel
        {
            get
            {
                var missing = GetMissingInputLabels();
                return missing.Count == 0 ? L("LOCCSM_Tester_NothingMissing", "Nothing missing.") : string.Join(", ", missing);
            }
        }

        public bool CoveredSouth { get { return coveredButtons.South; } }
        public bool CoveredEast { get { return coveredButtons.East; } }
        public bool CoveredWest { get { return coveredButtons.West; } }
        public bool CoveredNorth { get { return coveredButtons.North; } }
        public bool CoveredLeftShoulder { get { return coveredButtons.LeftShoulder; } }
        public bool CoveredRightShoulder { get { return coveredButtons.RightShoulder; } }
        public bool CoveredLeftStickButton { get { return coveredButtons.LeftStick; } }
        public bool CoveredRightStickButton { get { return coveredButtons.RightStick; } }
        public bool CoveredBack { get { return coveredButtons.Back; } }
        public bool CoveredStart { get { return coveredButtons.Start; } }
        public bool CoveredGuide { get { return coveredButtons.Guide; } }
        public bool CoveredDpadUp { get { return coveredButtons.DpadUp; } }
        public bool CoveredDpadDown { get { return coveredButtons.DpadDown; } }
        public bool CoveredDpadLeft { get { return coveredButtons.DpadLeft; } }
        public bool CoveredDpadRight { get { return coveredButtons.DpadRight; } }
        public bool CoveredLeftTrigger { get { return maxLeftTrigger >= TriggerFullPressThreshold; } }
        public bool CoveredRightTrigger { get { return maxRightTrigger >= TriggerFullPressThreshold; } }
        public bool CoveredLeftStickRange { get { return maxLeftStickMagnitude >= StickEdgeThreshold; } }
        public bool CoveredRightStickRange { get { return maxRightStickMagnitude >= StickEdgeThreshold; } }

        public int GuidedTestProgress
        {
            get
            {
                if (GuidedTestInputs == null || GuidedTestInputs.Count == 0)
                {
                    return 0;
                }

                return Math.Max(0, Math.Min(100, (int)Math.Round(guidedTestStepIndex * 100d / GuidedTestInputs.Count)));
            }
        }

        public bool IsGuidedTestRunning
        {
            get { return isGuidedTestRunning; }
        }

        public bool HasGuidedTestReport
        {
            get { return hasGuidedTestReport; }
        }

        public bool IsGuidedTestReportSuccess
        {
            get { return isGuidedTestReportSuccess; }
        }

        public string GuidedTestButtonLabel
        {
            get
            {
                return isGuidedTestRunning
                    ? L("LOCCSM_Tester_StopGuidedTest", "Stop guided test")
                    : L("LOCCSM_Tester_StartGuidedTest", "Start guided test");
            }
        }

        public string GuidedTestReportLabel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(guidedTestReportLabel))
                {
                    return L("LOCCSM_Tester_GuidedTestReportIdle", "Start a guided pass. The report will appear here when you stop or finish.");
                }

                return guidedTestReportLabel;
            }
        }

        public string GuidedTestStatusLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (isGuidedTestRunning)
                {
                    var current = GetCurrentGuidedInputLabel();
                    if (current == null)
                    {
                        return L("LOCCSM_Tester_GuidedTestComplete", "Guided test complete. All normalized controls were seen.");
                    }

                    return string.Format(L("LOCCSM_Tester_GuidedTestStepFormat", "Next: {0}"), current);
                }

                if (IsGuidedTestFullyCovered())
                {
                    return L("LOCCSM_Tester_GuidedTestComplete", "Guided test complete. All normalized controls were seen.");
                }

                if (hasGuidedTestReport)
                {
                    return L("LOCCSM_Tester_GuidedTestStoppedShort", "Guided test stopped. Review the report on the right.");
                }

                return L("LOCCSM_Tester_GuidedTestReady", "Start a guided pass to verify every normalized input.");
            }
        }

        public string GuidedTestNextInputLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "--";
                }

                if (!isGuidedTestRunning)
                {
                    return IsGuidedTestFullyCovered()
                        ? L("LOCCSM_Tester_GuidedTestCompleteShort", "Complete")
                        : "--";
                }

                var current = GetCurrentGuidedInputLabel();
                return current == null ? L("LOCCSM_Tester_GuidedTestCompleteShort", "Complete") : current;
            }
        }

        public string GuidedTestActionLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_NoControllers", "No controller connected");
                }

                if (IsGuidedTestFullyCovered())
                {
                    return L("LOCCSM_Tester_AllControlsCovered", "All normalized controls covered.");
                }

                if (!isGuidedTestRunning)
                {
                    return hasGuidedTestReport
                        ? L("LOCCSM_Tester_GuidedTestStoppedShort", "Guided test stopped. Review the report on the right.")
                        : L("LOCCSM_Tester_GuidedTestReady", "Start a guided pass to verify every normalized input.");
                }

                return L("LOCCSM_Tester_PressThisControl", "Press this control");
            }
        }

        public int HealthScore
        {
            get
            {
                var drift = EvaluatedRestDrift;
                var driftPenalty = drift <= HealthyDeadzoneThreshold ? 0d : Math.Min(100d, (drift - HealthyDeadzoneThreshold) * 600d);
                return Math.Max(0, Math.Min(100, (int)Math.Round(100d - driftPenalty)));
            }
        }

        public string HealthScoreDisplayLabel
        {
            get
            {
                return HealthConfidence.IsReady
                    ? string.Format("{0}%", HealthScore)
                    : "--";
            }
        }

        public int HealthDisplayProgress
        {
            get { return HealthConfidence.IsReady ? HealthScore : HealthConfidence.ProgressPercent; }
        }

        public string HealthConfidenceLabel
        {
            get { return GetConfidenceLabel(HealthConfidence); }
        }

        private DiagnosticConfidence HealthConfidence
        {
            get { return DiagnosticConfidenceEvaluator.ForHealth(State.IsConnected, restDriftDiagnostics.SampleCount); }
        }

        public string HealthLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_NoController", "No controller");
                }

                if (!HealthConfidence.IsReady)
                {
                    return L("LOCCSM_Tester_CollectingData", "Collecting data");
                }

                var drift = EvaluatedRestDrift;
                if (drift >= AttentionDriftThreshold)
                {
                    return L("LOCCSM_Tester_HealthAttentionRequired", "Attention required");
                }

                if (drift >= MinorDriftThreshold)
                {
                    return L("LOCCSM_Tester_HealthNeedsReview", "Needs review");
                }

                if (HealthScore >= 90)
                {
                    return L("LOCCSM_Tester_HealthExcellent", "Excellent");
                }

                if (HealthScore >= 75)
                {
                    return L("LOCCSM_Tester_HealthGood", "Good");
                }

                return L("LOCCSM_Tester_HealthGood", "Good");
            }
        }

        public string HealthSummaryLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (!HealthConfidence.IsReady)
                {
                    return string.Format(L("LOCCSM_Tester_HealthCollectingFormat", "Release both sticks while stable rest samples are collected ({0}%)."), HealthConfidence.ProgressPercent);
                }

                if (EvaluatedRestDrift < HealthyDeadzoneThreshold)
                {
                    return L("LOCCSM_Tester_HealthSummaryCentered", "Centered sticks look stable right now.");
                }

                if (EvaluatedRestDrift < AttentionDriftThreshold)
                {
                    return L("LOCCSM_Tester_HealthSummarySmallDrift", "Small centered-stick movement is visible. Release the sticks and watch whether it settles.");
                }

                return L("LOCCSM_Tester_HealthSummaryReview", "Centered-stick drift is high enough to review deadzone, calibration, or hardware condition.");
            }
        }

        public string HealthDriftFactorLabel
        {
            get
            {
                return string.Format(L("LOCCSM_Tester_HealthDriftFactorFormat", "Rest drift used for health: {0:0.000} ({1})"),
                    EvaluatedRestDrift,
                    GetDriftStatus(EvaluatedRestDrift));
            }
        }

        public string HealthRangeFactorLabel
        {
            get
            {
                return string.Format(L("LOCCSM_Tester_HealthRangeFactorFormat", "Outer range seen: LS {0}% / RS {1}%"),
                    LeftStickMaxReachPercent,
                    RightStickMaxReachPercent);
            }
        }

        public string HealthCoverageFactorLabel
        {
            get
            {
                return string.Format(L("LOCCSM_Tester_HealthCoverageFactorFormat", "Quick checks: {0}% ({1})"),
                    QuickTestProgress,
                    ButtonCoverageLabel);
            }
        }

        public string ExportReportStatusLabel
        {
            get { return exportReportStatusLabel; }
        }

        public string CompatibilityReportStatusLabel
        {
            get { return compatibilityReportStatusLabel; }
        }

        public string CompatibilityAssistantStatus
        {
            get
            {
                var severity = compatibilityAssessment == null
                    ? GamepadCompatibilitySeverity.Limited
                    : compatibilityAssessment.Severity;
                return severity.ToString();
            }
        }

        public string CompatibilityAssistantStatusLabel
        {
            get
            {
                if (compatibilityAssessment == null || !State.IsConnected)
                {
                    return L("LOCCSM_Tester_CompatibilityDisconnected", "No mapped controller");
                }

                switch (compatibilityAssessment.Severity)
                {
                    case GamepadCompatibilitySeverity.Ready:
                        return L("LOCCSM_Tester_CompatibilityReady", "Ready");
                    case GamepadCompatibilitySeverity.Info:
                        return L("LOCCSM_Tester_CompatibilityReadyWithNotes", "Ready with notes");
                    case GamepadCompatibilitySeverity.Warning:
                        return L("LOCCSM_Tester_CompatibilityReview", "Review recommended");
                    default:
                        return L("LOCCSM_Tester_CompatibilityLimited", "Limited mapping");
                }
            }
        }

        public string CompatibilityInputModeLabel
        {
            get
            {
                if (compatibilityAssessment == null)
                {
                    return L("LOCCSM_Tester_InputModeUnknown", "Not determined by SDL");
                }

                switch (compatibilityAssessment.InputMode)
                {
                    case GamepadInputMode.XInput:
                        return "XInput";
                    case GamepadInputMode.DirectInput:
                        return "DirectInput";
                    case GamepadInputMode.NativeHid:
                        return L("LOCCSM_Tester_InputModeNativeHid", "Native / HID through SDL");
                    default:
                        return L("LOCCSM_Tester_InputModeUnknown", "Not determined by SDL");
                }
            }
        }

        public string CompatibilityMappingCoverageLabel
        {
            get
            {
                if (compatibilityAssessment == null || !compatibilityAssessment.HasMapping)
                {
                    return L("LOCCSM_Tester_MappingCoverageUnavailable", "Mapping coverage unavailable");
                }

                return string.Format(
                    L("LOCCSM_Tester_MappingCoverageFormat", "{0}% of standard controls mapped"),
                    compatibilityAssessment.MappingCoveragePercent);
            }
        }

        public string ControllerSummary
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCCSM_Tester_ConnectControllerAndPress", "Connect a controller and press any button.");
                }

                return string.Format("{0} inputs active | LT {1}% | RT {2}% | Rest drift peak {3}",
                    ActiveButtonCount,
                    LeftTriggerPercent,
                    RightTriggerPercent,
                    MaxDriftLabel);
            }
        }

        public string DeviceIdLabel
        {
            get
            {
                if (!State.IsConnected || State.VendorId == 0)
                {
                    return string.Empty;
                }

                return string.Format("VID: {0:X4}  PID: {1:X4}", State.VendorId, State.ProductId);
            }
        }

        public string DeviceModelLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return string.Empty;
                }

                return GamepadDeviceNames.GetDisplayName(State.ControllerName, State.VendorId, State.ProductId, State.Layout, State.EightBitDoModel);
            }
        }

        public string DeviceCapabilitiesLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_DeviceCapabilitiesFormat", "{0} normalized controls, 2 sticks, 2 analog triggers, {1} extra controls"),
                    CountNormalizedControls(),
                    State.ExtraButtons == null ? 0 : State.ExtraButtons.Count);
            }
        }

        public string DeviceApiLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return string.Format(L("LOCCSM_Tester_DeviceApiFormat", "{0} via SDL GameController"), State.Layout);
            }
        }

        public string DeviceRumbleCapabilityLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return settings.EnableRumbleTests
                    ? L("LOCCSM_Tester_RumbleCapabilityEnabled", "Rumble test enabled; hardware support depends on controller mode and driver.")
                    : L("LOCCSM_Tester_RumbleCapabilityDisabled", "Rumble test disabled in plugin settings.");
            }
        }

        public string ExtraButtonDetailLabel
        {
            get
            {
                if (!HasExtraButtons)
                {
                    return L("LOCCSM_Tester_NoExtraButtons", "No additional buttons exposed by SDL.");
                }

                var builder = new StringBuilder();
                for (var index = 0; index < State.ExtraButtons.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(State.ExtraButtons[index].Label);
                }

                return builder.ToString();
            }
        }

        public string SelectedVisualSchemeKey
        {
            get { return selectedVisualSchemeKey; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (!string.IsNullOrEmpty(selectedVisualSchemeKey))
                    {
                        OnPropertyChanged("SelectedVisualSchemeKey");
                    }

                    return;
                }

                if (selectedVisualSchemeKey == value)
                {
                    isVisualSchemeManuallySelected = true;
                    return;
                }

                selectedVisualSchemeKey = value;
                isVisualSchemeManuallySelected = true;
                NotifyVisualSchemeChanged();
            }
        }

        public double ControllerVisualWidth
        {
            get { return EffectiveVisualSchemeDefinition.TestWidth; }
        }

        public double ControllerVisualHeight
        {
            get { return EffectiveVisualSchemeDefinition.TestHeight; }
        }

        public double GuidedControllerVisualWidth
        {
            get { return EffectiveVisualSchemeDefinition.GuidedWidth; }
        }

        public double GuidedControllerVisualHeight
        {
            get { return EffectiveVisualSchemeDefinition.GuidedHeight; }
        }

        public string SouthLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Cross";
                }

                return UsesSwitchProLabels ? "B" : "A";
            }
        }

        public string EastLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Circle";
                }

                return UsesSwitchProLabels ? "A" : "B";
            }
        }

        public string WestLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Square";
                }

                return UsesSwitchProLabels ? "Y" : "X";
            }
        }

        public string NorthLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Triangle";
                }

                return UsesSwitchProLabels ? "X" : "Y";
            }
        }

        public string LeftShoulderLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "L1";
                }

                return UsesSwitchProLabels ? "L" : "LB";
            }
        }

        public string RightShoulderLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "R1";
                }

                return UsesSwitchProLabels ? "R" : "RB";
            }
        }

        public string LeftTriggerLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "L2";
                }

                return UsesSwitchProLabels ? "ZL" : "LT";
            }
        }

        public string RightTriggerLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "R2";
                }

                return UsesSwitchProLabels ? "ZR" : "RT";
            }
        }

        public string LeftStickButtonLabel
        {
            get { return UsesPlayStationLabels ? "L3" : UsesSwitchProLabels ? "L Stick" : "LS"; }
        }

        public string RightStickButtonLabel
        {
            get { return UsesPlayStationLabels ? "R3" : UsesSwitchProLabels ? "R Stick" : "RS"; }
        }

        public string BackButtonLabel
        {
            get { return UsesPlayStationLabels ? "Share" : UsesSwitchProLabels ? "Minus" : "View"; }
        }

        public string StartButtonLabel
        {
            get { return UsesPlayStationLabels ? "Options" : UsesSwitchProLabels ? "Plus" : "Menu"; }
        }

        public string GuideButtonLabel
        {
            get { return UsesPlayStationLabels ? "PS" : "Guide"; }
        }

        public string DpadUpLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Up" : "D-Up"; }
        }

        public string DpadDownLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Down" : "D-Down"; }
        }

        public string DpadLeftLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Left" : "D-Left"; }
        }

        public string DpadRightLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Right" : "D-Right"; }
        }

        private bool UsesSwitchProLabels
        {
            get { return ControllerVisualSchemeCatalog.UsesSwitchProLabels(EffectiveVisualSchemeKey); }
        }

        private bool UsesPlayStationLabels
        {
            get { return ControllerVisualSchemeCatalog.UsesPlayStationLabels(EffectiveVisualSchemeKey); }
        }

        public bool IsEightBitDoLayout
        {
            get { return State.Layout == GamepadLayout.EightBitDo; }
        }

        public bool IsEightBitDoPro3Artwork
        {
            get
            {
                return State.Layout == GamepadLayout.EightBitDo &&
                    (State.EightBitDoModel == EightBitDoModel.Pro2 ||
                     State.EightBitDoModel == EightBitDoModel.Pro3);
            }
        }

        public bool IsEightBitDoUltimate2CArtwork
        {
            get { return State.Layout == GamepadLayout.EightBitDo && State.EightBitDoModel == EightBitDoModel.Ultimate2CWireless; }
        }

        public bool IsEightBitDoUltimate2Artwork
        {
            get
            {
                return State.Layout == GamepadLayout.EightBitDo &&
                    (State.EightBitDoModel == EightBitDoModel.Ultimate2Wireless ||
                     State.EightBitDoModel == EightBitDoModel.Unknown);
            }
        }

        public bool IsSwitchProLayout
        {
            get { return State.Layout == GamepadLayout.SwitchPro; }
        }

        public bool IsXboxLayout
        {
            get { return State.Layout == GamepadLayout.Xbox; }
        }

        public bool IsPlayStationLayout
        {
            get { return State.Layout == GamepadLayout.PlayStation; }
        }

        public bool IsDualSenseLayout
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.DualSense; }
        }

        public bool IsXboxVisualScheme
        {
            get { return IsXboxOneVisualScheme; }
        }

        public bool IsXboxOneVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.XboxOne; }
        }

        public bool IsXboxSeriesVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.XboxSeries; }
        }

        public bool IsSteamControllerVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.SteamController; }
        }

        public bool IsPlayStationVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.PlayStation; }
        }

        public bool IsSwitchProVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.SwitchPro; }
        }

        public bool IsEightBitDoUltimateVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.EightBitDoUltimate; }
        }

        public bool IsEightBitDoUltimate2VisualScheme
        {
            get { return IsEightBitDoUltimateVisualScheme; }
        }

        public bool IsEightBitDoProVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.EightBitDoPro; }
        }

        public bool IsUniversalControllerArtwork
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.Universal; }
        }

        public bool IsGenericLayout
        {
            get { return State.Layout == GamepadLayout.Generic || State.Layout == GamepadLayout.Unknown; }
        }

        public void Start()
        {
            pollingService.Start();
        }

        public void SelectByVendorProduct(ushort vendorId, ushort productId)
        {
            RequestControllerSelection(vendorId, productId, null);
        }

        public void RequestControllerSelection(ushort vendorId, ushort productId, string name)
        {
            pendingSelectVendorId = vendorId;
            pendingSelectProductId = productId;
            pendingSelectName = name;
            hasPendingControllerSelection = vendorId != 0 || productId != 0 || !string.IsNullOrWhiteSpace(name);
            RefreshControllers();
            TryApplyPendingControllerSelection();
        }

        private void OnStateUpdated(object sender, GamepadState nextState)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                controllerRefreshTick++;
                if (!IsControllerSelectorOpen && (controllerRefreshTick == 1 || controllerRefreshTick >= 60))
                {
                    controllerRefreshTick = 0;
                    RefreshControllers();
                }

                latestInputState = nextState;
                UpdateDiagnostics(nextState);
                State = CreateDisplayState(nextState);
                SyncDetectedVisualScheme();
                RaiseRumbleCanExecuteChanged();
                startCenterCalibrationCommand.RaiseCanExecuteChanged();
                startLatencyTestCommand.RaiseCanExecuteChanged();
                startButtonCaptureCommand.RaiseCanExecuteChanged();
                startStickCaptureCommand.RaiseCanExecuteChanged();
                exportLatencyCommand.RaiseCanExecuteChanged();
                exportSticksCommand.RaiseCanExecuteChanged();
            }));
        }

        private string EffectiveVisualSchemeKey
        {
            get
            {
                return string.IsNullOrEmpty(selectedVisualSchemeKey)
                    ? ControllerVisualSchemeCatalog.Detect(State)
                    : selectedVisualSchemeKey;
            }
        }

        private ControllerVisualSchemeDefinition EffectiveVisualSchemeDefinition
        {
            get { return ControllerVisualSchemeCatalog.GetDefinition(EffectiveVisualSchemeKey, L); }
        }

        private void InitializeVisualSchemeOptions()
        {
            foreach (var option in ControllerVisualSchemeCatalog.CreateOptions(L))
            {
                VisualSchemeOptions.Add(option);
            }
        }

        private void InitializeGuidedTestInputs()
        {
            GuidedTestInputs.Add(new GuidedTestInputItem("South"));
            GuidedTestInputs.Add(new GuidedTestInputItem("East"));
            GuidedTestInputs.Add(new GuidedTestInputItem("West"));
            GuidedTestInputs.Add(new GuidedTestInputItem("North"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftShoulder"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightShoulder"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftTrigger"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightTrigger"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftStick"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightStick"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Back"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Start"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Guide"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadUp"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadDown"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadLeft"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadRight"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftStickRange"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightStickRange"));
            RefreshGuidedTestInputs();
        }

        private void SyncDetectedVisualScheme()
        {
            if (isVisualSchemeManuallySelected)
            {
                return;
            }

            var detectedSchemeKey = ControllerVisualSchemeCatalog.Detect(State);
            if (selectedVisualSchemeKey == detectedSchemeKey)
            {
                return;
            }

            selectedVisualSchemeKey = detectedSchemeKey;
            NotifyVisualSchemeChanged();
        }

        private void NotifyVisualSchemeChanged()
        {
            OnPropertyChanged("SelectedVisualSchemeKey");
            OnPropertyChanged("ControllerVisualWidth");
            OnPropertyChanged("ControllerVisualHeight");
            OnPropertyChanged("GuidedControllerVisualWidth");
            OnPropertyChanged("GuidedControllerVisualHeight");
            OnPropertyChanged("IsDualSenseLayout");
            OnPropertyChanged("IsXboxVisualScheme");
            OnPropertyChanged("IsXboxOneVisualScheme");
            OnPropertyChanged("IsXboxSeriesVisualScheme");
            OnPropertyChanged("IsSteamControllerVisualScheme");
            OnPropertyChanged("IsPlayStationVisualScheme");
            OnPropertyChanged("IsSwitchProVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimateVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimate2VisualScheme");
            OnPropertyChanged("IsEightBitDoProVisualScheme");
            OnPropertyChanged("IsUniversalControllerArtwork");
            OnPropertyChanged("SouthLabel");
            OnPropertyChanged("EastLabel");
            OnPropertyChanged("WestLabel");
            OnPropertyChanged("NorthLabel");
            OnPropertyChanged("LeftShoulderLabel");
            OnPropertyChanged("RightShoulderLabel");
            OnPropertyChanged("LeftTriggerLabel");
            OnPropertyChanged("RightTriggerLabel");
            OnPropertyChanged("LeftStickButtonLabel");
            OnPropertyChanged("RightStickButtonLabel");
            OnPropertyChanged("BackButtonLabel");
            OnPropertyChanged("StartButtonLabel");
            OnPropertyChanged("GuideButtonLabel");
            OnPropertyChanged("DpadUpLabel");
            OnPropertyChanged("DpadDownLabel");
            OnPropertyChanged("DpadLeftLabel");
            OnPropertyChanged("DpadRightLabel");
            OnPropertyChanged("AnalogCoverageLabel");
            OnPropertyChanged("QuickTestMissingLabel");
            RefreshGuidedTestInputs();
        }

        private void UpdateDiagnostics(GamepadState nextState)
        {
            UpdateLatency(nextState);

            if (!nextState.IsConnected)
            {
                previousButtons = null;
                previousExtraButtons = null;
                previousLeftTrigger = 0f;
                previousRightTrigger = 0f;
                leftStickTrail.Reset();
                rightStickTrail.Reset();
                StopStickCapture();
                if (isGuidedTestRunning)
                {
                    StopGuidedTest(false);
                }

                return;
            }

            UpdateCenterCalibration(nextState);

            TrackRestDrift(nextState);
            leftStickTrail.Update(nextState.LeftStick, DateTime.UtcNow);
            rightStickTrail.Update(nextState.RightStick, DateTime.UtcNow);

            if (isStickCaptureRunning)
            {
                leftStickDiagnostics.AddSample(nextState.LeftStick);
                rightStickDiagnostics.AddSample(nextState.RightStick);
                EvaluateStickCaptureCompletion();
            }

            if (!isFullscreenSimplifiedMode || isButtonCaptureRunning)
            {
                UpdateCoverage(nextState);
                UpdateGuidedTestProgress(nextState);
            }

            if (previousButtons == null)
            {
                previousButtons = CopyButtons(nextState.Buttons);
                previousExtraButtons = CopyExtraButtons(nextState.ExtraButtons);
                previousLeftTrigger = nextState.LeftTrigger;
                previousRightTrigger = nextState.RightTrigger;
                return;
            }

            TrackButtonChange(SouthLabel, previousButtons.South, nextState.Buttons.South);
            TrackButtonChange(EastLabel, previousButtons.East, nextState.Buttons.East);
            TrackButtonChange(WestLabel, previousButtons.West, nextState.Buttons.West);
            TrackButtonChange(NorthLabel, previousButtons.North, nextState.Buttons.North);
            TrackButtonChange(LeftShoulderLabel, previousButtons.LeftShoulder, nextState.Buttons.LeftShoulder);
            TrackButtonChange(RightShoulderLabel, previousButtons.RightShoulder, nextState.Buttons.RightShoulder);
            TrackButtonChange(BackButtonLabel, previousButtons.Back, nextState.Buttons.Back);
            TrackButtonChange(StartButtonLabel, previousButtons.Start, nextState.Buttons.Start);
            TrackButtonChange(GuideButtonLabel, previousButtons.Guide, nextState.Buttons.Guide);
            TrackButtonChange("Touchpad", previousButtons.Touchpad, nextState.Buttons.Touchpad);
            TrackButtonChange(LeftStickButtonLabel, previousButtons.LeftStick, nextState.Buttons.LeftStick);
            TrackButtonChange(RightStickButtonLabel, previousButtons.RightStick, nextState.Buttons.RightStick);
            TrackButtonChange(LeftTriggerLabel, previousLeftTrigger > 0.02f, nextState.LeftTrigger > 0.02f);
            TrackButtonChange(RightTriggerLabel, previousRightTrigger > 0.02f, nextState.RightTrigger > 0.02f);
            TrackButtonChange(DpadUpLabel, previousButtons.DpadUp, nextState.Buttons.DpadUp);
            TrackButtonChange(DpadDownLabel, previousButtons.DpadDown, nextState.Buttons.DpadDown);
            TrackButtonChange(DpadLeftLabel, previousButtons.DpadLeft, nextState.Buttons.DpadLeft);
            TrackButtonChange(DpadRightLabel, previousButtons.DpadRight, nextState.Buttons.DpadRight);
            TrackExtraButtonChanges(previousExtraButtons, nextState.ExtraButtons);

            previousButtons = CopyButtons(nextState.Buttons);
            previousExtraButtons = CopyExtraButtons(nextState.ExtraButtons);
            previousLeftTrigger = nextState.LeftTrigger;
            previousRightTrigger = nextState.RightTrigger;
        }

        private void TrackExtraButtonChanges(IList<ExtraButtonState> previous, IList<ExtraButtonState> current)
        {
            if (current == null)
            {
                return;
            }

            for (var index = 0; index < current.Count; index++)
            {
                var currentButton = current[index];
                var previousPressed = false;
                if (previous != null)
                {
                    for (var previousIndex = 0; previousIndex < previous.Count; previousIndex++)
                    {
                        if (previous[previousIndex].RawIndex == currentButton.RawIndex)
                        {
                            previousPressed = previous[previousIndex].IsPressed;
                            break;
                        }
                    }
                }

                TrackButtonChange(currentButton.Label, previousPressed, currentButton.IsPressed);
            }
        }

        private void TrackButtonChange(string inputName, bool previous, bool current)
        {
            if (previous == current)
            {
                return;
            }

            if (isLatencyTestRunning)
            {
                TrackLatencyTest(current);
            }

            if (!isInputLogEnabled)
            {
                return;
            }

            InputHistory.Insert(0, new InputHistoryItem
            {
                Timestamp = DateTime.Now,
                InputName = inputName,
                State = current ? "Pressed" : "Released"
            });

            while (InputHistory.Count > 80)
            {
                InputHistory.RemoveAt(InputHistory.Count - 1);
            }

            inputLogExportStatusLabel = string.Format(L("LOCCSM_Tester_InputLogEntriesFormat", "{0} entries ready to export."), InputHistory.Count);
            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
        }

        private void RefreshControllers()
        {
            var controllers = pollingService.GetControllers();
            if (controllers == null)
            {
                controllers = new GamepadControllerInfo[0];
            }

            if (ControllerListUnchanged(controllers))
            {
                TryApplyPendingControllerSelection();
                return;
            }

            var selectedInstanceId = selectedController != null ? (int?)selectedController.InstanceId : null;
            isRefreshingControllers = true;
            try
            {
                Controllers.Clear();
                foreach (var controller in controllers)
                {
                    Controllers.Add(controller);
                }

                if (Controllers.Count == 0)
                {
                    selectedController = null;
                    OnPropertyChanged("SelectedController");
                    OnPropertyChanged("IsControllerSelectorVisible");
                    OnPropertyChanged("HasMultipleControllers");
                    OnPropertyChanged("MappingStatusLabel");
                    return;
                }

                GamepadControllerInfo nextSelection = FindRequestedController();
                if (nextSelection != null)
                {
                    hasPendingControllerSelection = false;
                }
                else if (selectedInstanceId.HasValue)
                {
                    foreach (var controller in Controllers)
                    {
                        if (controller.InstanceId == selectedInstanceId.Value)
                        {
                            nextSelection = controller;
                            break;
                        }
                    }
                }

                if (nextSelection == null)
                {
                    nextSelection = Controllers[0];
                }

                selectedController = nextSelection;
                OnPropertyChanged("SelectedController");
                pollingService.SelectController(nextSelection.InstanceId);
            }
            finally
            {
                isRefreshingControllers = false;
            }

            OnPropertyChanged("IsControllerSelectorVisible");
            OnPropertyChanged("HasMultipleControllers");
            OnPropertyChanged("MappingStatusLabel");
        }

        private bool TryApplyPendingControllerSelection()
        {
            if (!hasPendingControllerSelection || Controllers == null || Controllers.Count == 0)
            {
                return false;
            }

            var match = FindRequestedController();
            if (match == null)
            {
                return false;
            }

            hasPendingControllerSelection = false;
            SelectedController = match;
            return true;
        }

        private GamepadControllerInfo FindRequestedController()
        {
            if (!hasPendingControllerSelection || Controllers == null || Controllers.Count == 0)
            {
                return null;
            }

            if (pendingSelectVendorId != 0 || pendingSelectProductId != 0)
            {
                foreach (var controller in Controllers)
                {
                    if (controller.VendorId == pendingSelectVendorId && controller.ProductId == pendingSelectProductId)
                    {
                        return controller;
                    }
                }

                foreach (var aliasProductId in ControllerDeviceIdentity.GetBluetoothAliasProductIds(
                    pendingSelectVendorId, pendingSelectProductId))
                {
                    foreach (var controller in Controllers)
                    {
                        if (controller.VendorId == pendingSelectVendorId && controller.ProductId == aliasProductId)
                        {
                            return controller;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(pendingSelectName))
            {
                foreach (var controller in Controllers)
                {
                    if (string.Equals(controller.Name, pendingSelectName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(controller.DisplayName, pendingSelectName, StringComparison.OrdinalIgnoreCase))
                    {
                        return controller;
                    }
                }
            }

            return null;
        }

        private bool ControllerListUnchanged(IReadOnlyList<GamepadControllerInfo> incoming)
        {
            if (Controllers.Count != incoming.Count)
            {
                return false;
            }

            for (var i = 0; i < incoming.Count; i++)
            {
                var found = false;
                for (var j = 0; j < Controllers.Count; j++)
                {
                    if (Controllers[j].InstanceId == incoming[i].InstanceId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanRunRumble()
        {
            return settings.EnableRumbleTests && State.IsConnected && !isRumbleRunning;
        }

        private void RunSimpleRumble(string label, ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            RunRumblePattern(label, () =>
            {
                pollingService.TryRumble(lowFrequency, highFrequency, durationMs);
                Thread.Sleep((int)durationMs + 80);
            });
        }

        private void TestPulseRumble()
        {
            RunRumblePattern(L("LOCCSM_Tester_Pulse", "Pulse"), () =>
            {
                for (var index = 0; index < 3; index++)
                {
                    pollingService.TryRumble(18000, 56000, 130);
                    Thread.Sleep(210);
                }
            });
        }

        private void TestAlternatingRumble()
        {
            RunRumblePattern(L("LOCCSM_Tester_Alternating", "Alternating"), () =>
            {
                for (var index = 0; index < 4; index++)
                {
                    pollingService.TryRumble(56000, 0, 150);
                    Thread.Sleep(210);
                    pollingService.TryRumble(0, 56000, 150);
                    Thread.Sleep(210);
                }
            });
        }

        private void TestRampRumble()
        {
            RunRumblePattern(L("LOCCSM_Tester_Ramp", "Ramp"), () =>
            {
                for (var step = 1; step <= 5; step++)
                {
                    var strength = (ushort)(step * 12000);
                    pollingService.TryRumble(strength, strength, 150);
                    Thread.Sleep(210);
                }
            });
        }

        private void TestBurstRumble()
        {
            RunRumblePattern(L("LOCCSM_Tester_Burst", "Burst"), () =>
            {
                for (var index = 0; index < 6; index++)
                {
                    pollingService.TryRumble(42000, 42000, 45);
                    Thread.Sleep(95);
                }
            });
        }

        private void RunRumblePattern(string label, Action pattern)
        {
            SetRumbleState(true, string.Format(L("LOCCSM_Tester_RumbleRunningFormat", "{0} running..."), label));
            Task.Run(() =>
            {
                try
                {
                    pattern();
                    pollingService.TryRumble(0, 0, 1);
                    SetRumbleState(false, string.Format(L("LOCCSM_Tester_RumbleCompleteFormat", "{0} complete."), label));
                }
                catch
                {
                    SetRumbleState(false, string.Format(L("LOCCSM_Tester_RumbleFailedFormat", "{0} failed."), label));
                }
            });
        }

        private void SetRumbleState(bool isRunning, string statusLabel)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                isRumbleRunning = isRunning;
                rumbleStatusLabel = statusLabel;
                OnPropertyChanged("IsRumbleRunning");
                OnPropertyChanged("IsFullscreenInputCaptureActive");
                NotifyThemeTestStateChanged();
                OnPropertyChanged("RumbleStatusLabel");
                RaiseRumbleCanExecuteChanged();
            }));
        }

        private void RaiseRumbleCanExecuteChanged()
        {
            rumbleCommand.RaiseCanExecuteChanged();
            lightRumbleCommand.RaiseCanExecuteChanged();
            mediumRumbleCommand.RaiseCanExecuteChanged();
            heavyRumbleCommand.RaiseCanExecuteChanged();
            lowMotorRumbleCommand.RaiseCanExecuteChanged();
            highMotorRumbleCommand.RaiseCanExecuteChanged();
            pulseRumbleCommand.RaiseCanExecuteChanged();
            alternatingRumbleCommand.RaiseCanExecuteChanged();
            rampRumbleCommand.RaiseCanExecuteChanged();
            burstRumbleCommand.RaiseCanExecuteChanged();
        }

        private void ResetDiagnostics()
        {
            isGuidedTestRunning = false;
            guidedTestStepIndex = 0;
            hasGuidedTestReport = false;
            isGuidedTestReportSuccess = false;
            guidedTestReportLabel = null;
            if (GuidedTestReportItems != null)
            {
                GuidedTestReportItems.Clear();
            }
            stickCaptureCompletedAutomatically = false;
            stickCaptureReachedLimit = false;
            restDriftDiagnostics.Reset();
            maxLeftStickMagnitude = 0d;
            maxRightStickMagnitude = 0d;
            maxLeftTrigger = 0f;
            maxRightTrigger = 0f;
            coveredButtons = new GamepadButtonState();
            leftStickDiagnostics.Reset();
            rightStickDiagnostics.Reset();
            leftStickTrail.Reset();
            rightStickTrail.Reset();
            ClearInputHistory();
            ResetCalibration();
            ResetLatency();
            NotifyStateChanged();
        }

        private void ClearInputHistory()
        {
            InputHistory.Clear();
            inputLogExportStatusLabel = L("LOCCSM_Tester_InputLogExportReady", "Enable input log and press buttons to collect entries.");
            OnPropertyChanged("InputHistory");
            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
        }

        private void StartCenterCalibration()
        {
            if (!State.IsConnected || isCenterCalibrationRunning)
            {
                return;
            }

            isCenterCalibrationRunning = true;
            centerCalibrationEndsAt = DateTime.UtcNow.AddMilliseconds(CenterCalibrationDurationMilliseconds);
            centerCalibrationSamples = 0;
            leftCenterXSum = 0d;
            leftCenterYSum = 0d;
            rightCenterXSum = 0d;
            rightCenterYSum = 0d;
            leftCenterMaxNoise = 0d;
            rightCenterMaxNoise = 0d;
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
            resetCalibrationCommand.RaiseCanExecuteChanged();
        }

        private void ToggleGuidedTest()
        {
            if (isGuidedTestRunning)
            {
                StopGuidedTest(false);
                return;
            }

            StartGuidedTest();
        }

        private void StartGuidedTest()
        {
            if (!State.IsConnected)
            {
                return;
            }

            ResetDiagnostics();
            isGuidedTestRunning = true;
            guidedTestStepIndex = 0;
            hasGuidedTestReport = false;
            isGuidedTestReportSuccess = false;
            guidedTestReportLabel = null;
            if (GuidedTestReportItems != null)
            {
                GuidedTestReportItems.Clear();
            }
            RefreshGuidedTestInputs();
            NotifyGuidedTestUi();
        }

        private void StopGuidedTest(bool completed)
        {
            if (!isGuidedTestRunning)
            {
                return;
            }

            isGuidedTestRunning = false;
            if (completed || IsGuidedTestFullyCovered())
            {
                if (GuidedTestInputs != null)
                {
                    guidedTestStepIndex = GuidedTestInputs.Count;
                }

                completed = true;
            }

            BuildGuidedTestReport(completed);
            RefreshGuidedTestInputs();
            NotifyGuidedTestUi();
        }

        private void BuildGuidedTestReport(bool completed)
        {
            if (GuidedTestReportItems == null)
            {
                GuidedTestReportItems = new ObservableCollection<GuidedTestReportItem>();
            }
            else
            {
                GuidedTestReportItems.Clear();
            }

            if (GuidedTestInputs == null || GuidedTestInputs.Count == 0)
            {
                hasGuidedTestReport = false;
                isGuidedTestReportSuccess = false;
                guidedTestReportLabel = null;
                return;
            }

            var total = GuidedTestInputs.Count;
            var coveredCount = Math.Max(0, Math.Min(guidedTestStepIndex, total));
            for (var index = 0; index < total; index++)
            {
                var label = GuidedTestInputs[index].Label;
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = GetGuidedInputLabel(GuidedTestInputs[index].Key);
                }

                GuidedTestReportItems.Add(new GuidedTestReportItem
                {
                    Label = label,
                    IsPassed = index < coveredCount
                });
            }

            var percent = (int)Math.Round(coveredCount * 100d / total);
            isGuidedTestReportSuccess = completed || coveredCount >= total;
            if (isGuidedTestReportSuccess)
            {
                guidedTestReportLabel = L("LOCCSM_Tester_GuidedTestReportNoneMissing", "No controls were left unverified.");
            }
            else
            {
                guidedTestReportLabel = string.Format(
                    L("LOCCSM_Tester_GuidedTestReportStopped", "Guided test stopped after {0} of {1} controls ({2}%)."),
                    coveredCount,
                    total,
                    percent);
            }

            hasGuidedTestReport = true;
        }

        private bool IsGuidedTestFullyCovered()
        {
            return GuidedTestInputs != null
                && GuidedTestInputs.Count > 0
                && guidedTestStepIndex >= GuidedTestInputs.Count;
        }

        private void NotifyGuidedTestUi()
        {
            OnPropertyChanged("IsGuidedTestRunning");
            OnPropertyChanged("HasGuidedTestReport");
            OnPropertyChanged("IsGuidedTestReportSuccess");
            OnPropertyChanged("GuidedTestProgress");
            OnPropertyChanged("GuidedTestButtonLabel");
            OnPropertyChanged("GuidedTestStatusLabel");
            OnPropertyChanged("GuidedTestNextInputLabel");
            OnPropertyChanged("GuidedTestActionLabel");
            OnPropertyChanged("GuidedTestReportLabel");
            OnPropertyChanged("GuidedTestReportItems");
        }

        private void OpenGuidedTest()
        {
            SelectedTabIndex = TabGuided;
        }

        private void ResetCalibration()
        {
            if (!CanResetCalibration())
            {
                return;
            }

            isCenterCalibrationRunning = false;
            centerCalibrationSamples = 0;
            leftCenterXSum = 0d;
            leftCenterYSum = 0d;
            rightCenterXSum = 0d;
            rightCenterYSum = 0d;
            leftCenterMaxNoise = 0d;
            rightCenterMaxNoise = 0d;
            calibratedLeftCenterX = 0d;
            calibratedLeftCenterY = 0d;
            calibratedRightCenterX = 0d;
            calibratedRightCenterY = 0d;
            calibratedLeftCenterNoise = 0d;
            calibratedRightCenterNoise = 0d;
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            OnPropertyChanged("LeftCalibrationCenterLabel");
            OnPropertyChanged("RightCalibrationCenterLabel");
            OnPropertyChanged("LeftRecommendedDeadzoneLabel");
            OnPropertyChanged("RightRecommendedDeadzoneLabel");
            OnPropertyChanged("LeftRecommendedDeadzonePercent");
            OnPropertyChanged("RightRecommendedDeadzonePercent");
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
            resetCalibrationCommand.RaiseCanExecuteChanged();
        }

        private bool CanResetCalibration()
        {
            return isCenterCalibrationRunning || centerCalibrationSamples > 0;
        }

        private bool CanResetStickRange()
        {
            return !isStickCaptureRunning
                && (leftStickDiagnostics.SampleCount > 0
                    || rightStickDiagnostics.SampleCount > 0
                    || maxLeftStickMagnitude > 0d
                    || maxRightStickMagnitude > 0d
                    || stickCaptureCompletedAutomatically
                    || stickCaptureReachedLimit);
        }

        private void ResetStickRangeDiagnostics()
        {
            if (isStickCaptureRunning)
            {
                return;
            }

            stickCaptureCompletedAutomatically = false;
            stickCaptureReachedLimit = false;
            maxLeftStickMagnitude = 0d;
            maxRightStickMagnitude = 0d;
            leftStickDiagnostics.Reset();
            rightStickDiagnostics.Reset();
            leftStickTrail.Reset();
            rightStickTrail.Reset();
            OnPropertyChanged("LeftStickPathGeometry");
            OnPropertyChanged("RightStickPathGeometry");
            OnPropertyChanged("LeftStickTrailRecentGeometry");
            OnPropertyChanged("LeftStickTrailMidGeometry");
            OnPropertyChanged("LeftStickTrailFadeGeometry");
            OnPropertyChanged("RightStickTrailRecentGeometry");
            OnPropertyChanged("RightStickTrailMidGeometry");
            OnPropertyChanged("RightStickTrailFadeGeometry");
            OnPropertyChanged("LeftStickCircularCoverageGeometry");
            OnPropertyChanged("RightStickCircularCoverageGeometry");
            OnPropertyChanged("LeftStickCircularCoveragePercent");
            OnPropertyChanged("RightStickCircularCoveragePercent");
            OnPropertyChanged("LeftStickMaxReachPercent");
            OnPropertyChanged("RightStickMaxReachPercent");
            OnPropertyChanged("LeftStickCircularCoverageLabel");
            OnPropertyChanged("RightStickCircularCoverageLabel");
            OnPropertyChanged("LeftStickPathSampleLabel");
            OnPropertyChanged("RightStickPathSampleLabel");
            OnPropertyChanged("LeftStickMaxReachLabel");
            OnPropertyChanged("RightStickMaxReachLabel");
            OnPropertyChanged("LeftStickAxisRangeLabel");
            OnPropertyChanged("RightStickAxisRangeLabel");
            OnPropertyChanged("LeftStickAverageMagnitudeLabel");
            OnPropertyChanged("RightStickAverageMagnitudeLabel");
            OnPropertyChanged("LeftRangeQualityLabel");
            OnPropertyChanged("RightRangeQualityLabel");
            OnPropertyChanged("LeftRangeQualityPercent");
            OnPropertyChanged("RightRangeQualityPercent");
            OnPropertyChanged("LeftRangeDisplayProgress");
            OnPropertyChanged("RightRangeDisplayProgress");
            OnPropertyChanged("LeftRangeConfidenceLabel");
            OnPropertyChanged("RightRangeConfidenceLabel");
            OnPropertyChanged("HealthRangeFactorLabel");
            OnPropertyChanged("StickCaptureStatusLabel");
            OnPropertyChanged("DiagnosticRadarValues");
            resetStickRangeCommand.RaiseCanExecuteChanged();
        }

        private void ResetLatency()
        {
            lastStateSampleAt = null;
            lastInputEventAt = null;
            currentPollingIntervalMs = 0d;
            pollingIntervalSumMs = 0d;
            pollingIntervalMinMs = double.MaxValue;
            pollingIntervalMaxMs = 0d;
            pollingIntervalSamples = 0;
            inputEventIntervalSumMs = 0d;
            inputEventIntervalMinMs = double.MaxValue;
            inputEventIntervalMaxMs = 0d;
            inputEventIntervalSamples = 0;
            currentInputEventIntervalMs = 0d;
            latencyRateHistory.Clear();
            hasLatencyTestStarted = false;
            isLatencyTestRunning = false;
            lastLatencyMs = 0d;
            bestLatencyMs = 0d;
            latencyTestSumMs = 0d;
            latencyTestSamples = 0;
            latencyTestDurationSeconds = 0d;
            latencyStatusLabel = L("LOCCSM_Tester_LatencyWaiting", "Waiting for input changes.");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("IsLatencyTestRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            OnPropertyChanged("LatencyRateGraphValues");
            OnPropertyChanged("DiagnosticRadarValues");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startStickCaptureCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ToggleLatencyTest()
        {
            if (isLatencyTestRunning)
            {
                StopLatencyTest();
                return;
            }

            StartLatencyTest();
        }

        private void ToggleButtonCapture()
        {
            isButtonCaptureRunning = !isButtonCaptureRunning;
            RefreshFullscreenDisplayState();
            OnPropertyChanged("IsButtonCaptureRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("ButtonCaptureButtonLabel");
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startStickCaptureCommand.RaiseCanExecuteChanged();
            startLatencyTestCommand.RaiseCanExecuteChanged();
        }

        private void ToggleStickCapture()
        {
            if (isStickCaptureRunning)
            {
                StopStickCapture();
                return;
            }

            if (!State.IsConnected)
            {
                OnPropertyChanged("StickCaptureStatusLabel");
                return;
            }

            if (isLatencyTestRunning)
            {
                StopLatencyTest();
            }

            if (isButtonCaptureRunning)
            {
                isButtonCaptureRunning = false;
                OnPropertyChanged("IsButtonCaptureRunning");
                OnPropertyChanged("ButtonCaptureButtonLabel");
            }

            ResetStickRangeDiagnostics();
            restDriftDiagnostics.Reset();
            stickCaptureCompletedAutomatically = false;
            stickCaptureReachedLimit = false;
            isStickCaptureRunning = true;
            RefreshFullscreenDisplayState();
            OnPropertyChanged("IsStickCaptureRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("StickCaptureButtonLabel");
            OnPropertyChanged("StickCaptureStatusLabel");
            OnPropertyChanged("SessionRestDriftLabel");
            startStickCaptureCommand.RaiseCanExecuteChanged();
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetStickRangeCommand.RaiseCanExecuteChanged();
        }

        private void StopStickCapture()
        {
            if (!isStickCaptureRunning)
            {
                return;
            }

            isStickCaptureRunning = false;
            RefreshFullscreenDisplayState();
            OnPropertyChanged("IsStickCaptureRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("StickCaptureButtonLabel");
            OnPropertyChanged("StickCaptureStatusLabel");
            startStickCaptureCommand.RaiseCanExecuteChanged();
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetStickRangeCommand.RaiseCanExecuteChanged();
        }

        private void EvaluateStickCaptureCompletion()
        {
            if (leftStickDiagnostics.CoveragePercent >= 100 && rightStickDiagnostics.CoveragePercent >= 100)
            {
                stickCaptureCompletedAutomatically = true;
                StopStickCapture();
                return;
            }

            if (leftStickDiagnostics.HasReachedSamplingLimit || rightStickDiagnostics.HasReachedSamplingLimit)
            {
                stickCaptureReachedLimit = true;
                StopStickCapture();
            }
        }

        private void StartLatencyTest()
        {
            if (!State.IsConnected)
            {
                return;
            }

            isLatencyTestRunning = true;
            isButtonCaptureRunning = false;
            isStickCaptureRunning = false;
            RefreshFullscreenDisplayState();
            hasLatencyTestStarted = true;
            lastStateSampleAt = null;
            lastInputEventAt = null;
            currentInputEventIntervalMs = 0d;
            inputEventIntervalSumMs = 0d;
            inputEventIntervalMinMs = double.MaxValue;
            inputEventIntervalMaxMs = 0d;
            inputEventIntervalSamples = 0;
            lastLatencyMs = 0d;
            bestLatencyMs = 0d;
            latencyTestSumMs = 0d;
            latencyTestSamples = 0;
            latencyTestDurationSeconds = 0d;
            latencyRateHistory.Clear();
            latencyTestStartedAt = DateTime.UtcNow;
            latencyStatusLabel = L("LOCCSM_Tester_LatencyArmed", "Latency test armed. Press any controller button.");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("IsLatencyTestRunning");
            OnPropertyChanged("IsButtonCaptureRunning");
            OnPropertyChanged("IsStickCaptureRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("ButtonCaptureButtonLabel");
            OnPropertyChanged("StickCaptureButtonLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            OnPropertyChanged("LatencyRateGraphValues");
            OnPropertyChanged("DiagnosticRadarValues");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startStickCaptureCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void StopLatencyTest()
        {
            if (!isLatencyTestRunning)
            {
                return;
            }

            isLatencyTestRunning = false;
            latencyTestDurationSeconds = Math.Max(0d, (DateTime.UtcNow - latencyTestStartedAt).TotalSeconds);
            latencyStatusLabel = latencyTestSamples == 0
                ? L("LOCCSM_Tester_LatencyStoppedNoSample", "Latency test stopped. No button press was captured.")
                : string.Format(L("LOCCSM_Tester_LatencyStoppedFormat", "Latency test stopped. Last captured value: {0:0} ms."), lastLatencyMs);
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("IsLatencyTestRunning");
            OnPropertyChanged("IsFullscreenInputCaptureActive");
            NotifyThemeTestStateChanged();
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            startButtonCaptureCommand.RaiseCanExecuteChanged();
            startStickCaptureCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ExportReport()
        {
            try
            {
                var fileName = string.Format("GamepadTester-report-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCCSM_Tester_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildReportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_ReportExportedFormat", "Report exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_ReportExportFailedFormat", "Could not export report: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
        }

        private void ExportInputLog()
        {
            try
            {
                if (InputHistory.Count == 0)
                {
                    inputLogExportStatusLabel = L("LOCCSM_Tester_InputLogExportEmpty", "No input log entries to export yet.");
                    OnPropertyChanged("InputLogExportStatusLabel");
                    return;
                }

                var fileName = string.Format("GamepadTester-input-log-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    inputLogExportStatusLabel = L("LOCCSM_Tester_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("InputLogExportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildInputLogText(), Encoding.UTF8);
                inputLogExportStatusLabel = string.Format(L("LOCCSM_Tester_InputLogExportedFormat", "Input log exported to {0}"), path);
            }
            catch (Exception ex)
            {
                inputLogExportStatusLabel = string.Format(L("LOCCSM_Tester_InputLogExportFailedFormat", "Could not export input log: {0}"), ex.Message);
            }

            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
        }

        private void ExportLatencyData()
        {
            try
            {
                if (inputEventIntervalSamples == 0)
                {
                    exportReportStatusLabel = L("LOCCSM_Tester_LatencyExportEmpty", "No latency samples to export yet.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                var fileName = string.Format("GamepadTester-latency-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCCSM_Tester_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildLatencyExportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_LatencyExportedFormat", "Latency data exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_LatencyExportFailedFormat", "Could not export latency data: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ExportStickData()
        {
            try
            {
                var fileName = string.Format("GamepadTester-sticks-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCCSM_Tester_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildStickExportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_SticksExportedFormat", "Stick data exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCCSM_Tester_SticksExportFailedFormat", "Could not export stick data: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
        }

        private void ExportCompatibilityReport()
        {
            try
            {
                var fileName = string.Format("GamepadTester-compatibility-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    compatibilityReportStatusLabel = L("LOCCSM_Tester_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("CompatibilityReportStatusLabel");
                    return;
                }

                var report = GamepadCompatibilityReportBuilder.Build(
                    State,
                    SelectedController,
                    HealthConfidenceLabel,
                    LeftRangeConfidenceLabel,
                    RightRangeConfidenceLabel,
                    LatencyConfidenceLabel);
                File.WriteAllText(path, report, Encoding.UTF8);
                compatibilityReportStatusLabel = string.Format(
                    L("LOCCSM_Tester_CompatibilityReportExportedFormat", "Compatibility report exported to {0}"),
                    path);
            }
            catch (Exception ex)
            {
                compatibilityReportStatusLabel = string.Format(
                    L("LOCCSM_Tester_CompatibilityReportFailedFormat", "Could not export compatibility report: {0}"),
                    ex.Message);
            }

            OnPropertyChanged("CompatibilityReportStatusLabel");
        }

        private static string PromptExportPath(string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true
            };

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
            {
                dialog.InitialDirectory = documents;
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private string BuildLatencyExportText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester latency data");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("[Summary]");
            log.AppendLine(string.Format("Current rate: {0}", PollingRateCurrentLabel));
            log.AppendLine(string.Format("Max rate: {0}", PollingRateMaxValueLabel));
            log.AppendLine(string.Format("Average rate: {0}", PollingRateAverageValueLabel));
            log.AppendLine(string.Format("Estimated interval: {0}", EstimatedDelayLabel));
            log.AppendLine(string.Format("Jitter: {0}", PollingJitterLabel));
            log.AppendLine(string.Format("Samples: {0}", inputEventIntervalSamples));
            log.AppendLine(string.Format("Confidence: {0}", LatencyConfidenceLabel));
            log.AppendLine(string.Format("Best interval: {0:0.0} ms", inputEventIntervalMinMs == double.MaxValue ? 0d : inputEventIntervalMinMs));
            log.AppendLine(string.Format("Worst interval: {0:0.0} ms", inputEventIntervalMaxMs));
            log.AppendLine(string.Format("Average interval: {0:0.0} ms", inputEventIntervalSamples == 0 ? 0d : inputEventIntervalSumMs / inputEventIntervalSamples));
            log.AppendLine();
            log.AppendLine("[Recent rates]");
            foreach (var rate in latencyRateHistory)
            {
                log.AppendLine(string.Format("{0:0.0} Hz", rate));
            }

            return log.ToString();
        }

        private string BuildStickExportText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester stick diagnostics");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("[Left stick]");
            log.AppendLine(LeftStickVector);
            log.AppendLine(LeftStickDriftStatus);
            log.AppendLine(LeftStickAngleLabel);
            log.AppendLine(LeftStickCurrentMagnitudeLabel);
            log.AppendLine(LeftStickMaxReachLabel);
            log.AppendLine(LeftStickAxisRangeLabel);
            log.AppendLine(LeftStickAverageMagnitudeLabel);
            log.AppendLine(LeftStickCircularCoverageLabel);
            log.AppendLine(LeftStickPathSampleLabel);
            log.AppendLine(LeftRangeQualityLabel);
            log.AppendLine(string.Format("Confidence: {0}", LeftRangeConfidenceLabel));
            log.AppendLine();
            log.AppendLine("[Right stick]");
            log.AppendLine(RightStickVector);
            log.AppendLine(RightStickDriftStatus);
            log.AppendLine(RightStickAngleLabel);
            log.AppendLine(RightStickCurrentMagnitudeLabel);
            log.AppendLine(RightStickMaxReachLabel);
            log.AppendLine(RightStickAxisRangeLabel);
            log.AppendLine(RightStickAverageMagnitudeLabel);
            log.AppendLine(RightStickCircularCoverageLabel);
            log.AppendLine(RightStickPathSampleLabel);
            log.AppendLine(RightRangeQualityLabel);
            log.AppendLine(string.Format("Confidence: {0}", RightRangeConfidenceLabel));
            log.AppendLine();
            log.AppendLine("[Calibration]");
            log.AppendLine(CalibrationStatusLabel);
            log.AppendLine(LeftCalibrationCenterLabel);
            log.AppendLine(RightCalibrationCenterLabel);
            log.AppendLine(LeftRecommendedDeadzoneLabel);
            log.AppendLine(RightRecommendedDeadzoneLabel);

            return log.ToString();
        }

        private string BuildInputLogText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester input log");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("Timestamp\tInput\tState");

            for (var i = InputHistory.Count - 1; i >= 0; i--)
            {
                var item = InputHistory[i];
                log.AppendLine(string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}\t{1}\t{2}", item.Timestamp, item.InputName, item.State));
            }

            return log.ToString();
        }

        private string BuildReportText()
        {
            var report = new StringBuilder();
            report.AppendLine("Gamepad Tester report");
            report.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            report.AppendLine();

            report.AppendLine("[Device]");
            report.AppendLine(string.Format("Name: {0}", State.ControllerName));
            report.AppendLine(string.Format("Display name: {0}", DeviceModelLabel));
            report.AppendLine(string.Format("VID/PID: {0}", string.IsNullOrWhiteSpace(DeviceIdLabel) ? "Unknown" : DeviceIdLabel));
            report.AppendLine(string.Format("Layout: {0}", State.Layout));
            if (State.Layout == GamepadLayout.EightBitDo)
            {
                report.AppendLine(string.Format("8BitDo model: {0}", State.EightBitDoModel));
            }
            report.AppendLine();

            report.AppendLine("[Summary]");
            report.AppendLine(string.Format("Health score: {0}", HealthScoreDisplayLabel));
            report.AppendLine(string.Format("Health label: {0}", HealthLabel));
            report.AppendLine(string.Format("Confidence: {0}", HealthConfidenceLabel));
            report.AppendLine(HealthSummaryLabel);
            report.AppendLine(HealthDriftFactorLabel);
            report.AppendLine(HealthRangeFactorLabel);
            report.AppendLine(HealthCoverageFactorLabel);
            report.AppendLine();

            report.AppendLine("[Current input]");
            report.AppendLine(string.Format("Active buttons: {0}", ActiveButtonCount));
            report.AppendLine(string.Format("Left stick: {0} | {1}", LeftStickVector, LeftStickDriftStatus));
            report.AppendLine(string.Format("Right stick: {0} | {1}", RightStickVector, RightStickDriftStatus));
            report.AppendLine(string.Format("Left trigger: {0}%", LeftTriggerPercent));
            report.AppendLine(string.Format("Right trigger: {0}%", RightTriggerPercent));
            report.AppendLine();

            report.AppendLine("[Session checks]");
            report.AppendLine(string.Format("Progress: {0}%", QuickTestProgress));
            report.AppendLine(ButtonCoverageLabel);
            report.AppendLine(AnalogCoverageLabel);
            report.AppendLine(string.Format("Missing: {0}", QuickTestMissingLabel));
            report.AppendLine();

            report.AppendLine("[Sticks]");
            report.AppendLine(string.Format("Left circular coverage: {0}", LeftStickCircularCoverageLabel));
            report.AppendLine(string.Format("Left max reach: {0}", LeftStickMaxReachLabel));
            report.AppendLine(string.Format("Left range: {0}", LeftStickAxisRangeLabel));
            report.AppendLine(string.Format("Right circular coverage: {0}", RightStickCircularCoverageLabel));
            report.AppendLine(string.Format("Right max reach: {0}", RightStickMaxReachLabel));
            report.AppendLine(string.Format("Right range: {0}", RightStickAxisRangeLabel));
            report.AppendLine();

            report.AppendLine("[Calibration]");
            report.AppendLine(CalibrationStatusLabel);
            report.AppendLine(string.Format("Left center: {0}", LeftCalibrationCenterLabel));
            report.AppendLine(string.Format("Left deadzone: {0}", LeftRecommendedDeadzoneLabel));
            report.AppendLine(string.Format("Right center: {0}", RightCalibrationCenterLabel));
            report.AppendLine(string.Format("Right deadzone: {0}", RightRecommendedDeadzoneLabel));
            report.AppendLine();

            report.AppendLine("[Latency]");
            report.AppendLine(string.Format("Manual latency: {0}", LatencyResultLabel));
            report.AppendLine(LatencyStatsLabel);
            report.AppendLine(PollingLatencyAverageLabel);
            report.AppendLine(InputEventLatencyAverageLabel);

            return report.ToString();
        }

        private void UpdateCenterCalibration(GamepadState nextState)
        {
            if (!isCenterCalibrationRunning)
            {
                return;
            }

            centerCalibrationSamples++;
            leftCenterXSum += nextState.LeftStick.X;
            leftCenterYSum += nextState.LeftStick.Y;
            rightCenterXSum += nextState.RightStick.X;
            rightCenterYSum += nextState.RightStick.Y;
            leftCenterMaxNoise = Math.Max(leftCenterMaxNoise, nextState.LeftStick.Magnitude);
            rightCenterMaxNoise = Math.Max(rightCenterMaxNoise, nextState.RightStick.Magnitude);

            if (DateTime.UtcNow < centerCalibrationEndsAt)
            {
                return;
            }

            isCenterCalibrationRunning = false;
            calibratedLeftCenterX = leftCenterXSum / Math.Max(1, centerCalibrationSamples);
            calibratedLeftCenterY = leftCenterYSum / Math.Max(1, centerCalibrationSamples);
            calibratedRightCenterX = rightCenterXSum / Math.Max(1, centerCalibrationSamples);
            calibratedRightCenterY = rightCenterYSum / Math.Max(1, centerCalibrationSamples);
            calibratedLeftCenterNoise = leftCenterMaxNoise;
            calibratedRightCenterNoise = rightCenterMaxNoise;
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
            resetCalibrationCommand.RaiseCanExecuteChanged();
        }

        private void UpdateLatency(GamepadState nextState)
        {
            if (!isLatencyTestRunning)
            {
                return;
            }

            if (!nextState.IsConnected)
            {
                lastStateSampleAt = null;
                return;
            }

            var now = DateTime.UtcNow;
            if (lastStateSampleAt.HasValue)
            {
                currentPollingIntervalMs = (now - lastStateSampleAt.Value).TotalMilliseconds;
                if (currentPollingIntervalMs > 0d && currentPollingIntervalMs < 1000d)
                {
                    pollingIntervalSamples++;
                    pollingIntervalSumMs += currentPollingIntervalMs;
                    pollingIntervalMinMs = Math.Min(pollingIntervalMinMs, currentPollingIntervalMs);
                    pollingIntervalMaxMs = Math.Max(pollingIntervalMaxMs, currentPollingIntervalMs);
                }
            }

            lastStateSampleAt = now;
        }

        private static string GetHzLabel(double intervalMs)
        {
            if (intervalMs <= 0d || intervalMs >= 10000d || double.IsNaN(intervalMs) || double.IsInfinity(intervalMs))
            {
                return "- Hz";
            }

            var hz = 1000d / intervalMs;
            return hz < 10d
                ? string.Format("{0:0.0} Hz", hz)
                : string.Format("{0:0} Hz", hz);
        }

        private void TrackLatencyTest(bool isPressed)
        {
            if (!isLatencyTestRunning || !isPressed)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (!lastInputEventAt.HasValue)
            {
                lastInputEventAt = now;
                latencyStatusLabel = L("LOCCSM_Tester_FirstInputObserved", "First input observed. Press repeatedly for an average.");
                OnPropertyChanged("LatencyStatusLabel");
                return;
            }

            lastLatencyMs = Math.Max(0d, (now - lastInputEventAt.Value).TotalMilliseconds);
            currentInputEventIntervalMs = lastLatencyMs;
            lastInputEventAt = now;
            if (lastLatencyMs <= 0d || lastLatencyMs >= 10000d)
            {
                return;
            }

            latencyTestSamples++;
            latencyTestSumMs += lastLatencyMs;
            bestLatencyMs = latencyTestSamples == 1 ? lastLatencyMs : Math.Min(bestLatencyMs, lastLatencyMs);
            inputEventIntervalSamples++;
            inputEventIntervalSumMs += lastLatencyMs;
            inputEventIntervalMinMs = Math.Min(inputEventIntervalMinMs, lastLatencyMs);
            inputEventIntervalMaxMs = Math.Max(inputEventIntervalMaxMs, lastLatencyMs);

            latencyRateHistory.Enqueue(1000d / lastLatencyMs);
            while (latencyRateHistory.Count > LatencyGraphMaxSamples)
            {
                latencyRateHistory.Dequeue();
            }

            latencyStatusLabel = string.Format(L("LOCCSM_Tester_LatencyCapturedFormat", "Captured {0:0} ms."), lastLatencyMs);
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            OnPropertyChanged("LatencyRateGraphValues");
            OnPropertyChanged("DiagnosticRadarValues");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void UpdateCoverage(GamepadState nextState)
        {
            maxLeftTrigger = Math.Max(maxLeftTrigger, nextState.LeftTrigger);
            maxRightTrigger = Math.Max(maxRightTrigger, nextState.RightTrigger);
            maxLeftStickMagnitude = Math.Max(maxLeftStickMagnitude, nextState.LeftStick.Magnitude);
            maxRightStickMagnitude = Math.Max(maxRightStickMagnitude, nextState.RightStick.Magnitude);

            coveredButtons.South = coveredButtons.South || nextState.Buttons.South;
            coveredButtons.East = coveredButtons.East || nextState.Buttons.East;
            coveredButtons.West = coveredButtons.West || nextState.Buttons.West;
            coveredButtons.North = coveredButtons.North || nextState.Buttons.North;
            coveredButtons.LeftShoulder = coveredButtons.LeftShoulder || nextState.Buttons.LeftShoulder;
            coveredButtons.RightShoulder = coveredButtons.RightShoulder || nextState.Buttons.RightShoulder;
            coveredButtons.Back = coveredButtons.Back || nextState.Buttons.Back;
            coveredButtons.Start = coveredButtons.Start || nextState.Buttons.Start;
            coveredButtons.Guide = coveredButtons.Guide || nextState.Buttons.Guide;
            coveredButtons.LeftStick = coveredButtons.LeftStick || nextState.Buttons.LeftStick;
            coveredButtons.RightStick = coveredButtons.RightStick || nextState.Buttons.RightStick;
            coveredButtons.DpadUp = coveredButtons.DpadUp || nextState.Buttons.DpadUp;
            coveredButtons.DpadDown = coveredButtons.DpadDown || nextState.Buttons.DpadDown;
            coveredButtons.DpadLeft = coveredButtons.DpadLeft || nextState.Buttons.DpadLeft;
            coveredButtons.DpadRight = coveredButtons.DpadRight || nextState.Buttons.DpadRight;
        }

        private void UpdateGuidedTestProgress(GamepadState nextState)
        {
            if (!isGuidedTestRunning || GuidedTestInputs == null || guidedTestStepIndex >= GuidedTestInputs.Count)
            {
                return;
            }

            var currentKey = GuidedTestInputs[guidedTestStepIndex].Key;
            if (!IsGuidedInputActive(currentKey, nextState))
            {
                return;
            }

            guidedTestStepIndex++;
            RefreshGuidedTestInputs();
            if (guidedTestStepIndex >= GuidedTestInputs.Count)
            {
                StopGuidedTest(true);
                return;
            }

            NotifyGuidedTestUi();
        }

        private bool IsGuidedInputActive(string key, GamepadState nextState)
        {
            switch (key)
            {
                case "South":
                    return nextState.Buttons.South;
                case "East":
                    return nextState.Buttons.East;
                case "West":
                    return nextState.Buttons.West;
                case "North":
                    return nextState.Buttons.North;
                case "LeftShoulder":
                    return nextState.Buttons.LeftShoulder;
                case "RightShoulder":
                    return nextState.Buttons.RightShoulder;
                case "LeftTrigger":
                    return nextState.LeftTrigger >= TriggerFullPressThreshold;
                case "RightTrigger":
                    return nextState.RightTrigger >= TriggerFullPressThreshold;
                case "LeftStick":
                    return nextState.Buttons.LeftStick;
                case "RightStick":
                    return nextState.Buttons.RightStick;
                case "Back":
                    return nextState.Buttons.Back;
                case "Start":
                    return nextState.Buttons.Start;
                case "Guide":
                    return nextState.Buttons.Guide;
                case "DpadUp":
                    return nextState.Buttons.DpadUp;
                case "DpadDown":
                    return nextState.Buttons.DpadDown;
                case "DpadLeft":
                    return nextState.Buttons.DpadLeft;
                case "DpadRight":
                    return nextState.Buttons.DpadRight;
                case "LeftStickRange":
                    return nextState.LeftStick.Magnitude >= StickEdgeThreshold;
                case "RightStickRange":
                    return nextState.RightStick.Magnitude >= StickEdgeThreshold;
                default:
                    return false;
            }
        }

        private static void AddMissingButton(ICollection<string> missing, bool isCovered, string label)
        {
            if (!isCovered)
            {
                missing.Add(label);
            }
        }

        private List<string> GetMissingInputLabels()
        {
            var missing = new List<string>();
            AddMissingButton(missing, coveredButtons.South, SouthLabel);
            AddMissingButton(missing, coveredButtons.East, EastLabel);
            AddMissingButton(missing, coveredButtons.West, WestLabel);
            AddMissingButton(missing, coveredButtons.North, NorthLabel);
            AddMissingButton(missing, coveredButtons.LeftShoulder, LeftShoulderLabel);
            AddMissingButton(missing, coveredButtons.RightShoulder, RightShoulderLabel);
            AddMissingButton(missing, coveredButtons.LeftStick, LeftStickButtonLabel);
            AddMissingButton(missing, coveredButtons.RightStick, RightStickButtonLabel);
            AddMissingButton(missing, coveredButtons.Back, BackButtonLabel);
            AddMissingButton(missing, coveredButtons.Start, StartButtonLabel);
            AddMissingButton(missing, coveredButtons.Guide, GuideButtonLabel);
            AddMissingButton(missing, coveredButtons.DpadUp, DpadUpLabel);
            AddMissingButton(missing, coveredButtons.DpadDown, DpadDownLabel);
            AddMissingButton(missing, coveredButtons.DpadLeft, DpadLeftLabel);
            AddMissingButton(missing, coveredButtons.DpadRight, DpadRightLabel);

            if (maxLeftTrigger < TriggerFullPressThreshold)
            {
                missing.Add(LeftTriggerLabel + " 100%");
            }

            if (maxRightTrigger < TriggerFullPressThreshold)
            {
                missing.Add(RightTriggerLabel + " 100%");
            }

            if (maxLeftStickMagnitude < StickEdgeThreshold)
            {
                missing.Add("LS edge");
            }

            if (maxRightStickMagnitude < StickEdgeThreshold)
            {
                missing.Add("RS edge");
            }

            return missing;
        }

        private void RefreshGuidedTestInputs()
        {
            if (GuidedTestInputs == null)
            {
                return;
            }

            var currentKey = GetCurrentGuidedInputKey();
            for (var index = 0; index < GuidedTestInputs.Count; index++)
            {
                var item = GuidedTestInputs[index];
                item.Label = GetGuidedInputLabel(item.Key);
                item.IsCovered = index < guidedTestStepIndex;
                item.IsCurrent = isGuidedTestRunning && State.IsConnected && item.Key == currentKey;
            }
        }

        private string GetCurrentGuidedInputKey()
        {
            if (GuidedTestInputs == null || guidedTestStepIndex < 0 || guidedTestStepIndex >= GuidedTestInputs.Count)
            {
                return null;
            }

            return GuidedTestInputs[guidedTestStepIndex].Key;
        }

        private string GetCurrentGuidedInputLabel()
        {
            var key = GetCurrentGuidedInputKey();
            return key == null ? null : GetGuidedInputLabel(key);
        }

        private string GetGuidedInputLabel(string key)
        {
            switch (key)
            {
                case "South":
                    return SouthLabel;
                case "East":
                    return EastLabel;
                case "West":
                    return WestLabel;
                case "North":
                    return NorthLabel;
                case "LeftShoulder":
                    return LeftShoulderLabel;
                case "RightShoulder":
                    return RightShoulderLabel;
                case "LeftTrigger":
                    return LeftTriggerLabel + " 100%";
                case "RightTrigger":
                    return RightTriggerLabel + " 100%";
                case "LeftStick":
                    return LeftStickButtonLabel;
                case "RightStick":
                    return RightStickButtonLabel;
                case "Back":
                    return BackButtonLabel;
                case "Start":
                    return StartButtonLabel;
                case "Guide":
                    return GuideButtonLabel;
                case "DpadUp":
                    return DpadUpLabel;
                case "DpadDown":
                    return DpadDownLabel;
                case "DpadLeft":
                    return DpadLeftLabel;
                case "DpadRight":
                    return DpadRightLabel;
                case "LeftStickRange":
                    return "LS edge";
                case "RightStickRange":
                    return "RS edge";
                default:
                    return key;
            }
        }

        private bool IsGuidedInputCovered(string key)
        {
            switch (key)
            {
                case "South":
                    return coveredButtons.South;
                case "East":
                    return coveredButtons.East;
                case "West":
                    return coveredButtons.West;
                case "North":
                    return coveredButtons.North;
                case "LeftShoulder":
                    return coveredButtons.LeftShoulder;
                case "RightShoulder":
                    return coveredButtons.RightShoulder;
                case "LeftTrigger":
                    return maxLeftTrigger >= TriggerFullPressThreshold;
                case "RightTrigger":
                    return maxRightTrigger >= TriggerFullPressThreshold;
                case "LeftStick":
                    return coveredButtons.LeftStick;
                case "RightStick":
                    return coveredButtons.RightStick;
                case "Back":
                    return coveredButtons.Back;
                case "Start":
                    return coveredButtons.Start;
                case "Guide":
                    return coveredButtons.Guide;
                case "DpadUp":
                    return coveredButtons.DpadUp;
                case "DpadDown":
                    return coveredButtons.DpadDown;
                case "DpadLeft":
                    return coveredButtons.DpadLeft;
                case "DpadRight":
                    return coveredButtons.DpadRight;
                case "LeftStickRange":
                    return maxLeftStickMagnitude >= StickEdgeThreshold;
                case "RightStickRange":
                    return maxRightStickMagnitude >= StickEdgeThreshold;
                default:
                    return false;
            }
        }

        private static GamepadButtonState CopyButtons(GamepadButtonState buttons)
        {
            return new GamepadButtonState
            {
                South = buttons.South,
                East = buttons.East,
                West = buttons.West,
                North = buttons.North,
                LeftShoulder = buttons.LeftShoulder,
                RightShoulder = buttons.RightShoulder,
                Back = buttons.Back,
                Start = buttons.Start,
                Guide = buttons.Guide,
                Touchpad = buttons.Touchpad,
                LeftStick = buttons.LeftStick,
                RightStick = buttons.RightStick,
                DpadUp = buttons.DpadUp,
                DpadDown = buttons.DpadDown,
                DpadLeft = buttons.DpadLeft,
                DpadRight = buttons.DpadRight
            };
        }

        private static List<ExtraButtonState> CopyExtraButtons(IList<ExtraButtonState> buttons)
        {
            var copy = new List<ExtraButtonState>();
            if (buttons == null)
            {
                return copy;
            }

            for (var index = 0; index < buttons.Count; index++)
            {
                copy.Add(new ExtraButtonState
                {
                    RawIndex = buttons[index].RawIndex,
                    Label = buttons[index].Label,
                    IsPressed = buttons[index].IsPressed
                });
            }

            return copy;
        }

        private GamepadState CreateDisplayState(GamepadState source)
        {
            if (source == null || !isFullscreenSimplifiedMode)
            {
                return source ?? new GamepadState();
            }

            var showButtons = isButtonCaptureRunning;
            var showSticks = isButtonCaptureRunning || isStickCaptureRunning;
            var display = new GamepadState
            {
                IsConnected = source.IsConnected,
                ControllerName = source.ControllerName,
                VendorId = source.VendorId,
                ProductId = source.ProductId,
                Layout = source.Layout,
                EightBitDoModel = source.EightBitDoModel,
                SdlVersion = source.SdlVersion,
                SdlGuid = source.SdlGuid,
                SdlMapping = source.SdlMapping,
                AxisCount = source.AxisCount,
                ButtonCount = source.ButtonCount,
                HatCount = source.HatCount,
                LeftStick = showSticks
                    ? new StickState { X = source.LeftStick.X, Y = source.LeftStick.Y }
                    : new StickState(),
                RightStick = showSticks
                    ? new StickState { X = source.RightStick.X, Y = source.RightStick.Y }
                    : new StickState(),
                LeftTrigger = showButtons ? source.LeftTrigger : 0f,
                RightTrigger = showButtons ? source.RightTrigger : 0f,
                Buttons = showButtons ? CopyButtons(source.Buttons) : new GamepadButtonState(),
                ExtraButtons = CopyExtraButtons(source.ExtraButtons)
            };

            if (!showButtons)
            {
                for (var index = 0; index < display.ExtraButtons.Count; index++)
                {
                    display.ExtraButtons[index].IsPressed = false;
                }
            }

            return display;
        }

        private void RefreshFullscreenDisplayState()
        {
            if (latestInputState != null)
            {
                State = CreateDisplayState(latestInputState);
            }
        }

        private void TrackRestDrift(GamepadState nextState)
        {
            restDriftDiagnostics.AddSample(nextState, DateTime.UtcNow);
        }

        private static int CountPressedButtons(GamepadButtonState buttons)
        {
            var count = 0;
            foreach (var pressed in EnumerateButtonValues(buttons))
            {
                if (pressed)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountNormalizedControls()
        {
            return 19;
        }

        private static int CountPressedExtraButtons(IList<ExtraButtonState> buttons)
        {
            if (buttons == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < buttons.Count; index++)
            {
                if (buttons[index].IsPressed)
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerable<bool> EnumerateButtonValues(GamepadButtonState buttons)
        {
            yield return buttons.South;
            yield return buttons.East;
            yield return buttons.West;
            yield return buttons.North;
            yield return buttons.LeftShoulder;
            yield return buttons.RightShoulder;
            yield return buttons.Back;
            yield return buttons.Start;
            yield return buttons.Guide;
            yield return buttons.LeftStick;
            yield return buttons.RightStick;
            yield return buttons.DpadUp;
            yield return buttons.DpadDown;
            yield return buttons.DpadLeft;
            yield return buttons.DpadRight;
        }

        private string GetDriftStatus(double magnitude)
        {
            if (magnitude < HealthyDeadzoneThreshold)
            {
                return L("LOCCSM_Tester_NoDrift", "No drift");
            }

            if (magnitude < MinorDriftThreshold)
            {
                return L("LOCCSM_Tester_DriftSafe", "Safe");
            }

            if (magnitude < AttentionDriftThreshold)
            {
                return L("LOCCSM_Tester_MinorDrift", "Minor drift");
            }

            return L("LOCCSM_Tester_MajorDrift", "Major drift");
        }

        private string GetCircularCoverageLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format(L("LOCCSM_Tester_CircularCoverageFormat", "Circular coverage: {0}% ({1}/72 sectors)"), tracker.CoveragePercent, tracker.CoveredSectors);
        }

        private string GetPathSampleLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format(L("LOCCSM_Tester_PathSamplesFormat", "Path samples: {0}"), tracker.PathPoints.Count);
        }

        private string GetAxisRangeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return L("LOCCSM_Tester_RangeNoSamples", "Range: no samples");
            }

            return string.Format(L("LOCCSM_Tester_RangeFormat", "Range X {0:0.00}..{1:0.00}  Y {2:0.00}..{3:0.00}"),
                tracker.MinX,
                tracker.MaxX,
                tracker.MinY,
                tracker.MaxY);
        }

        private string GetAverageMagnitudeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return L("LOCCSM_Tester_AverageZero", "Average: 0%");
            }

            return string.Format(L("LOCCSM_Tester_AverageFormat", "Average: {0}%"), Math.Min(100, (int)Math.Round(tracker.AverageMagnitude * 100d)));
        }

        private string GetAngleLabel(StickState stick)
        {
            if (stick.Magnitude < 0.05d)
            {
                return L("LOCCSM_Tester_AngleCenter", "Angle: center");
            }

            var angle = Math.Atan2(stick.Y, stick.X) * 180d / Math.PI;
            if (angle < 0d)
            {
                angle += 360d;
            }

            return string.Format(L("LOCCSM_Tester_AngleFormat", "Angle: {0:0} deg"), angle);
        }

        private string GetRecommendedDeadzoneLabel(double noise)
        {
            return string.Format(L("LOCCSM_Tester_RecommendedDeadzoneFormat", "Recommended deadzone: {0}%"), GetRecommendedDeadzonePercent(noise));
        }

        private static int GetRecommendedDeadzonePercent(double noise)
        {
            var recommended = Math.Max(0.04d, Math.Min(0.25d, noise + 0.025d));
            return (int)Math.Round(recommended * 100d);
        }

        private string GetRangeQualityLabel(StickDiagnosticsTracker tracker)
        {
            var confidence = GetRangeConfidence(tracker);
            if (confidence.Stage == DiagnosticStage.NotEvaluated)
            {
                return L("LOCCSM_Tester_RangeNotMeasured", "Range not measured yet.");
            }

            if (!confidence.IsReady)
            {
                return string.Format(L("LOCCSM_Tester_RangeCollectingFormat", "Collecting range data: {0}% ({1}/72 directions)"),
                    confidence.ProgressPercent,
                    tracker.ExploredSectors);
            }

            return string.Format(L("LOCCSM_Tester_RangeQualityFormat", "Outer range quality: {0}%"), GetRangeQualityPercent(tracker));
        }

        private static DiagnosticConfidence GetRangeConfidence(StickDiagnosticsTracker tracker)
        {
            return DiagnosticConfidenceEvaluator.ForStickRange(tracker.SampleCount, tracker.ExploredSectors);
        }

        private string GetConfidenceLabel(DiagnosticConfidence confidence)
        {
            if (confidence.Stage == DiagnosticStage.NotEvaluated)
            {
                return L("LOCCSM_Tester_ConfidenceNotEvaluated", "Confidence: not evaluated");
            }

            if (confidence.Stage == DiagnosticStage.Collecting)
            {
                return string.Format(L("LOCCSM_Tester_ConfidenceCollectingFormat", "Confidence: collecting ({0}%)"), confidence.ProgressPercent);
            }

            var level = confidence.Level == DiagnosticConfidenceLevel.High
                ? L("LOCCSM_Tester_ConfidenceHigh", "high")
                : confidence.Level == DiagnosticConfidenceLevel.Medium
                    ? L("LOCCSM_Tester_ConfidenceMedium", "medium")
                    : L("LOCCSM_Tester_ConfidenceLow", "low");
            return string.Format(L("LOCCSM_Tester_ConfidenceReadyFormat", "Confidence: {0} ({1} samples)"), level, confidence.SampleCount);
        }

        private static int GetRangeQualityPercent(StickDiagnosticsTracker tracker)
        {
            return Math.Max(0, Math.Min(100, (int)Math.Round(tracker.MaxMagnitude * 100d)));
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private string L(string key, string fallback)
        {
            if (localizer == null)
            {
                return fallback;
            }

            var value = localizer(key);
            return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
        }

        private void NotifyThemeTestStateChanged()
        {
            OnPropertyChanged("IsAnyTestRunning");
            OnPropertyChanged("ActiveTestKind");
            OnPropertyChanged("CanNavigateBack");
        }

        private void RefreshCompatibilityAssessment()
        {
            var current = state ?? new GamepadState();
            var extraButtonCount = current.ExtraButtons == null ? 0 : current.ExtraButtons.Count;
            if (compatibilityMetadataInitialized &&
                compatibilityConnected == current.IsConnected &&
                string.Equals(compatibilityControllerName, current.ControllerName, StringComparison.Ordinal) &&
                compatibilityVendorId == current.VendorId &&
                compatibilityProductId == current.ProductId &&
                compatibilityLayout == current.Layout &&
                string.Equals(compatibilitySdlGuid, current.SdlGuid, StringComparison.Ordinal) &&
                string.Equals(compatibilitySdlMapping, current.SdlMapping, StringComparison.Ordinal) &&
                compatibilityAxisCount == current.AxisCount &&
                compatibilityButtonCount == current.ButtonCount &&
                compatibilityHatCount == current.HatCount &&
                compatibilityExtraButtonCount == extraButtonCount)
            {
                return;
            }

            compatibilityMetadataInitialized = true;
            compatibilityConnected = current.IsConnected;
            compatibilityControllerName = current.ControllerName;
            compatibilityVendorId = current.VendorId;
            compatibilityProductId = current.ProductId;
            compatibilityLayout = current.Layout;
            compatibilitySdlGuid = current.SdlGuid;
            compatibilitySdlMapping = current.SdlMapping;
            compatibilityAxisCount = current.AxisCount;
            compatibilityButtonCount = current.ButtonCount;
            compatibilityHatCount = current.HatCount;
            compatibilityExtraButtonCount = extraButtonCount;
            compatibilityAssessment = GamepadCompatibilityService.Assess(current);
            if (CompatibilityFindings != null)
            {
                CompatibilityFindings.Clear();
                foreach (var finding in compatibilityAssessment.Findings)
                {
                    CompatibilityFindings.Add(CreateCompatibilityFindingView(finding));
                }
            }

            OnPropertyChanged("CompatibilityAssistantStatus");
            OnPropertyChanged("CompatibilityAssistantStatusLabel");
            OnPropertyChanged("CompatibilityInputModeLabel");
            OnPropertyChanged("CompatibilityMappingCoverageLabel");
            OnPropertyChanged("CompatibilityFindings");
        }

        private GamepadCompatibilityFindingView CreateCompatibilityFindingView(GamepadCompatibilityFinding finding)
        {
            var title = string.Empty;
            var detail = string.Empty;
            switch (finding.Code)
            {
                case "NoController":
                    title = L("LOCCSM_NoControllers", "No controller connected");
                    detail = L("LOCCSM_Tester_ConnectControllerAndPress", "Connect a controller and press any button.");
                    break;
                case "MappingUnavailable":
                    title = L("LOCCSM_Tester_MappingStatus", "Mapping status");
                    detail = L("LOCCSM_Tester_CompatibilityMappingUnavailableDetail", "The controller is connected, but SDL did not return its mapping text. Test every control and export the report if anything is missing.");
                    break;
                case "MappingComplete":
                    title = L("LOCCSM_Tester_MappingStatus", "Mapping status");
                    detail = L("LOCCSM_Tester_CompatibilityMappingCompleteDetail", "SDL exposes every standard button, axis, shoulder and trigger binding expected by the tester.");
                    break;
                case "MissingBindings":
                    title = L("LOCCSM_Tester_MappingStatus", "Mapping status");
                    detail = string.Format(
                        L("LOCCSM_Tester_CompatibilityMissingBindingsDetail", "Missing: {0}. Try another controller mode or driver and run the guided test again."),
                        finding.Evidence);
                    break;
                case "InsufficientAxes":
                    title = L("LOCCSM_Tester_Capabilities", "Capabilities");
                    detail = string.Format(
                        L("LOCCSM_Tester_CompatibilityInsufficientAxesDetail", "SDL reports only {0} axes. A standard dual-stick controller normally exposes at least four."),
                        finding.Evidence);
                    break;
                case "FewButtons":
                    title = L("LOCCSM_Tester_Capabilities", "Capabilities");
                    detail = string.Format(
                        L("LOCCSM_Tester_CompatibilityFewButtonsDetail", "SDL reports {0} raw buttons. Use the guided test to confirm the standard controls."),
                        finding.Evidence);
                    break;
                case "EightBitDoModeUnknown":
                    title = L("LOCCSM_Tester_InputApi", "Input API");
                    detail = L("LOCCSM_Tester_CompatibilityEightBitDoModeDetail", "SDL does not expose a reliable XInput/DInput flag. If controls are missing, switch the controller to XInput and reconnect it.");
                    break;
                default:
                    title = finding.Code;
                    detail = finding.Evidence ?? string.Empty;
                    break;
            }

            return new GamepadCompatibilityFindingView
            {
                Severity = finding.Severity.ToString(),
                Title = title,
                Detail = detail
            };
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged("State");
            OnPropertyChanged("LeftStickDotX");
            OnPropertyChanged("LeftStickDotY");
            OnPropertyChanged("RightStickDotX");
            OnPropertyChanged("RightStickDotY");
            OnPropertyChanged("CompactLeftStickDotX");
            OnPropertyChanged("CompactLeftStickDotY");
            OnPropertyChanged("CompactRightStickDotX");
            OnPropertyChanged("CompactRightStickDotY");
            OnPropertyChanged("LeftStickDiagnosticsDotX");
            OnPropertyChanged("LeftStickDiagnosticsDotY");
            OnPropertyChanged("RightStickDiagnosticsDotX");
            OnPropertyChanged("RightStickDiagnosticsDotY");
            OnPropertyChanged("LeftTriggerPercent");
            OnPropertyChanged("RightTriggerPercent");
            OnPropertyChanged("LiveLeftTriggerPercent");
            OnPropertyChanged("LiveRightTriggerPercent");
            OnPropertyChanged("LiveLeftTriggerLabel");
            OnPropertyChanged("LiveRightTriggerLabel");
            OnPropertyChanged("IsLeftTriggerActive");
            OnPropertyChanged("IsRightTriggerActive");
            OnPropertyChanged("LeftStickDriftPercent");
            OnPropertyChanged("RightStickDriftPercent");
            OnPropertyChanged("IsDpadActive");
            OnPropertyChanged("ActiveButtonCount");
            OnPropertyChanged("ExtraActiveButtonCount");
            OnPropertyChanged("HasExtraButtons");
            OnPropertyChanged("IsFavoriteButtonActive");
            OnPropertyChanged("ExtraButtonSummaryLabel");
            OnPropertyChanged("LeftStickVector");
            OnPropertyChanged("RightStickVector");
            OnPropertyChanged("LeftStickDriftStatus");
            OnPropertyChanged("RightStickDriftStatus");
            OnPropertyChanged("MaxDriftLabel");
            OnPropertyChanged("SessionRestDriftLabel");
            OnPropertyChanged("LeftStickPathPoints");
            OnPropertyChanged("RightStickPathPoints");
            OnPropertyChanged("LeftStickPathGeometry");
            OnPropertyChanged("RightStickPathGeometry");
            OnPropertyChanged("LeftStickTrailRecentGeometry");
            OnPropertyChanged("LeftStickTrailMidGeometry");
            OnPropertyChanged("LeftStickTrailFadeGeometry");
            OnPropertyChanged("RightStickTrailRecentGeometry");
            OnPropertyChanged("RightStickTrailMidGeometry");
            OnPropertyChanged("RightStickTrailFadeGeometry");
            OnPropertyChanged("LeftStickCircularCoverageGeometry");
            OnPropertyChanged("RightStickCircularCoverageGeometry");
            OnPropertyChanged("LeftStickCircularCoveragePercent");
            OnPropertyChanged("RightStickCircularCoveragePercent");
            OnPropertyChanged("LeftStickMaxReachPercent");
            OnPropertyChanged("RightStickMaxReachPercent");
            OnPropertyChanged("LeftStickCurrentMagnitudePercent");
            OnPropertyChanged("RightStickCurrentMagnitudePercent");
            OnPropertyChanged("LeftStickCircularCoverageLabel");
            OnPropertyChanged("RightStickCircularCoverageLabel");
            OnPropertyChanged("LeftStickPathSampleLabel");
            OnPropertyChanged("RightStickPathSampleLabel");
            OnPropertyChanged("LeftStickMaxReachLabel");
            OnPropertyChanged("RightStickMaxReachLabel");
            OnPropertyChanged("LeftStickCurrentMagnitudeLabel");
            OnPropertyChanged("RightStickCurrentMagnitudeLabel");
            OnPropertyChanged("LeftStickAngleLabel");
            OnPropertyChanged("RightStickAngleLabel");
            OnPropertyChanged("LeftStickAxisRangeLabel");
            OnPropertyChanged("RightStickAxisRangeLabel");
            OnPropertyChanged("LeftStickAverageMagnitudeLabel");
            OnPropertyChanged("RightStickAverageMagnitudeLabel");
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            OnPropertyChanged("LeftCalibrationCenterLabel");
            OnPropertyChanged("RightCalibrationCenterLabel");
            OnPropertyChanged("LeftRecommendedDeadzoneLabel");
            OnPropertyChanged("RightRecommendedDeadzoneLabel");
            OnPropertyChanged("LeftRecommendedDeadzonePercent");
            OnPropertyChanged("RightRecommendedDeadzonePercent");
            OnPropertyChanged("LeftRangeQualityLabel");
            OnPropertyChanged("RightRangeQualityLabel");
            OnPropertyChanged("LeftRangeQualityPercent");
            OnPropertyChanged("RightRangeQualityPercent");
            OnPropertyChanged("LeftRangeDisplayProgress");
            OnPropertyChanged("RightRangeDisplayProgress");
            OnPropertyChanged("LeftRangeConfidenceLabel");
            OnPropertyChanged("RightRangeConfidenceLabel");
            OnPropertyChanged("StickCaptureStatusLabel");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencyConfidenceLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("QuickTestProgress");
            OnPropertyChanged("QuickTestLabel");
            OnPropertyChanged("ButtonCoverageLabel");
            OnPropertyChanged("AnalogCoverageLabel");
            OnPropertyChanged("QuickTestMissingLabel");
            OnPropertyChanged("GuidedTestProgress");
            OnPropertyChanged("GuidedTestButtonLabel");
            OnPropertyChanged("GuidedTestStatusLabel");
            OnPropertyChanged("GuidedTestNextInputLabel");
            OnPropertyChanged("GuidedTestActionLabel");
            OnPropertyChanged("GuidedTestReportLabel");
            OnPropertyChanged("HasGuidedTestReport");
            OnPropertyChanged("IsGuidedTestReportSuccess");
            OnPropertyChanged("IsGuidedTestRunning");
            RefreshGuidedTestInputs();
            OnPropertyChanged("CoveredSouth");
            OnPropertyChanged("CoveredEast");
            OnPropertyChanged("CoveredWest");
            OnPropertyChanged("CoveredNorth");
            OnPropertyChanged("CoveredLeftShoulder");
            OnPropertyChanged("CoveredRightShoulder");
            OnPropertyChanged("CoveredLeftStickButton");
            OnPropertyChanged("CoveredRightStickButton");
            OnPropertyChanged("CoveredBack");
            OnPropertyChanged("CoveredStart");
            OnPropertyChanged("CoveredGuide");
            OnPropertyChanged("CoveredDpadUp");
            OnPropertyChanged("CoveredDpadDown");
            OnPropertyChanged("CoveredDpadLeft");
            OnPropertyChanged("CoveredDpadRight");
            OnPropertyChanged("CoveredLeftTrigger");
            OnPropertyChanged("CoveredRightTrigger");
            OnPropertyChanged("CoveredLeftStickRange");
            OnPropertyChanged("CoveredRightStickRange");
            OnPropertyChanged("HealthScore");
            OnPropertyChanged("HealthScoreDisplayLabel");
            OnPropertyChanged("HealthDisplayProgress");
            OnPropertyChanged("HealthConfidenceLabel");
            OnPropertyChanged("HealthLabel");
            OnPropertyChanged("HealthSummaryLabel");
            OnPropertyChanged("HealthDriftFactorLabel");
            OnPropertyChanged("HealthRangeFactorLabel");
            OnPropertyChanged("HealthCoverageFactorLabel");
            OnPropertyChanged("DiagnosticRadarValues");
            OnPropertyChanged("DiagnosticRadarLabels");
            OnPropertyChanged("ControllerSummary");
            OnPropertyChanged("HasController");
            OnPropertyChanged("IsNoControllerVisible");
            OnPropertyChanged("IsNoControllerOverlayVisible");
            OnPropertyChanged("DeviceIdLabel");
            OnPropertyChanged("DeviceModelLabel");
            OnPropertyChanged("DeviceCapabilitiesLabel");
            OnPropertyChanged("DeviceApiLabel");
            OnPropertyChanged("DeviceRumbleCapabilityLabel");
            OnPropertyChanged("CompatibilityReportStatusLabel");
            OnPropertyChanged("CompatibilityAssistantStatus");
            OnPropertyChanged("CompatibilityAssistantStatusLabel");
            OnPropertyChanged("CompatibilityInputModeLabel");
            OnPropertyChanged("CompatibilityMappingCoverageLabel");
            OnPropertyChanged("ExtraButtonDetailLabel");
            OnPropertyChanged("BackendLabel");
            OnPropertyChanged("MappingStatusLabel");
            OnPropertyChanged("RumbleStatusLabel");
            OnPropertyChanged("SouthLabel");
            OnPropertyChanged("EastLabel");
            OnPropertyChanged("WestLabel");
            OnPropertyChanged("NorthLabel");
            OnPropertyChanged("LeftShoulderLabel");
            OnPropertyChanged("RightShoulderLabel");
            OnPropertyChanged("LeftTriggerLabel");
            OnPropertyChanged("RightTriggerLabel");
            OnPropertyChanged("LeftStickButtonLabel");
            OnPropertyChanged("RightStickButtonLabel");
            OnPropertyChanged("BackButtonLabel");
            OnPropertyChanged("StartButtonLabel");
            OnPropertyChanged("GuideButtonLabel");
            OnPropertyChanged("DpadUpLabel");
            OnPropertyChanged("DpadDownLabel");
            OnPropertyChanged("DpadLeftLabel");
            OnPropertyChanged("DpadRightLabel");
            OnPropertyChanged("IsEightBitDoLayout");
            OnPropertyChanged("IsEightBitDoPro3Artwork");
            OnPropertyChanged("IsEightBitDoUltimate2CArtwork");
            OnPropertyChanged("IsEightBitDoUltimate2Artwork");
            OnPropertyChanged("IsSwitchProLayout");
            OnPropertyChanged("IsXboxLayout");
            OnPropertyChanged("IsPlayStationLayout");
            OnPropertyChanged("IsDualSenseLayout");
            OnPropertyChanged("IsXboxVisualScheme");
            OnPropertyChanged("IsXboxOneVisualScheme");
            OnPropertyChanged("IsXboxSeriesVisualScheme");
            OnPropertyChanged("IsSteamControllerVisualScheme");
            OnPropertyChanged("IsPlayStationVisualScheme");
            OnPropertyChanged("IsSwitchProVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimateVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimate2VisualScheme");
            OnPropertyChanged("IsEightBitDoProVisualScheme");
            OnPropertyChanged("IsUniversalControllerArtwork");
            OnPropertyChanged("IsGenericLayout");
            openGuidedTestCommand.RaiseCanExecuteChanged();
            startGuidedTestCommand.RaiseCanExecuteChanged();
            openGuidedTabCommand.RaiseCanExecuteChanged();
            exportCompatibilityReportCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            ClearInputHistory();
            settings.PropertyChanged -= OnSettingsPropertyChanged;
            pollingService.StateUpdated -= OnStateUpdated;
            pollingService.Dispose();
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args == null || string.IsNullOrEmpty(args.PropertyName) ||
                args.PropertyName == "ShowDeviceSelectorWhenSingleController")
            {
                OnPropertyChanged("IsControllerSelectorVisible");
            }

            if (args == null || string.IsNullOrEmpty(args.PropertyName) ||
                args.PropertyName == "ShowSidebarItem")
            {
                OnPropertyChanged("IsSidebarRestartRequired");
            }
        }
    }
}
