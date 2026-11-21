using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string startingMenuScene = "StartingMenu";
    [Tooltip("Additional scenes only for the main game, such as spawn regions, merchant, storage box, and Home region.")]
    [SerializeField] private string[] mainGameScenes = { "MainGame" };
    [Tooltip("Additional tutorial-only scenes. Assign the scenes you create for the tutorial level.")]
    [SerializeField] private string[] tutorialScenes = { "Tutorial" };
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
    [Header("Run Data (kept in memory across menu visits)")]
    [SerializeField] private TutorialSO tutorialData;
    [SerializeField] private GameplaySaveSO gameplaySave;
    public bool IsResuming { get; private set; }
    public bool HasSavedGame => gameplaySave != null && gameplaySave.isSaved;
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
        _ = GameDataManager.Instance;
        if (gameplaySave == null) gameplaySave = ScriptableObject.CreateInstance<GameplaySaveSO>();
        // A flag from an authored asset does not represent a saved runtime session after restarting the app.
        gameplaySave.isSaved = false;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void StartGame() => StartLevel(mainGameScenes, true);
    public void StartTutorial() => StartLevel(tutorialScenes, false);

    private void StartLevel(string[] levelScenes, bool loadMap)
    {
        if (IsLoading || gameStarted) return;
        var scenes = new List<string> { mainScene };
        if (levelScenes != null) scenes.AddRange(levelScenes);
        if (loadMap) scenes.Add(mapScene);
        if (!ValidateScenes(scenes)) return;
        IsResuming = false;
        if (loadMap) gameplaySave.isSaved = false;
        GameDataManager.Instance.SelectLevel(loadMap ? GameplayLevel.MainGame : GameplayLevel.Tutorial, true);
        Tutorial.ClearPendingRequests();
        if (loadMap || !HasSavedGame) GetTutorialData().ResetProgress();
        StartCoroutine(LoadGame(scenes, false));
    }

    public void ResumeGame()
    {
        if (IsLoading || gameStarted || !HasSavedGame || GameInput.HasConflicts) return;
        var scenes = new List<string> { mainScene };
        if (mainGameScenes != null) scenes.AddRange(mainGameScenes);
        scenes.Add(mapScene);
        if (!ValidateScenes(scenes)) return;
        IsResuming = true;
        GameDataManager.Instance.SelectLevel(GameplayLevel.MainGame, false);
        Tutorial.ClearPendingRequests();
        StartCoroutine(LoadGame(scenes, false));
    }

    public TutorialSO GetTutorialData(TutorialSO preferred = null)
    {
        if (tutorialData == null) tutorialData = preferred != null ? preferred : ScriptableObject.CreateInstance<TutorialSO>();
        return tutorialData;
    }

    private void CaptureRun()
    {
        Player player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null) player.CancelEncounterForMenu();
        // The persistent manager retains both profiles. Tutorial visits never overwrite the main-game save.
        if (GameDataManager.Instance.ActiveLevel == GameplayLevel.MainGame) gameplaySave.isSaved = player != null;
    }

    public void ReturnToMenu()
    {
        if (IsLoading || GameInput.HasConflicts) return;
        var scenes = new List<string> { startingMenuScene };
        if (!ValidateScenes(scenes)) return;
        GameInput.CancelCapture();
        GameInput.Consume();
        CaptureRun();
        Tutorial.ClearPendingRequests();
        StartCoroutine(LoadGame(scenes, true));
    }

    private bool ValidateScenes(List<string> scenes)
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string scene in scenes)
        {
            if (string.IsNullOrWhiteSpace(scene) || !unique.Add(System.IO.Path.GetFileNameWithoutExtension(scene)) ||
                !Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("SceneLoader requires distinct valid scenes enabled in the build scene list. Check: " + scene, this);
                return false;
            }
        }
        if (loadingPanel == null || loadingPanel == gameObject || !loadingPanel.transform.IsChildOf(transform))
        {
            Debug.LogError("Assign a loading panel beneath SceneLoader so it survives scene changes.", this);
            return false;
        }
        return true;
    }

    private IEnumerator LoadGame(List<string> scenes, bool returningToMenu)
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
        float weight = 0.9f / scenes.Count;
        for (int i = 0; i < scenes.Count; i++)
        {
            yield return LoadScene(scenes[i], i == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive, i * weight, weight);
            if (sceneLoadFailed) { FinishFailedLoad(); yield break; }
        }
        Scene main = SceneManager.GetSceneByName(System.IO.Path.GetFileNameWithoutExtension(scenes[0]));
        if (main.IsValid() && main.isLoaded) SceneManager.SetActiveScene(main);
        SetProgress(0.95f, "Finishing loading...");
        yield return null;
        // Release assets from the previous level while the persistent screen still covers the transition.
        yield return Resources.UnloadUnusedAssets();
        // Let the map's Awake/OnEnable/Start callbacks bind their runtime references.
        SetProgress(1f, "Ready");
        while (displayedProgress < 1f) yield return null;
        // Present the fully filled bar for a frame before hiding it.
        yield return null;
        gameStarted = !returningToMenu;
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
