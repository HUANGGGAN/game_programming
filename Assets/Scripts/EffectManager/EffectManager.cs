using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class EffectManager : MonoBehaviour
{
    [Serializable]
    public class EffectIconRegistration
    {
        public int ID;
        [Tooltip("Levels using this icon. Empty means a fallback for all levels of this ID.")]
        public List<int> levels = new List<int>();
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
    private readonly Dictionary<int, ActiveEffect> strongestEffects = new Dictionary<int, ActiveEffect>();

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

        strongestEffects.Clear();
        foreach (ActiveEffect effect in source.ActiveEffects)
        {
            if (effect == null || effect.remainTime <= 0f)
                continue;
            if (!strongestEffects.TryGetValue(effect.effectID, out ActiveEffect strongest) || effect.level > strongest.level)
                strongestEffects[effect.effectID] = effect;
        }
        foreach (ActiveEffect effect in strongestEffects.Values)
        {
            Sprite icon = null;
            Sprite fallback = null;
            foreach (EffectIconRegistration registration in effectIcons)
            {
                if (registration != null && registration.ID == effect.effectID)
                {
                    if (registration.levels == null || registration.levels.Count == 0)
                    {
                        if (fallback == null) fallback = registration.effect_icon;
                    }
                    else if (registration.levels.Contains(effect.level))
                    {
                        icon = registration.effect_icon;
                        break;
                    }
                }
            }
            if (icon == null) icon = fallback;
            if (icon == null)
                continue;
            visibleEffects.Add(effect.effectID);
            if (!frames.TryGetValue(effect.effectID, out GameObject frame) || frame == null)
            {
                frame = Instantiate(effectIconPrefab, effectIconSlotGrid, false);
                frames[effect.effectID] = frame;
            }
            Transform iconTransform = frame.transform.Find("EffectImage");
            Image image = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (image != null)
            {
                image.sprite = icon;
                image.raycastTarget = false;
            }
            foreach (TMP_Text text in frame.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "LevelText")
                {
                    text.text = ToRoman(effect.level);
                    text.raycastTarget = false;
                }
            frame.SetActive(true);
        }
        foreach (KeyValuePair<int, GameObject> entry in frames)
            if (entry.Value != null && !visibleEffects.Contains(entry.Key))
                entry.Value.SetActive(false);
    }

    private static readonly int[] RomanValues = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    private static readonly string[] RomanSymbols = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    private static string ToRoman(int level)
    {
        var result = new StringBuilder();
        for (int i = 0; i < RomanValues.Length; i++)
            while (level >= RomanValues[i])
            {
                result.Append(RomanSymbols[i]);
                level -= RomanValues[i];
            }
        return result.ToString();
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
