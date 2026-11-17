using System;
using UnityEngine;

// Shared lookup table; effect visuals belong to each enemy's EffectHolder.
public class EffectPool : MonoBehaviour
{
    [Serializable]
    public class EffectImageRegistration
    {
        public int ID;
        public Sprite image;
    }

    public static EffectPool Instance { get; private set; }
    [SerializeField] private EffectImageRegistration[] effects = Array.Empty<EffectImageRegistration>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public Sprite GetImage(int effectID)
    {
        if (effects != null)
            foreach (EffectImageRegistration entry in effects)
                if (entry != null && entry.ID == effectID && entry.image != null)
                    return entry.image;
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
