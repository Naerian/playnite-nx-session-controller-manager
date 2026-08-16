# Session Protection

## How a controller joins a session

A connected controller is not automatically considered a player. After the game starts, Controller Session Manager looks for intentional input: button presses, trigger movement above a threshold or substantial stick movement. Releases, tiny drift, packet-only changes and Guide/Home presses are ignored.

## Automatic/adaptive mode

Automatic mode begins as single player. Meaningful input from another connected controller can transfer ownership, covering the common case where the wrong controller was connected at launch. During a confirmed incident, reconnecting the missing controller or intentionally using an eligible replacement resolves the overlay.

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

Force-pause suspends only a verified offline foreground game process through the external host. Its safety lease resumes the process when the incident resolves, the game ends, Playnite closes or communication is lost. Online detection is best effort; always test a game before enabling this mode permanently.
