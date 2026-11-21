using UnityEngine;
using UnityEngine.UI;

// Keep this component outside the panel that it hides.
public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button continueButton;
    private StateManager stateManager;
    private int openedFrame;
    private void Awake()
    {
        if (continueButton == null && pauseMenu != null)
            continueButton = pauseMenu.transform.Find("ContinueButton")?.GetComponent<Button>();
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
    }
    private void OnEnable()
    {
        stateManager = StateManager.Instance;
        stateManager.StateChanged += ApplyState;
        GameInput.Changed += RefreshValidation;
        RefreshValidation();
        ApplyState(stateManager.CurrentState);
    }
    private void LateUpdate()
    {
        if (stateManager == null || !GameInput.Pressed(GameBinding.Leave)) return;
        if (stateManager.CurrentState == GameState.Pause)
        {
            if (openedFrame == Time.frameCount) return;
            GameInput.Consume();
            Continue();
        }
        else if (stateManager.IsGameplay)
        {
            GameInput.Consume();
            Open();
        }
    }
    public void Open()
    {
        if (pauseMenu != null) stateManager.RequestState(GameState.Pause);
    }
    public void Continue()
    {
        GameInput.CancelCapture();
        GameInput.Consume();
        if (GameInput.HasConflicts) return;
        stateManager.CloseState(GameState.Pause);
    }
    public void Toggle()
    {
        if (stateManager.CurrentState == GameState.Pause) Continue();
        else Open();
    }
    private void ApplyState(GameState state)
    {
        if (state == GameState.Pause) openedFrame = Time.frameCount;
        else GameInput.CancelCapture();
        if (pauseMenu != null) pauseMenu.SetActive(state == GameState.Pause);
    }
    private void RefreshValidation()
    {
        if (continueButton != null) continueButton.interactable = !GameInput.HasConflicts;
    }
    private void OnDisable()
    {
        GameInput.Changed -= RefreshValidation;
        GameInput.CancelCapture();
        if (stateManager != null)
        {
            stateManager.StateChanged -= ApplyState;
            stateManager.CloseState(GameState.Pause);
        }
    }
    private void OnDestroy() { if (continueButton != null) continueButton.onClick.RemoveListener(Continue); }
}
