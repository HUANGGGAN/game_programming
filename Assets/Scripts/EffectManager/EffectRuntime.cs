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

            int level = definition.level;
            float duration = definition.duration;
            // Carry matching levels upward until the resulting level is unique.
            while (true)
            {
                ActiveEffect matching = activeEffects.Find(effect => effect != null &&
                    effect.effectID == definition.effectID && effect.level == level && effect.remainTime > 0f);
                if (matching == null)
                    break;
                duration = Mathf.Max(duration, matching.remainTime);
                activeEffects.Remove(matching);
                if (level < int.MaxValue)
                    level++;
            }
            activeEffects.Add(new ActiveEffect
            {
                effectID = definition.effectID,
                remainTime = duration,
                level = level
            });
            changed = true;
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

    public const float TickInterval = 0.5f;

    public static bool Tick(List<ActiveEffect> activeEffects, float deltaTime, Action<float> applyDamage, ref float elapsed)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            elapsed = 0f;
            return false;
        }
        elapsed += Mathf.Max(0f, deltaTime);
        bool changed = false;
        while (elapsed >= TickInterval && activeEffects.Count > 0)
        {
            elapsed -= TickInterval;
            changed |= TickStep(activeEffects, applyDamage);
        }
        if (activeEffects.Count == 0)
            elapsed = 0f;
        return changed;
    }

    private static bool TickStep(List<ActiveEffect> activeEffects, Action<float> applyDamage)
    {
        bool changed = false;
        var highestLevels = new Dictionary<int, ActiveEffect>();
        // Snapshot because damage callbacks can remove effects or kill the entity.
        ActiveEffect[] snapshot = activeEffects.ToArray();
        foreach (ActiveEffect effect in snapshot)
        {
            if (effect == null || effect.remainTime <= 0f || effect.level <= 0)
                continue;
            if (!highestLevels.TryGetValue(effect.effectID, out ActiveEffect highest) || effect.level > highest.level)
                highestLevels[effect.effectID] = effect;
        }
        foreach (ActiveEffect effect in snapshot)
        {
            if (effect == null)
            {
                changed |= activeEffects.Remove(null);
                continue;
            }
            if (!activeEffects.Contains(effect))
                continue;
            float activeDelta = Mathf.Min(TickInterval, Mathf.Max(0f, effect.remainTime));
            if (highestLevels.TryGetValue(effect.effectID, out ActiveEffect highest) && highest == effect && activeDelta > 0f)
            {
                if (effect.effectID == Fire || effect.effectID == Bleed || effect.effectID == Shock)
                    applyDamage?.Invoke(effect.level * activeDelta);
                if (effect.effectID == Freeze)
                    applyDamage?.Invoke(effect.level * activeDelta * 0.5f);
            }
                

            if (!activeEffects.Contains(effect))
                continue;
            effect.remainTime -= TickInterval;
            if (effect.remainTime <= 0f)
                changed |= activeEffects.Remove(effect);
        }
        return changed;
    }

    public static float GetMovementMultiplier(IReadOnlyList<ActiveEffect> activeEffects)
    {
        int freezeLevel = 0;
        int shockLevel = 0;
        if (activeEffects == null)
            return 1f;
        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect == null || effect.remainTime <= 0f || effect.level <= 0)
                continue;
            if (effect.effectID == Faint)
                return 0f;
            if (effect.effectID == Freeze)
                freezeLevel = Mathf.Max(freezeLevel, effect.level);
            else if (effect.effectID == Shock)
                shockLevel = Mathf.Max(shockLevel, effect.level);
        }
        if (freezeLevel >= 4)
            return 0f;
        return (freezeLevel > 0 ? 2f / (2f + freezeLevel) : 1f) *
            (shockLevel > 0 ? 3f / (3f + shockLevel) : 1f);
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

