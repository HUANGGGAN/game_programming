using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { Playing, Inventory, Tutorial, Merchant }

public class StateManager : MonoBehaviour
{
    private static StateManager instance;
    public static StateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<StateManager>();
                if (instance == null)
                    instance = new GameObject(nameof(StateManager)).AddComponent<StateManager>();
            }
            return instance;
        }
    }

    public GameState CurrentState { get; private set; } = GameState.Playing;
    private GameState stateBeforeTutorial = GameState.Playing;
    public event Action<GameState> StateChanged;
    public event Action<GameState> StateChanging;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        ApplyTimeScale();
    }

    public bool RequestState(GameState state)
    {
        if (CurrentState == state)
            return true;
        // Only closing the tutorial can restore the state underneath it.
        if (CurrentState == GameState.Tutorial)
            return false;
        if (state == GameState.Tutorial)
            stateBeforeTutorial = CurrentState;
        SetState(state);
        return true;
    }

    public void CloseState(GameState state)
    {
        if (CurrentState != state)
            return;
        SetState(state == GameState.Tutorial ? stateBeforeTutorial : GameState.Playing);
    }

    private void SetState(GameState state)
    {
        StateChanging?.Invoke(state);
        CurrentState = state;
        ApplyTimeScale();
        StateChanged?.Invoke(state);
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = CurrentState == GameState.Playing ? 1f : 0f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame &&
            (CurrentState == GameState.Inventory || CurrentState == GameState.Merchant))
            CloseState(CurrentState);
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;
        instance = null;
        Time.timeScale = 1f;
    }
}
