using UnityEngine;

public class Merchant : Interactable
{
    [SerializeField] private ShopManager shopManager;

    public override string GetHint() => "Press F to shop";

    public override bool TryInteract()
    {
        if (!IsInRange(InteractionPlayer) || StateManager.Instance.CurrentState != GameState.Playing)
            return false;
        if (shopManager == null)
            shopManager = FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shopManager == null)
            return false;
        return shopManager.Open(InteractionPlayer.GetComponentInParent<Player>());
    }
}
