using UnityEngine;
using UnityEngine.InputSystem;

// Attach to an always-active UI manager, outside MapPanel.
public class MapUI : MonoBehaviour
{
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private RectTransform map;
    [SerializeField] private RectTransform playerPosition;
    [SerializeField] private Player player;
    [SerializeField] private Vector2 worldBottomLeft = new Vector2(-150f, -150f);
    [SerializeField] private Vector2 worldTopRight = new Vector2(150f, 150f);
    private StateManager stateManager;

    private void OnEnable()
    {
        ResolveReferences();
        stateManager = StateManager.Instance;
        stateManager.StateChanged += ApplyState;
        ApplyState(stateManager.CurrentState);
    }

    private void ResolveReferences()
    {
        if (map == null && mapPanel != null)
            map = mapPanel.transform.Find("Map") as RectTransform;
        if (playerPosition == null && map != null)
            playerPosition = map.Find("PlayerPosition") as RectTransform;
        if (player == null) player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            ToggleMap();
    }

    public void ToggleMap()
    {
        if (stateManager == null) return;
        if (stateManager.CurrentState == GameState.Map)
        {
            stateManager.CloseState(GameState.Map);
            return;
        }
        if (stateManager.CurrentState != GameState.Playing) return;
        ResolveReferences();
        if (mapPanel == null || map == null || playerPosition == null || player == null || player.IsDying)
            return;
        stateManager.RequestState(GameState.Map);
    }

    private void ApplyState(GameState state)
    {
        if (mapPanel == null) return;
        bool open = state == GameState.Map;
        mapPanel.SetActive(open);
        if (!open) return;
        Canvas.ForceUpdateCanvases();
        RefreshPlayerPosition();
    }

    public void RefreshPlayerPosition()
    {
        ResolveReferences();
        if (map == null || playerPosition == null || player == null) return;
        Vector3 world = player.transform.position;
        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(worldBottomLeft.x, worldTopRight.x, world.x),
            Mathf.InverseLerp(worldBottomLeft.y, worldTopRight.y, world.y));
        playerPosition.anchorMin = map.pivot;
        playerPosition.anchorMax = map.pivot;
        playerPosition.anchoredPosition = Vector2.Scale(normalized - map.pivot, map.rect.size);
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.StateChanged -= ApplyState;
            stateManager.CloseState(GameState.Map);
        }
        if (mapPanel != null) mapPanel.SetActive(false);
    }
}
