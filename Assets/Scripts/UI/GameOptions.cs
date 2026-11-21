using System;
using UnityEngine;

public enum GameOption { DeathPenalty, ShowMeleeRange }
public static class GameOptions
{
    public static bool DeathPenalty { get; private set; } = true;
    public static bool ShowMeleeRange { get; private set; } = true;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { DeathPenalty = ShowMeleeRange = true; Changed = null; }
    public static bool Get(GameOption option) => option == GameOption.DeathPenalty ? DeathPenalty : ShowMeleeRange;
    public static void Toggle(GameOption option)
    {
        if (option == GameOption.DeathPenalty) DeathPenalty = !DeathPenalty;
        else ShowMeleeRange = !ShowMeleeRange;
        Changed?.Invoke();
    }
}
