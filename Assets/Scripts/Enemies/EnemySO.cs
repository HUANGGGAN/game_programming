using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectDefinition
{
    public int effectID;
    public float duration = 1f;
    [Range(0f, 1f)] public float probability = 1f;
    [Min(1)] public int level = 1;
}

[Serializable]
public class ActiveEffect
{
    public int effectID;
    public float remainTime;
    public int level;
}

public static class EffectRuntime
{
    public const int Fire = 1;
    public const int Faint = 2;
    public const int Bleed = 3;

    public static void Apply(List<ActiveEffect> activeEffects, EffectDefinition[] definitions, float probabilityMultiplier = 1f, int[] blockedEffectIDs = null)
    {
        if (activeEffects == null || definitions == null)
            return;

        foreach (EffectDefinition definition in definitions)
        {
            if (definition == null || definition.duration <= 0f || definition.level <= 0)
                continue;
            if (Contains(blockedEffectIDs, definition.effectID))
                continue;

            float probability = Mathf.Clamp01(definition.probability * Mathf.Max(0f, probabilityMultiplier));
            if (probability <= 0f || (probability < 1f && UnityEngine.Random.value >= probability))
                continue;

            ActiveEffect current = null;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].effectID == definition.effectID)
                {
                    current = activeEffects[i];
                    break;
                }
            }

            if (current == null)
            {
                activeEffects.Add(new ActiveEffect
                {
                    effectID = definition.effectID,
                    remainTime = definition.duration,
                    level = definition.level
                });
            }
            else if (definition.level > current.level)
            {
                current.level = definition.level;
                current.remainTime = definition.duration;
            }
            else if (definition.level == current.level)
            {
                current.remainTime = Mathf.Max(current.remainTime, definition.duration);
            }
        }
    }

    private static bool Contains(int[] values, int value)
    {
        if (values == null)
            return false;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == value)
                return true;
        }

        return false;
    }

    public static void Tick(List<ActiveEffect> activeEffects, float deltaTime, Action<float> applyDamage)
    {
        if (activeEffects == null)
            return;

        deltaTime = Mathf.Max(0f, deltaTime);
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            float activeDelta = Mathf.Min(deltaTime, Mathf.Max(0f, effect.remainTime));
            if ((effect.effectID == Fire || effect.effectID == Bleed) && activeDelta > 0f)
                applyDamage?.Invoke(Mathf.Max(0, effect.level) * activeDelta);

            effect.remainTime -= deltaTime;
            if (effect.remainTime <= 0f)
                activeEffects.RemoveAt(i);
        }
    }

    public static bool HasEffect(List<ActiveEffect> activeEffects, int effectID)
    {
        if (activeEffects == null)
            return false;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i] != null && activeEffects[i].effectID == effectID && activeEffects[i].remainTime > 0f)
                return true;
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemySO : ScriptableObject
{
    [Header("Visuals")]
    public Sprite sprite;
    public bool spriteFacesRightByDefault = true;

    [Header("Status Effects")]
    public EffectDefinition[] effects;
    public int[] resistances;
    public int[] weaknesses;

    [Header("Spawn Settings")]
    [Min(0.01f)] public float enemy_point = 1f;
    [Min(0f)] public float enemy_difficulty = 1f;
    [Min(0)] public int money = 1;

    [Header("Combat Stats")]
    public float attackRange = 1.5f;
    public float damage = 10f;
    public float knockbackForce = 4f;
    public float knockbackResistance = 0f;
    public float speed = 2.5f;
    public float attackInterval = 1.2f;
    public float attackDuration = 0.4f;
    public float HP = 35f;
    public int[] attackAttributes; // Attribute IDs for the bullet's attack type (e.g., fire, ice, poison)
}
