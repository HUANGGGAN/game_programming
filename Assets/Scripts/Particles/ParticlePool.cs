using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    [Serializable]
    public class EffectRegistration
    {
        public string id;
        public ParticleSystem prefab;
        [Min(0)] public int initialPoolSize = 4;
    }

    private class EffectPool
    {
        public ParticleSystem prefab;
        public readonly Queue<ParticleSystem> available = new Queue<ParticleSystem>();
    }

    private struct PlayingEffect
    {
        public ParticleSystem particles;
        public EffectPool pool;
    }

    public static ParticlePool Instance { get; private set; }
    [SerializeField] private EffectRegistration[] effects = Array.Empty<EffectRegistration>();

    private readonly Dictionary<string, EffectPool> pools = new Dictionary<string, EffectPool>();
    private readonly List<PlayingEffect> playing = new List<PlayingEffect>();
    private Transform inactiveRoot;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one ParticlePool should be active.", this);
            enabled = false;
            return;
        }
        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        if (initialized)
            return;
        initialized = true;

        GameObject staging = new GameObject("Inactive Particles");
        staging.transform.SetParent(transform, false);
        staging.SetActive(false);
        inactiveRoot = staging.transform;

        if (effects == null)
            return;
        foreach (EffectRegistration entry in effects)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null)
            {
                Debug.LogWarning("ParticlePool registration needs an ID and a particle prefab.", this);
                continue;
            }
            if (pools.ContainsKey(entry.id))
            {
                Debug.LogWarning($"ParticlePool ID '{entry.id}' is registered more than once.", this);
                continue;
            }

            EffectPool pool = new EffectPool { prefab = entry.prefab };
            pools.Add(entry.id, pool);
            for (int i = 0; i < Mathf.Max(0, entry.initialPoolSize); i++)
                pool.available.Enqueue(CreateEffect(pool));
        }
    }

    private ParticleSystem CreateEffect(EffectPool pool)
    {
        // Instantiate inactive so playOnAwake cannot emit while prewarming.
        ParticleSystem particles = Instantiate(pool.prefab, inactiveRoot, false);
        particles.gameObject.SetActive(false);
        particles.transform.SetParent(transform, false);
        foreach (ParticleSystem child in particles.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = child.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    public ParticleSystem Play(string id, Vector3 position)
    {
        return Play(id, position, Quaternion.identity);
    }

    public ParticleSystem Play(string id, Vector3 position, Quaternion rotation)
    {
        if (!isActiveAndEnabled)
            return null;
        InitializePools();
        if (string.IsNullOrWhiteSpace(id) || !pools.TryGetValue(id, out EffectPool pool))
        {
            Debug.LogWarning($"Particle effect ID '{id}' is not registered.", this);
            return null;
        }

        ParticleSystem particles = null;
        while (particles == null && pool.available.Count > 0)
            particles = pool.available.Dequeue();
        if (particles == null)
            particles = CreateEffect(pool);

        particles.transform.SetPositionAndRotation(position, rotation);
        particles.transform.localScale = pool.prefab.transform.localScale;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.gameObject.SetActive(true);
        playing.Add(new PlayingEffect { particles = particles, pool = pool });
        particles.Play(true);
        return particles;
    }

    private void Update()
    {
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            ParticleSystem particles = playing[i].particles;
            if (particles == null || !particles.gameObject.activeInHierarchy ||
                (!particles.isPlaying && !particles.IsAlive(true)))
                ReturnEffect(i);
        }
    }

    // By default, stop emitting and let existing particles finish. Immediate stops clear them.
    public void Stop(ParticleSystem particles, bool immediate = false)
    {
        if (particles == null)
            return;
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            if (playing[i].particles != particles)
                continue;
            if (immediate)
                ReturnEffect(i);
            else
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return;
        }
    }

    public void StopAll()
    {
        for (int i = playing.Count - 1; i >= 0; i--)
            ReturnEffect(i);
    }

    private void ReturnEffect(int index)
    {
        PlayingEffect effect = playing[index];
        playing.RemoveAt(index);
        if (effect.particles == null)
            return;
        effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.particles.gameObject.SetActive(false);
        effect.pool.available.Enqueue(effect.particles);
    }

    private void OnDisable()
    {
        StopAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
