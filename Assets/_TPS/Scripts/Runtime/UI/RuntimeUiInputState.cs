using UnityEngine;

namespace TPS.Runtime.UI
{
    public enum RuntimeInputMode
    {
        Gameplay = 0,
        UI = 1
    }

    public static class RuntimeUiInputState
    {
        public static event System.Action<RuntimeInputMode> OnModeChanged;

        public static bool IsUiFocused { get; private set; }
        public static RuntimeInputMode CurrentMode => IsUiFocused ? RuntimeInputMode.UI : RuntimeInputMode.Gameplay;

        public static void SetUiFocused(bool focused)
        {
            if (IsUiFocused == focused)
            {
                return;
            }

            IsUiFocused = focused;
            Cursor.lockState = focused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = focused;
            OnModeChanged?.Invoke(CurrentMode);
        }

        public static void ToggleUiFocused()
        {
            SetUiFocused(!IsUiFocused);
        }

        public static void RestoreGameplayFocus()
        {
            SetUiFocused(false);
        }
    }
}
