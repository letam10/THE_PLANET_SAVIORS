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
        public static bool IsUiFocused { get; private set; }
        public static RuntimeInputMode CurrentMode => IsUiFocused ? RuntimeInputMode.UI : RuntimeInputMode.Gameplay;

        public static void SetUiFocused(bool focused)
        {
            IsUiFocused = focused;
            Cursor.lockState = focused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = focused;
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
