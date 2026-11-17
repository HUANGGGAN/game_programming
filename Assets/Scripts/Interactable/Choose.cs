using UnityEngine;

public class Choose : MonoBehaviour
{
    [SerializeField] private PickableItem item1;
    [SerializeField] private PickableItem item2;
    private bool chosen;

    private void OnEnable()
    {
        if (item1 == null || item2 == null || item1 == item2)
        {
            Debug.LogWarning("Assign two different PickableItems to Choose.", this);
            return;
        }
        item1.PickedUp += OnPickedUp;
        item2.PickedUp += OnPickedUp;
    }

    private void OnPickedUp(PickableItem pickedItem)
    {
        if (chosen) return;
        chosen = true;
        PickableItem other = pickedItem == item1 ? item2 : item1;
        if (other == null) return;
        other.gameObject.SetActive(false);
        Destroy(other.gameObject);
    }

    private void OnDisable()
    {
        if (item1 != null) item1.PickedUp -= OnPickedUp;
        if (item2 != null) item2.PickedUp -= OnPickedUp;
    }
}
