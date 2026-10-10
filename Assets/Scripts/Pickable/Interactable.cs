using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [SerializeField, Min(0f)] private float interactionRange = 1.5f;
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private TMP_Text hintText;
    [SerializeField, Min(0f)] private float hintFontSize = 6f;
    [SerializeField] private string interactionHint = "Press F to interact";
    [SerializeField] private UnityEvent onInteract = new UnityEvent();

    private static readonly List<Interactable> activeInteractables = new List<Interactable>();
    private static Interactable selected;
    protected Transform InteractionPlayer { get; private set; }

    public bool IsInRange(Transform player)
    {
        return player != null && ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude
            <= interactionRange * interactionRange;
    }

    protected virtual void OnEnable()
    {
        activeInteractables.Add(this);
        SetHintVisible(false);
    }

    protected virtual void OnDisable()
    {
        activeInteractables.Remove(this);
        SetHintVisible(false);
        if (selected == this)
            selected = null;
    }

    public virtual string GetHint() => interactionHint;
    public virtual Color GetHintColor() => Color.white;

    public virtual bool TryInteract()
    {
        onInteract.Invoke();
        return true;
    }

    public static void UpdateInteraction(Transform player)
    {
        if (Time.timeScale == 0f)
        {
            ClearSelection();
            return;
        }

        Interactable nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Interactable candidate in activeInteractables)
        {
            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            float distance = ((Vector2)candidate.transform.position - (Vector2)player.position).sqrMagnitude;
            if (distance <= candidate.interactionRange * candidate.interactionRange && distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        if (selected != nearest)
        {
            ClearSelection();
            selected = nearest;
        }

        if (selected == null)
            return;

        selected.InteractionPlayer = player;
        selected.SetHintVisible(true);
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            selected.TryInteract();
            if (selected != null)
                selected.SetHintVisible(true);
        }
    }

    public static void ClearSelection()
    {
        if (selected != null)
            selected.SetHintVisible(false);
        selected = null;
    }

    private void SetHintVisible(bool visible)
    {
        if (visible && hintText == null)
        {
            GameObject hintObject = new GameObject("Interaction Hint");
            hintObject.transform.SetParent(transform, false);
            TextMeshPro text = hintObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(40f, 8f);
            text.rectTransform.localScale = Vector3.one * 1f;
            text.sortingOrder = short.MaxValue;
            hintText = text;
        }

        if (hintText == null)
            return;

        hintText.enabled = visible;
        if (visible)
        {
            hintText.fontSize = hintFontSize;
            hintText.text = GetHint();
            hintText.color = GetHintColor();
            hintText.transform.position = transform.position + hintOffset;
            hintText.transform.rotation = Quaternion.identity;
        }
    }
}
