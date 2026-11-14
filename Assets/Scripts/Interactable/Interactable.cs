using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [SerializeField, Min(0f)] private float interactionRange = 1.5f;
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
    }

    protected virtual void OnDisable()
    {
        activeInteractables.Remove(this);
        if (selected == this)
            ClearSelection();
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
        if (player == null || Time.timeScale == 0f)
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
        InteractionHint.SetHint(visible ? GetHint() : string.Empty,
            visible ? GetHintColor() : Color.white, visible ? transform : null);
    }
}
