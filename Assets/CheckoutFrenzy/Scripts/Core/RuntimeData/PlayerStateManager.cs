using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public class PlayerStateManager
    {
        public PlayerState CurrentState { get; private set; }

        private readonly Stack<PlayerState> stateStack = new();

        public PlayerStateManager()
        {
            PushState(CurrentState);
        }

        public void PushState(PlayerState newState)
        {
            if (CurrentState != newState)
            {
                stateStack.Push(CurrentState);
                CurrentState = newState;
                HandleTimeScale(newState);
                UpdateCrosshair(newState);
            }
        }

        public void PopState()
        {
            if (stateStack.Count > 0)
            {
                PlayerState restored = stateStack.Pop();
                CurrentState = restored;
                HandleTimeScale(restored);
                UpdateCrosshair(restored);
            }
        }

        public void ClearAll()
        {
            stateStack.Clear();
        }

        private void UpdateCrosshair(PlayerState state)
        {
            bool visible = state is PlayerState.Free
                or PlayerState.Holding
                or PlayerState.Moving;

            UIEvents.RaiseCrosshairVisibilityChanged(visible);
        }

        private void HandleTimeScale(PlayerState state)
        {
            Time.timeScale = (state == PlayerState.Paused) ? 0f : 1f;
        }
    }
}
