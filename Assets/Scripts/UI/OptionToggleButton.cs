using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OptionToggleButton : MonoBehaviour
{
    [SerializeField] private GameOption option;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite onIcon;
    [SerializeField] private Sprite offIcon;
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
        if (icon == null) icon = GetComponent<Image>();
        button.onClick.AddListener(Toggle);
    }
    private void OnEnable() { GameOptions.Changed += Refresh; Refresh(); }
    private void Toggle() => GameOptions.Toggle(option);
    private void Refresh() { if (icon != null) icon.sprite = GameOptions.Get(option) ? onIcon : offIcon; }
    private void OnDisable() => GameOptions.Changed -= Refresh;
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Toggle); }
}
