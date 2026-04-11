using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class MerchantAnchor : MonoBehaviour, IInteractable, IStateResolvable
    {
        [SerializeField] private string _merchantId = "merchant";
        [SerializeField] private ShopDefinition _shopDefinition;

        public ShopDefinition ShopDefinition => _shopDefinition;

        public string GetInteractionPrompt()
        {
            return EconomyService.Instance != null && EconomyService.Instance.CanAccessShop(_shopDefinition)
                ? "Press [E] to trade"
                : "Shop unavailable";
        }

        public void Interact(GameObject interactor)
        {
            if (EconomyService.Instance == null || !EconomyService.Instance.CanAccessShop(_shopDefinition))
            {
                return;
            }

            if (RuntimeMenuCanvasController.Instance != null)
            {
                RuntimeMenuCanvasController.Instance.ToggleMerchantShop(this);
                return;
            }

            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.ToggleShop(this);
            }
        }

        private void OnEnable()
        {
            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Unregister(this);
            }
        }

        public void ResolveState()
        {
            if (GameStateManager.Instance == null || EconomyService.Instance == null || _shopDefinition == null)
            {
                return;
            }

            GameStateManager.Instance.SetBool($"shop.{_merchantId}.available", EconomyService.Instance.CanAccessShop(_shopDefinition));
        }
    }
}
