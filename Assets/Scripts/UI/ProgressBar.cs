using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private UnityProgressBar.ProgressBar bar;

    public void SetProgress(float progress)
    {
        if (bar == null)
            bar = GetComponentInChildren<UnityProgressBar.ProgressBar>(true);
        if (bar == null)
            return;

        bar.MinValue = 0f;
        bar.MaxValue = 1f;
        bar.SetValueWithoutNotify(Mathf.Clamp01(progress));
    }

    public void SetVisible(bool visible)
    {
        if (!visible)
            SetProgress(0f);
        gameObject.SetActive(visible);
    }
}
