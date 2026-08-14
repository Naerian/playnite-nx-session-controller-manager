# Controller Session Manager

Controller Session Manager is a Playnite extension for monitoring controllers and, incrementally, protecting game sessions when an active controller disconnects.

Version 0.1.0 establishes the installable foundation:

- controller inventory from Playnite's official controller API;
- connection, disconnection and last-input tracking;
- periodic low-frequency reconciliation;
- localized settings and diagnostics;
- English fallback localization;
- basic Playnite theme elements: `ControllerStatus`, `ControllerCount` and `PrimaryController`.

Pause, external overlay, battery providers and GameInput native integration are intentionally not enabled in this foundation release. See `docs/ROADMAP.md` for the staged plan.

