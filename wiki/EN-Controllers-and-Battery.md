# Controllers & Battery

## Device list

The Controllers page shows the detected name, optional alias, assigned icon, connection type, battery state and provider as themed badges. Custom names and icons are stored by hardware identity where possible. Desktop can also remember the friendly controller associated with each XInput player slot so Fullscreen can reuse it safely.

Use the vibration action to match a row to the controller in your hands. Vibration availability depends on the controller, driver and active protocol.

Unnamed USB HID leftovers such as a generic **Game Controller** row are hidden until Windows or Playnite identifies the pad. Known VID/PID values replace that placeholder with the model name.

## Connection type

Controller Session Manager combines device metadata and Windows transport evidence. USB means a wired path was found, Bluetooth requires Bluetooth-specific evidence, and wireless represents a receiver or dongle. An XInput wrapper (`&ig_`) is treated as a cable or 2.4 GHz dongle unless it is an Xbox-licensed pad that also appears on Bluetooth. Some drivers hide this distinction; the plugin prefers an honest unknown or generic wireless result over a guessed transport.

## Battery levels

XInput generally exposes four coarse levels: Empty, Low, Medium and Full. These levels are shown with semantic colors on the battery badge. They are not converted into percentages because doing so would imply precision the API does not provide.

Windows Bluetooth battery is read only for real Bluetooth HID paths. BLE gamepads can keep the percentage on a separate `BTHLE\DEV_{address}` node that shares the Bluetooth address with the HID path; the plugin correlates those siblings without decoding proprietary input reports. This supports Bluetooth devices such as 8BitDo. An XInput dongle or cable wrapper does not inherit that BLE reading, so a Medium value cannot stick after you switch to the receiver. Xbox-licensed pads may still use XInput over Bluetooth and then keep the XInput battery API.

A strict Sony HID fallback remains available for documented DualSense and DualShock 4 reports; Bluetooth data must pass its CRC and the device must match a verified Sony VID/PID. Some DualSense Bluetooth driver paths expose neither that safe battery channel nor provider-backed rumble. The plugin leaves those capabilities unavailable instead of sending speculative proprietary reports. Unverified receiver byte patterns are deliberately not interpreted. **Unknown** therefore means that none of the safe providers returned a trustworthy value; it does not imply a full battery.

The provider badge is not a brand or transport label. An 8BitDo controller may appear through DInput/HID on Bluetooth and through an XInput-compatible endpoint on its dongle. Playnite SDK inventory and callbacks are authoritative for connection state. XInput monitors translated slots and Windows PnP supplies verified Bluetooth properties. Equivalent observations are collapsed by XInput path/slot or equivalent device path; a numeric ID alone is never compared across providers.

## HID diagnostics

Use **Advanced > Export HID diagnostics** when a device is missing, duplicated or has no battery. The report inventories relevant interfaces and capabilities without sending vendor commands. Attach it to a GitHub issue together with the exact model, connection mode and driver software.

## Support report

Use **Advanced > Support report** for normal incident diagnosis. It includes effective settings, provider choices, anonymized controller fingerprints, current session state and the latest connection/pause/incident events. It excludes HID paths, serial numbers, user folders and Playnite log contents. The lower-level HID diagnostic can contain device paths or serial information, so review that separate file before posting it publicly.
