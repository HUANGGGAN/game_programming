using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectHolder : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private EffectPool effectTable;
    [SerializeField, Tooltip("Authored icon renderers, in display order. Found under this holder if unassigned.")]
    private SpriteRenderer[] icons;
    private EffectPool lastTable;
    private readonly HashSet<int> shownIDs = new HashSet<int>();

    private void OnEnable()
    {
        if (player == null) player = GetComponentInParent<Player>();
        if (icons == null || icons.Length == 0) icons = GetComponentsInChildren<SpriteRenderer>(true);
        if (player != null) player.EffectsChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        // Also handle a table created after this player, or replaced on scene changes.
        EffectPool table = effectTable != null ? effectTable : EffectPool.Instance;
        if (!ReferenceEquals(table, lastTable)) Refresh();
    }

    public void Refresh()
    {
        lastTable = effectTable != null ? effectTable : EffectPool.Instance;
        shownIDs.Clear();
        int index = 0;
        if (icons == null) return;
        if (lastTable != null && player != null && player.isActiveAndEnabled && !player.IsDying)
            foreach (ActiveEffect effect in player.ActiveEffects)
            {
                if (effect == null || effect.level <= 0 || effect.remainTime <= 0f || !shownIDs.Add(effect.effectID)) continue;
                int level = effect.level;
                foreach (ActiveEffect candidate in player.ActiveEffects)
                    if (candidate != null && candidate.effectID == effect.effectID && candidate.remainTime > 0f)
                        level = Mathf.Max(level, candidate.level);
                Sprite image = lastTable.GetImage(effect.effectID, level);
                if (image == null) continue;
                while (index < icons.Length && icons[index] == null) index++;
                if (index >= icons.Length) break;
                icons[index++].sprite = image;
            }
        for (; index < icons.Length; index++)
            if (icons[index] != null) icons[index].sprite = null;
    }

    private void OnDisable()
    {
        if (player != null) player.EffectsChanged -= Refresh;
        if (icons != null)
            foreach (SpriteRenderer icon in icons)
                if (icon != null) icon.sprite = null;
    }
}
