using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.Sessions
{
    public enum SessionEventType
    {
        ControllerActivated,
        DisconnectSuspected,
        DisconnectCancelled,
        DisconnectConfirmed,
        DisconnectResolved,
        ControllerTakeover
    }

    public sealed class SessionEventArgs : EventArgs
    {
        public SessionEventType Type { get; set; }
        public string ControllerKey { get; set; }
        public string ControllerName { get; set; }
        public string ReplacementControllerKey { get; set; }
        public string ReplacementControllerName { get; set; }
        public string InputEvidence { get; set; }
    }

    public sealed class SessionControllerSnapshot
    {
        public string ControllerKey { get; set; }
        public string Name { get; set; }
        public DateTime? LastInputUtc { get; set; }
        public DateTime? ActivatedUtc { get; set; }
        public DateTime? MissingSinceUtc { get; set; }
        public DateTime? ConfirmedUtc { get; set; }
        public bool DisconnectConfirmed { get; set; }
        public string InputEvidence { get; set; }
    }

    public sealed class GameSessionManager
    {
        private static readonly TimeSpan PreSessionInputGrace = TimeSpan.FromSeconds(10);
        private readonly Dictionary<string, SessionControllerSnapshot> activeControllers =
            new Dictionary<string, SessionControllerSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> activationFloors =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> knownConnectedKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SessionEventArgs> EventOccurred;

        public bool IsRunning { get; private set; }
        public Guid GameId { get; private set; }
        public DateTime StartedUtc { get; private set; }

        public IReadOnlyList<SessionControllerSnapshot> ActiveControllers
        {
            get
            {
                return activeControllers.Values.Select(Clone).OrderByDescending(a => a.LastInputUtc).ToList();
            }
        }

        public int SuspectedDisconnectCount
        {
            get { return activeControllers.Values.Count(a => a.MissingSinceUtc.HasValue && !a.DisconnectConfirmed); }
        }

        public int ConfirmedDisconnectCount
        {
            get { return activeControllers.Values.Count(a => a.DisconnectConfirmed); }
        }

        public void Start(Guid gameId, DateTime nowUtc)
        {
            activeControllers.Clear();
            activationFloors.Clear();
            knownConnectedKeys.Clear();
            GameId = gameId;
            StartedUtc = nowUtc;
            IsRunning = true;
        }

        public void Stop()
        {
            activeControllers.Clear();
            activationFloors.Clear();
            knownConnectedKeys.Clear();
            IsRunning = false;
            GameId = Guid.Empty;
        }

        public bool SeedInitialController(IEnumerable<ControllerDeviceSnapshot> controllers, DateTime nowUtc)
        {
            if (!IsRunning || activeControllers.Count > 0)
            {
                return false;
            }

            var snapshot = (controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>()).ToList();
            foreach (var connectedKey in snapshot.Where(a => a.IsConnected).Select(GetControllerKey))
            {
                knownConnectedKeys.Add(connectedKey);
            }
            // The public snapshot is already physically deduplicated. Do not apply the
            // short-lived Playnite input suppression here: a pure SDK controller may be
            // the only safe startup owner available.
            var candidate = snapshot.Where(a => a.IsConnected)
                .GroupBy(GetControllerKey, StringComparer.OrdinalIgnoreCase)
                .Select(a => a.OrderByDescending(b => b.LastInputUtc.HasValue)
                    .ThenByDescending(b => b.LastInputUtc)
                    .ThenBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase)
                    .First())
                .OrderByDescending(a => a.LastInputUtc.HasValue)
                .ThenByDescending(a => a.LastInputUtc)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .FirstOrDefault();
            if (candidate == null)
            {
                return false;
            }

            var key = GetControllerKey(candidate);
            var active = new SessionControllerSnapshot
            {
                ControllerKey = key,
                Name = candidate.Name,
                LastInputUtc = candidate.LastInputUtc,
                ActivatedUtc = nowUtc,
                InputEvidence = "SessionStartFallback"
            };
            activeControllers[key] = active;
            activationFloors.Remove(key);
            Raise(SessionEventType.ControllerActivated, active);
            return true;
        }

        public void Update(IEnumerable<ControllerDeviceSnapshot> controllers, DateTime nowUtc,
            bool allowControllerTakeover = false, bool protectAllActiveControllers = true)
        {
            if (!IsRunning)
            {
                return;
            }

            var snapshot = (controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>()).ToList();
            var connected = snapshot.Where(a => a.IsConnected)
                .GroupBy(GetControllerKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(a => a.Key, a => a.OrderByDescending(b => b.LastInputUtc).First(),
                    StringComparer.OrdinalIgnoreCase);
            var newlyConnectedKeys = new HashSet<string>(connected.Keys.Where(a =>
                !knownConnectedKeys.Contains(a)), StringComparer.OrdinalIgnoreCase);

            var eligible = connected.Values.Where(a => IsEligibleForActivation(a, snapshot, nowUtc))
                .OrderByDescending(a => a.LastInputUtc).ToList();
            if (protectAllActiveControllers)
            {
                foreach (var controller in eligible)
                {
                    ActivateOrRefresh(controller);
                }
            }
            else
            {
                UpdateMostRecentController(eligible);
            }

            foreach (var active in activeControllers.Values)
            {
                ControllerDeviceSnapshot observed;
                if (connected.TryGetValue(active.ControllerKey, out observed))
                {
                    active.Name = observed.Name;
                    if (observed.LastInputUtc.HasValue)
                    {
                        active.LastInputUtc = observed.LastInputUtc;
                        active.InputEvidence = observed.LastInputKind;
                    }

                    if (active.MissingSinceUtc.HasValue)
                    {
                        var wasConfirmed = active.DisconnectConfirmed;
                        active.MissingSinceUtc = null;
                        active.ConfirmedUtc = null;
                        active.DisconnectConfirmed = false;
                        Raise(wasConfirmed ? SessionEventType.DisconnectResolved :
                            SessionEventType.DisconnectCancelled, active);
                    }
                }
                else if (!active.MissingSinceUtc.HasValue)
                {
                    active.MissingSinceUtc = nowUtc;
                    active.DisconnectConfirmed = false;
                    Raise(SessionEventType.DisconnectSuspected, active);
                }
            }

            if (allowControllerTakeover)
            {
                ResolveTakeovers(connected, newlyConnectedKeys, protectAllActiveControllers, nowUtc);
            }

            knownConnectedKeys.Clear();
            foreach (var key in connected.Keys)
            {
                knownConnectedKeys.Add(key);
            }
        }

        public void Tick(DateTime nowUtc, TimeSpan gracePeriod)
        {
            if (!IsRunning)
            {
                return;
            }

            foreach (var active in activeControllers.Values.Where(a =>
                a.MissingSinceUtc.HasValue && !a.DisconnectConfirmed &&
                nowUtc - a.MissingSinceUtc.Value >= gracePeriod).ToList())
            {
                active.DisconnectConfirmed = true;
                active.ConfirmedUtc = nowUtc;
                Raise(SessionEventType.DisconnectConfirmed, active);
            }
        }

        private bool IsEligibleForActivation(ControllerDeviceSnapshot controller,
            IEnumerable<ControllerDeviceSnapshot> snapshot, DateTime nowUtc)
        {
            if (!controller.LastInputUtc.HasValue ||
                controller.LastInputUtc.Value < StartedUtc - PreSessionInputGrace ||
                IsLikelyDuplicateFallback(controller, snapshot, nowUtc))
            {
                return false;
            }

            DateTime floor;
            return !activationFloors.TryGetValue(GetControllerKey(controller), out floor) ||
                controller.LastInputUtc.Value > floor;
        }

        private SessionControllerSnapshot ActivateOrRefresh(ControllerDeviceSnapshot controller)
        {
            var key = GetControllerKey(controller);
            SessionControllerSnapshot active;
            if (!activeControllers.TryGetValue(key, out active))
            {
                active = new SessionControllerSnapshot
                {
                    ControllerKey = key,
                    Name = controller.Name,
                    LastInputUtc = controller.LastInputUtc,
                    ActivatedUtc = controller.LastInputUtc,
                    InputEvidence = controller.LastInputKind
                };
                activeControllers[key] = active;
                activationFloors.Remove(key);
                Raise(SessionEventType.ControllerActivated, active);
            }
            else
            {
                active.Name = controller.Name;
                active.LastInputUtc = controller.LastInputUtc;
                active.InputEvidence = controller.LastInputKind;
            }

            return active;
        }

        private void UpdateMostRecentController(IList<ControllerDeviceSnapshot> eligible)
        {
            var current = activeControllers.Values.FirstOrDefault();
            if (current == null)
            {
                if (eligible.Count > 0)
                {
                    ActivateOrRefresh(eligible[0]);
                }
                return;
            }

            if (string.Equals(current.InputEvidence, "SessionStartFallback",
                StringComparison.OrdinalIgnoreCase) && eligible.Count > 0)
            {
                var observedOwner = eligible.FirstOrDefault(a => string.Equals(GetControllerKey(a),
                    current.ControllerKey, StringComparison.OrdinalIgnoreCase));
                var actualController = observedOwner ?? eligible[0];
                var actualKey = GetControllerKey(actualController);
                if (!string.Equals(actualKey, current.ControllerKey, StringComparison.OrdinalIgnoreCase))
                {
                    Retire(current);
                    activeControllers.Remove(current.ControllerKey);
                }
                ActivateOrRefresh(actualController);
                return;
            }

            var currentObservation = eligible.FirstOrDefault(a => string.Equals(GetControllerKey(a),
                current.ControllerKey, StringComparison.OrdinalIgnoreCase));
            if (currentObservation != null)
            {
                ActivateOrRefresh(currentObservation);
            }
            else
            {
                return;
            }

            if (current.MissingSinceUtc.HasValue || current.DisconnectConfirmed || eligible.Count == 0)
            {
                return;
            }

            var mostRecent = eligible[0];
            var mostRecentKey = GetControllerKey(mostRecent);
            if (string.Equals(mostRecentKey, current.ControllerKey, StringComparison.OrdinalIgnoreCase) ||
                !mostRecent.LastInputUtc.HasValue ||
                (current.LastInputUtc.HasValue && mostRecent.LastInputUtc.Value <= current.LastInputUtc.Value))
            {
                return;
            }

            Retire(current);
            activeControllers.Remove(current.ControllerKey);
            ActivateOrRefresh(mostRecent);
        }

        private void ResolveTakeovers(IDictionary<string, ControllerDeviceSnapshot> connected,
            ISet<string> newlyConnectedKeys, bool protectAllActiveControllers, DateTime nowUtc)
        {
            var usedReplacementKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var removals = new List<string>();
            foreach (var missing in activeControllers.Values.Where(a =>
                a.MissingSinceUtc.HasValue).ToList())
            {
                var replacement = connected
                    .Where(a => !string.Equals(a.Key, missing.ControllerKey, StringComparison.OrdinalIgnoreCase) &&
                        !usedReplacementKeys.Contains(a.Key) &&
                        IsAvailableReplacement(a.Key, missing, protectAllActiveControllers) &&
                        ((a.Value.LastInputUtc.HasValue && IsReplacementSettled(a.Value, nowUtc) &&
                            a.Value.LastInputUtc.Value > missing.MissingSinceUtc.Value) ||
                         (newlyConnectedKeys.Contains(a.Key) && a.Value.IsInputNeutral != false)))
                    .OrderByDescending(a => a.Value.LastInputUtc)
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(replacement.Key))
                {
                    continue;
                }

                usedReplacementKeys.Add(replacement.Key);
                removals.Add(missing.ControllerKey);
                SessionControllerSnapshot replacementSession;
                if (!activeControllers.TryGetValue(replacement.Key, out replacementSession))
                {
                    replacementSession = new SessionControllerSnapshot
                    {
                        ControllerKey = replacement.Key,
                        Name = replacement.Value.Name,
                        LastInputUtc = replacement.Value.LastInputUtc,
                        ActivatedUtc = replacement.Value.LastInputUtc ?? nowUtc,
                        InputEvidence = newlyConnectedKeys.Contains(replacement.Key)
                            ? "ConnectedAfterDisconnect"
                            : replacement.Value.LastInputKind
                    };
                    activeControllers[replacement.Key] = replacementSession;
                }

                Raise(SessionEventType.ControllerTakeover, missing, replacementSession);
            }

            foreach (var key in removals)
            {
                Retire(activeControllers[key]);
                activeControllers.Remove(key);
            }
        }

        private bool IsAvailableReplacement(string key, SessionControllerSnapshot missing,
            bool protectAllActiveControllers)
        {
            if (!protectAllActiveControllers)
            {
                return true;
            }

            SessionControllerSnapshot candidate;
            if (!activeControllers.TryGetValue(key, out candidate))
            {
                return true;
            }

            // A controller first activated after this player went missing is not assigned to
            // another established player. Retain a small tolerance for poll/event ordering.
            return missing.MissingSinceUtc.HasValue && candidate.ActivatedUtc.HasValue &&
                candidate.ActivatedUtc.Value >= missing.MissingSinceUtc.Value.AddMilliseconds(-500);
        }

        private static bool IsReplacementSettled(ControllerDeviceSnapshot candidate, DateTime nowUtc)
        {
            if (candidate.IsInputNeutral.HasValue)
            {
                return candidate.IsInputNeutral.Value && candidate.InputNeutralSinceUtc.HasValue &&
                    nowUtc - candidate.InputNeutralSinceUtc.Value >= TimeSpan.FromMilliseconds(100);
            }

            // Playnite's fallback events do not expose axes or release state. Give those events
            // a short settling period instead of keeping the overlay open forever.
            return candidate.LastInputUtc.HasValue &&
                nowUtc - candidate.LastInputUtc.Value >= TimeSpan.FromMilliseconds(250);
        }

        private void Retire(SessionControllerSnapshot controller)
        {
            if (controller != null && controller.LastInputUtc.HasValue)
            {
                activationFloors[controller.ControllerKey] = controller.LastInputUtc.Value;
            }
        }

        private void Raise(SessionEventType type, SessionControllerSnapshot controller,
            SessionControllerSnapshot replacement = null)
        {
            var handler = EventOccurred;
            if (handler != null)
            {
                handler(this, new SessionEventArgs
                {
                    Type = type,
                    ControllerKey = controller.ControllerKey,
                    ControllerName = controller.Name,
                    ReplacementControllerKey = replacement == null ? null : replacement.ControllerKey,
                    ReplacementControllerName = replacement == null ? null : replacement.Name,
                    InputEvidence = replacement == null ? controller.InputEvidence : replacement.InputEvidence
                });
            }
        }

        private static string GetControllerKey(ControllerDeviceSnapshot controller)
        {
            return string.IsNullOrWhiteSpace(controller.HardwareId)
                ? controller.ControllerId
                : controller.HardwareId;
        }

        private static bool IsLikelyDuplicateFallback(ControllerDeviceSnapshot controller,
            IEnumerable<ControllerDeviceSnapshot> snapshot, DateTime nowUtc)
        {
            if (!string.Equals(controller.ProviderId, "Playnite", StringComparison.OrdinalIgnoreCase) ||
                !controller.LastInputUtc.HasValue)
            {
                return false;
            }

            if (nowUtc - controller.LastInputUtc.Value < TimeSpan.FromMilliseconds(750))
            {
                return true;
            }

            return snapshot.Any(a => a.IsConnected &&
                !string.Equals(a.ProviderId, "Playnite", StringComparison.OrdinalIgnoreCase) &&
                a.LastInputUtc.HasValue &&
                Math.Abs((a.LastInputUtc.Value - controller.LastInputUtc.Value).TotalMilliseconds) <= 750);
        }

        private static SessionControllerSnapshot Clone(SessionControllerSnapshot value)
        {
            return new SessionControllerSnapshot
            {
                ControllerKey = value.ControllerKey,
                Name = value.Name,
                LastInputUtc = value.LastInputUtc,
                ActivatedUtc = value.ActivatedUtc,
                MissingSinceUtc = value.MissingSinceUtc,
                ConfirmedUtc = value.ConfirmedUtc,
                DisconnectConfirmed = value.DisconnectConfirmed,
                InputEvidence = value.InputEvidence
            };
        }
    }
}
