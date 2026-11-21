using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Keeps one UI EventSystem active across menu, gameplay, and additive map scenes.</summary>
[DefaultExecutionOrder(-32000)]
public sealed class EventSystemGuard : MonoBehaviour
{
    private static EventSystemGuard instance;
    private readonly HashSet<EventSystem> suppressed = new HashSet<EventSystem>();
    private EventSystem owner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        GameObject root = new GameObject(nameof(EventSystemGuard));
        root.AddComponent<EventSystemGuard>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSingleEventSystem();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureSingleEventSystem();

    // Run before EventSystem.Update, also catching EventSystems created or enabled at runtime.
    private void Update() => EnsureSingleEventSystem();

    private void EnsureSingleEventSystem()
    {
        suppressed.RemoveWhere(system => system == null);
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (owner == null || !owner.isActiveAndEnabled)
        {
            owner = null;
            foreach (EventSystem system in systems)
                if (system.enabled) { owner = system; break; }
            // Restore a system we previously suppressed when the owner scene unloads.
            if (owner == null)
                foreach (EventSystem system in systems)
                    if (suppressed.Contains(system)) { owner = system; break; }
        }
        foreach (EventSystem system in systems)
        {
            if (system == owner)
            {
                if (suppressed.Remove(system)) system.enabled = true;
            }
            else if (system.enabled)
            {
                suppressed.Add(system);
                system.enabled = false;
            }
        }
        if (owner != null && EventSystem.current != owner) EventSystem.current = owner;
    }

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
