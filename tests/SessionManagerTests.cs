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
            AdaptiveScopePromotesSustainedAlternatingControllers();
            AdaptiveScopeDoesNotPromoteOneAccidentalControllerSwitch();
            PlayniteXInputBridgeUsesPathSlotInsteadOfInstanceId();
            Console.WriteLine("Session manager tests passed: 19 scenarios.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
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
