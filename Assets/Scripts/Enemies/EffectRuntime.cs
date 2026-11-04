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

public interface IEffectSource
{
    IReadOnlyList<ActiveEffect> ActiveEffects { get; }
    event Action EffectsChanged;
    bool RemoveEffect(int effectID);
    void ClearEffects();
}

public static class EffectRuntime
{
    public const int Fire = 1;
    public const int Faint = 2;
    public const int Bleed = 3;
    public const int Freeze = 5;
    public const int Shock = 6;
    public const int HPStealing = 7;
    public const int MPStealing = 8;

    public static bool Apply(List<ActiveEffect> activeEffects, EffectDefinition[] definitions, float probabilityMultiplier = 1f, int[] blockedEffectIDs = null,
        Action<int, int> restoreAttacker = null, bool allowTimedEffects = true)
    {
        if (activeEffects == null || definitions == null)
            return false;

        bool changed = false;

        foreach (EffectDefinition definition in definitions)
        {
            if (definition == null || definition.level <= 0)
                continue;
            bool stealing = definition.effectID == HPStealing || definition.effectID == MPStealing;
            if (!stealing && (!allowTimedEffects || definition.duration <= 0f))
                continue;
            if (Contains(blockedEffectIDs, definition.effectID))
                continue;

            float probability = Mathf.Clamp01(definition.probability * Mathf.Max(0f, probabilityMultiplier));
            if (probability <= 0f || (probability < 1f && UnityEngine.Random.value >= probability))
                continue;

            if (stealing)
            {
                restoreAttacker?.Invoke(definition.effectID, (int)Math.Min(int.MaxValue, (long)definition.level * 5));
                continue;
            }
            if (definition.effectID == Fire)
                changed |= Remove(activeEffects, Freeze);
            else if (definition.effectID == Freeze)
                changed |= Remove(activeEffects, Fire);

            ActiveEffect current = null;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null && activeEffects[i].effectID == definition.effectID)
                {
                    current = activeEffects[i];
                    break;
                }
            }

            if (current == null)
            {
                changed = true;
                activeEffects.Add(new ActiveEffect
                {
                    effectID = definition.effectID,
                    remainTime = definition.duration,
                    level = definition.level
                });
            }
            else if (definition.level > current.level)
            {
                changed = true;
                current.level = definition.level;
                current.remainTime = definition.duration;
            }
            else if (definition.level == current.level)
            {
                changed |= definition.duration > current.remainTime;
                current.remainTime = Mathf.Max(current.remainTime, definition.duration);
            }
        }
        return changed;
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

    public static bool Remove(List<ActiveEffect> activeEffects, int effectID)
    {
        if (activeEffects == null)
            return false;
        return activeEffects.RemoveAll(effect => effect != null && effect.effectID == effectID) > 0;
    }

    public static bool Clear(List<ActiveEffect> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
            return false;
        activeEffects.Clear();
        return true;
    }

    public static bool Tick(List<ActiveEffect> activeEffects, float deltaTime, Action<float> applyDamage)
    {
        if (activeEffects == null || activeEffects.Count == 0)
            return false;

        bool changed = false;
        deltaTime = Mathf.Max(0f, deltaTime);
        // Damage callbacks can clear effects (for example, when an enemy dies).
        foreach (ActiveEffect effect in activeEffects.ToArray())
        {
            if (effect == null)
            {
                changed |= activeEffects.Remove(null);
                continue;
            }
            if (!activeEffects.Contains(effect))
                continue;
            float activeDelta = Mathf.Min(deltaTime, Mathf.Max(0f, effect.remainTime));
            if ((effect.effectID == Fire || effect.effectID == Bleed || effect.effectID == Freeze || effect.effectID == Shock) && activeDelta > 0f)
                applyDamage?.Invoke(Mathf.Max(0, effect.level) * activeDelta);

            if (!activeEffects.Contains(effect))
                continue;
            effect.remainTime -= deltaTime;
            if (effect.remainTime <= 0f)
                changed |= Remove(activeEffects, effect.effectID);
        }
        return changed;
    }

    public static float GetMovementMultiplier(IReadOnlyList<ActiveEffect> activeEffects)
    {
        float multiplier = 1f;
        if (activeEffects == null)
            return multiplier;
        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect == null || effect.remainTime <= 0f || effect.level <= 0)
                continue;
            if (effect.effectID == Faint || (effect.effectID == Freeze && effect.level >= 4))
                return 0f;
            if (effect.effectID == Freeze)
                multiplier *= 2f / (2f + effect.level);
            else if (effect.effectID == Shock)
                multiplier *= 3f / (3f + effect.level);
        }
        return multiplier;
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

