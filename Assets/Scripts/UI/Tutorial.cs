using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }

    [SerializeField] private TutorialSO tutorialData;
    public TutorialSO Data => tutorialData;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipTutorialButton;

    private struct Request
    {
        public string id;
        public string text;
        public Sprite image;
    }

    private static readonly Dictionary<string, Request> pendingRequests = new Dictionary<string, Request>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPendingRequests() => pendingRequests.Clear();

    public static void RequestTutorial(string id, string text)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (Instance != null && Instance.isActiveAndEnabled)
            Instance.ShowTutorial(id, text);
        else
            pendingRequests[id] = new Request { id = id, text = text };
    }

    public static void ClearPendingRequests() => pendingRequests.Clear();

    public static void NotifyItemObtained(ItemSO item)
    {
        if (item == null) return;
        if (item is MultiWeaponSO)
            RequestTutorial("multi-weapon", "Press T to switch attack mode (A/B)");
        if (item is WeaponSO && (item.tag == ItemTag.Fire || item.tag == ItemTag.Frost))
            RequestTutorial("weapon-elements", "Enemies have resistances and weaknesses.\nFind suitable weapons to attack them.");
        if (item.tag == ItemTag.Chest)
            RequestTutorial("locked-chests", "Some chests are locked.\nFind the merchant at home to open them.");
    }

    private void Update()
    {
        if (SceneLoader.IsLoading) return;
        foreach (Request request in pendingRequests.Values)
            ShowTutorial(request.id, request.text, request.image);
        pendingRequests.Clear();
        if (IsShowing && stateManager != null && stateManager.CurrentState != GameState.Tutorial && !stateManager.IsPausedTutorial)
            Close();
        if (IsShowing || requests.Count == 0) return;
        stateManager = StateManager.Instance;
        if (!stateManager.RequestState(GameState.Tutorial)) return;
        Request next = requests.Dequeue();
        queuedIds.Remove(next.id);
        Display(next);
    }

    private readonly HashSet<string> queuedIds = new HashSet<string>();
    private readonly Queue<Request> requests = new Queue<Request>();
    private StateManager stateManager;
    private int displayedFrame;
    public bool IsShowing { get; private set; }
    public bool TutorialsSkipped => tutorialData != null && tutorialData.tutorialsSkipped;

    private void LateUpdate()
    {
        // Handle dismissal after menu input so this key cannot also close the underlying panel.
        if (!IsShowing || displayedFrame == Time.frameCount || stateManager == null ||
            stateManager.CurrentState != GameState.Tutorial) return;
        if (GameInput.Pressed(GameBinding.Inventory) || GameInput.Pressed(GameBinding.Leave))
        {
            GameInput.Consume();
            Continue();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one Tutorial controller should be active.", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (SceneLoader.Instance != null) tutorialData = SceneLoader.Instance.GetTutorialData(tutorialData);
        else if (tutorialData == null) tutorialData = ScriptableObject.CreateInstance<TutorialSO>();
        if (continueButton != null)
            continueButton.onClick.AddListener(Continue);
        if (skipTutorialButton != null)
            skipTutorialButton.onClick.AddListener(SkipTutorial);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    // Returns true when a new tutorial is accepted for display, including queued requests.
    public bool ShowTutorial(string tutorialId, string text, Sprite image = null)
    {
        if (TutorialsSkipped || !isActiveAndEnabled || string.IsNullOrWhiteSpace(tutorialId) ||
            tutorialData.HasRead(tutorialId) || queuedIds.Contains(tutorialId))
            return false;

        if (tutorialPanel == null || tutorialText == null || continueButton == null)
        {
            Debug.LogError("Assign Tutorial's panel, text, and Continue button in the Inspector.", this);
            return false;
        }

        Request request = new Request { id = tutorialId, text = text, image = image };
        // Display in Update so a tutorial cannot interrupt another state transition.
        requests.Enqueue(request);
        queuedIds.Add(tutorialId);
        return true;
    }

    public bool HasRead(string tutorialId) => tutorialData.HasRead(tutorialId);

    private void Display(Request request)
    {
        displayedFrame = Time.frameCount;
        IsShowing = true;
        tutorialData.MarkRead(request.id);
        tutorialText.text = request.text ?? string.Empty;
        if (tutorialImage != null)
        {
            tutorialImage.sprite = request.image;
            tutorialImage.gameObject.SetActive(request.image != null);
        }
        tutorialPanel.SetActive(true);
    }

    public void Continue()
    {
        if (!IsShowing)
            return;

        if (requests.Count > 0)
        {
            Request next = requests.Dequeue();
            queuedIds.Remove(next.id);
            Display(next);
            return;
        }

        Close();
    }

    public void SkipTutorial()
    {
        tutorialData.tutorialsSkipped = true;
        requests.Clear();
        queuedIds.Clear();
        Close();
    }

    public void Close()
    {
        bool wasShowing = IsShowing;
        IsShowing = false;
        requests.Clear();
        queuedIds.Clear();
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        if (wasShowing)
            stateManager?.CloseState(GameState.Tutorial);
    }

    private void OnDisable()
    {
        Close();
        requests.Clear();
        queuedIds.Clear();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(Continue);
        if (skipTutorialButton != null)
            skipTutorialButton.onClick.RemoveListener(SkipTutorial);
        if (Instance == this)
            Instance = null;
    }
}
