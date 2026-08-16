# Controllers & Battery

## Device list

The controller table shows the detected name, user name, assigned icon, connection type, battery state, last input and available actions. Custom names and icons are stored by hardware identity where possible. Desktop can also remember the friendly controller associated with each XInput player slot so Fullscreen can reuse it safely.

Use the vibration action to match a row to the controller in your hands. Vibration availability depends on the controller, driver and active protocol.

## Connection type

Controller Session Manager combines device metadata and Windows transport evidence. USB means a wired path was found, Bluetooth requires Bluetooth-specific evidence, and wireless represents a receiver or dongle. Some drivers hide this distinction; the plugin prefers an honest unknown or generic wireless result over a guessed transport.

## Battery levels

XInput generally exposes four coarse levels: Empty, Low, Medium and Full. These levels are shown with semantic colors. They are not converted into percentages because doing so would imply precision the API does not provide.

Many proprietary USB receivers expose no standard battery collection. A vendor-specific implementation may exist, but it must be understood and tested per protocol. **Unknown** therefore means that none of the safe providers returned a trustworthy value; it does not imply a full battery.

## HID diagnostics

Use **Advanced > Export HID diagnostics** when a device is missing, duplicated or has no battery. The report inventories relevant interfaces and capabilities without sending vendor commands. Attach it to a GitHub issue together with the exact model, connection mode and driver software.
