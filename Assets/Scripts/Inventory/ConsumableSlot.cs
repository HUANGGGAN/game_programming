using UnityEngine;
using UnityEngine.UI;

public class ConsumableSlot : MonoBehaviour
{
    [SerializeField] private Image consumableIconImage;

    public void SetConsumableIcon(Sprite icon)
    {
        Image image = consumableIconImage != null ? consumableIconImage : GetComponent<Image>();
        if (image == null)
            return;
        image.sprite = icon;
        image.preserveAspect = true;
        image.color = icon != null ? Color.white : Color.clear;
    }
}
