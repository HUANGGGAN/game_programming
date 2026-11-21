using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent menu-to-game loader. Keep its loading canvas under this object.</summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public static bool IsLoading { get; private set; }
    public static event Action<bool> LoadingChanged;

    [Header("Scenes (must be enabled in the build scene list)")]
    [SerializeField] private string mainScene = "Main";
    [SerializeField] private string mapScene = "Map_0_0";
    [Header("Loading UI (children of this object)")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private ProgressBar progressBar;
    [Header("Progress Animation")]
    [SerializeField, Min(0.01f), Tooltip("Maximum bar fill per second. 0.5 fills the whole bar in two seconds.")]
    private float progressFillSpeed = 0.5f;
    [SerializeField, Min(0.1f), Tooltip("Controls gradual progress while Unity reports no change. Larger values move more slowly.")]
    private float simulatedProgressTime = 8f;
    private float displayedProgress;
    private float targetProgress;
    private bool gameStarted;
    private bool sceneLoadFailed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsLoading = false;
        LoadingChanged = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // Assign this method to the StartingMenu button's On Click event.
    public void StartGame()
    {
        if (IsLoading || gameStarted) return;
        if (string.IsNullOrWhiteSpace(mainScene) || string.IsNullOrWhiteSpace(mapScene) || mainScene == mapScene ||
            !Application.CanStreamedLevelBeLoaded(mainScene) || !Application.CanStreamedLevelBeLoaded(mapScene))
        {
            Debug.LogError("Enable Main and Map_0_0 in the build scene list, and assign two different valid scene names on SceneLoader.", this);
            return;
        }
        if (loadingPanel == null || loadingPanel == gameObject || !loadingPanel.transform.IsChildOf(transform))
        {
            Debug.LogError("Assign a loading panel beneath SceneLoader so it survives the menu scene unloading.", this);
            return;
        }
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        SetLoading(true);
        sceneLoadFailed = false;
        displayedProgress = targetProgress = 0f;
        if (progressBar != null) progressBar.SetProgress(0f);
        loadingPanel.SetActive(true);
        CanvasGroup group = loadingPanel.GetComponent<CanvasGroup>();
        if (group == null) group = loadingPanel.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = false;
        Canvas canvas = loadingPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
        }
        SetProgress(0f, "Loading game...");
        // Render the loading screen before starting expensive scene work.
        yield return null;
        yield return LoadScene(mainScene, LoadSceneMode.Single, 0f, 0.45f);
        if (sceneLoadFailed) { FinishFailedLoad(); yield break; }
        yield return LoadScene(mapScene, LoadSceneMode.Additive, 0.45f, 0.5f);
        if (sceneLoadFailed) { FinishFailedLoad(); yield break; }
        Scene main = SceneManager.GetSceneByName(System.IO.Path.GetFileNameWithoutExtension(mainScene));
        if (main.IsValid() && main.isLoaded) SceneManager.SetActiveScene(main);
        // Let the map's Awake/OnEnable/Start callbacks bind their runtime references.
        SetProgress(1f, "Ready");
        while (displayedProgress < 1f) yield return null;
        // Present the fully filled bar for a frame before hiding it.
        yield return null;
        gameStarted = true;
        loadingPanel.SetActive(false);
        SetLoading(false);
    }

    private IEnumerator LoadScene(string sceneName, LoadSceneMode mode, float start, float weight)
    {
        AsyncOperation operation = BeginLoad(sceneName, mode);
        if (operation == null) { sceneLoadFailed = true; yield break; }
        float elapsed = 0f;
        while (!operation.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float reported = Mathf.Clamp01(operation.progress / 0.9f);
            // Approach the stage's end gradually, leaving room until scene activation completes.
            float simulated = 0.98f * (1f - Mathf.Exp(-elapsed / Mathf.Max(0.1f, simulatedProgressTime)));
            float stageProgress = Mathf.Min(0.98f, Mathf.Max(reported, simulated));
            SetProgress(start + stageProgress * weight, "Loading " + sceneName + "...");
            yield return null;
        }
        SetProgress(start + weight, "Loading " + sceneName + "...");
    }

    private AsyncOperation BeginLoad(string sceneName, LoadSceneMode mode)
    {
        try { return SceneManager.LoadSceneAsync(sceneName, mode); }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return null;
        }
    }

    private void SetProgress(float progress, string message)
    {
        targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(progress));
        if (loadingText != null) loadingText.text = message;
    }

    private void Update()
    {
        if (!IsLoading || sceneLoadFailed) return;
        // Unscaled time keeps the bar moving while gameplay is paused. Limit catch-up after a stall.
        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress,
            Mathf.Max(0.01f, progressFillSpeed) * Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        if (progressBar != null) progressBar.SetProgress(displayedProgress);
    }

    private void FinishFailedLoad()
    {
        if (loadingText != null) loadingText.text = "Loading failed. Check the scene configuration and Console.";
        Debug.LogError("SceneLoader failed to load the game scenes.", this);
        // Keep the loading screen up and gameplay paused rather than running with a missing map.
    }

    private static void SetLoading(bool loading)
    {
        IsLoading = loading;
        Time.timeScale = loading ? 0f : 1f;
        LoadingChanged?.Invoke(loading);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (IsLoading) SetLoading(false);
    }
}
