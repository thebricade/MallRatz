using System;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class UIEvents
    {
        public static event Action<bool> OnCrosshairVisibilityChanged;
        public static void RaiseCrosshairVisibilityChanged(bool visible) => OnCrosshairVisibilityChanged?.Invoke(visible);

        public static event Action<float> OnHoldProgressChanged;
        public static void RaiseHoldProgressChanged(float progress) => OnHoldProgressChanged?.Invoke(progress);

        public static event Action<bool> OnInteractionAvailable;
        public static void RaiseInteractionAvailable(bool available) => OnInteractionAvailable?.Invoke(available);

        public static event Action<string> OnInteractMessageRequested;
        public static void RaiseInteractMessage(string message) => OnInteractMessageRequested?.Invoke(message);

        public static event Action<string, Color?, float> OnMessageLogged;
        public static void RaiseMessage(string message, Color? color = null, float duration = 1f) => OnMessageLogged?.Invoke(message, color, duration);

        public static event Action<ActionType, bool, System.Action> OnActionUIToggled;
        public static void RaiseActionUI(ActionType actionType, bool active, System.Action callback) => OnActionUIToggled?.Invoke(actionType, active, callback);

        public static event System.Action OnUIPanelOpened;
        public static void RaiseUIPanelOpened() => OnUIPanelOpened?.Invoke();

        public static event System.Action OnUIPanelClosed;
        public static void RaiseUIPanelClosed() => OnUIPanelClosed?.Invoke();
    }
}
