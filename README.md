# Controller Session Manager

Controller Session Manager is a Playnite extension for monitoring controllers and, incrementally, protecting game sessions when an active controller disconnects.

Version 0.1.1 establishes the installable foundation:

- controller inventory through XInput, with Playnite's controller API as an additional source;
- connection, disconnection and last-input tracking;
- periodic low-frequency reconciliation;
- a tabbed settings panel and localized diagnostics;
- English fallback localization;
- basic Playnite theme elements: `ControllerStatus`, `ControllerCount` and `PrimaryController`.

Pause, external overlay and GameInput native integration are intentionally not enabled in this foundation release. XInput reports coarse battery states for compatible wireless controllers. See `docs/ROADMAP.md` for the staged plan.
