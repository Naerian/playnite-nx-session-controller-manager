using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Sessions;

internal static class SessionManagerTests
{
    private static readonly Guid GameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static int Main()
    {
        try
        {
            LocalMultiplayerKeepsPlayersIndependent();
            RetiredControllerRequiresFreshInput();
            MostRecentModeTracksOnlyOneController();
            ConnectedButUnusedControllerNeverJoins();
            AutomaticTakeoverCanResolveDuringGrace();
            LocalMultiplayerRequiresAnUnassignedReplacement();
            IntentionalInputRejectsNoiseAndReleases();
            TakeoverWaitsForNeutralControls();
            ActiveSessionsUseResponsivePolling();
            BriefReconnectCancelsIncidentWithoutInput();
            PauseTargetMustBelongToGameProcessTree();
            PauseRejectsUnrelatedForegroundWindow();
            PauseKeyProfilesAcceptOnlyDocumentedKeys();
            PauseAttemptIsOneShotPerIncident();
            OnlineOnlyMetadataIsStrongEvidence();
            GenericMultiplayerMetadataIsNotSessionEvidence();
            WeakNetworkEvidenceRetainsDisconnectOverlay();
            AdaptiveScopePromotesSustainedAlternatingControllers();
            AdaptiveScopeDoesNotPromoteOneAccidentalControllerSwitch();
            PlayniteXInputBridgeUsesPathSlotInsteadOfInstanceId();
            DualSenseUsbBatteryReportIsParsed();
            DualShock4UsbBatteryReportIsParsed();
            UnknownPlayStationReportIsRejected();
            BluetoothTransportOverridesEightBitDoReceiverHint();
            WindowsBluetoothBatteryUsesCoarseLevels();
            EquivalentHidPathsAreDeduplicated();
            PlayniteLifecycleOverridesSupplementalPresence();
            PlayniteLifecycleReceivesSupplementalCapabilities();
            InitializedAuthoritySuppressesUnmatchedProviderRows();
            ProviderFallbackRemainsAvailableBeforeSdkInitialization();
            EmptySdkInventoryUsesDegradedProviderFallback();
            NumericIdsDoNotMergeAcrossUnrelatedProviders();
            DisconnectEventMatchesStableXInputSlot();
            HidVidPidRestoresSupplementalCapabilities();
            HidVidPidParserRejectsInvalidPaths();
            RecentPrelaunchInputSeedsSession();
            StalePrelaunchInputDoesNotSeedSession();
            SessionStartFallbackArmsOnlyOneConnectedController();
            SessionStartFallbackUsesMostRecentController();
            RealInputReplacesSessionStartFallback();
            NewlyConnectedControllerResolvesIncidentWithoutInput();
            AlreadyConnectedControllerStillRequiresInputForTakeover();
            Console.WriteLine("Session manager tests passed: 42 scenarios.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void PlayniteLifecycleOverridesSupplementalPresence()
    {
        var sdk = Snapshot("playnite:path:XINPUT#0", "Playnite", 42, "XInput Controller #1",
            "XINPUT#0", false);
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, xinput }, true).Single();
        Equal(false, merged.IsConnected,
            "A connected XInput observation must not undo a Playnite disconnect callback.");
        Equal("Playnite", merged.LifecycleProviderId,
            "The projected row must identify Playnite as its lifecycle authority.");
    }

    private static void PlayniteLifecycleReceivesSupplementalCapabilities()
    {
        var sdk = Snapshot("playnite:path:XINPUT#0", "Playnite", 91, "XInput Controller #1",
            "XINPUT#0", true);
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.HardwareId = "2DC8:310B:receiver";
        xinput.BatteryLevel = "Full";
        xinput.ConnectionType = "Wireless";
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, xinput }, true).Single();
        Equal("8BitDo Ultimate 2 Wireless", merged.Name,
            "Provider metadata should enrich the SDK lifecycle row.");
        Equal("Full", merged.BatteryLevel, "Provider battery should remain available.");
        Equal("XInput", merged.ProviderId, "The actionable rumble provider should be retained.");
    }

    private static void InitializedAuthoritySuppressesUnmatchedProviderRows()
    {
        var sdk = Snapshot("playnite:path:HID#A", "Playnite", 7, "DualSense", "HID#A", true);
        var stale = Snapshot("xinput:slot:2", "XInput", 2, "Stale controller", string.Empty, true);
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, stale }, true);
        Equal(1, merged.Count,
            "Supplemental polling must not create a second physical controller after SDK initialization.");
    }

    private static void ProviderFallbackRemainsAvailableBeforeSdkInitialization()
    {
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "Xbox Controller", string.Empty, true);
        var merged = ControllerSnapshotMerger.Merge(new[] { xinput }, false);
        Equal(1, merged.Count,
            "XInput should remain a startup fallback if the SDK inventory is unavailable.");
    }

    private static void EmptySdkInventoryUsesDegradedProviderFallback()
    {
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "Xbox Controller", string.Empty, true);
        var merged = ControllerSnapshotMerger.Merge(new[] { xinput }, true);
        Equal(1, merged.Count,
            "A completely empty SDK registry must not make a controller disappear when XInput is available.");
    }

    private static void NumericIdsDoNotMergeAcrossUnrelatedProviders()
    {
        var sdk = Snapshot("playnite:path:HID#A", "Playnite", 0, "DirectInput device", "HID#A", true);
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "Different controller", string.Empty, true);
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, xinput }, true).Single();
        Equal("Playnite", merged.ProviderId,
            "An SDL instance ID and XInput slot with the same integer must never be conflated.");
    }

    private static void DisconnectEventMatchesStableXInputSlot()
    {
        var existing = Snapshot("playnite:path:XINPUT#0", "Playnite", 17,
            "XInput Controller #1", "XINPUT#0", true);
        var incoming = Snapshot("playnite:instance:93", "Playnite", 93,
            "XInput Controller #1", "device/xinput#0/controller", false);
        Equal(existing, ControllerSnapshotMerger.FindAuthoritativeEventTarget(incoming,
            new[] { existing }),
            "A reconnect-specific InstanceId must not prevent an immediate SDK disconnect from finding its XInput lifecycle row.");
    }

    private static void HidVidPidRestoresSupplementalCapabilities()
    {
        var sdk = Snapshot("playnite:path:HID#VID_054C&PID_0CE6#PLAYNITE", "Playnite", 800,
            "DualSense Wireless Controller", @"HID#VID_054C&PID_0CE6#PLAYNITE", true);
        var sdl = Snapshot("sdl:instance:12", "SDL", 12, "DualSense",
            @"\\?\hid#vid_054c&pid_0ce6#SDL", true);
        sdl.VendorId = 0x054C;
        sdl.ProductId = 0x0CE6;
        sdl.BatteryLevel = "Medium";
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, sdl }, true).Single();
        Equal("SDL", merged.ProviderId,
            "VID/PID evidence should recover SDL rumble capabilities when paths and instance IDs differ.");
        Equal("Medium", merged.BatteryLevel,
            "VID/PID evidence should preserve verified DualSense battery data.");
    }

    private static void HidVidPidParserRejectsInvalidPaths()
    {
        ushort vendor;
        ushort product;
        Equal(true, ControllerBridgeIdentity.TryGetVidPid(
            @"\\?\hid#vid_2dc8&pid_310b#device", out vendor, out product),
            "A normal HID path should expose VID and PID.");
        Equal((ushort)0x2DC8, vendor, "The HID vendor ID should be parsed as hexadecimal.");
        Equal((ushort)0x310B, product, "The HID product ID should be parsed as hexadecimal.");
        Equal(false, ControllerBridgeIdentity.TryGetVidPid("HID#VID_ZZZZ", out vendor, out product),
            "Malformed identifiers must not participate in physical-device correlation.");
    }
    private static void RecentPrelaunchInputSeedsSession()
    {
        var start = new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(-2)) }, start, true, false);
        Equal(1, manager.ActiveControllers.Count,
            "The controller used immediately before Desktop launches a game should seed the session.");
    }

    private static void StalePrelaunchInputDoesNotSeedSession()
    {
        var start = new DateTime(2026, 8, 16, 15, 30, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(-30)) }, start, true, false);
        Equal(0, manager.ActiveControllers.Count,
            "Old input must not make an idle connected controller part of a new session.");
    }
    private static ControllerDeviceSnapshot Snapshot(string id, string provider, int instance,
        string name, string path, bool connected)
    {
        return new ControllerDeviceSnapshot
        {
            ControllerId = id,
            ProviderId = provider,
            ProviderInstanceId = instance,
            Name = name,
            DetectedName = name,
            HardwareId = id,
            Path = path,
            IsConnected = connected,
            IsEnabled = true,
            ConnectionType = "Unknown",
            BatteryLevel = "Unknown",
            BatteryProviderId = "None",
            LastSeenUtc = DateTime.UtcNow
        };
    }

    private static void BluetoothTransportOverridesEightBitDoReceiverHint()
    {
        var bluetoothPath = @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012";
        Equal("Bluetooth", ControllerDeviceIdentity.GetConnectionType(
            "8BitDo Ultimate 2 Wireless", 0x2DC8, 0x6012, bluetoothPath),
            "A verified Bluetooth HID path must override model-level receiver hints.");
        Equal("Wired", ControllerDeviceIdentity.GetConnectionType(
            "8BitDo Ultimate 2 Wireless", 0x2DC8, 0x6012, @"\\?\usb#vid_2dc8&pid_6012"),
            "A USB path must remain wired even when the same product supports wireless modes.");
        Equal("Wireless", ControllerDeviceIdentity.GetConnectionType(
            "8BitDo Ultimate 2 Wireless", 0x2DC8, 0x310B, @"\\?\hid#vid_2dc8&pid_310b&ig_00"),
            "A receiver must use generic wireless when the path provides no Bluetooth or USB evidence.");
    }

    private static void WindowsBluetoothBatteryUsesCoarseLevels()
    {
        Equal("Low", WindowsBluetoothBatteryProvider.ToLevel(24),
            "The Windows value shown for the tester's controller should map to Low.");
        Equal("Full", WindowsBluetoothBatteryProvider.ToLevel(83),
            "The locally observed Windows value should map to Full.");
        Equal(true, WindowsBluetoothBatteryProvider.IsBluetoothPath(
            @"HID\{00001812-0000-1000-8000-00805F9B34FB}_DEV_VID&122DC8_PID&6012"),
            "The Bluetooth LE HID service UUID must be recognized.");
    }

    private static void EquivalentHidPathsAreDeduplicated()
    {
        var basePath = @"\\?\hid#vid_2dc8&pid_6012#device#{4d1e55b2-f16f-11cf-88cb-001111000030}";
        Equal(true, ControllerBridgeIdentity.PathsReferToSameDevice(basePath,
            @"\\?\HID\VID_2DC8&PID_6012\DEVICE"),
            "SDL and Playnite representations of the same HID interface should collapse.");
        Equal(false, ControllerBridgeIdentity.PathsReferToSameDevice(basePath,
            @"\\?\HID\VID_2DC8&PID_6012\OTHER"),
            "Two distinct HID instances must not be merged merely because VID/PID match.");
    }

    private static void DualSenseUsbBatteryReportIsParsed()
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[53] = 0x06;
        string level;
        Equal(true, PlayStationHidBatteryProvider.TryParseReport(0x0CE6, report, out level),
            "A documented DualSense USB status report should be accepted.");
        Equal("Medium", level, "DualSense capacity 6 should map to the coarse medium level.");
    }

    private static void DualShock4UsbBatteryReportIsParsed()
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[30] = 0x03;
        string level;
        Equal(true, PlayStationHidBatteryProvider.TryParseReport(0x09CC, report, out level),
            "A documented DualShock 4 USB status report should be accepted.");
        Equal("Medium", level, "DualShock 4 capacity 3 should map to the coarse medium level.");
    }

    private static void UnknownPlayStationReportIsRejected()
    {
        string level;
        Equal(false, PlayStationHidBatteryProvider.TryParseReport(0x0CE6, new byte[64], out level),
            "A report without the documented report ID must be rejected.");
        Equal(false, PlayStationHidBatteryProvider.TryParseReport(0x1234, new byte[64], out level),
            "An unverified product ID must never use the Sony provider.");
    }

    private static void PlayniteXInputBridgeUsesPathSlotInsteadOfInstanceId()
    {
        Equal((int?)0, ControllerBridgeIdentity.GetXInputSlot("XINPUT#0"),
            "A Playnite reconnect may change InstanceId but must retain the physical XInput slot from Path.");
        Equal((int?)3, ControllerBridgeIdentity.GetXInputSlot("device/xinput#3/controller"),
            "XInput path matching should be case-insensitive and accept normalized separators.");
        Equal((int?)null, ControllerBridgeIdentity.GetXInputSlot("HID#VID_054C&PID_0CE6"),
            "A non-XInput HID path must not be merged into an XInput slot.");
    }

    private static void OnlineOnlyMetadataIsStrongEvidence()
    {
        string match;
        Equal(true, OnlineSessionDetector.HasOnlineMetadata(new[] { "Single Player", "Online-only" }, out match),
            "Online-only metadata should prevent forced suspension.");
        Equal("Online-only", match, "The matched metadata value should be retained for diagnostics.");
    }

    private static void GenericMultiplayerMetadataIsNotSessionEvidence()
    {
        string match;
        Equal(false, OnlineSessionDetector.HasOnlineMetadata(new[] { "Online Co-op", "Multiplayer" }, out match),
            "A game capability alone must not claim that the current session is online.");
    }

    private static void WeakNetworkEvidenceRetainsDisconnectOverlay()
    {
        Equal(false, new OnlineDetectionResult
        {
            Evidence = OnlineEvidenceKind.EstablishedTcpConnection
        }.IsNotificationOnlySafe,
            "One established TCP connection may block forced suspension but must not hide the disconnect overlay.");
        Equal(true, new OnlineDetectionResult
        {
            Evidence = OnlineEvidenceKind.Metadata
        }.IsNotificationOnlySafe,
            "Explicit online-only metadata may use the non-blocking notification path.");
    }

    private static void AdaptiveScopePromotesSustainedAlternatingControllers()
    {
        var start = new DateTime(2026, 8, 16, 18, 0, 0, DateTimeKind.Utc);
        var detector = new AdaptiveSessionScopeDetector();
        detector.Reset(start);
        detector.Observe(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1));
        detector.Observe(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(2)) }, start.AddSeconds(2));
        detector.Observe(new[] { Device("A", start.AddSeconds(3)), Device("B", start.AddSeconds(2)) }, start.AddSeconds(3));
        Equal(true, detector.Observe(new[] { Device("A", start.AddSeconds(3)),
            Device("B", start.AddSeconds(4)) }, start.AddSeconds(4)),
            "Repeated alternating input should promote the session to local multiplayer.");
    }

    private static void AdaptiveScopeDoesNotPromoteOneAccidentalControllerSwitch()
    {
        var start = new DateTime(2026, 8, 16, 18, 10, 0, DateTimeKind.Utc);
        var detector = new AdaptiveSessionScopeDetector();
        detector.Reset(start);
        detector.Observe(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1));
        detector.Observe(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(2)) }, start.AddSeconds(2));
        detector.Observe(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(3)) }, start.AddSeconds(3));
        Equal(false, detector.Observe(new[] { Device("A", start.AddSeconds(1)),
            Device("B", start.AddSeconds(4)) }, start.AddSeconds(4)),
            "Trying one controller and then continuing with another must remain a single-player session.");
    }

    private static void LocalMultiplayerKeepsPlayersIndependent()
    {
        var start = new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(1)) },
            start.AddSeconds(1), true, true);
        manager.Update(new[] { Device("B", start.AddSeconds(1)) }, start.AddSeconds(2), true, true);
        manager.Tick(start.AddSeconds(4), TimeSpan.FromSeconds(1));
        manager.Update(new[] { Device("B", start.AddSeconds(5)) }, start.AddSeconds(5), true, true);

        Equal(2, manager.ActiveControllers.Count, "An existing co-op player must not replace a missing player.");
        Equal(1, manager.ConfirmedDisconnectCount, "The missing co-op controller must remain protected.");
    }

    private static void RetiredControllerRequiresFreshInput()
    {
        var start = new DateTime(2026, 8, 16, 11, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1), true, true);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(2), true, true);
        manager.Tick(start.AddSeconds(4), TimeSpan.FromSeconds(1));
        manager.Update(new[] { Device("C", start.AddSeconds(5)) }, start.AddSeconds(5), true, true);
        Equal("C", manager.ActiveControllers.Single().ControllerKey, "A new controller should take over.");

        manager.Update(new[] { Device("A", start.AddSeconds(1)), Device("C", start.AddSeconds(5)) },
            start.AddSeconds(6), true, true);
        Equal(1, manager.ActiveControllers.Count, "Stale input must not reactivate a retired controller.");

        manager.Update(new[] { Device("A", start.AddSeconds(7)), Device("C", start.AddSeconds(5)) },
            start.AddSeconds(7), true, true);
        Equal(2, manager.ActiveControllers.Count, "Fresh input may add the controller back in co-op mode.");
    }

    private static void MostRecentModeTracksOnlyOneController()
    {
        var start = new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1), true, false);
        manager.Update(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(2)) },
            start.AddSeconds(2), true, false);
        Equal("B", manager.ActiveControllers.Single().ControllerKey,
            "Single-player mode should follow the most recently used controller.");

        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(3), true, false);
        manager.Tick(start.AddSeconds(5), TimeSpan.FromSeconds(1));
        manager.Update(new[] { Device("A", start.AddSeconds(6)) }, start.AddSeconds(6), true, false);
        Equal("A", manager.ActiveControllers.Single().ControllerKey,
            "Fresh input from another controller should resolve a confirmed single-player incident.");
    }

    private static void BriefReconnectCancelsIncidentWithoutInput()
    {
        var start = new DateTime(2026, 8, 16, 13, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1), false, true);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(2), false, true);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(2.5), false, true);
        Equal(0, manager.SuspectedDisconnectCount, "A reconnect during grace must cancel the incident.");
    }

    private static void IntentionalInputRejectsNoiseAndReleases()
    {
        Equal(false, IntentionalInputDetector.IsXInputIntentional(
            1, 0, 0, 0, 0, 0, 200, 600, 0, 0, 0, 0, 0, 0),
            "Button releases and small analog noise must not count as participation.");
        Equal(true, IntentionalInputDetector.IsXInputIntentional(
            0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            "A new digital press must count as intentional input.");
        Equal(false, IntentionalInputDetector.IsSdlGameplayButton(5),
            "SDL Guide/PS/Home must not count as gameplay participation.");
        Equal(true, IntentionalInputDetector.IsSdlGameplayButton(0),
            "SDL gameplay buttons must remain eligible.");
        Equal(false, IntentionalInputDetector.IsSdlIntentional(
            1, 0, new short[] { 0 }, new short[] { 100 }, new short[] { 500 }, 17, 17, false),
            "SDL releases and minor axis drift must not count as participation.");
        Equal(true, IntentionalInputDetector.IsSdlIntentional(
            0, 0, new short[] { 0 }, new short[] { 0 }, new short[] { 15000 }, 17, 17, false),
            "A deliberate SDL axis movement must count as intentional input.");
        Equal(false, IntentionalInputDetector.IsSdlIntentional(
            0, 0, new short[] { -32768 }, new short[] { 0 }, new short[] { -32768 }, 17, 17, false),
            "Returning to the SDL baseline while powering off must not count as input.");
    }

    private static void TakeoverWaitsForNeutralControls()
    {
        var start = new DateTime(2026, 8, 16, 12, 55, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1), true, false);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(2), true, false);

        var replacement = Device("B", start.AddSeconds(3));
        replacement.IsInputNeutral = false;
        replacement.InputNeutralSinceUtc = null;
        manager.Update(new[] { replacement }, start.AddSeconds(3), true, false);
        Equal("A", manager.ActiveControllers.Single().ControllerKey,
            "A held replacement input must not dismiss the incident.");

        replacement.IsInputNeutral = true;
        replacement.InputNeutralSinceUtc = start.AddSeconds(3.1);
        manager.Update(new[] { replacement }, start.AddSeconds(3.15), true, false);
        Equal("A", manager.ActiveControllers.Single().ControllerKey,
            "The replacement must remain neutral for the settling window.");
        manager.Update(new[] { replacement }, start.AddSeconds(3.2), true, false);
        Equal("B", manager.ActiveControllers.Single().ControllerKey,
            "A settled replacement should safely resolve the incident.");
    }

    private static void ActiveSessionsUseResponsivePolling()
    {
        Equal(TimeSpan.FromMilliseconds(50), InputPollingPolicy.GetInterval(true),
            "Active game sessions must sample short stick movements reliably.");
        Equal(TimeSpan.FromMilliseconds(250), InputPollingPolicy.GetInterval(false),
            "Idle inventory polling should remain low frequency.");
    }

    private static void ConnectedButUnusedControllerNeverJoins()
    {
        var start = new DateTime(2026, 8, 16, 12, 30, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { ConnectedDevice("DualSense"), Device("8BitDo", start.AddSeconds(1)) },
            start.AddSeconds(1), true, false);
        Equal("8BitDo", manager.ActiveControllers.Single().ControllerKey,
            "A controller that is merely connected must not become the session owner.");

        manager.Update(new[] { Device("8BitDo", start.AddSeconds(1)) }, start.AddSeconds(2), true, false);
        Equal(0, manager.SuspectedDisconnectCount,
            "Disconnecting an unused controller must not create an incident.");
    }

    private static void SessionStartFallbackArmsOnlyOneConnectedController()
    {
        var start = new DateTime(2026, 8, 16, 12, 35, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        Equal(true, manager.SeedInitialController(new[]
        {
            ConnectedDevice("DualSense"),
            ConnectedDevice("8BitDo")
        }, start), "A session with connected controllers must arm one conservative fallback owner.");
        Equal(1, manager.ActiveControllers.Count,
            "The startup fallback must never infer multiple local players from connection alone.");

        var owner = manager.ActiveControllers.Single().ControllerKey;
        var unused = owner == "DualSense" ? "8BitDo" : "DualSense";
        manager.Update(new[] { ConnectedDevice(owner) }, start.AddSeconds(1), true, false);
        Equal(0, manager.SuspectedDisconnectCount,
            "Removing an unassigned startup controller must not create an incident.");

        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(2), true, false);
        Equal(1, manager.SuspectedDisconnectCount,
            "Removing the conservative startup owner must create an incident.");
    }

    private static void SessionStartFallbackUsesMostRecentController()
    {
        var start = new DateTime(2026, 8, 16, 12, 40, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.SeedInitialController(new[]
        {
            Device("DualSense", start.AddMinutes(-4)),
            Device("8BitDo", start.AddMinutes(-1))
        }, start);
        Equal("8BitDo", manager.ActiveControllers.Single().ControllerKey,
            "The freshest known controller must win the startup fallback even when its input is stale.");
    }

    private static void RealInputReplacesSessionStartFallback()
    {
        var start = new DateTime(2026, 8, 16, 12, 42, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.SeedInitialController(new[]
        {
            ConnectedDevice("A-inferred"),
            ConnectedDevice("B-real")
        }, start);

        manager.Update(new[]
        {
            ConnectedDevice("A-inferred"),
            Device("B-real", start.AddSeconds(1))
        }, start.AddSeconds(1), true, false);
        Equal("B-real", manager.ActiveControllers.Single().ControllerKey,
            "Real post-start input must replace an inferred startup owner immediately.");
    }

    private static void NewlyConnectedControllerResolvesIncidentWithoutInput()
    {
        var start = new DateTime(2026, 8, 16, 12, 44, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.SeedInitialController(new[] { ConnectedDevice("A") }, start);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(1), true, false);
        manager.Update(new[] { ConnectedDevice("B") }, start.AddSeconds(2), true, false);
        Equal("B", manager.ActiveControllers.Single().ControllerKey,
            "A controller connected after the incident must be accepted as an intentional replacement.");
        Equal(0, manager.SuspectedDisconnectCount,
            "A newly connected replacement must resolve the incident immediately.");
    }

    private static void AlreadyConnectedControllerStillRequiresInputForTakeover()
    {
        var start = new DateTime(2026, 8, 16, 12, 46, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.SeedInitialController(new[] { ConnectedDevice("A"), ConnectedDevice("B") }, start);
        var owner = manager.ActiveControllers.Single().ControllerKey;
        var spare = owner == "A" ? "B" : "A";
        manager.Update(new[] { ConnectedDevice(spare) }, start.AddSeconds(1), true, false);
        Equal(owner, manager.ActiveControllers.Single().ControllerKey,
            "A controller that was already connected must not take over without intentional input.");
        Equal(1, manager.SuspectedDisconnectCount,
            "The incident must remain until the pre-existing spare is actually used.");
    }

    private static void AutomaticTakeoverCanResolveDuringGrace()
    {
        var start = new DateTime(2026, 8, 16, 12, 45, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)) }, start.AddSeconds(1), true, false);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(2), true, false);
        manager.Update(new[] { Device("B", start.AddSeconds(3)) }, start.AddSeconds(3), true, false);

        Equal("B", manager.ActiveControllers.Single().ControllerKey,
            "Fresh input from another controller should take over during the grace period.");
        Equal(0, manager.SuspectedDisconnectCount,
            "An automatic takeover during grace must resolve before an overlay appears.");
    }

    private static void LocalMultiplayerRequiresAnUnassignedReplacement()
    {
        var start = new DateTime(2026, 8, 16, 12, 50, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        manager.Update(new[] { Device("A", start.AddSeconds(1)), Device("B", start.AddSeconds(1)) },
            start.AddSeconds(1), true, true);
        manager.Update(new[] { Device("B", start.AddSeconds(3)) }, start.AddSeconds(2), true, true);
        Equal(2, manager.ActiveControllers.Count,
            "An existing co-op player must not replace a missing player.");

        manager.Update(new[] { Device("B", start.AddSeconds(3)), Device("C", start.AddSeconds(4)) },
            start.AddSeconds(4), true, true);
        Equal(true, manager.ActiveControllers.Any(a => a.ControllerKey == "B"),
            "The other active co-op player must remain assigned.");
        Equal(true, manager.ActiveControllers.Any(a => a.ControllerKey == "C"),
            "A fresh unassigned controller should replace the missing co-op controller.");
        Equal(2, manager.ActiveControllers.Count,
            "Replacing a missing co-op controller must not create an extra player.");
    }

    private static void PauseTargetMustBelongToGameProcessTree()
    {
        var parents = new Dictionary<int, int>
        {
            { 200, 100 },
            { 300, 200 },
            { 400, 999 },
            { 500, 600 },
            { 600, 500 }
        };
        Equal(true, GamePauseService.IsProcessInTree(100, 100, parents),
            "The launched game process must be accepted.");
        Equal(true, GamePauseService.IsProcessInTree(300, 100, parents),
            "A descendant game process must be accepted.");
        Equal(false, GamePauseService.IsProcessInTree(400, 100, parents),
            "An unrelated foreground process must be rejected.");
        Equal(false, GamePauseService.IsProcessInTree(500, 100, parents),
            "A malformed process cycle must be rejected safely.");
        Equal(IntPtr.Size == 8 ? 40 : 28, GamePauseService.NativeInputSize,
            "The SendInput structure must match the native Windows ABI.");
    }

    private static void PauseRejectsUnrelatedForegroundWindow()
    {
        var receipt = new GamePauseService().TrySendEscape(int.MaxValue, DateTime.UtcNow);
        Equal(false, receipt.WasSent,
            "The pause service must never send input when the foreground process is unrelated.");
    }

    private static void PauseKeyProfilesAcceptOnlyDocumentedKeys()
    {
        Equal(true, GamePauseService.IsSupportedKey("Escape"), "Escape must be supported.");
        Equal(true, GamePauseService.IsSupportedKey("P"), "Letter pause keys must be supported.");
        Equal(true, GamePauseService.IsSupportedKey("F12"), "Function keys must be supported.");
        Equal(false, GamePauseService.IsSupportedKey("Ctrl+P"),
            "Key combinations must be rejected until modifiers are implemented safely.");
        Equal(false, GamePauseService.IsSupportedKey("F13"), "Unsupported function keys must be rejected.");
    }

    private static void PauseAttemptIsOneShotPerIncident()
    {
        var gate = new PauseAttemptGate();
        Equal(true, gate.TryBegin(), "The first confirmed controller may request pause.");
        Equal(false, gate.TryBegin(), "A second co-op disconnect must not repeat the pause key.");
        gate.Reset();
        Equal(true, gate.TryBegin(), "A later independent incident may request pause again.");
    }

    private static ControllerDeviceSnapshot Device(string key, DateTime inputUtc)
    {
        return new ControllerDeviceSnapshot
        {
            ControllerId = key,
            HardwareId = key,
            Name = key,
            ProviderId = "XInput",
            IsConnected = true,
            LastInputUtc = inputUtc,
            LastInputKind = InputEvidenceKind.DigitalButton.ToString(),
            IsInputNeutral = true,
            InputNeutralSinceUtc = inputUtc == DateTime.MinValue ? inputUtc : inputUtc.AddSeconds(-1)
        };
    }

    private static ControllerDeviceSnapshot ConnectedDevice(string key)
    {
        var device = Device(key, DateTime.MinValue);
        device.LastInputUtc = null;
        return device;
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
