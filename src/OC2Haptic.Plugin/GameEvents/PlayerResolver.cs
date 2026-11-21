using OC2Haptic.Core;
using UnityEngine;

namespace OC2Haptic.GameEvents
{
    internal static class PlayerResolver
    {
        internal static GameEvent FromFlight(Component item, object thrower, double nowMs)
        {
            if (item == null) return null;
            return FromChef(thrower as Component, item.GetInstanceID(), GameEventType.ThrowSuccess,
                nowMs, "ClientThrowableItem.StartFlight");
        }

        internal static GameEvent FromChef(Component chef, int itemId, GameEventType type, double nowMs, string source)
        {
            if (chef == null) return null;
            var controls = chef.GetComponent<PlayerControls>();
            var identity = chef.GetComponent<PlayerIDProvider>();
            // Teleportals have throwers too. Require the chef's input/identity components.
            if (controls == null || identity == null || !identity.IsLocallyControlled()) return null;
            PlayerControls.ControlSchemeData scheme = controls.ControlScheme;
            if (scheme == null) return null;
            ControlPadInput.PadNum pad = PlayerInputLookup.GetPadForPlayer(scheme.Player);
            if (pad == ControlPadInput.PadNum.Invalid) return null;
            return new GameEvent(type, EventScope.LocalPlayer, nowMs, (int)scheme.Player, (int)pad, itemId,
                true, PCPadInputProvider.IsKeyboard(pad), EventConfidence.ConfirmedState, source);
        }

        internal static GameEvent FromTeam(GameEventType type, int eventTeam, int orderId, int boundPad,
            double nowMs, string source)
        {
            var providers = PlayerIDProvider.s_AllProviders;
            if (providers == null || boundPad < 0) return null;
            int localTeam = -1;
            int player = -1;
            // Existing provider list, visited only on a real kitchen event; no per-frame scene search.
            for (int index = 0; index < providers.Count; index++)
            {
                PlayerIDProvider provider = providers._items[index];
                if (provider == null || !provider.IsLocallyControlled()) continue;
                PlayerControls controls = provider.GetComponent<PlayerControls>();
                if (controls == null || controls.ControlScheme == null) continue;
                PlayerInputLookup.Player candidate = controls.ControlScheme.Player;
                ControlPadInput.PadNum pad = PlayerInputLookup.GetPadForPlayer(candidate);
                if ((int)pad != boundPad || PCPadInputProvider.IsKeyboard(pad)) continue;
                int team = (int)provider.GetTeam();
                if (team < 0 || team > 1) continue;
                if (localTeam >= 0 && localTeam != team) return null; // Ambiguous ownership stays silent.
                localTeam = team;
                player = (int)candidate;
            }
            if (localTeam < 0) return null;
            return new GameEvent(type, EventScope.LocalTeam, nowMs, player, boundPad, orderId, true, false,
                EventConfidence.ConfirmedState, source, eventTeam, localTeam);
        }
    }
}
