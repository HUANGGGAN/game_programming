using System.Collections.Generic;
using UnityEngine;

public class EffectHolder : MonoBehaviour
{
    [SerializeField] private EnemyChase enemy;
    [SerializeField] private EffectPool effectTable;
    [SerializeField, Tooltip("Authored icon renderers, in display order. Found under this holder if unassigned.")]
    private SpriteRenderer[] icons;
    private EffectPool lastTable;
    private readonly HashSet<int> shownIDs = new HashSet<int>();

    private void OnEnable()
    {
        if (enemy == null) enemy = GetComponentInParent<EnemyChase>();
        if (icons == null || icons.Length == 0) icons = GetComponentsInChildren<SpriteRenderer>(true);
        if (enemy != null) enemy.EffectsChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        // Also handle a table created after this enemy, or replaced on scene changes.
        EffectPool table = effectTable != null ? effectTable : EffectPool.Instance;
        if (!ReferenceEquals(table, lastTable)) Refresh();
    }

    public void Refresh()
    {
        lastTable = effectTable != null ? effectTable : EffectPool.Instance;
        shownIDs.Clear();
        int index = 0;
        if (icons == null) return;
        if (lastTable != null && enemy != null && enemy.isActiveAndEnabled && enemy.CurrentHealth > 0f &&
            !(enemy is TreeBoss) && !(enemy is Boss))
            foreach (ActiveEffect effect in enemy.ActiveEffects)
            {
                if (effect == null || effect.level <= 0 || effect.remainTime <= 0f || !shownIDs.Add(effect.effectID)) continue;
                Sprite image = lastTable.GetImage(effect.effectID);
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
        if (enemy != null) enemy.EffectsChanged -= Refresh;
        if (icons != null)
            foreach (SpriteRenderer icon in icons)
                if (icon != null) icon.sprite = null;
    }
}
