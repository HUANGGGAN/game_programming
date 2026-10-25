using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }

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

    private readonly HashSet<string> readTutorials = new HashSet<string>();
    private readonly HashSet<string> queuedIds = new HashSet<string>();
    private readonly Queue<Request> requests = new Queue<Request>();
    private StateManager stateManager;
    public bool IsShowing { get; private set; }
    public bool TutorialsSkipped { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one Tutorial controller should be active.", this);
            enabled = false;
            return;
        }
        Instance = this;
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
            readTutorials.Contains(tutorialId) || queuedIds.Contains(tutorialId))
            return false;

        if (tutorialPanel == null || tutorialText == null || continueButton == null)
        {
            Debug.LogError("Assign Tutorial's panel, text, and Continue button in the Inspector.", this);
            return false;
        }

        Request request = new Request { id = tutorialId, text = text, image = image };
        if (IsShowing)
        {
            requests.Enqueue(request);
            queuedIds.Add(tutorialId);
        }
        else
        {
            stateManager = StateManager.Instance;
            stateManager.RequestState(GameState.Tutorial);
            Display(request);
        }
        return true;
    }

    public bool HasRead(string tutorialId) => readTutorials.Contains(tutorialId);

    private void Display(Request request)
    {
        IsShowing = true;
        readTutorials.Add(request.id);
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
        TutorialsSkipped = true;
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
