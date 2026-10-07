using UnityEngine;
using UnityEngine.UI;

public class WeaponSlot : MonoBehaviour
{
    [SerializeField] private Image weaponIconImage;

    public void SetWeaponIcon(Sprite icon)
    {
        Image targetImage = weaponIconImage != null ? weaponIconImage : GetComponent<Image>();

        if (targetImage == null)
            return;

        targetImage.sprite = icon;
        targetImage.preserveAspect = true;
        targetImage.color = icon != null ? Color.white : Color.clear;
    }

    public void SetEquipped(bool equipped)
    {
        Image targetImage = weaponIconImage != null ? weaponIconImage : GetComponent<Image>();

        if (targetImage == null || targetImage.sprite == null)
            return;

        Color color = targetImage.color;
        color.a = equipped ? 1f : 0.3f;
        targetImage.color = color;
    }
}