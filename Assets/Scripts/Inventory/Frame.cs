using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Frame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Serializable]
    public struct RarityColor
    {
        public ItemRarity rarity;
        public Color color;

        public RarityColor(ItemRarity rarity, Color color)
        {
            this.rarity = rarity;
            this.color = color;
        }
    }

    [SerializeField] private Image frameImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private InventorySlot slot;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color validDropColor = new Color(0.3f, 0.7f, 1f, 0.5f);
    [SerializeField] private Color hoveredDropColor = new Color(0.8f, 0.95f, 1f, 1f);
    [SerializeField] private RarityColor[] rarityColors =
    {
        new RarityColor(ItemRarity.Common, new Color(0.7f, 0.7f, 0.7f, 1f)),
        new RarityColor(ItemRarity.Uncommon, new Color(0.2f, 0.8f, 0.3f, 1f)),
        new RarityColor(ItemRarity.Rare, new Color(0.2f, 0.5f, 1f, 1f)),
        new RarityColor(ItemRarity.Epic, new Color(0.7f, 0.3f, 0.9f, 1f)),
        new RarityColor(ItemRarity.Legendary, new Color(1f, 0.65f, 0.1f, 1f)),
        new RarityColor(ItemRarity.Mythic, new Color(1f, 0.2f, 0.3f, 1f))
    };

    private bool pointerInside;

    private void OnEnable()
    {
        ResolveReferences();
        RefreshColor();
    }

    private void OnValidate()
    {
        ResolveReferences();
        RefreshColor();
    }

    private void ResolveReferences()
    {
        if (frameImage == null)
            frameImage = GetComponent<Image>();
        if (borderImage == null)
        {
            foreach (Transform child in transform)
            {
                if (!string.Equals(child.name, "Border", StringComparison.OrdinalIgnoreCase))
                    continue;
                borderImage = child.GetComponent<Image>();
                break;
            }
        }
        if (borderImage != null)
            borderImage.raycastTarget = false;
        if (slot == null)
            slot = GetComponentInChildren<InventorySlot>(true);
    }

    private void LateUpdate()
    {
        RefreshColor();
    }

    private void RefreshColor()
    {
        Color color = defaultColor;
        Color borderColor = Color.clear;
        if (slot != null && slot.isActiveAndEnabled)
        {
            if (!slot.IsDragging && slot.CurrentItem != null && rarityColors != null)
            {
                foreach (RarityColor entry in rarityColors)
                {
                    if (entry.rarity != slot.CurrentItem.rarity)
                        continue;
                    color = entry.color;
                    break;
                }
            }
            if (!slot.IsDragging && slot.CanReceiveDropFrom(InventorySlot.DraggedSlot))
                borderColor = pointerInside ? hoveredDropColor : validDropColor;
        }
        if (frameImage != null)
            frameImage.color = color;
        if (borderImage != null)
            borderImage.color = borderColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        RefreshColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshColor();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Accept drops on the frame border as well as on the child slot.
        if (slot != null && slot.isActiveAndEnabled)
            slot.OnDrop(eventData);
    }

    private void OnDisable()
    {
        pointerInside = false;
        if (frameImage != null)
            frameImage.color = defaultColor;
        if (borderImage != null)
            borderImage.color = Color.clear;
    }
}
