using UnityEngine;

public class Storage : Interactable
{
    [SerializeField] private StorageManager storageManager;

    public override string GetHint() => "Press F to open storage";

    public override bool TryInteract()
    {
        if (!IsInRange(InteractionPlayer) || StateManager.Instance.CurrentState != GameState.Playing)
            return false;
        if (storageManager == null)
            storageManager = FindFirstObjectByType<StorageManager>(FindObjectsInactive.Include);
        if (storageManager == null)
            return false;
        bool opened = storageManager.Open();
        if (opened) Interactable.ClearSelection();
        return opened;
    }
}
