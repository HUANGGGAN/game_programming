using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectManager : MonoBehaviour
{
    [Serializable]
    public class EffectIconRegistration
    {
        public int ID;
        public Sprite effect_icon;
    }

    [Tooltip("A component implementing IEffectSource, such as Player or EnemyChase.")]
    [SerializeField] private MonoBehaviour effectSource;
    [SerializeField] private Transform effectIconSlotGrid;
    [SerializeField] private GameObject effectIconPrefab;
    [SerializeField] private EffectIconRegistration[] effectIcons;

    private IEffectSource source;
    private readonly Dictionary<int, GameObject> frames = new Dictionary<int, GameObject>();
    private readonly HashSet<int> visibleEffects = new HashSet<int>();

    private void OnEnable()
    {
        source = effectSource != null ? effectSource as IEffectSource : GetComponentInParent<IEffectSource>();
        if (source == null)
        {
            Debug.LogError("Assign an IEffectSource component to EffectManager.", this);
            return;
        }
        source.EffectsChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        visibleEffects.Clear();
        if (source == null || effectIconSlotGrid == null || effectIconPrefab == null || effectIcons == null)
        {
            foreach (GameObject frame in frames.Values)
                if (frame != null)
                    frame.SetActive(false);
            return;
        }

        foreach (ActiveEffect effect in source.ActiveEffects)
        {
            if (effect == null || effect.remainTime <= 0f)
                continue;
            Sprite icon = null;
            foreach (EffectIconRegistration registration in effectIcons)
            {
                if (registration != null && registration.ID == effect.effectID)
                {
                    icon = registration.effect_icon;
                    break;
                }
            }
            if (icon == null)
                continue;
            visibleEffects.Add(effect.effectID);
            if (!frames.TryGetValue(effect.effectID, out GameObject frame) || frame == null)
            {
                frame = Instantiate(effectIconPrefab, effectIconSlotGrid, false);
                frames[effect.effectID] = frame;
            }
            Transform iconTransform = frame.transform.Find("EffectIconImage");
            Image image = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (image != null)
            {
                image.sprite = icon;
                image.raycastTarget = false;
            }
            frame.SetActive(true);
        }
        foreach (KeyValuePair<int, GameObject> entry in frames)
            if (entry.Value != null && !visibleEffects.Contains(entry.Key))
                entry.Value.SetActive(false);
    }

    private void OnDisable()
    {
        if (source != null)
            source.EffectsChanged -= Refresh;
        source = null;
        foreach (GameObject frame in frames.Values)
            if (frame != null)
                frame.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (GameObject frame in frames.Values)
            if (frame != null)
                Destroy(frame);
        frames.Clear();
    }
}
