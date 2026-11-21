using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyboardBindingButton : MonoBehaviour
{
    [SerializeField] private GameBinding binding;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color selectionColor = Color.yellow;
    [SerializeField] private Color conflictColor = new Color(1f, 0.6f, 0.6f, 1f);
    private Button button;
    private ColorBlock originalColors;
    private bool selecting;
    private int selectionFrame;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalColors = button.colors;
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        button.onClick.AddListener(BeginSelection);
    }
    private void OnEnable() { GameInput.Changed += Refresh; Refresh(); }
    private void Refresh()
    {
        if (label != null) label.text = GameInput.Label(binding);
        if (button == null) return;
        ColorBlock colors = originalColors;
        if (GameInput.HasConflict(binding) || selecting)
        {
            Color tint = selecting ? selectionColor : conflictColor;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.pressedColor = tint;
        }
        button.colors = colors;
    }
    public void BeginSelection()
    {
        if (StateManager.Instance.CurrentState != GameState.Pause) return;
        GameInput.BeginCapture(this);
        selecting = true;
        Refresh();
        selectionFrame = Time.frameCount;
    }
    private void Update()
    {
        if (!selecting || selectionFrame == Time.frameCount || Keyboard.current == null) return;
        foreach (var key in Keyboard.current.allKeys)
        {
            if (!key.wasPressedThisFrame || !GameInput.IsAllowed(key.keyCode)) continue;
            GameInput.Set(binding, key.keyCode);
            GameInput.CancelCapture();
            break;
        }
    }
    public void StopSelection()
    {
        if (selecting && button != null) button.colors = originalColors;
        selecting = false;
        Refresh();
    }
    private void OnDisable()
    {
        GameInput.Changed -= Refresh;
        if (GameInput.Capturing == this) GameInput.CancelCapture();
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(BeginSelection); }
}
