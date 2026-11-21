using System.Collections.Generic;
using UnityEngine;

public partial class TreeBoss
{
    [Header("Poke Skill")]
    [SerializeField] private GameObject circleAttackIndicatorPrefab;
    [SerializeField, Min(0.1f)] private float pokeAttackCDTime = 5f;
    [SerializeField, Min(1)] private int pokeAttackCount = 5;
    [SerializeField, Min(0.01f)] private float pokeAttackInterval = 0.5f;
    [SerializeField, Min(0.01f)] private float pokeAttackDuration = 0.7f;
    [SerializeField, Min(0f)] private float pokeAttackRemainTime = 1.5f;
    [SerializeField, Min(0f)] private float pokeAttackDamage = 10f;
    [SerializeField, Min(0f)] private float pokeAttackKnockbackForce = 5f;
    [SerializeField] private EffectDefinition[] pokeAttackEffect;

    [Header("Summon Skill")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField, Min(0.1f)] private float summonCDTime = 16f;
    [SerializeField, Min(1)] private int summonPoint = 3;
    [SerializeField, Min(0f)] private float spawnMinDistance = 3f;
    [SerializeField] private Vector2 summonBottomLeft = new Vector2(-140f, 7f);
    [SerializeField] private Vector2 summonTopRight = new Vector2(-125f, 21f);

    [Header("Vine Skill")]
    [SerializeField] private GameObject vineAttackIndicatorPrefab;
    [SerializeField] private Vine vinePrefab;
    [SerializeField] private Sprite[] vineImages = new Sprite[2];
    [SerializeField, Min(0.1f)] private float vineCDTime = 11f;
    [SerializeField, Min(0.01f)] private float vineHintDuration = 4f;
    [SerializeField, Min(0.01f)] private float vineAttackDuration = 3f;
    [SerializeField, Min(1f)] private float vineHealth = 36f;
    [SerializeField, Min(0f)] private float vineWidth = 40f;
    [SerializeField] private float vineSpawnX = -145.8f;
    [SerializeField] private Vector2 vineSpawnY = new Vector2(7f, 22f);
    [Header("Skill Pools")]
    [SerializeField, Min(0)] private int initialSkillPoolSize = 5;

    private class Telegraph
    {
        public GameObject root;
        public Transform fill;
        public Vector3 fillScale;
        public GameObject half, full;
        public CircleCollider2D circle;
        public float age, duration, damage;
    }
    private readonly List<GameObject> circlePool = new List<GameObject>();
    private readonly List<GameObject> hintPool = new List<GameObject>();
    private readonly List<GameObject> vinePool = new List<GameObject>();
    private readonly List<Telegraph> pokes = new List<Telegraph>();
    private readonly List<Telegraph> hints = new List<Telegraph>();
    private readonly List<Collider2D> pokeOverlaps = new List<Collider2D>();
    private Transform skillRoot, inactiveSkillRoot;
    private float pokeCooldown, summonCooldown, vineCooldown, nextPoke;
    private float checkpointRemaining;
    private int pokesRemaining, phase;

    private void InitializeSkills()
    {
        if (skillRoot == null)
        {
            skillRoot = new GameObject("Tree Boss Skills").transform;
            skillRoot.SetParent(transform, false);
            inactiveSkillRoot = new GameObject("Inactive Skills").transform;
            inactiveSkillRoot.SetParent(skillRoot, false);
            inactiveSkillRoot.gameObject.SetActive(false);
            Prewarm(circlePool, circleAttackIndicatorPrefab);
            Prewarm(hintPool, vineAttackIndicatorPrefab);
            Prewarm(vinePool, vinePrefab != null ? vinePrefab.gameObject : null);
        }
        phase = 0;
        checkpointRemaining = 0f;
        pokesRemaining = 0;
        pokeCooldown = pokeAttackCDTime;
        summonCooldown = summonCDTime;
        vineCooldown = vineCDTime;
    }

    private void Prewarm(List<GameObject> pool, GameObject prefab)
    {
        if (prefab == null) return;
        for (int i = 0; i < initialSkillPoolSize; i++) CreateSkill(pool, prefab);
    }

    private GameObject CreateSkill(List<GameObject> pool, GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, inactiveSkillRoot);
        instance.SetActive(false);
        instance.transform.SetParent(skillRoot, false);
        pool.Add(instance);
        return instance;
    }

    private GameObject Acquire(List<GameObject> pool, GameObject prefab)
    {
        foreach (GameObject instance in pool)
            if (instance != null && !instance.activeSelf) return instance;
        return CreateSkill(pool, prefab);
    }

    private void TickSkills(float dt)
    {
        int targetPhase = CurrentHealth <= MaxHealth / 3f ? 2 : CurrentHealth <= MaxHealth * 2f / 3f ? 1 : 0;
        if (checkpointRemaining > 0f)
        {
            checkpointRemaining = Mathf.Max(0f, checkpointRemaining - dt);
            return;
        }
        if (phase < targetPhase)
        {
            phase++;
            checkpointRemaining = 1f;
            // Shorten an already running cooldown along with the new base cooldown.
            pokeCooldown = Mathf.Max(0f, pokeCooldown - 0.5f);
            summonCooldown = Mathf.Max(0f, summonCooldown - 2f);
            CheckpointReached?.Invoke();
            return;
        }

        pokeCooldown -= dt;
        summonCooldown -= dt;
        vineCooldown -= dt;
        if (pokeCooldown <= 0f && pokesRemaining == 0)
        {
            pokeCooldown = Mathf.Max(0.1f, pokeAttackCDTime - phase * 0.5f);
            pokesRemaining = pokeAttackCount + phase;
            nextPoke = 0f;
        }
        if (pokesRemaining > 0)
        {
            nextPoke -= dt;
            while (nextPoke <= 0f && pokesRemaining > 0)
            {
                SpawnPoke();
                pokesRemaining--;
                nextPoke += Mathf.Max(0.01f, pokeAttackInterval - phase * 0.05f);
            }
        }
        if (summonCooldown <= 0f)
        {
            summonCooldown = Mathf.Max(0.1f, summonCDTime - phase * 2f);
            SummonSlimes();
        }
        if (vineCooldown <= 0f)
        {
            vineCooldown = Mathf.Max(0.1f, vineCDTime);
            for (int i = 0; i < 1 + phase; i++)
                SpawnVineHint();
        }
        TickPokes(dt);
        if (!combatEnabled || IsDead) return;
        TickVineHints(dt);
        foreach (GameObject instance in vinePool)
            if (instance != null && instance.activeSelf) instance.GetComponent<Vine>().Tick(dt);
    }

    private Telegraph CreateTelegraph(GameObject root, Vector3 position)
    {
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;
        Transform fill = root.transform.Find("FillPart");
        var attack = new Telegraph { root = root, fill = fill, fillScale = fill != null ? fill.localScale : Vector3.one };
        if (fill != null) fill.localScale = new Vector3(0f, 0f, 1f);
        root.SetActive(true);
        return attack;
    }

    private void SpawnPoke()
    {
        if (circleAttackIndicatorPrefab == null || PlayerTarget == null) return;
        GameObject root = Acquire(circlePool, circleAttackIndicatorPrefab);
        Telegraph poke = CreateTelegraph(root, PlayerTarget.position);
        Transform half = root.transform.Find("PokeHalfImage"), full = root.transform.Find("PokeFullImage");
        poke.half = half != null ? half.gameObject : null;
        poke.full = full != null ? full.gameObject : null;
        if (poke.half != null) poke.half.SetActive(false);
        if (poke.full != null) poke.full.SetActive(false);
        poke.circle = root.GetComponentInChildren<CircleCollider2D>(true);
        if (poke.circle != null) poke.circle.isTrigger = true;
        poke.duration = Mathf.Max(0.01f, pokeAttackDuration - phase * 0.1f);
        poke.damage = pokeAttackDamage + phase;
        pokes.Add(poke);
    }

    private void TickPokes(float dt)
    {
        for (int i = pokes.Count - 1; i >= 0; i--)
        {
            Telegraph poke = pokes[i];
            poke.age += dt;
            float progress = Mathf.Clamp01(poke.age / poke.duration);
            if (poke.fill != null) poke.fill.localScale = new Vector3(progress, progress, 1f);
            if (poke.half != null) poke.half.SetActive(progress >= 0.5f && progress < 1f);
            if (poke.full != null) poke.full.SetActive(progress >= 1f);
            if (progress < 1f) continue;
            Physics2D.SyncTransforms();
            Player player = PlayerTarget != null ? PlayerTarget.GetComponentInParent<Player>() : null;
            bool hit = false;
            if (player != null && !player.IsDying && poke.circle != null && poke.circle.isActiveAndEnabled)
            {
                pokeOverlaps.Clear();
                poke.circle.Overlap(ContactFilter2D.noFilter, pokeOverlaps);
                foreach (Collider2D collider in pokeOverlaps)
                    if (collider.GetComponentInParent<Player>() == player) { hit = true; break; }
            }
            if (!hit && poke.age < poke.duration + pokeAttackRemainTime) continue;
            poke.root.SetActive(false);
            pokes.RemoveAt(i);
            if (hit && player.TakeDamage(poke.damage))
            {
                Vector2 direction = (Vector2)(player.transform.position - transform.position);
                player.ApplyKnockback(direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up, pokeAttackKnockbackForce);
                player.ApplyEffects(pokeAttackEffect, RestoreEffectResource);
            }
            if (!combatEnabled) return;
        }
    }

    private void SummonSlimes()
    {
        if (slimePrefab == null || PlayerTarget == null || EnemyPoolSpawner.Instance == null) return;
        EnemyChase slime = slimePrefab.GetComponent<EnemyChase>();
        if (slime == null) return;
        Vector2 p = PlayerTarget.position;
        float maxDistance = 0f;
        foreach (Vector2 corner in new[] { summonBottomLeft, summonTopRight,
            new Vector2(summonBottomLeft.x, summonTopRight.y), new Vector2(summonTopRight.x, summonBottomLeft.y) })
            maxDistance = Mathf.Max(maxDistance, Vector2.Distance(p, corner));
        EnemyPoolSpawner.Instance.SpawnBatch(new SpawnRequest {
            centerPosition = PlayerTarget.position, minRadius = spawnMinDistance, maxRadius = maxDistance,
            totalPoint = slime.EnemyPoint * (summonPoint + (phase >= 2 ? 1 : 0)),
            ignoreDifficulty = true, statMultiplier = 1f, allowedPrefabs = new[] { slimePrefab },
            isPositionAllowed = (_, point) => point.x >= summonBottomLeft.x && point.x <= summonTopRight.x &&
                point.y >= summonBottomLeft.y && point.y <= summonTopRight.y
        });
    }

    private void SpawnVineHint()
    {
        if (vineAttackIndicatorPrefab == null || vinePrefab == null) return;
        GameObject root = Acquire(hintPool, vineAttackIndicatorPrefab);
        Telegraph hint = CreateTelegraph(root, new Vector3(vineSpawnX, Random.Range(vineSpawnY.x, vineSpawnY.y), transform.position.z));
        if (hint.fill != null) hint.fill.localScale = new Vector3(0f, hint.fillScale.y, hint.fillScale.z);
        hints.Add(hint);
    }

    private void TickVineHints(float dt)
    {
        for (int i = hints.Count - 1; i >= 0; i--)
        {
            Telegraph hint = hints[i];
            hint.age += dt;
            float progress = Mathf.Clamp01(hint.age / Mathf.Max(0.01f, vineHintDuration));
            if (hint.fill != null) hint.fill.localScale = new Vector3(progress, hint.fillScale.y, hint.fillScale.z);
            if (progress < 1f) continue;
            Vector3 position = hint.root.transform.position;
            hint.root.SetActive(false);
            hints.RemoveAt(i);
            GameObject root = Acquire(vinePool, vinePrefab.gameObject);
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;
            Sprite image = PickVineImage();
            root.GetComponent<Vine>().Configure(image, vineHealth, vineAttackDuration, vineWidth);
            if (visuals != null && visuals.Length > 0 && visuals[0] != null)
            {
                SpriteRenderer bossVisual = visuals[0];
                int order = bossVisual.sortingOrder;
                foreach (SpriteRenderer visual in visuals)
                    if (visual != null && visual.sortingLayerID == bossVisual.sortingLayerID)
                        order = Mathf.Min(order, visual.sortingOrder);
                foreach (SpriteRenderer vineVisual in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    vineVisual.sortingLayerID = bossVisual.sortingLayerID;
                    vineVisual.sortingOrder = order - 1;
                }
            }
            root.SetActive(true);
        }
    }

    private Sprite PickVineImage()
    {
        if (vineImages == null) return null;
        int count = 0;
        foreach (Sprite image in vineImages) if (image != null) count++;
        if (count == 0) return null;
        int selected = Random.Range(0, count);
        foreach (Sprite image in vineImages)
            if (image != null && selected-- == 0) return image;
        return null;
    }

    private void StopSkills()
    {
        combatEnabled = false;
        pokesRemaining = 0;
        pokes.Clear();
        hints.Clear();
        foreach (var pool in new[] { circlePool, hintPool, vinePool })
            foreach (GameObject instance in pool)
                if (instance != null) instance.SetActive(false);
    }
}
