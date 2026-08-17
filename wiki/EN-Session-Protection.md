# Session Protection

## How a controller joins a session

A connected controller is not automatically considered a player. Controller Session Manager looks for intentional input immediately before and after game startup: button presses, trigger movement above a threshold or substantial stick movement. A ten-second pre-launch window preserves the controller used to start a game from Desktop when the game later captures input exclusively. If the game captures every input before Playnite can observe one, exactly one connected controller is armed as a conservative startup owner. The first real input can replace that inference immediately; other connected controllers do not become players merely by being present. Releases, tiny drift, packet-only changes and Guide/Home presses are ignored.

## Automatic/adaptive mode

Automatic mode begins as single player. Meaningful input from another connected controller can transfer ownership, covering the common case where the wrong controller was connected at launch. During a confirmed incident, reconnecting the missing controller or intentionally using an eligible replacement resolves the overlay.

The overlay is shown during the suspected-disconnect phase for fast feedback, while pause actions still wait for the configured grace period. A controller connected after the incident is treated as an intentional replacement because some drivers hide the Home button used to power it on. A controller that was already present must produce a button press or stick movement.

Appearance settings can hide the icon beside the controller name and the pause/warning status icon independently. Both switches are reflected in the live overlay preview.

Sustained alternating input from several controllers promotes the session to local multiplayer. Every participant is then protected independently. One active player's existing controller cannot silently replace another missing player, but a new or unassigned device can take that slot.

## Per-game session policy

Open a game's context menu and select **Controller Session Manager > Session protection**:

- **Use global settings**: inherit the general configuration.
- **Automatic / adaptive**: normal handover plus automatic local multiplayer detection.
- **Local multiplayer**: explicitly protect every active participant.
- **Disabled**: do not track disconnect incidents for this game.

The effective item has a check mark.

## Pause policy

The separate **Automatic pause** submenu offers global inheritance, overlay only, offline force-pause with online notification fallback, Escape or the configured key. Key delivery occurs once only after foreground process-tree verification.

Force-pause suspends only a verified offline foreground game process through the external host. Its safety lease resumes the process when the incident resolves, the game ends, Playnite closes or communication is lost. Strong online-only metadata uses a non-blocking warning. A public TCP connection alone also prevents suspension but retains the disconnect overlay because it may only be telemetry or a platform service. Online detection is best effort; always test a game before enabling this mode permanently.
