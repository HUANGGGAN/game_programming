using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [SerializeField, Min(0f)] private float interactionRange = 1.5f;
    [SerializeField] private string interactionHint = "Press {Interact} to interact";
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

    public virtual string GetHint() => FormatInteractionHint(interactionHint);

    protected static string FormatInteractionHint(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string key = GameInput.Label(GameBinding.Interact);
        // Support existing serialized hints as well as new configurable templates.
        return text.Replace("{Interact}", key).Replace("Press F", "Press " + key)
            .Replace("press F", "press " + key).Replace("F to ", key + " to ");
    }
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
        if (GameInput.Pressed(GameBinding.Interact))
        {
            GameInput.Consume();
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
        InteractionHint.SetHint(visible ? FormatInteractionHint(GetHint()) : string.Empty,
            visible ? GetHintColor() : Color.white, visible ? transform : null);
    }
}
