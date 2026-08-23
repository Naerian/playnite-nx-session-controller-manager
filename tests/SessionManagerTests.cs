using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Overlay;
using ControllerSessionManager.PlayniteIntegration;
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
            PauseAttemptIsOneShotPerIncident();
            OnlineOnlyMetadataIsStrongEvidence();
            GenericMultiplayerMetadataIsNotSessionEvidence();
            WeakNetworkEvidenceRetainsDisconnectOverlay();
            AdaptiveScopePromotesSustainedAlternatingControllers();
            AdaptiveScopeDoesNotPromoteOneAccidentalControllerSwitch();
            PlayniteXInputBridgeUsesPathSlotInsteadOfInstanceId();
            DualSenseUsbBatteryReportIsParsed();
            DualSenseSyntheticDongleReportIsRejected();
            DualShock4UsbBatteryReportIsParsed();
            UnknownPlayStationReportIsRejected();
            LowBatteryNotificationTrackerLatchesAndRecovers();
            BluetoothTransportOverridesEightBitDoReceiverHint();
            EightBitDoXInputWrapperIsNotBluetooth();
            HidPathMetadataRestoresConnectionWithoutSdl();
            BluetoothLeHidPathExposesVidPid();
            PointerDevicesAreNotTreatedAsControllers();
            GenericUsbHidLeftoverIsNotPublishedAsInventory();
            GenericPlayniteNameUsesMappedIdentity();
            GenericHidNameDoesNotReplacePlayniteIdentity();
            WindowsBluetoothBatteryUsesCoarseLevels();
            XInputWrapperIsNotUsedAsBluetoothBatteryContainer();
            BluetoothLeBatteryAddressIsParsedFromSiblingNodes();
            PlayniteBluetoothRowReceivesHidBatteryWithoutXInput();
            BluetoothHardwareIdsAcceptVendorEncodings();
            EquivalentHidPathsAreDeduplicated();
            PlayniteLifecycleOverridesSupplementalPresence();
            PlayniteLifecycleReceivesSupplementalCapabilities();
            InitializedAuthoritySuppressesUnmatchedProviderRows();
            DistinctVidXInputPadIsListedBesidePlaynitePad();
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
            XInputDongleReconnectRestoresSdkDisconnect();
            DongleReconnectResolvesVolatileXInputSlot();
            MergeKeepsHardwareIdWhenXInputSlotIsVolatile();
            PlayniteDongleHidMatchesSoleXInputWithoutPath();
            ControllerIconsFollowVidAndDefaultFallback();
            ColorPickerStoresOpacityInHex();
            ColorPickerMathRoundTripsHueAndOpacity();
            DisplayHoldKeepsSettledControllerDuringHotPlugGap();
            DisplayHoldIgnoresUnsettledReplacement();
            DisplayHoldAppliesSameVendorTransportImmediately();
            DisplayHoldCollapsesDongleAndBluetoothOverlap();
            DisplayHoldAddsSecondPadImmediately();
            UnknownConnectionIsExcludedFromDisplayAndToasts();
            SameModelHidIsNotListedBesideXInput();
            DongleXInputSupersedesStalePlayniteBluetooth();
            IndependentBluetoothPadIsKeptBesideXInput();
            BluetoothPlayniteDoesNotBindDongleXInput();
            DonglePlayniteDoesNotInheritBluetoothFromHidLeftover();
            XboxBluetoothMayBindXInputCapability();
            DisplayHoldPromotesVolatileDongleOverHeldBluetooth();
            TransportSwitchHonorsWrapperDisconnectWhenPeerConnected();
            BluetoothDisconnectHonoredWhileXInputStillPresent();
            GenericIconIsKeptWhenChosen();
            OverlayIpcAcceptsGamepadSilhouettes();
            Console.WriteLine("Session manager tests passed: 77 scenarios.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void XInputDongleReconnectRestoresSdkDisconnect()
    {
        Equal(false, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(true, true, false, 3),
            "A still-connected Playnite row must not be rewritten.");
        Equal(false, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, false, true, 3),
            "XInput must be present before a Playnite disconnect can recover.");
        Equal(false, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, true, false, 2),
            "An SDK disconnect waits for stable XInput samples before recovering.");
        Equal(true, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, true, false, 3),
            "Three XInput samples after an SDK disconnect must restore the dongle pad.");
        Equal(true, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, true, true, 1),
            "A provider-owned disconnect may restore as soon as XInput returns.");
        Equal(false, ProviderLifecyclePolicy.ShouldMarkDisconnected(2),
            "Two missing XInput samples are still treated as a transient gap.");
        Equal(true, ProviderLifecyclePolicy.ShouldMarkDisconnected(3),
            "Three missing XInput samples still confirm a fallback disconnect.");
        Equal(false, ProviderLifecyclePolicy.ShouldHonorSdkDisconnect(true),
            "Playnite disconnect callbacks must not drop a dongle pad while XInput still sees it.");
        Equal(true, ProviderLifecyclePolicy.ShouldHonorSdkDisconnect(false),
            "A Playnite disconnect is honored once XInput has also dropped the slot.");
    }

    private static void TransportSwitchHonorsWrapperDisconnectWhenPeerConnected()
    {
        Equal(true, ProviderLifecyclePolicy.ShouldHonorSdkDisconnect(true, true, true),
            "A dongle Playnite row must disconnect once Bluetooth of the same VID is already connected.");
    }

    private static void BluetoothDisconnectHonoredWhileXInputStillPresent()
    {
        Equal(true, ProviderLifecyclePolicy.ShouldHonorSdkDisconnect(true, false, false),
            "A Bluetooth HID disconnect must be honored even if an XInput slot is still occupied.");
    }

    private static void DongleReconnectResolvesVolatileXInputSlot()
    {
        var start = new DateTime(2026, 8, 19, 21, 0, 0, DateTimeKind.Utc);
        var manager = new GameSessionManager();
        manager.Start(GameId, start);
        var dongle = Device("hardware:2DC8:310B:1", start);
        dongle.VendorId = 0x2DC8;
        dongle.ProductId = 0x310B;
        manager.Update(new[] { dongle }, start, true, false);
        manager.Update(new ControllerDeviceSnapshot[0], start.AddSeconds(1), true, false);
        Equal(1, manager.SuspectedDisconnectCount,
            "A dongle that drops off XInput must still raise a disconnect incident.");

        var slot = ConnectedDevice("xinput:slot:0");
        slot.VendorId = 0x2DC8;
        slot.ProductId = 0x310B;
        slot.HardwareId = "xinput:slot:0";
        manager.Update(new[] { slot }, start.AddSeconds(2), true, false);
        Equal(0, manager.SuspectedDisconnectCount,
            "The same dongle VID/PID on a volatile XInput slot must close the overlay.");
        Equal("hardware:2DC8:310B:1", manager.ActiveControllers.Single().ControllerKey,
            "The session must keep the stable hardware id across the dongle reconnect.");
    }

    private static void MergeKeepsHardwareIdWhenXInputSlotIsVolatile()
    {
        var sdk = Snapshot("playnite:path:HID#IG", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        sdk.HardwareId = "hardware:2DC8:310B:1";
        sdk.VendorId = 0x2DC8;
        sdk.ProductId = 0x310B;
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "XInput Controller (Player 1)",
            string.Empty, true);
        xinput.HardwareId = "xinput:slot:0";
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, xinput }, true).Single();
        Equal("hardware:2DC8:310B:1", merged.HardwareId,
            "A dongle reconnect must not replace the stable hardware id with xinput:slot:N.");
    }

    private static void PlayniteDongleHidMatchesSoleXInputWithoutPath()
    {
        var sdk = Snapshot("playnite:path:HID#IG", "Playnite", 8, "XInput Controller #1",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", false);
        sdk.VendorId = 0x2DC8;
        sdk.ProductId = 0x310B;
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "XInput Controller (Player 1)",
            string.Empty, true);
        Equal("xinput:slot:0", ControllerSnapshotMerger.FindCapability(sdk, new[] { xinput }).ControllerId,
            "A Playnite HID &ig_ row must correlate with the only connected XInput slot.");
        Equal(true, ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, true, false, 3),
            "That correlated XInput slot can restore the Playnite disconnect after three samples.");
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

    private static void DistinctVidXInputPadIsListedBesidePlaynitePad()
    {
        var dualsense = Snapshot("playnite:path:HID#DS", "Playnite", 1, "DualSense",
            @"\\?\hid#vid_054c&pid_0ce6", true);
        dualsense.VendorId = 0x054C;
        dualsense.ProductId = 0x0CE6;
        var eightBitDo = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        eightBitDo.VendorId = 0x2DC8;
        eightBitDo.ProductId = 0x310B;
        eightBitDo.ConnectionType = "Wireless";
        var connected = ControllerSnapshotMerger.Merge(new[] { dualsense, eightBitDo }, true)
            .Where(a => a.IsConnected).ToList();
        Equal(2, connected.Count,
            "An 8BitDo XInput dongle must stay listed in Mandos beside a DualSense Playnite row.");
        Equal(true, connected.Any(a => a.VendorId == 0x054C),
            "DualSense must remain in the merged inventory.");
        Equal(true, connected.Any(a => a.VendorId == 0x2DC8),
            "The 8BitDo XInput observation must not be dropped just because Playnite already listed another pad.");
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
            "A 2.4GHz receiver PID (0x310B) is a USB device and must not appear in BTHENUM; " +
            "the name-based heuristic must resolve it to Wireless.");
        Equal("Bluetooth", ControllerDeviceIdentity.GetConnectionType(
            "Xbox Wireless Controller Bluetooth", 0x045E, 0x0B13,
            @"\\?\hid#vid_045e&pid_0b13&ig_00"),
            "An XInput wrapper whose product name includes Bluetooth stays Bluetooth without brand PID rules.");
    }

    private static void EightBitDoXInputWrapperIsNotBluetooth()
    {
        Equal("Wireless", ControllerDeviceIdentity.GetConnectionType(
            "8BitDo Ultimate 2 Wireless", 0x2DC8, 0x310A,
            @"\\?\hid#vid_2dc8&pid_310a&ig_00"),
            "An 8BitDo XInput dongle wrapper must stay wireless even if a sibling BLE HID exists.");
        Equal("Unknown", ControllerDeviceIdentity.GetConnectionType(
            "XInput Controller (Player 1)", 0x2DC8, 0x310A,
            @"\\?\hid#vid_2dc8&pid_310a&ig_00"),
            "A generic XInput wrapper name must not inherit Bluetooth from a BLE alias PID.");
        Equal("Unknown", ControllerDeviceIdentity.GetConnectionType(
            "XInput Controller (Player 1)", 0x2DC8, 0x310B, string.Empty),
            "An XInput slot without a HID path must not inherit Bluetooth from leftover BLE nodes.");
        Equal("Wireless", ControllerDeviceIdentity.GetConnectionType(
            "8BitDo Ultimate 2 Wireless", 0x2DC8, 0x310B, string.Empty),
            "A wireless product name on an empty XInput path stays wireless, not Bluetooth.");
        var wrapper = new ControllerMetadata
        {
            DisplayName = "8BitDo Ultimate 2 Wireless",
            DevicePath = @"\\?\hid#vid_2dc8&pid_310a&ig_00",
            VendorId = 0x2DC8,
            ProductId = 0x310A,
            ConnectionType = "Wireless"
        };
        Equal(false, new WindowsBluetoothBatteryProvider().Supports(wrapper),
            "Windows Bluetooth battery must not attach to an XInput dongle wrapper.");
        var bluetooth = new ControllerMetadata
        {
            DisplayName = "8BitDo Ultimate 2 Wireless",
            DevicePath =
                @"HID\{00001812-0000-1000-8000-00805F9B34FB}_DEV_VID&122DC8_PID&6012",
            VendorId = 0x2DC8,
            ProductId = 0x6012,
            ConnectionType = "Bluetooth"
        };
        Equal(true, new WindowsBluetoothBatteryProvider().Supports(bluetooth),
            "A real Bluetooth HID path must remain eligible for Windows battery lookup.");
        var mislabeledWireless = new ControllerMetadata
        {
            DisplayName = "8BitDo Ultimate 2 Wireless",
            DevicePath =
                @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6013",
            VendorId = 0x2DC8,
            ProductId = 0x6013,
            ConnectionType = "Wireless"
        };
        Equal(true, new WindowsBluetoothBatteryProvider().Supports(mislabeledWireless),
            "A Bluetooth HID mislabelled Wireless from the product name must still read Windows battery.");
        Equal("8BitDo Ultimate 2 Wireless",
            ControllerDeviceIdentity.GetDisplayName(string.Empty, 0x2DC8, 0x6013),
            "Ultimate 2 Bluetooth PID 6013 must map like 6012.");
    }

    private static void HidPathMetadataRestoresConnectionWithoutSdl()
    {
        ControllerMetadata metadata;
        Equal(true, HidDiagnosticsService.TryBuildMetadataFromPath(
            @"\\?\hid#vid_2dc8&pid_310a&ig_01#8&3946da90&0&0000", null, out metadata),
            "An XInput wrapper path should yield HID metadata without opening SDL.");
        Equal((ushort)0x2DC8, metadata.VendorId, "The 8BitDo vendor ID should be parsed from the HID path.");
        Equal((ushort)0x310A, metadata.ProductId, "The Ultimate 2C XInput PID should be parsed from the HID path.");
        Equal(false, HidDiagnosticsService.TryBuildMetadataFromPath(
            @"\\?\hid#vid_2dc8&pid_310a&mi_01&col01#7&184bcbf2&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}\kbd",
            null, out metadata),
            "Keyboard collections on the same receiver must not become controller rows.");
        Equal(false, HidDiagnosticsService.TryBuildMetadataFromPath(
            @"\\?\hid#vid_046d&pid_c092#hid_device_system_mouse",
            null, out metadata),
            "A Windows mouse collection must not become a controller row.");
    }

    private static void PointerDevicesAreNotTreatedAsControllers()
    {
        Equal(true, ControllerDeviceIdentity.IsLikelyNonController(
            "HID-compliant mouse", @"\\?\HID#VID_046D&PID_C092"),
            "A mouse product string must not enter the controller inventory.");
        Equal(true, ControllerDeviceIdentity.IsLikelyNonController(
            "Ratón USB", @"\\?\HID#VID_046D&PID_C539"),
            "A Spanish mouse label must not enter the controller inventory.");
        Equal(false, ControllerDeviceIdentity.IsLikelyNonController(
            "DualSense Wireless Controller", @"\\?\HID#VID_054C&PID_0CE6"),
            "A real gamepad must keep passing the non-controller filter.");
    }

    private static void GenericUsbHidLeftoverIsNotPublishedAsInventory()
    {
        Equal(false, ControllerDeviceIdentity.IsPublishableHidCapability(
            "Game Controller", @"\\?\hid#vid_1234&pid_5678"),
            "An unnamed USB HID collection must not become a Mandos row.");
        Equal(true, ControllerDeviceIdentity.IsPublishableHidCapability(
            "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012"),
            "A Bluetooth LE gamepad HID path must still enrich Playnite battery and transport.");
        Equal(true, ControllerDeviceIdentity.IsPublishableHidCapability(
            "DualSense Wireless Controller", @"\\?\hid#vid_054c&pid_0ce6"),
            "A known USB gamepad may still publish HID capabilities such as battery.");
        var sdk = Snapshot("playnite:path:HID#VID_2DC8&PID_6012", "Playnite", 1,
            "8BitDo Ultimate 2 Wireless", @"HID#VID_2DC8&PID_6012", true);
        var leftover = Snapshot("hardware:1234:5678:1", "HID", 0, "Game Controller",
            @"\\?\hid#vid_1234&pid_5678", true);
        leftover.VendorId = 0x1234;
        leftover.ProductId = 0x5678;
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, leftover }, true);
        Equal(1, merged.Count,
            "Generic USB HID leftovers must stay hidden once Playnite owns the inventory.");
    }

    private static void GenericPlayniteNameUsesMappedIdentity()
    {
        Equal("8BitDo Ultimate 2 Wireless",
            ControllerDeviceIdentity.ResolvePlayniteDisplayName("Game Controller", 0x2DC8, 0x6012),
            "Playnite's generic HID placeholder should take the mapped model name.");
        Equal(true, ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(
            "Game Controller", @"\\?\hid#vid_2dc8&pid_6012", 0x2DC8, 0x6012),
            "A known VID/PID behind a generic Playnite name is still a real pad.");
        Equal(false, ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(
            "Game Controller", @"\\?\hid#vid_1234&pid_5678", 0x1234, 0x5678),
            "An unnamed USB HID placeholder must wait until the controller is identified.");
        Equal(true, ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(
            "Game Controller",
            @"HID\{00001812-0000-1000-8000-00805F9B34FB}_DEV_VID&122DC8_PID&6012",
            0x2DC8, 0x6012),
            "A Bluetooth HID path may appear as Game Controller before Playnite names it.");
    }

    private static void GenericHidNameDoesNotReplacePlayniteIdentity()
    {
        var sdk = Snapshot("playnite:path:HID#VID_054C&PID_0CE6", "Playnite", 3,
            "DualSense Wireless Controller", @"HID#VID_054C&PID_0CE6", true);
        var hid = Snapshot("hid:hardware:054C:0CE6:1", "HID", 0, "Game Controller",
            @"\\?\hid#vid_054c&pid_0ce6", true);
        hid.VendorId = 0x054C;
        hid.ProductId = 0x0CE6;
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, hid }, true).Single();
        Equal("DualSense Wireless Controller", merged.Name,
            "A generic HID fallback name must not replace the Playnite identity.");
    }

    private static void BluetoothLeHidPathExposesVidPid()
    {
        ushort vendor;
        ushort product;
        Equal(true, ControllerBridgeIdentity.TryGetVidPid(
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&022dc8_pid&301b",
            out vendor, out product),
            "Bluetooth LE HID paths encode VID/PID after VID& and PID&.");
        Equal((ushort)0x2DC8, vendor, "The trailing four VID& digits are the USB vendor ID.");
        Equal((ushort)0x301B, product, "The PID& value should identify the Bluetooth 8BitDo endpoint.");
        ControllerMetadata metadata;
        Equal(true, HidDiagnosticsService.TryBuildMetadataFromPath(
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&022dc8_pid&301b",
            null, out metadata),
            "A Bluetooth LE HID gamepad path should be accepted without SDL.");
        Equal("Bluetooth", metadata.ConnectionType,
            "The Bluetooth HID service UUID must classify the transport.");
        Equal("8BitDo Ultimate 2C Wireless", metadata.DisplayName,
            "PID 301B should keep the friendly Ultimate 2C name.");
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

    private static void XInputWrapperIsNotUsedAsBluetoothBatteryContainer()
    {
        Equal(true, WindowsBluetoothBatteryProvider.IsXInputWrapperPath(
            @"\\?\hid#vid_2dc8&pid_310a&ig_00"),
            "The XInput IG wrapper must not be treated as the Bluetooth device node.");
        Equal(false, WindowsBluetoothBatteryProvider.IsXInputWrapperPath(
            @"HID\{00001812-0000-1000-8000-00805F9B34FB}_DEV_VID&122DC8_PID&301B"),
            "A Bluetooth HID path must remain eligible for Windows battery lookup.");
    }

    private static void BluetoothLeBatteryAddressIsParsedFromSiblingNodes()
    {
        string address;
        Equal(true, WindowsBluetoothBatteryProvider.TryExtractBluetoothAddress(
            @"BTHLE\DEV_E417D8BCF47A\6&2F1A9C3&0&01", out address),
            "The BLE battery node stores the device address after DEV_.");
        Equal("E417D8BCF47A", address, "BTHLE DEV_ should yield the 12-hex Bluetooth address.");
        Equal(true, WindowsBluetoothBatteryProvider.TryExtractBluetoothAddress(
            @"HID\{00001812-0000-1000-8000-00805F9B34FB}_DEV_VID&122DC8_PID&6012_8&1_E417D8BCF47A",
            out address),
            "The HID gamepad path should expose the same address as the BTHLE battery node.");
        Equal("E417D8BCF47A", address,
            "The Bluetooth base UUID tail must not be mistaken for the device address.");
        Equal(true, WindowsBluetoothBatteryProvider.TryExtractBluetoothAddress(
            "E4:17:D8:BC:F4:7A", out address),
            "Colon-separated Bluetooth addresses should normalize to 12 hex digits.");
        Equal("E417D8BCF47A", address, "Colon MAC values should drop separators.");
        Equal(false, WindowsBluetoothBatteryProvider.TryExtractBluetoothAddress(
            @"\\?\hid#vid_2dc8&pid_310a&ig_00", out address),
            "An XInput wrapper path should not invent a Bluetooth address.");
    }

    private static void PlayniteBluetoothRowReceivesHidBatteryWithoutXInput()
    {
        var sdk = Snapshot("playnite:instance:3", "Playnite", 3,
            "8BitDo Ultimate 2 Wireless", "SDL#JOYSTICK", true);
        var hid = Snapshot("hardware:2DC8:6012:1", "HID", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012_e417d8bcf47a",
            true);
        hid.VendorId = 0x2DC8;
        hid.ProductId = 0x6012;
        hid.BatteryLevel = "Full";
        hid.BatteryProviderId = "Windows.BluetoothPnP";
        hid.ConnectionType = "Bluetooth";
        var merged = ControllerSnapshotMerger.Merge(new[] { sdk, hid }, true).Single();
        Equal("Full", merged.BatteryLevel,
            "A Playnite DInput/BLE row should inherit Windows battery from the HID observation.");
        Equal("Bluetooth", merged.ConnectionType,
            "The BLE HID service path should classify the Playnite row as Bluetooth.");
        Equal(1, ControllerSnapshotMerger.Merge(new[] { sdk, hid }, true).Count,
            "The HID capability row must enrich Playnite instead of appearing as a second controller.");
    }

    private static void BluetoothHardwareIdsAcceptVendorEncodings()
    {
        Equal(true, HidDiagnosticsService.HardwareIdContainsVid(
            "{00001124-0000-1000-8000-00805f9b34fb}_VID&122DC8_PID&301B", 0x2DC8),
            "BTHENUM keys encode the 8BitDo vendor as VID&12.");
        Equal(true, HidDiagnosticsService.HardwareIdContainsPid(
            "{00001124-0000-1000-8000-00805f9b34fb}_VID&122DC8_PID&301B", 0x301B),
            "BTHENUM keys encode the Bluetooth PID as PID&.");
        var aliases = new List<ushort>(
            ControllerDeviceIdentity.GetBluetoothAliasProductIds(0x2DC8, 0x310A));
        Equal(1, aliases.Count, "Bluetooth presence uses the exact product ID only.");
        Equal((ushort)0x310A, aliases[0],
            "No hardcoded sibling PID aliases — battery correlates via address/container instead.");
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

    private static void DualSenseSyntheticDongleReportIsRejected()
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[1] = report[2] = report[3] = report[4] = 0x7F;
        report[8] = 0x08;
        string level;
        Equal(true, PlayStationHidBatteryProvider.IsSyntheticDualSenseDisconnectReport(report),
            "Centered-stick DualSense USB sentinel should be recognized.");
        Equal(false, PlayStationHidBatteryProvider.TryParseReport(0x0CE6, report, out level),
            "Synthetic dongle disconnect reports must not yield a battery level.");
        Equal(false, PlayStationHidBatteryProvider.TryParseReport(0x0DF2, report, out level),
            "DualSense Edge must apply the same synthetic dongle filter.");
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

    private static void LowBatteryNotificationTrackerLatchesAndRecovers()
    {
        var tracker = new LowBatteryNotificationTracker();
        Equal(true, tracker.ShouldShow("pad-a", "Low", "Low", true),
            "First transition into Low must raise a notification.");
        Equal(false, tracker.ShouldShow("pad-a", "Low", "Low", true),
            "A latched Low episode must not spam notifications.");
        Equal(false, tracker.ShouldShow("pad-a", "Medium", "Low", true),
            "First recovered sample only starts the debounce.");
        Equal(false, tracker.ShouldShow("pad-a", "Medium", "Low", true),
            "Second recovered sample clears the latch without notifying.");
        Equal(true, tracker.ShouldShow("pad-a", "Empty", "Low", true),
            "After recovery, a new Empty episode must notify again.");
        Equal(false, LowBatteryNotificationTracker.IsAtOrBelowThreshold("Low", "Empty"),
            "Empty-only threshold must ignore Low.");
        Equal(true, LowBatteryNotificationTracker.IsAtOrBelowThreshold("Empty", "Empty"),
            "Empty-only threshold must still match Empty.");
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

    private static void ColorPickerStoresOpacityInHex()
    {
        Equal(59, ColorPickerMath.AlphaToPercent(0x96),
            "The default overlay dim alpha should read as 59 percent.");
        Equal((byte)0x96, ColorPickerMath.PercentToAlpha(59),
            "A 59 percent opacity slider should restore the overlay dim alpha.");
        Equal("#96000000", ColorPickerMath.ToHex(0x96, 0, 0, 0),
            "Saved colors must keep the alpha byte in #AARRGGBB form.");
        byte alpha;
        byte red;
        byte green;
        byte blue;
        Equal(true, ColorPickerMath.TryParseHex("#80FF0000", out alpha, out red, out green, out blue),
            "An 8-digit hex value with alpha must parse.");
        Equal((byte)0x80, alpha, "The first two hex digits are the opacity.");
        Equal((byte)255, red, "A semi-transparent red must keep its red channel.");
    }

    private static void ControllerIconsFollowVidAndDefaultFallback()
    {
        Equal("dualsense", ControllerIconCatalog.Suggest(0x054C, 0x0CE6, "Wireless Controller"),
            "Sony DualSense VID/PID should select the DualSense silhouette.");
        Equal("dualshock", ControllerIconCatalog.Suggest(0x054C, 0x09CC, "Wireless Controller"),
            "DualShock 4 VID/PID should select the DualShock silhouette.");
        Equal("xbox-series", ControllerIconCatalog.Suggest(0x045E, 0x0B13, "Xbox Wireless Controller"),
            "Xbox Series VID/PID should select the Series silhouette.");
        Equal("xbox-one", ControllerIconCatalog.Suggest(0x045E, 0x02EA, "Xbox Controller"),
            "Xbox One VID/PID should select the One silhouette.");
        Equal("switch-pro", ControllerIconCatalog.Suggest(0x057E, 0x2009, "Pro Controller"),
            "Nintendo VID should select the Switch Pro silhouette.");
        Equal("8bitdo-ultimate", ControllerIconCatalog.Suggest(0x2DC8, 0x310B, "Xbox Controller"),
            "8BitDo Ultimate VID/PID should select the Ultimate silhouette.");
        Equal("8bitdo-ultimate-3", ControllerIconCatalog.Suggest(0x2DC8, 0x202F, "Xbox Controller"),
            "8BitDo Ultimate 3 VID/PID should select the Ultimate 3 silhouette.");
        Equal("8bitdo-pro", ControllerIconCatalog.Suggest(0x2DC8, 0x6009, "8BitDo Pro 3"),
            "8BitDo Pro VID/PID should select the Pro silhouette.");
        Equal("steam", ControllerIconCatalog.Suggest(0x28DE, 0x1102, "Steam Controller"),
            "Valve VID should select the Steam Controller silhouette.");
        Equal("default", ControllerIconCatalog.Suggest(0, 0, "Arcade Stick"),
            "Unknown VID should fall back to Default.");
        Equal("Default.svg", ControllerIconCatalog.GetFileName("gamepad-4"),
            "Removed Lucide gamepad ids should resolve to Default.svg.");
    }

    private static void DisplayHoldKeepsSettledControllerDuringHotPlugGap()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 19, 22, 0, 0, DateTimeKind.Utc);
        var dongle = Snapshot("hardware:2DC8:310B:1", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        dongle.VendorId = 0x2DC8;
        dongle.ProductId = 0x310B;
        Equal(1, hold.Apply(new[] { dongle }, start).Count,
            "A settled 8BitDo row must populate the display hold.");
        var held = hold.Apply(new ControllerDeviceSnapshot[0], start.AddMilliseconds(500));
        Equal(1, held.Count,
            "Mandos must keep the last settled pad while Wireless/Bluetooth is bouncing.");
        Equal("hardware:2DC8:310B:1", held[0].HardwareId,
            "The held row must stay the settled 8BitDo identity.");
        Equal(0, hold.Apply(new ControllerDeviceSnapshot[0],
            start.Add(ControllerDisplayHold.HoldDuration).AddMilliseconds(1)).Count,
            "A real disconnect still clears Mandos after the hold window.");
    }

    private static void DisplayHoldIgnoresUnsettledReplacement()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 19, 22, 1, 0, DateTimeKind.Utc);
        var dongle = Snapshot("hardware:2DC8:310B:1", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        dongle.VendorId = 0x2DC8;
        dongle.ProductId = 0x310B;
        hold.Apply(new[] { dongle }, start);
        var ghost = Snapshot("xinput:slot:0", "XInput", 0, "XInput Controller (Player 1)",
            string.Empty, true);
        var display = hold.Apply(new[] { ghost }, start.AddMilliseconds(200));
        Equal("hardware:2DC8:310B:1", display.Single().HardwareId,
            "A VID-less XInput slot must not replace the settled 8BitDo icon identity.");
        Equal(false, ControllerDisplayHold.ShouldSyncProfile(ghost),
            "Unsettled observations must not create a Default.svg profile.");
    }

    private static void DisplayHoldAppliesSameVendorTransportImmediately()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 19, 22, 2, 0, DateTimeKind.Utc);
        var dongle = Snapshot("hardware:2DC8:310B:1", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        dongle.VendorId = 0x2DC8;
        dongle.ProductId = 0x310B;
        dongle.ConnectionType = "Wireless";
        var bluetooth = Snapshot("hardware:2DC8:6012:1", "Playnite", 2, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        hold.Apply(new[] { dongle }, start);
        var switched = hold.Apply(new[] { bluetooth }, start.AddMilliseconds(50));
        Equal(1, switched.Count, "A Wireless/Bluetooth switch must keep a single Mandos card.");
        Equal("hardware:2DC8:310B:1", switched.Single().HardwareId,
            "The card must keep the original pad identity so the icon and profile do not reset.");
        Equal("Bluetooth", switched.Single().ConnectionType,
            "The same card must show Bluetooth as soon as that transport is the live one.");
        var bounced = hold.Apply(new[] { dongle }, start.AddMilliseconds(100));
        Equal("Wireless", bounced.Single().ConnectionType,
            "Switching back to the dongle must update the connection type immediately.");
        Equal("hardware:2DC8:310B:1", bounced.Single().HardwareId,
            "Bouncing transports must not recreate the controller identity.");
    }

    private static void DisplayHoldCollapsesDongleAndBluetoothOverlap()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 19, 23, 0, 0, DateTimeKind.Utc);
        var dongle = Snapshot("hardware:2DC8:310B:1", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        dongle.VendorId = 0x2DC8;
        dongle.ProductId = 0x310B;
        dongle.ConnectionType = "Wireless";
        dongle.BatteryLevel = "Unknown";
        var bluetooth = Snapshot("hardware:2DC8:6012:1", "HID", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        bluetooth.BatteryLevel = "Full";
        var overlap = hold.Apply(new[] { dongle, bluetooth }, start);
        Equal(1, overlap.Count,
            "Dongle XInput and Bluetooth HID of the same 8BitDo must be one Mandos card.");
        Equal("Wireless", overlap.Single().ConnectionType,
            "A connected XInput dongle is the live transport; leftover Bluetooth HID must not win.");
        var wirelessOnly = hold.Apply(new[] { dongle }, start.AddMilliseconds(80));
        Equal(1, wirelessOnly.Count, "The Mandos card must stay a single row after the overlap.");
        Equal("Wireless", wirelessOnly.Single().ConnectionType,
            "The same card must update to Wireless when the dongle is the only remaining transport.");
        Equal(overlap.Single().HardwareId, wirelessOnly.Single().HardwareId,
            "Updating the transport must not create a second controller identity.");
    }

    private static void DisplayHoldAddsSecondPadImmediately()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 20, 0, 10, 0, DateTimeKind.Utc);
        var dualsense = Snapshot("hardware:054C:0CE6:1", "Playnite", 1, "DualSense",
            @"\\?\hid#vid_054c&pid_0ce6", true);
        dualsense.VendorId = 0x054C;
        dualsense.ProductId = 0x0CE6;
        hold.Apply(new[] { dualsense }, start);
        var eightBitDo = Snapshot("hardware:2DC8:310B:1", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        eightBitDo.VendorId = 0x2DC8;
        eightBitDo.ProductId = 0x310B;
        var both = hold.Apply(new[] { dualsense, eightBitDo }, start.AddMilliseconds(40));
        Equal(2, both.Count,
            "A newly connected second pad must appear in Mandos immediately, without the shrink debounce.");
        Equal(true, both.Any(a => a.VendorId == 0x054C) && both.Any(a => a.VendorId == 0x2DC8),
            "DualSense and 8BitDo must both remain listed.");
    }

    private static void UnknownConnectionIsExcludedFromDisplayAndToasts()
    {
        Equal(true, ControllerDeviceIdentity.IsUnknownConnection("Unknown"),
            "Unknown must be treated as a non-actionable connection.");
        Equal(true, ControllerDeviceIdentity.IsUnknownConnection((string)null),
            "A missing connection type must be treated as Unknown.");
        Equal(false, ControllerDeviceIdentity.IsUnknownConnection("Wireless"),
            "A known wireless transport must remain visible and notifiable.");
        var dock = Snapshot("hardware:2DC8:310B:dock", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b", true);
        dock.VendorId = 0x2DC8;
        dock.ProductId = 0x310B;
        dock.ConnectionType = "Unknown";
        Equal(true, ControllerDeviceIdentity.IsUnknownConnection(dock),
            "An 8BitDo charging-dock leftover with Unknown connection must be filterable.");
    }

    private static void SameModelHidIsNotListedBesideXInput()
    {
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.VendorId = 0x2DC8;
        xinput.ProductId = 0x310B;
        xinput.ConnectionType = "Wireless";
        var hid = Snapshot("hardware:2DC8:6012:1", "HID", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        hid.VendorId = 0x2DC8;
        hid.ProductId = 0x6012;
        hid.ConnectionType = "Bluetooth";
        var merged = ControllerSnapshotMerger.Merge(new[] { xinput, hid }, true);
        Equal(1, merged.Count,
            "A Bluetooth HID leftover must not appear beside the same 8BitDo XInput slot.");
        Equal("XInput", merged.Single().ProviderId,
            "The remaining row must be the XInput dongle observation.");
    }

    private static void DongleXInputSupersedesStalePlayniteBluetooth()
    {
        var bluetooth = Snapshot("playnite:path:BTH", "Playnite", 2, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.VendorId = 0x2DC8;
        xinput.ProductId = 0x310B;
        xinput.ConnectionType = "Wireless";
        var connected = ControllerSnapshotMerger.Merge(new[] { bluetooth, xinput }, true)
            .Where(a => a.IsConnected).ToList();
        Equal(1, connected.Count,
            "A 2.4 GHz XInput slot must replace a leftover Playnite Bluetooth row of the same pad.");
        Equal("Wireless", connected.Single().ConnectionType,
            "The Mandos card must show Wireless after switching from Bluetooth to the dongle.");
        Equal("XInput", connected.Single().ProviderId,
            "The live gameplay path for the dongle is XInput, not the stale BLE HID node.");
    }

    private static void IndependentBluetoothPadIsKeptBesideXInput()
    {
        var start = new DateTime(2026, 8, 19, 23, 12, 0, DateTimeKind.Utc);
        var bluetooth = Snapshot("playnite:path:BTH", "Playnite", 2, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        bluetooth.LastInputUtc = start.AddSeconds(4);
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.VendorId = 0x2DC8;
        xinput.ProductId = 0x310B;
        xinput.ConnectionType = "Wireless";
        xinput.LastInputUtc = start;
        var connected = ControllerSnapshotMerger.Merge(new[] { bluetooth, xinput }, true)
            .Where(a => a.IsConnected).ToList();
        Equal(2, connected.Count,
            "A second 8BitDo on Bluetooth with newer input must stay listed beside a dongle pad.");
    }

    private static void BluetoothPlayniteDoesNotBindDongleXInput()
    {
        var bluetooth = Snapshot("playnite:path:BTH", "Playnite", 2, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.VendorId = 0x2DC8;
        xinput.ProductId = 0x310B;
        xinput.ConnectionType = "Wireless";
        Equal(true, ControllerSnapshotMerger.FindCapability(bluetooth, new[] { xinput }) == null,
            "Bluetooth DInput and 2.4 GHz XInput are different Windows devices; matching them by name copies the wrong radio onto Mandos.");
    }

    private static void DonglePlayniteDoesNotInheritBluetoothFromHidLeftover()
    {
        var playnite = Snapshot("playnite:path:HID#IG", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        playnite.VendorId = 0x2DC8;
        playnite.ProductId = 0x310B;
        playnite.ConnectionType = "Wireless";
        var hid = Snapshot("hardware:2DC8:6012:1", "HID", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        hid.VendorId = 0x2DC8;
        hid.ProductId = 0x6012;
        hid.ConnectionType = "Bluetooth";
        var connected = ControllerSnapshotMerger.Merge(new[] { playnite, hid }, true)
            .Where(a => a.IsConnected).ToList();
        Equal(1, connected.Count,
            "A leftover BLE HID node must not appear beside the Playnite dongle row.");
        Equal("Wireless", connected.Single().ConnectionType,
            "The dongle row must not inherit Bluetooth from a same-name HID leftover.");
        Equal(true, connected.Single().Path.IndexOf("&ig_", StringComparison.OrdinalIgnoreCase) >= 0,
            "The live path must stay the XInput wrapper, not the BLE HID interface.");
    }

    private static void XboxBluetoothMayBindXInputCapability()
    {
        var playnite = Snapshot("playnite:path:XBOXBT", "Playnite", 1, "Xbox Wireless Controller",
            @"\\?\hid#vid_045e&pid_0b13&ig_00", true);
        playnite.VendorId = 0x045E;
        playnite.ProductId = 0x0B13;
        playnite.ConnectionType = "Bluetooth";
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "Xbox Wireless Controller",
            @"\\?\hid#vid_045e&pid_0b13&ig_00", true);
        xinput.VendorId = 0x045E;
        xinput.ProductId = 0x0B13;
        xinput.ConnectionType = "Bluetooth";
        Equal("xinput:slot:0", ControllerSnapshotMerger.FindCapability(playnite, new[] { xinput }).ControllerId,
            "Xbox-licensed pads speak XInput over Bluetooth and must still receive the XInput slot.");
    }

    private static void DisplayHoldPromotesVolatileDongleOverHeldBluetooth()
    {
        var hold = new ControllerDisplayHold();
        var start = new DateTime(2026, 8, 19, 23, 20, 0, DateTimeKind.Utc);
        var bluetooth = Snapshot("hardware:2DC8:6012:1", "Playnite", 2, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}_dev_vid&122dc8_pid&6012", true);
        bluetooth.VendorId = 0x2DC8;
        bluetooth.ProductId = 0x6012;
        bluetooth.ConnectionType = "Bluetooth";
        hold.Apply(new[] { bluetooth }, start);
        var xinput = Snapshot("xinput:slot:0", "XInput", 0, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        xinput.VendorId = 0x2DC8;
        xinput.ProductId = 0x310B;
        xinput.HardwareId = "xinput:slot:0";
        xinput.ConnectionType = "Wireless";
        var switched = hold.Apply(new[] { xinput }, start.AddMilliseconds(40));
        Equal("Wireless", switched.Single().ConnectionType,
            "A live dongle XInput slot must update Mandos immediately even before a stable hardware id exists.");
        Equal("hardware:2DC8:6012:1", switched.Single().HardwareId,
            "The Mandos card must keep the settled identity while the XInput wrapper enumerates.");
    }

    private static void GenericIconIsKeptWhenChosen()
    {
        var controller = Snapshot("hardware:2DC8:310B:1", "Playnite", 1, "8BitDo Ultimate 2 Wireless",
            @"\\?\hid#vid_2dc8&pid_310b&ig_00", true);
        controller.VendorId = 0x2DC8;
        controller.ProductId = 0x310B;
        Equal("default", ControllerIconCatalog.ResolveId(controller, "default"),
            "Choosing Generic must keep Default.svg instead of the VID silhouette.");
        Equal("8BitdoUltimate2.svg", ControllerIconCatalog.ResolveFileName(controller, null),
            "A missing profile still uses VID to pick the 8BitDo silhouette instead of Default.svg.");
        Equal("dualsense", ControllerIconCatalog.ResolveId(controller, "dualsense"),
            "An explicit picker choice must win over VID suggestion.");
    }

    private static void OverlayIpcAcceptsGamepadSilhouettes()
    {
        var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
        var largest = 0;
        foreach (var svg in Directory.GetFiles(Path.Combine(root, "Gamepads"), "*.svg"))
        {
            var document = XDocument.Load(svg);
            var geometry = string.Join(" ", document.Descendants()
                .Select(a => (string)a.Attribute("d"))
                .Where(a => !string.IsNullOrWhiteSpace(a)));
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(geometry ?? string.Empty));
            if (encoded.Length > largest)
            {
                largest = encoded.Length;
            }
        }

        Equal(true, largest > 16384,
            "Gamepads silhouettes exceed the old 16 KB IPC cap that dropped every toast.");
        var framing = 512;
        Equal(true, largest * 2 + framing < OverlayIpcLimits.MaxLineCharacters,
            "A disconnect overlay with two silhouette payloads must still fit the IPC line limit.");
    }

    private static void ColorPickerMathRoundTripsHueAndOpacity()
    {
        double hue;
        double saturation;
        double value;
        ColorPickerMath.RgbToHsv(0, 255, 0, out hue, out saturation, out value);
        Equal(true, hue > 119 && hue < 121, "Pure green should sit near 120 degrees.");
        byte red;
        byte green;
        byte blue;
        ColorPickerMath.HsvToRgb(hue, saturation, value, out red, out green, out blue);
        Equal((byte)0, red, "Green should round-trip without a red channel.");
        Equal((byte)255, green, "Green should round-trip at full value.");
        Equal((byte)0, blue, "Green should round-trip without a blue channel.");
        Equal(100, ColorPickerMath.AlphaToPercent(255), "Fully opaque colors are 100 percent.");
        Equal((byte)0, ColorPickerMath.PercentToAlpha(0), "Zero percent must be fully transparent.");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
