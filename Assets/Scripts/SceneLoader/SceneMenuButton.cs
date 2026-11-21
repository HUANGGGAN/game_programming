using UnityEngine;
using UnityEngine.UI;

public enum SceneMenuAction { MainGame, Tutorial, StartingMenu, Resume }

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class SceneMenuButton : MonoBehaviour
{
    [SerializeField] private SceneMenuAction action;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Activate);
    }

    private void OnEnable() => RefreshVisibility();

    private void Update()
    {
        if (action == SceneMenuAction.Resume) RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (action == SceneMenuAction.Resume)
            gameObject.SetActive(SceneLoader.Instance != null && SceneLoader.Instance.HasSavedGame);
    }

    public void Activate()
    {
        // Resolve at click time: the loader authored in a reloaded menu may have been replaced by its persistent instance.
        SceneLoader loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("No SceneLoader exists. Start from StartingMenu or add the configured loader to your test scene.", this);
            return;
        }
        switch (action)
        {
            case SceneMenuAction.MainGame: loader.StartGame(); break;
            case SceneMenuAction.Tutorial: loader.StartTutorial(); break;
            case SceneMenuAction.StartingMenu: loader.ReturnToMenu(); break;
            case SceneMenuAction.Resume: loader.ResumeGame(); break;
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Activate);
    }
}
