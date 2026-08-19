# Overview

Controller Manager is a Playnite extension for controller visibility, in-plugin testing and game-session protection on Windows.

## What it does

- Lists connected and known controllers with friendly names, custom aliases and assignable icons.
- Reports USB, Bluetooth or wireless-receiver transport when Windows exposes enough evidence.
- Shows XInput battery levels without inventing percentages.
- Provides vibration testing, a full Gamepad Tester tab and read-only HID diagnostics.
- Adds an adaptive controller and battery shortcut to the Desktop top panel.
- Shows configurable connection notifications while browsing Fullscreen.
- Tracks which controllers actually participate after a game starts.
- Shows an external overlay when a participating controller disconnects.
- Supports safe pause-key delivery and optional offline force-pause.
- Detects sustained local multiplayer activity and protects each participant independently.

## Design priorities

The extension favors stability over invasive interception. It does not inject into games, install input hooks or load arbitrary theme code into its external overlay. Fullscreen deliberately avoids SDL calls after native hot-unplug failures were found to terminate Playnite in some driver paths.

Automatic mode is intended for most users. A controller becomes part of the session only after meaningful input. Switching controllers in a single-player game normally transfers ownership, while sustained alternating input from several devices promotes the session to local multiplayer.

## Important limitations

Battery and transport information depend on Windows drivers and device firmware. Online-session detection is best effort, and no universal Windows API can determine whether every game is currently offline, online or local multiplayer. Test pause behavior with each game before relying on it.

Continue with [Installation & Quick Start](EN-Installation-and-Quick-Start).
