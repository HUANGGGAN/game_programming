using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameBinding
{
    Up, Left, Down, Right, Inventory, Leave, Weapon1, Weapon2, Weapon3,
    Consumable1, Consumable2, Consumable3, Reload, ChangeAttackMode, Teleport, OpenMap, AddCoin, GodMode, Run, Interact
}

public static class GameInput
{
    private static readonly Key[] defaults = { Key.W, Key.A, Key.S, Key.D, Key.E, Key.Escape,
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Z, Key.X, Key.C, Key.R, Key.T, Key.P, Key.M,
        Key.Equals, Key.Semicolon, Key.LeftShift, Key.F };
    private static Key[] keys = (Key[])defaults.Clone();
    private static int consumedFrame = -1;
    public static KeyboardBindingButton Capturing { get; private set; }
    public static event Action Changed;
    public static bool InputConsumed => consumedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        keys = (Key[])defaults.Clone();
        Capturing = null;
        Changed = null;
        consumedFrame = -1;
    }

    public static Key Get(GameBinding action) => keys[(int)action];
    public static bool HasConflict(GameBinding action)
    {
        int index = (int)action;
        Key key = keys[index];
        for (int i = 0; i < keys.Length; i++)
            if (i != index && (keys[i] == key || keys[i] == Alternate(key))) return true;
        return false;
    }
    public static bool HasConflicts
    {
        get
        {
            for (int i = 0; i < keys.Length; i++)
                if (HasConflict((GameBinding)i)) return true;
            return false;
        }
    }
    public static void Set(GameBinding action, Key key)
    {
        keys[(int)action] = key;
        Consume();
        Changed?.Invoke();
    }
    public static void Consume() => consumedFrame = Time.frameCount;
    public static void BeginCapture(KeyboardBindingButton button)
    {
        CancelCapture();
        Capturing = button;
        Consume();
    }
    public static void CancelCapture()
    {
        KeyboardBindingButton button = Capturing;
        Capturing = null;
        if (button != null) button.StopSelection();
    }

    private static Key Alternate(Key key) => key switch
    {
        Key.LeftShift => Key.RightShift, Key.RightShift => Key.LeftShift,
        Key.LeftCtrl => Key.RightCtrl, Key.RightCtrl => Key.LeftCtrl,
        Key.LeftAlt => Key.RightAlt, Key.RightAlt => Key.LeftAlt,
        _ => key
    };
    public static bool Pressed(GameBinding action)
    {
        if (Capturing != null || InputConsumed || Keyboard.current == null) return false;
        Key key = Get(action);
        return Keyboard.current[key].wasPressedThisFrame || Keyboard.current[Alternate(key)].wasPressedThisFrame;
    }
    public static bool Held(GameBinding action)
    {
        if (Capturing != null || Keyboard.current == null) return false;
        Key key = Get(action);
        return Keyboard.current[key].isPressed || Keyboard.current[Alternate(key)].isPressed;
    }
    public static string Label(GameBinding action)
    {
        Key key = Get(action);
        if (key == Key.Escape) return "Esc";
        if (key == Key.CapsLock) return "Caps";
        if (key == Key.Space) return "Space";
        if (key == Key.LeftShift || key == Key.RightShift) return "Shift";
        if (key == Key.LeftCtrl || key == Key.RightCtrl) return "Ctrl";
        if (key == Key.LeftAlt || key == Key.RightAlt) return "Alt";
        return Keyboard.current != null ? Keyboard.current[key].displayName : key.ToString();
    }

    public static bool IsAllowed(Key key)
    {
        string name = key.ToString();
        if (name.Length == 1 && name[0] >= 'A' && name[0] <= 'Z') return true;
        if (name.StartsWith("Digit", StringComparison.Ordinal)) return true;
        return key is Key.Minus or Key.Equals or Key.LeftBracket or Key.RightBracket or Key.Backslash or
            Key.Semicolon or Key.Quote or Key.Comma or Key.Period or Key.Slash or Key.Backquote or
            Key.Escape or Key.CapsLock or Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or Key.Space;
    }
}
