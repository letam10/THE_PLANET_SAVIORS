using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.Spawn;
using TPS.Runtime.Time;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class InnAnchor : MonoBehaviour, IInteractable
    {
        [Range(0, 23)] [SerializeField] private int _wakeHour = 7;
        [Range(0, 59)] [SerializeField] private int _wakeMinute = 0;

        public int WakeHour => _wakeHour;
        public int WakeMinute => _wakeMinute;

        public string GetInteractionPrompt()
        {
            return $"Press [E] to rest until {_wakeHour:00}:{_wakeMinute:00}";
        }

        public void Interact(GameObject interactor)
        {
            if (WorldClock.Instance == null)
            {
                return;
            }

            if (RuntimeMenuCanvasController.Instance != null)
            {
                RuntimeMenuCanvasController.Instance.OpenSleepPanel(this);
                return;
            }

            SleepNow();
        }

        public void SleepNow()
        {
            if (WorldClock.Instance == null)
            {
                return;
            }

            if (PartyService.Instance != null)
            {
                PartyService.Instance.RestorePartyAfterSleep();
            }

            WorldClock.Instance.SleepUntilNextDay(_wakeHour, _wakeMinute);
            RuntimeUiInputState.RestoreGameplayFocus();

            if (PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.EnsurePlayerOnValidGround("Default");
            }

            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.ShowMessage("The party rests until morning. Shops and schedules refresh.");
            }
        }
    }
}
