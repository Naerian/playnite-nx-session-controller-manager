using ControllerSessionManager.Tester.Models;
using ControllerSessionManager.Tester.Services;
using ControllerSessionManager.Tester.ViewModels;
using ControllerSessionManager.Tester.Views.ThemeIntegration;
using ControllerSessionManager.Tester.Views;
using ControllerSessionManager.Tester.Views.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ControllerSessionManager.Tester.Tests
{
    internal static class Program
    {
        private static int executed;

        [STAThread]
        private static int Main()
        {
            try
            {
                GamepadTesterThemeRuntime.SetInputProviderFactoryForTests(
                    () => new SimulatedGamepadInputProvider());
                TestProtocolRoundtrip();
                TestControllerIdentification();
                TestDiagnosticConfidence();
                TestRestDriftTracking();
                TestStickDiagnostics();
                TestStickMotionTrail();
                TestDesktopStickCaptureSession();
                TestSimulatedProvider();
                TestVisualSchemeCatalog();
                TestEmbeddedSidebarIcon();
                TestThemeHostXamlContract();
                TestLateThemeHostInitialization();
                TestThemeDeveloperContract();
                TestTriggerCheckBindings();
                TestFullscreenThemeBrushOverrides();
                TestFullscreenStickGuideBrushOverride();
                TestFullscreenStickPlotSize();
                TestFullscreenCaptureGating();
                TestCompatibilityReport();
                TestCompatibilityAssistant();
                TestLatencyRateChartRendering();
                TestLocalizationParity();
                Console.WriteLine("GamepadTester.Tests: {0} checks passed.", executed);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("GamepadTester.Tests failed: {0}", ex.Message);
                return 1;
            }
        }

        private static void TestProtocolRoundtrip()
        {
            var state = new GamepadState
            {
                IsConnected = true,
                ControllerName = "Pad|One",
                VendorId = 0x045E,
                ProductId = 0x0B13,
                Layout = GamepadLayout.Xbox,
                LeftStick = new StickState { X = -0.5f, Y = 0.25f },
                RightStick = new StickState { X = 1f, Y = -1f },
                LeftTrigger = 0.33f,
                RightTrigger = 1f,
                Buttons = new GamepadButtonState { South = true, DpadUp = true },
                ExtraButtons = new List<ExtraButtonState>
                {
                    new ExtraButtonState { RawIndex = 15, Label = "Extra 1", IsPressed = true }
                }
            };
            var controllers = new List<GamepadControllerInfo>
            {
                new GamepadControllerInfo
                {
                    InstanceId = 7,
                    JoystickIndex = 0,
                    Name = "Pad|One",
                    VendorId = 0x045E,
                    ProductId = 0x0B13,
                    Layout = GamepadLayout.Xbox
                }
            };

            var encoded = GamepadProtocol.EncodeSnapshot("token", state, controllers);
            GamepadProtocol.SnapshotMessage decoded;
            True(GamepadProtocol.TryParseSnapshot(encoded, "token", out decoded), "Snapshot protocol round-trips");
            Equal(true, decoded.State.IsConnected, "Connected flag is preserved");
            Equal("Pad|One", decoded.State.ControllerName, "Pipe characters in names are encoded");
            Equal((ushort)0x045E, decoded.State.VendorId, "VID is preserved");
            Equal(true, decoded.State.Buttons.South, "Face button bits are preserved");
            Equal(1, decoded.State.ExtraButtons.Count, "Extra buttons are preserved");
            Equal(7, decoded.Controllers[0].InstanceId, "Controller list is preserved");
        }

        private static void TestControllerIdentification()
        {
            Equal(GamepadLayout.EightBitDo, ControllerIdentificationService.DetectLayout("Xbox Controller", 0x2DC8, 0x310B), "8BitDo VID wins over XInput name");
            Equal(GamepadLayout.SwitchPro, ControllerIdentificationService.DetectLayout("Pro Controller", 0x057E, 0x2009), "Switch Pro VID/PID");
            Equal(GamepadLayout.PlayStation, ControllerIdentificationService.DetectLayout("DualSense Wireless Controller", 0x054C, 0x0CE6), "DualSense name");
            Equal(GamepadLayout.Xbox, ControllerIdentificationService.DetectLayout("Xbox Wireless Controller", 0x045E, 0x0B13), "Xbox name");
            Equal(GamepadLayout.Generic, ControllerIdentificationService.DetectLayout("Arcade Stick", 0, 0), "Generic fallback");
            Equal(EightBitDoModel.Pro2, ControllerIdentificationService.DetectEightBitDoModel("8BitDo Pro 2", 0x2DC8, 0), "8BitDo Pro 2 model");
            Equal(EightBitDoModel.Ultimate2Wireless, ControllerIdentificationService.DetectEightBitDoModel("8BitDo Ultimate 2", 0x2DC8, 0x310B), "8BitDo Ultimate model");
        }

        private static void TestDiagnosticConfidence()
        {
            Equal(DiagnosticStage.NotEvaluated, DiagnosticConfidenceEvaluator.ForHealth(false, 0).Stage, "Disconnected health is not evaluated");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForHealth(true, 119).Stage, "Health waits for stable samples");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForHealth(true, 120).Stage, "Health becomes ready at threshold");
            Equal(DiagnosticConfidenceLevel.High, DiagnosticConfidenceEvaluator.ForHealth(true, 300).Level, "Health reaches high confidence");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForLatency(true, 19).Stage, "Latency waits for samples");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForLatency(true, 20).Stage, "Latency becomes ready");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForStickRange(120, 35).Stage, "Range requires explored directions");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForStickRange(120, 36).Stage, "Range becomes ready");
        }

        private static void TestStickDiagnostics()
        {
            var tracker = new StickDiagnosticsTracker();
            for (var index = 0; index < 72; index++)
            {
                var angle = Math.PI * 2d * (index + 0.5d) / 72d;
                tracker.AddSample(new StickState
                {
                    X = (float)(Math.Cos(angle) * 0.7d),
                    Y = (float)(Math.Sin(angle) * 0.7d)
                });
            }

            True(tracker.ExploredSectors >= 60, "Circular movement explores enough sectors for high confidence");
            Equal(72, tracker.SampleCount, "Stick samples are counted");

            for (var index = tracker.SampleCount; index < StickDiagnosticsTracker.MaximumSamples + 20; index++)
            {
                tracker.AddSample(new StickState { X = 0.5f, Y = 0.5f });
            }

            Equal(StickDiagnosticsTracker.MaximumSamples, tracker.SampleCount, "Stick sampling stops at its safety limit");
            True(tracker.HasReachedSamplingLimit, "Stick tracker reports its safety limit");
            tracker.Reset();
            Equal(0, tracker.ExploredSectors, "Reset clears explored sectors");
            True(!tracker.HasReachedSamplingLimit, "Reset clears the stick sampling limit");
        }

        private static void TestStickMotionTrail()
        {
            var tracker = new StickMotionTrailTracker();
            var now = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

            tracker.Update(new StickState { X = 0f, Y = 0f }, now);
            Equal(0, tracker.SampleCount, "Resting sticks do not start a trail");
            True(tracker.RecentGeometry.IsEmpty(), "An empty trail has no recent geometry");

            tracker.Update(new StickState { X = 0.4f, Y = 0.1f }, now.AddMilliseconds(16));
            tracker.Update(new StickState { X = 0.6f, Y = 0.2f }, now.AddMilliseconds(32));
            tracker.Update(new StickState { X = 0.8f, Y = 0.1f }, now.AddMilliseconds(48));
            True(tracker.SampleCount >= 2, "Moving sticks append trail samples");
            True(!tracker.RecentGeometry.IsEmpty(), "Recent movement produces a visible trail");

            tracker.Update(new StickState { X = 0f, Y = 0f }, now.AddMilliseconds(1600));
            Equal(0, tracker.SampleCount, "Trail samples expire after the retention window");
            True(tracker.RecentGeometry.IsEmpty(), "Expired trails clear their geometry");
            True(tracker.MidGeometry.IsEmpty(), "Expired trails clear the mid fade");
            True(tracker.FadeGeometry.IsEmpty(), "Expired trails clear the oldest fade");

            tracker.Reset();
            Equal(0, tracker.SampleCount, "Reset clears remaining trail samples");
        }

        private static void TestDesktopStickCaptureSession()
        {
            var provider = new SimulatedGamepadInputProvider();
            var polling = new GamepadPollingService(provider);
            using (var viewModel = new GamepadTesterViewModel(polling))
            {
                var state = new GamepadState
                {
                    IsConnected = true,
                    ControllerName = "Simulated Xbox",
                    Layout = GamepadLayout.Xbox,
                    LeftStick = new StickState { X = 0.6f, Y = 0.2f },
                    RightStick = new StickState { X = -0.4f, Y = 0.5f }
                };

                SetPrivateField(viewModel, "state", state);
                SetPrivateField(viewModel, "latestInputState", state);
                var leftTracker = GetPrivateField<StickDiagnosticsTracker>(viewModel, "leftStickDiagnostics");
                var rightTracker = GetPrivateField<StickDiagnosticsTracker>(viewModel, "rightStickDiagnostics");

                InvokePrivate(viewModel, "UpdateDiagnostics", state);
                Equal(0, leftTracker.SampleCount, "Desktop stick sampling remains idle before the test starts");
                Equal(0, rightTracker.SampleCount, "Desktop right stick sampling remains idle before the test starts");

                viewModel.StartStickCaptureCommand.Execute(null);
                InvokePrivate(viewModel, "UpdateDiagnostics", state);
                Equal(1, leftTracker.SampleCount, "Desktop stick sampling starts explicitly");
                Equal(1, rightTracker.SampleCount, "Desktop right stick sampling starts explicitly");

                viewModel.StartStickCaptureCommand.Execute(null);
                InvokePrivate(viewModel, "UpdateDiagnostics", state);
                Equal(1, leftTracker.SampleCount, "Stopped desktop stick sampling remains frozen");
                Equal(1, rightTracker.SampleCount, "Stopped desktop right stick sampling remains frozen");

                viewModel.StartStickCaptureCommand.Execute(null);
                for (var index = 0; index < 60; index++)
                {
                    state.LeftStick = StickAtSector(index);
                    state.RightStick = StickAtSector(index);
                    InvokePrivate(viewModel, "UpdateDiagnostics", state);
                }

                for (var index = 0; index < 60; index++)
                {
                    state.LeftStick = StickAtSector(0);
                    state.RightStick = StickAtSector(0);
                    InvokePrivate(viewModel, "UpdateDiagnostics", state);
                }

                True(viewModel.IsStickCaptureRunning, "High-confidence stick data does not stop an incomplete circular test");
                True(leftTracker.CoveragePercent < 100 && rightTracker.CoveragePercent < 100, "Incomplete stick coverage remains below 100 percent");

                for (var index = 60; index < 72; index++)
                {
                    state.LeftStick = StickAtSector(index);
                    state.RightStick = StickAtSector(index);
                    InvokePrivate(viewModel, "UpdateDiagnostics", state);
                }

                Equal(100, leftTracker.CoveragePercent, "Left stick circular test reaches full coverage");
                Equal(100, rightTracker.CoveragePercent, "Right stick circular test reaches full coverage");
                True(!viewModel.IsStickCaptureRunning, "Stick sampling stops only after both sticks reach full coverage");
            }
        }

        private static StickState StickAtSector(int index)
        {
            var angle = Math.PI * 2d * (index + 0.5d) / 72d;
            return new StickState
            {
                X = (float)(Math.Cos(angle) * 0.95d),
                Y = (float)(Math.Sin(angle) * 0.95d)
            };
        }

        private static void TestRestDriftTracking()
        {
            var tracker = new RestDriftTracker();
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var activeState = new GamepadState
            {
                IsConnected = true,
                LeftStick = new StickState { X = 0.8f, Y = 0f }
            };

            tracker.AddSample(activeState, start);
            tracker.AddSample(activeState, start.AddSeconds(1));
            Equal(0, tracker.SampleCount, "Normal stick movement is excluded from rest drift");
            Equal(0d, tracker.MaxDrift, "Normal movement cannot trigger a drift warning");

            var restingState = new GamepadState
            {
                IsConnected = true,
                LeftStick = new StickState { X = 0.04f, Y = 0f }
            };
            tracker.AddSample(restingState, start.AddSeconds(2));
            tracker.AddSample(restingState, start.AddMilliseconds(2500));
            True(tracker.SampleCount > 0, "Stable centered offset is sampled as rest drift");
            True(tracker.MaxDrift >= 0.039d, "Stable centered offset is measured");

            tracker.Reset();
            Equal(0, tracker.SampleCount, "Rest drift reset clears samples");
            Equal(0d, tracker.MaxDrift, "Rest drift reset clears the peak");

            tracker.AddSample(restingState, start.AddSeconds(3));
            for (var index = 0; index < RestDriftTracker.MaximumSamples + 20; index++)
            {
                tracker.AddSample(restingState, start.AddSeconds(4).AddMilliseconds(index * 20));
            }

            Equal(RestDriftTracker.MaximumSamples, tracker.SampleCount, "Rest drift collection stops at high confidence");

            tracker.Reset();
            restingState.ExtraButtons.Add(new ExtraButtonState { RawIndex = 15, IsPressed = true });
            tracker.AddSample(restingState, start.AddSeconds(12));
            tracker.AddSample(restingState, start.AddSeconds(13));
            Equal(0, tracker.SampleCount, "Extra button activity is excluded from rest drift");
        }

        private static void TestSimulatedProvider()
        {
            var provider = new SimulatedGamepadInputProvider();
            provider.AddController(new GamepadControllerInfo { InstanceId = 7, Name = "Simulated Xbox" });
            provider.Enqueue(new GamepadState { IsConnected = true, ControllerName = "Simulated Xbox" });
            provider.SelectController(7);
            Equal(7, provider.SelectedInstanceId, "Simulated controller selection");
            True(provider.ReadState().IsConnected, "Simulated state queue");
            True(provider.TryRumble(1, 1, 1), "Simulated rumble");
            Equal(1, provider.RumbleCallCount, "Simulated rumble is recorded");
        }

        private static void TestVisualSchemeCatalog()
        {
            var definitions = ControllerVisualSchemeCatalog.CreateDefinitions(null).ToList();
            True(definitions.Count >= 8, "Visual scheme catalog is populated");
            Equal(definitions.Count, definitions.Select(item => item.Key).Distinct().Count(), "Visual scheme keys are unique");
            True(definitions.All(item => item.TestWidth > 0 && item.TestHeight > 0), "Visual schemes have valid dimensions");
        }

        private static void TestCompatibilityReport()
        {
            var state = new GamepadState
            {
                IsConnected = true,
                ControllerName = "Simulated Xbox",
                VendorId = 0x045E,
                ProductId = 0x0B13,
                Layout = GamepadLayout.Xbox,
                SdlVersion = "2.30.9",
                SdlGuid = "030000005e040000130b000000000000",
                SdlMapping = "simulated-mapping",
                AxisCount = 6,
                ButtonCount = 15,
                HatCount = 1
            };
            var report = GamepadCompatibilityReportBuilder.Build(state, null, "high", "medium", "medium", "low");
            True(report.Contains("VID: 045E"), "Compatibility report includes VID");
            True(report.Contains("SDL GUID: 030000005e040000130b000000000000"), "Compatibility report includes SDL GUID");
            True(report.Contains("Axes exposed: 6"), "Compatibility report includes capabilities");
            True(report.Contains("[Compatibility assistant]"), "Compatibility report includes assistant assessment");
            True(!report.Contains(Environment.UserName), "Compatibility report excludes the user name");
            True(!report.Contains("8BitDo model:"), "Non-8BitDo reports omit model-specific fields");

            state.Layout = GamepadLayout.EightBitDo;
            state.EightBitDoModel = EightBitDoModel.Pro2;
            report = GamepadCompatibilityReportBuilder.Build(state, null, "high", "medium", "medium", "low");
            True(report.Contains("8BitDo model: Pro2"), "8BitDo reports include the detected model");
        }

        private static void TestCompatibilityAssistant()
        {
            const string completeMapping =
                "030000005e040000130b000000000000,Xbox Controller," +
                "a:b0,b:b1,x:b2,y:b3,back:b6,start:b7,leftstick:b8,rightstick:b9," +
                "leftshoulder:b4,rightshoulder:b5,dpup:h0.1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2," +
                "leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Windows,";
            var state = new GamepadState
            {
                IsConnected = true,
                ControllerName = "Xbox Wireless Controller",
                Layout = GamepadLayout.Xbox,
                SdlMapping = completeMapping,
                AxisCount = 6,
                ButtonCount = 15
            };

            var assessment = GamepadCompatibilityService.Assess(state);
            Equal(GamepadCompatibilitySeverity.Ready, assessment.Severity, "Complete mapping is ready");
            Equal(GamepadInputMode.XInput, assessment.InputMode, "Xbox identity infers XInput");
            Equal(100, assessment.MappingCoveragePercent, "Complete mapping reaches full coverage");
            Equal(0, assessment.MissingBindings.Count, "Complete mapping has no missing bindings");

            state.SdlMapping = completeMapping.Replace("lefttrigger:a4,righttrigger:a5,", string.Empty);
            assessment = GamepadCompatibilityService.Assess(state);
            Equal(GamepadCompatibilitySeverity.Warning, assessment.Severity, "Missing triggers request review");
            True(assessment.MissingBindings.Contains("LT") && assessment.MissingBindings.Contains("RT"),
                "Missing trigger bindings use normalized Xbox names");

            state.SdlMapping = completeMapping;
            state.AxisCount = 2;
            assessment = GamepadCompatibilityService.Assess(state);
            Equal(GamepadCompatibilitySeverity.Limited, assessment.Severity, "Too few axes produce limited status");

            state.ControllerName = "8BitDo Ultimate 2";
            state.Layout = GamepadLayout.EightBitDo;
            state.AxisCount = 6;
            state.SdlMapping = completeMapping.Replace("Xbox Controller", "8BitDo Ultimate 2");
            assessment = GamepadCompatibilityService.Assess(state);
            Equal(GamepadInputMode.Unknown, assessment.InputMode, "8BitDo mode is not guessed without evidence");
            Equal(GamepadCompatibilitySeverity.Info, assessment.Severity, "Unknown 8BitDo mode is informational");
            True(assessment.Findings.Any(item => item.Code == "EightBitDoModeUnknown"),
                "8BitDo receives mode-switch guidance");
        }

        private static void TestEmbeddedSidebarIcon()
        {
            const string resourceName = "ControllerSessionManager.Icons.gamepad-tester.svg";
            using (var stream = typeof(GamepadState).Assembly.GetManifestResourceStream(resourceName))
            {
                True(stream != null, "Sidebar SVG is embedded in the plugin assembly");
                var document = XDocument.Load(stream);
                var paths = document.Descendants().Where(element => element.Name.LocalName == "path").ToArray();
                True(paths.Length > 0, "Sidebar SVG contains path geometry");
                True(paths.All(path => System.Windows.Media.Geometry.Parse((string)path.Attribute("d")) != null), "Sidebar SVG paths are valid WPF geometry");
            }
        }

        private static void TestThemeHostXamlContract()
        {
            True(typeof(DependencyObject).IsAssignableFrom(typeof(GamepadTesterThemeHost)),
                "Theme host is a WPF attached-property provider");

            var target = new DependencyObject();
            GamepadTesterThemeHost.SetBlock(target, "ButtonMap");
            Equal("ButtonMap", GamepadTesterThemeHost.GetBlock(target),
                "Theme host attached block round-trips");
        }

        private static void TestLateThemeHostInitialization()
        {
            var hostMessages = new List<string>();
            GamepadTesterThemeHost.Configure(new GamepadTesterSettings(), key => key, () => { }, hostMessages.Add);
            var host = new ContentControl { Tag = "GamepadTesterLauncher" };
            Equal(1, GamepadTesterThemeHost.Refresh(host),
                "Late theme host refresh finds its standard Tag marker");
            True(host.Content is GamepadTesterThemeLauncherControl,
                "Late theme host initializes from its standard Tag marker");

            var triggerHost = new ContentControl { Tag = "GamepadTester_TriggerCheck" };
            var initializedTriggers = GamepadTesterThemeHost.Refresh(triggerHost);
            True(initializedTriggers == 1,
                "Late theme host finds the fullscreen trigger block: " + string.Join(" | ", hostMessages));
            True(triggerHost.Content is GamepadTesterTriggerCheckControl,
                "Late theme host initializes the fullscreen trigger block");
            Equal("Ready", GamepadTesterThemeHost.GetInitializationState(triggerHost),
                "Initialized host exposes Ready state");
            Equal("TriggerCheck", GamepadTesterThemeHost.GetResolvedBlock(triggerHost),
                "Initialized host exposes its resolved block");
        }

        private static void TestThemeDeveloperContract()
        {
            var contractMessages = new List<string>();
            GamepadTesterThemeHost.Configure(new GamepadTesterSettings(), key => key, () => { },
                contractMessages.Add);
            Equal("1.1", GamepadTesterThemeContract.Version, "Theme contract has a stable version");
            True(GamepadTesterThemeContract.SupportsBlock("ButtonMap"), "Theme contract lists ButtonMap");
            True(GamepadTesterThemeContract.SupportsBlock("Launcher"), "Theme contract accepts Launcher alias");
            True(!GamepadTesterThemeContract.SupportsBlock("UnknownWidget"), "Theme contract rejects unknown blocks");

            var unknown = new ContentControl();
            GamepadTesterThemeHost.SetBlock(unknown, "UnknownWidget");
            Equal(0, GamepadTesterThemeHost.Refresh(unknown), "Unknown dynamic block is not initialized");
            Equal("UnknownBlock", GamepadTesterThemeHost.GetInitializationState(unknown),
                "Unknown block exposes a diagnostic state");
            var unknownLogCount = contractMessages.Count;
            Equal(0, GamepadTesterThemeHost.Refresh(unknown), "Repeated unknown block remains ignored");
            Equal(unknownLogCount, contractMessages.Count, "Repeated unknown block is not logged again");

            var ordinaryTaggedControl = new ContentControl { Tag = "Soft" };
            Equal(0, GamepadTesterThemeHost.Refresh(ordinaryTaggedControl),
                "Ordinary Playnite tags are not interpreted as Gamepad Tester blocks");
            Equal("Unmarked", GamepadTesterThemeHost.GetInitializationState(ordinaryTaggedControl),
                "Ordinary tagged controls remain unmarked");

            var occupied = new ContentControl { Content = new TextBlock { Text = "Theme content" } };
            GamepadTesterThemeHost.SetBlock(occupied, "ButtonMap");
            Equal(0, GamepadTesterThemeHost.Refresh(occupied), "Occupied host is not overwritten");
            Equal("Occupied", GamepadTesterThemeHost.GetInitializationState(occupied),
                "Occupied host explains why it stayed unchanged");

            var control = new GamepadTesterStatusBadgeControl(new GamepadTesterSettings(), key => key);
            Equal("1.1", control.ThemeContractVersion, "Embedded control exposes contract version");
            True(control.CanNavigateBack, "Embedded controls expose initial Back navigation state");

            var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
            var samplePath = Path.Combine(root, "docs", "theme-integration", "GamepadTesterSampleView.xaml");
            var contractPath = Path.Combine(root, "docs", "theme-integration", "CONTRACT.md");
            True(File.Exists(samplePath), "Theme developer sample view exists");
            True(File.Exists(contractPath), "Theme developer contract document exists");
            True(XDocument.Load(samplePath).Root != null, "Theme developer sample is valid XML");
            True(File.ReadAllText(samplePath).Contains("Content.CanNavigateBack"),
                "Theme developer sample guards its Back button with CanNavigateBack");
        }

        private static void TestTriggerCheckBindings()
        {
            var control = new GamepadTesterTriggerCheckControl(new GamepadTesterSettings(), key => key);
            control.DataContext = new ReadOnlyTriggerSource();
            var root = (StackPanel)control.Content;
            var meters = (Grid)root.Children[1];

            foreach (StackPanel meter in meters.Children)
            {
                var progress = (ProgressBar)meter.Children[1];
                var binding = BindingOperations.GetBinding(progress, RangeBase.ValueProperty);
                Equal(BindingMode.OneWay, binding.Mode,
                    "Fullscreen trigger meters use read-only bindings");
                BindingOperations.GetBindingExpression(progress, RangeBase.ValueProperty).UpdateTarget();
            }
        }

        private static void TestFullscreenStickPlotSize()
        {
            var control = new GamepadTesterStickCheckControl(new GamepadTesterSettings(), key => key);
            var panel = (Border)control.Content;
            var root = (StackPanel)panel.Child;
            var sticks = (Grid)root.Children[1];

            foreach (Grid stick in sticks.Children)
            {
                var plot = (Viewbox)stick.Children[0];
                True(plot.Width <= 120d && plot.Height <= 120d,
                    "Fullscreen stick plots fit compact theme cards");
            }
        }

        private static void TestFullscreenThemeBrushOverrides()
        {
            var control = new GamepadTesterButtonMapControl(new GamepadTesterSettings(), key => key);
            var background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Magenta);
            var buttonBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Navy);
            var border = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Lime);
            var text = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow);
            var themeHost = new ContentControl { Content = control };
            themeHost.Resources[GamepadTesterThemeControlBase.ControlBackgroundBrushKey] = background;
            themeHost.Resources[GamepadTesterThemeControlBase.ButtonBackgroundBrushKey] = buttonBackground;
            themeHost.Resources[GamepadTesterThemeControlBase.ControlBorderBrushKey] = border;
            themeHost.Resources[GamepadTesterThemeControlBase.TextBrushKey] = text;

            var panel = (Border)control.Content;
            var root = (Grid)panel.Child;
            var content = (StackPanel)root.Children[0];
            var actionArea = (Grid)content.Children[1];
            var actionButton = (Button)actionArea.Children[0];

            Equal(background, panel.Background, "Theme can override the Gamepad Tester panel background");
            Equal(border, panel.BorderBrush, "Theme can override the Gamepad Tester panel border");
            Equal(buttonBackground, actionButton.Background, "Theme can override Gamepad Tester buttons");
            Equal(text, actionButton.Foreground, "Theme can override Gamepad Tester text");
        }

        private static void TestFullscreenStickGuideBrushOverride()
        {
            var control = new GamepadTesterStickCheckControl(new GamepadTesterSettings(), key => key);
            var border = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Lime);
            var guide = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Cyan);
            var themeHost = new ContentControl { Content = control };
            themeHost.Resources[GamepadTesterThemeControlBase.ControlBorderBrushKey] = border;
            themeHost.Resources[GamepadTesterThemeControlBase.StickGuideBrushKey] = guide;

            var panel = (Border)control.Content;
            var root = (StackPanel)panel.Child;
            var sticks = (Grid)root.Children[1];
            var leftStick = (Grid)sticks.Children[0];
            var viewbox = (Viewbox)leftStick.Children[0];
            var plot = (Grid)viewbox.Child;
            var surface = (System.Windows.Shapes.Ellipse)plot.Children[0];
            var range = (System.Windows.Shapes.Ellipse)plot.Children[1];
            var verticalAxis = (System.Windows.Shapes.Line)plot.Children[2];
            var horizontalAxis = (System.Windows.Shapes.Line)plot.Children[3];

            Equal(border, panel.BorderBrush, "Stick guide override does not change panel borders");
            Equal(guide, surface.Stroke, "Theme can override the stick outer circle");
            Equal(guide, range.Stroke, "Theme can override the stick range guide");
            Equal(guide, verticalAxis.Stroke, "Theme can override the stick vertical guide");
            Equal(guide, horizontalAxis.Stroke, "Theme can override the stick horizontal guide");
        }

        private sealed class ReadOnlyTriggerSource
        {
            public string LiveLeftTriggerLabel { get { return "LT 25%"; } }
            public string LiveRightTriggerLabel { get { return "RT 75%"; } }
            public double LiveLeftTriggerPercent { get { return 25d; } }
            public double LiveRightTriggerPercent { get { return 75d; } }
        }

        private static void TestFullscreenCaptureGating()
        {
            var provider = new SimulatedGamepadInputProvider();
            var polling = new GamepadPollingService(provider);
            using (var viewModel = new GamepadTesterViewModel(polling))
            {
                var raw = new GamepadState
                {
                    IsConnected = true,
                    ControllerName = "Simulated Xbox",
                    Layout = GamepadLayout.Xbox,
                    LeftStick = new StickState { X = 0.75f, Y = -0.25f },
                    RightStick = new StickState { X = -0.5f, Y = 0.5f },
                    LeftTrigger = 0.8f,
                    RightTrigger = 0.6f,
                    Buttons = new GamepadButtonState { South = true, DpadRight = true }
                };

                SetPrivateField(viewModel, "state", raw);
                SetPrivateField(viewModel, "latestInputState", raw);
                viewModel.IsFullscreenSimplifiedMode = true;

                var display = CreateFullscreenDisplayState(viewModel, raw);
                True(!display.Buttons.South && display.LeftTrigger == 0f, "Fullscreen button map stays neutral before its test starts");
                Equal(0f, display.LeftStick.X, "Fullscreen sticks stay neutral before diagnostics starts");
                True(viewModel.CanNavigateBack, "Fullscreen Back navigation is available before capture starts");

                viewModel.StartButtonCaptureCommand.Execute(null);
                True(!viewModel.CanNavigateBack, "Button capture blocks Fullscreen Back navigation");
                True(ShouldBlockFullscreenClose(viewModel), "Button capture blocks the Fullscreen window close path");
                display = CreateFullscreenDisplayState(viewModel, raw);
                True(display.Buttons.South && display.LeftTrigger > 0f, "Button capture exposes buttons and triggers");
                Equal(0.75f, display.LeftStick.X, "Button capture exposes live stick movement on the controller scheme");
                viewModel.StartButtonCaptureCommand.Execute(null);
                True(viewModel.CanNavigateBack, "Stopping button capture restores Fullscreen Back navigation");

                viewModel.StartStickCaptureCommand.Execute(null);
                display = CreateFullscreenDisplayState(viewModel, raw);
                True(!display.Buttons.South && display.LeftTrigger == 0f, "Stick diagnostics does not activate controller buttons");
                Equal(0.75f, display.LeftStick.X, "Stick diagnostics exposes live stick movement");
                True(viewModel.IsFullscreenInputCaptureActive, "Stick diagnostics engages the fullscreen navigation guard");
                True(!viewModel.CanNavigateBack, "Stick diagnostics blocks Fullscreen Back navigation");
                viewModel.StartStickCaptureCommand.Execute(null);
                True(!viewModel.IsFullscreenInputCaptureActive, "Stopping stick diagnostics releases the fullscreen navigation guard");
                True(viewModel.CanNavigateBack, "Stopping stick diagnostics restores Fullscreen Back navigation");

                viewModel.StartLatencyTestCommand.Execute(null);
                True(!viewModel.CanNavigateBack, "Latency capture blocks Fullscreen Back navigation");
                SetPrivateField(viewModel, "inputEventIntervalSamples", 4);
                SetPrivateField(viewModel, "pollingIntervalSamples", 7);
                SetPrivateField(viewModel, "latencyTestStartedAt", DateTime.UtcNow.AddSeconds(-5));
                viewModel.StartLatencyTestCommand.Execute(null);
                True(viewModel.CanNavigateBack, "Stopping latency capture restores Fullscreen Back navigation");
                var frozenDuration = viewModel.LatencyTestDurationLabel;

                InvokePrivate(viewModel, "UpdateLatency", raw);
                InvokePrivate(viewModel, "TrackButtonChange", "A", false, true);
                Equal(4, GetPrivateField<int>(viewModel, "inputEventIntervalSamples"), "Stopped latency does not collect input samples");
                Equal(7, GetPrivateField<int>(viewModel, "pollingIntervalSamples"), "Stopped latency does not collect polling samples");
                Equal(frozenDuration, viewModel.LatencyTestDurationLabel, "Stopped latency keeps a frozen session duration");
            }
        }

        private static GamepadState CreateFullscreenDisplayState(GamepadTesterViewModel viewModel, GamepadState raw)
        {
            var method = typeof(GamepadTesterViewModel).GetMethod(
                "CreateDisplayState",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (GamepadState)method.Invoke(viewModel, new object[] { raw });
        }

        private static bool ShouldBlockFullscreenClose(GamepadTesterViewModel viewModel)
        {
            var method = typeof(global::ControllerSessionManager.Tester.TesterIntegration).GetMethod(
                "ShouldBlockFullscreenClose",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)method.Invoke(null, new object[] { viewModel });
        }

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string name)
        {
            var field = instance.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)field.GetValue(instance);
        }

        private static object InvokePrivate(object instance, string name, params object[] arguments)
        {
            var method = instance.GetType().GetMethod(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return method.Invoke(instance, arguments);
        }

        private static void TestLocalizationParity()
        {
            var localization = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Localization"));
            var englishPath = Path.Combine(localization, "en_US.xaml");
            True(File.Exists(englishPath), "English localization exists");
            var expected = ReadKeys(englishPath);

            foreach (var path in Directory.GetFiles(localization, "*.xaml"))
            {
                var actual = ReadKeys(path);
                var missing = expected.Except(actual).ToArray();
                True(missing.Length == 0, Path.GetFileName(path) + " is missing: " + string.Join(", ", missing));
            }
        }

        private static void TestLatencyRateChartRendering()
        {
            var chart = new LatencyRateChart
            {
                Values = new[] { 125d, 118d, 132d, 127d, 140d },
                AccentBrush = Brushes.LimeGreen,
                GridBrush = Brushes.DimGray,
                LabelBrush = Brushes.LightGray,
                PlotBackgroundBrush = Brushes.Black
            };

            chart.Measure(new Size(620d, 216d));
            chart.Arrange(new Rect(0d, 0d, 620d, 216d));
            chart.UpdateLayout();

            var bitmap = new RenderTargetBitmap(620, 216, 96d, 96d, PixelFormats.Pbgra32);
            bitmap.Render(chart);
            var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
            bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

            True(pixels.Any(channel => channel != 0), "Latency chart produces a visible WPF render");

            var radar = new DiagnosticRadarChart
            {
                Values = new[] { 92d, 84d, 78d, 100d, 68d, 90d },
                Labels = new[] { "Center", "LS", "RS", "Triggers", "Controls", "Timing" },
                AccentBrush = Brushes.DeepSkyBlue,
                GridBrush = Brushes.DimGray,
                LabelBrush = Brushes.White
            };

            radar.Measure(new Size(520d, 340d));
            radar.Arrange(new Rect(0d, 0d, 520d, 340d));
            radar.UpdateLayout();

            var radarBitmap = new RenderTargetBitmap(520, 340, 96d, 96d, PixelFormats.Pbgra32);
            radarBitmap.Render(radar);
            var radarPixels = new byte[radarBitmap.PixelWidth * radarBitmap.PixelHeight * 4];
            radarBitmap.CopyPixels(radarPixels, radarBitmap.PixelWidth * 4, 0);
            True(radarPixels.Any(channel => channel != 0), "Diagnostic radar produces a visible WPF render");
        }

        private static HashSet<string> ReadKeys(string path)
        {
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            return new HashSet<string>(XDocument.Load(path).Root.Elements()
                .Select(element => (string)element.Attribute(x + "Key"))
                .Where(key => !string.IsNullOrWhiteSpace(key)));
        }

        private static void Equal<T>(T expected, T actual, string name)
        {
            executed++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(string.Format("{0}: expected {1}, got {2}", name, expected, actual));
            }
        }

        private static void True(bool value, string name)
        {
            executed++;
            if (!value)
            {
                throw new InvalidOperationException(name);
            }
        }
    }
}
