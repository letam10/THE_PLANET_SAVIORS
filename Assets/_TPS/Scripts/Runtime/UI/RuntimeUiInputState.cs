using UnityEngine;

namespace TPS.Runtime.UI
{
    public static class RuntimeUiInputState
    {
        public static bool IsUiFocused { get; private set; }

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
    }
}
