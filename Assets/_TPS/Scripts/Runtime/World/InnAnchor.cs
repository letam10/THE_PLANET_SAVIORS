using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.Time;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class InnAnchor : MonoBehaviour, IInteractable
    {
        [Range(0, 23)] [SerializeField] private int _wakeHour = 7;
        [Range(0, 59)] [SerializeField] private int _wakeMinute = 0;

        public string GetInteractionPrompt()
        {
            return "Press [E] to sleep";
        }

        public void Interact(GameObject interactor)
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
            if (EconomyService.Instance != null)
            {
                EconomyService.Instance.RestockDaily(WorldClock.Instance.CurrentDay);
            }

            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.ShowMessage("The party rests until morning.");
            }
        }
    }
}
