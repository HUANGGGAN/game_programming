using TMPro;
using UnityEngine;

/// <summary>Displays the nearest interactable's hint in one shared scene text.</summary>
public class InteractionHint : MonoBehaviour
{
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);
    private static InteractionHint instance;
    private Transform target;

    private void OnEnable()
    {
        if (hintText == null)
            hintText = GetComponent<TMP_Text>();
        instance = this;
        target = null;
        if (hintText != null)
            hintText.text = string.Empty;
    }

    private void OnDisable()
    {
        if (instance != this) return;
        if (hintText != null)
            hintText.text = string.Empty;
        instance = null;
        target = null;
    }

    private void LateUpdate()
    {
        if (hintText == null) return;
        if (target == null)
        {
            hintText.text = string.Empty;
            return;
        }

        hintText.transform.position = target.position + worldOffset;
    }

    public static void SetHint(string text, Color color, Transform target)
    {
        if (instance == null || instance.hintText == null) return;
        instance.target = target;
        instance.hintText.text = text;
        instance.hintText.color = color;
        if (target != null)
            instance.hintText.transform.position = target.position + instance.worldOffset;
    }
}
