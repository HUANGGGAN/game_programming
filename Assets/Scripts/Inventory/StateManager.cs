using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { Playing, Inventory, Tutorial, Merchant, Purchase, Decomposition, Storage, Map, Boss }

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
    public bool IsGameplay => CurrentState == GameState.Playing || CurrentState == GameState.Boss;
    public bool IsBossEncounter => CurrentState == GameState.Boss ||
        (CurrentState == GameState.Inventory && stateBeforeInventory == GameState.Boss) ||
        (CurrentState == GameState.Tutorial && (stateBeforeTutorial == GameState.Boss ||
            (stateBeforeTutorial == GameState.Inventory && stateBeforeInventory == GameState.Boss)));
    private GameState stateBeforeInventory = GameState.Playing;
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
        if (IsBossEncounter && state != GameState.Inventory && state != GameState.Boss && state != GameState.Tutorial) return false;
        // Only closing the tutorial can restore the state underneath it.
        if (CurrentState == GameState.Tutorial)
            return false;
        if (state == GameState.Tutorial)
            stateBeforeTutorial = CurrentState;
        if (state == GameState.Inventory)
            stateBeforeInventory = CurrentState;
        SetState(state);
        return true;
    }

    public void CloseState(GameState state)
    {
        if (state == GameState.Boss && IsBossEncounter)
        {
            stateBeforeInventory = GameState.Playing;
            stateBeforeTutorial = GameState.Playing;
            SetState(GameState.Playing);
            return;
        }
        if (CurrentState != state)
            return;
        SetState(state == GameState.Tutorial ? stateBeforeTutorial :
            state == GameState.Inventory ? stateBeforeInventory :
            state == GameState.Purchase || state == GameState.Decomposition ? GameState.Merchant : GameState.Playing);
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
        Time.timeScale = IsGameplay ? 1f : 0f;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            ((Keyboard.current.escapeKey.wasPressedThisFrame &&
              (CurrentState == GameState.Inventory || CurrentState == GameState.Merchant || CurrentState == GameState.Purchase || CurrentState == GameState.Decomposition || CurrentState == GameState.Storage || CurrentState == GameState.Map)) ||
             (Keyboard.current.eKey.wasPressedThisFrame &&
              (CurrentState == GameState.Merchant || CurrentState == GameState.Purchase || CurrentState == GameState.Decomposition || CurrentState == GameState.Storage || CurrentState == GameState.Map))))
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
